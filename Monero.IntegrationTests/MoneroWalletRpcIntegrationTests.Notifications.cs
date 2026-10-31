using Monero.IntegrationTests.Utils;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests;

public partial class MoneroWalletRpcIntegrationTests
{
    /// <summary>
    /// A listener that keeps what it is notified of.
    /// </summary>
    private class CollectingWalletListener : MoneroWalletListener
    {
        private readonly object _lock = new();
        private readonly List<ulong> _blocks = [];
        private readonly List<MoneroOutputWallet> _received = [];
        private readonly List<(ulong Balance, ulong UnlockedBalance)> _balances = [];

        public List<ulong> Blocks { get { lock (_lock) return [.. _blocks]; } }
        public List<MoneroOutputWallet> Received { get { lock (_lock) return [.. _received]; } }
        public List<(ulong Balance, ulong UnlockedBalance)> Balances { get { lock (_lock) return [.. _balances]; } }

        public override void OnNewBlock(ulong height)
        {
            lock (_lock) _blocks.Add(height);
        }

        public override void OnBalancesChanged(ulong newBalance, ulong newUnlockedBalance)
        {
            lock (_lock) _balances.Add((newBalance, newUnlockedBalance));
        }

        public override void OnOutputReceived(MoneroOutputWallet output)
        {
            lock (_lock) _received.Add(output);
        }
    }

    // wait until the condition is true
    private static async Task WaitUntil(Func<bool> condition, string message, int timeoutMs = 60000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) Assert.Fail(message);
            await Task.Delay(500);
        }
    }

    // Can stop listening
    [Test, Category(Notifications)]
    public async Task TestStopListening()
    {
        var wallet = await TestUtils.CreateWalletRpc();
        try
        {
            var listener = new CollectingWalletListener();
            wallet.AddListener(listener);
            Assert.That(wallet.GetListeners(), Is.EqualTo(new[] { listener }));
            await Task.Delay(1000);

            wallet.RemoveListener(listener);
            Assert.That(wallet.GetListeners(), Is.Empty);

            // the same listener is registered once
            wallet.AddListener(listener);
            wallet.AddListener(listener);
            Assert.That(wallet.GetListeners(), Has.Count.EqualTo(1));
            wallet.RemoveListeners();
            Assert.That(wallet.GetListeners(), Is.Empty);
        }
        finally
        {
            await CloseWallet(wallet);
        }
    }

    // Is notified of the new blocks
    [Test, Category(Notifications)]
    public async Task TestNotifiedOfNewBlocks()
    {
        var wallet = await GetWallet();
        var listener = new CollectingWalletListener();
        wallet.AddListener(listener);
        try
        {
            await wallet.SetPollPeriodInMsAsync(1000);
            await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, 1);

            await WaitUntil(() => listener.Blocks.Count > 0, "The listener was not notified of a new block");
            Assert.That(listener.Blocks[0], Is.GreaterThan(0UL));
        }
        finally
        {
            wallet.RemoveListener(listener);
            await wallet.SetPollPeriodInMsAsync(TestUtils.SyncPeriodInMs);
        }
    }

    // Can wait for the next block
    [Test, Category(Notifications)]
    public async Task TestWaitForNextBlock()
    {
        var wallet = await GetWallet();
        await wallet.SetPollPeriodInMsAsync(1000);
        try
        {
            var next = wallet.WaitForNextBlockAsync();
            await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, 1);

            var height = await next.WaitAsync(TimeSpan.FromSeconds(60));

            Assert.That(height, Is.GreaterThan(0UL));
        }
        finally
        {
            await wallet.SetPollPeriodInMsAsync(TestUtils.SyncPeriodInMs);
        }
    }

    // Can cancel the wait for the next block
    [Test, Category(Notifications)]
    public async Task TestCancelWaitForNextBlock()
    {
        var wallet = await GetWallet();
        using var source = new CancellationTokenSource(500);

        Assert.ThrowsAsync<TaskCanceledException>(() => wallet.WaitForNextBlockAsync(source.Token));

        // the wallet has no listener left
        Assert.That(wallet.GetListeners(), Is.Empty);
    }

    // Can be created and receive funds, as test_create_and_receive of monero-python, except that the funds are not confirmed by the miner.
    //
    // The receiver is notified of a received output when one of its polls finds the tx confirmed and still locked, with fewer than ten
    // confirmations. monero-cpp doesn't announce the txs of the pool, and it announces the unlock of a tx only if it saw the tx locked before.
    // monero-python starts the miner of the node and waits until the tx is confirmed. With the difficulty of 500 of its network a block takes
    // about two seconds, so the tx stays locked for about twenty and the receiver finds it, although the test is marked flaky there "because of
    // mining speed". The difficulty of this network is 25 (see docker-compose.yml), where the miner makes several blocks per second: a refresh
    // of the wallet, every five seconds, brings tens of blocks, so the tx is unlocked in the wallet when the receiver polls. The receiver has the
    // funds but nothing to be notified of.
    //
    // So this test doesn't start the miner. It waits until the node that mines has the tx in its pool, and generates the one block that confirms
    // it. The tx stays locked for as long as the receiver needs, whatever the speed of the node, and the test needs no Retry. What it doesn't
    // test, and the Python one does, is the notification while the node is mining.
    [Test, Category(Relays), Category(Notifications)]
    public async Task TestCreateAndReceive()
    {
        var wallet = await GetWallet();

        // create random wallet
        var receiver = await TestUtils.CreateWalletRpc();
        try
        {
            // listen for received funds
            var listener = new CollectingWalletListener();
            receiver.AddListener(listener);
            Assert.That(receiver.GetListeners(), Is.Not.Empty);

            // wait for txs to confirm and for sufficient unlocked balance
            await PrepareToSend(wallet, MaxFee);

            // no node may mine from here on: a block that the test didn't ask for could unlock the tx before the receiver polls
            foreach (var node in new[] { MiningUtils.GetDaemon(), Daemon })
            {
                if (await MiningUtils.IsMining(node)) await MiningUtils.StopMining(node);
            }

            // send funds to the receiver
            var sent = await wallet.CreateTxAsync(new MoneroTxConfig(await receiver.GetPrimaryAddressAsync(), MaxFee) { AccountIndex = 0, Relay = true });
            Assert.That(sent.Hash, Is.Not.Null);

            // confirm the funds with one block, which has the tx once the node that mines has it, and leaves it locked
            await DaemonUtils.WaitForTxsInPool(MiningUtils.GetDaemon(), [sent.Hash!]);
            await ConfirmAndUnlock(wallet, 1);

            var tx = await wallet.GetTxAsync(sent.Hash!);
            Assert.That(tx, Is.Not.Null);
            Assert.That(tx!.IsFailed, Is.Not.True, $"Tx failed in mempool: {tx.Hash}");
            Assert.That(tx.IsConfirmed, Is.True, "The block didn't confirm the tx");

            // receiver should notify listeners of received outputs within a few rpc refresh / poll periods
            await WaitUntil(() => listener.Received.Count > 0, "The listener was not notified of the received output", (int)(TestUtils.SyncPeriodInMs * 10));

            var output = listener.Received[0];
            Assert.That(output.Amount, Is.EqualTo(MaxFee));
            Assert.That(output.AccountIndex, Is.EqualTo(0u));
            Assert.That(output.SubaddressIndex, Is.EqualTo(0u));
            Assert.That(output.GetTxWallet(), Is.Not.Null);
        }
        finally
        {
            await CloseWallet(receiver);
        }
    }
}
