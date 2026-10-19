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

    // A wallet is notified of the funds it receives. The poller of monero-wallet-rpc in monero-cpp races with the calls of the wallet and
    // sometimes misses the output (the reference tests of Python expect this to fail for the same reason), so the test is repeated
    [Test, Category(Relays), Category(Notifications), Retry(3)]
    public async Task TestNotifiedOfReceivedFunds()
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount * 2);

        var recipient = await TestUtils.CreateWalletRpc();
        try
        {
            var listener = new CollectingWalletListener();
            recipient.AddListener(listener);
            await recipient.SetPollPeriodInMsAsync(1000);

            // send the funds, and confirm them
            await wallet.CreateTxAsync(new MoneroTxConfig(await recipient.GetPrimaryAddressAsync(), amount) { AccountIndex = 0, Relay = true });
            await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, 1);

            await WaitUntil(() => listener.Received.Count > 0, "The listener was not notified of the received output");
            var output = listener.Received[0];
            Assert.That(output.Amount, Is.EqualTo(amount));
            Assert.That(output.AccountIndex, Is.EqualTo(0u));
            Assert.That(output.SubaddressIndex, Is.EqualTo(0u));
            Assert.That(output.GetTxWallet(), Is.Not.Null);

            await WaitUntil(() => listener.Balances.Count > 0, "The listener was not notified of a change of the balance");
            Assert.That(listener.Balances.Any(balances => balances.Balance == amount), Is.True);
        }
        finally
        {
            await CloseWallet(recipient);
        }

        await ConfirmAndUnlock(wallet);
    }
}
