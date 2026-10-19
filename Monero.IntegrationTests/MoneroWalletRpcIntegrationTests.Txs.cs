using Monero.Common;
using Monero.IntegrationTests.Utils;
using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests;

public partial class MoneroWalletRpcIntegrationTests
{
    /// <summary>The maximum fee of a transaction, in atomic units.</summary>
    private const ulong MaxFee = 7_500_000UL * 10_000UL;

    // helpers

    // mine the blocks that confirm the txs in the pool and unlock their outputs, and sync the wallet to them
    private static async Task ConfirmAndUnlock(MoneroWallet wallet, ulong numBlocks = 11)
    {
        await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, numBlocks);
        var height = await BlockchainUtils.WaitForHeight(await MiningUtils.GetDaemon().GetHeightAsync());

        while (await wallet.GetHeightAsync() < height) await wallet.SyncAsync();
        await wallet.SyncAsync();
    }

    // wait for the pool to clear and for a sufficient unlocked balance in the account
    private static async Task PrepareToSend(MoneroWallet wallet, ulong amount)
    {
        if ((await wallet.GetTxsAsync(new MoneroTxQuery { InTxPool = true })).Count > 0 || await wallet.GetUnlockedBalanceAsync(0) < amount)
        {
            await ConfirmAndUnlock(wallet);
        }

        Assert.That(await wallet.GetUnlockedBalanceAsync(0), Is.GreaterThanOrEqualTo(amount), "The account doesn't have a sufficient unlocked balance");
    }

    private static void TestRelayedTx(MoneroTxWallet tx)
    {
        Assert.That(tx.Hash, Has.Length.EqualTo(64));
        Assert.That(tx.IsRelayed, Is.True);
        Assert.That(tx.IsFailed, Is.Not.True);
        Assert.That(tx.IsConfirmed, Is.False);
        Assert.That(tx.IsOutgoing, Is.True);
        Assert.That(tx.Fee, Is.GreaterThan(0UL));
        Assert.That(tx.OutgoingTransfer, Is.Not.Null);
        Assert.That(tx.OutgoingTransfer!.Destinations, Is.Not.Empty);
    }

    // Can't send funds to an invalid address
    [Test, Category(Relays)]
    public async Task TestValidateInputsSendingFunds()
    {
        var wallet = await GetWallet();
        var config = new MoneroTxConfig("my invalid address", MaxFee) { AccountIndex = 0 };

        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.CreateTxAsync(config));

        Assert.That(error!.Message, Is.EqualTo("Invalid destination address"));
    }

    // Can send to the wallet itself
    [Test, Category(Relays)]
    public async Task TestSendToSelf()
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount);
        var balance1 = await wallet.GetBalanceAsync();
        var unlockedBalance1 = await wallet.GetUnlockedBalanceAsync();

        var config = new MoneroTxConfig((await wallet.GetIntegratedAddressAsync()).IntegratedAddress!, amount) { AccountIndex = 0, Relay = true };
        var tx = await wallet.CreateTxAsync(config);

        TestRelayedTx(tx);
        Assert.That(await wallet.GetUnlockedBalanceAsync(), Is.LessThan(unlockedBalance1));
        Assert.That(await wallet.GetBalanceAsync(), Is.EqualTo(balance1 - tx.Fee!.Value), "The balance after the send is not the balance before less the fee");
    }

    // Can send to an external wallet
    [Test, Category(Relays)]
    public async Task TestSendToExternal()
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount);

        var recipient = await TestUtils.CreateWalletRpc();
        try
        {
            var recipientAddress = await recipient.GetPrimaryAddressAsync();
            var balance1 = await wallet.GetBalanceAsync();
            var unlockedBalance1 = await wallet.GetUnlockedBalanceAsync();

            // send funds to the recipient
            var config = new MoneroTxConfig((await wallet.GetIntegratedAddressAsync(recipientAddress, "54491f3bb3572a37")).IntegratedAddress!, amount)
            {
                AccountIndex = 0,
                Relay = true,
            };
            var tx = await wallet.CreateTxAsync(config);

            // the balances of the sender
            TestRelayedTx(tx);
            Assert.That(await wallet.GetUnlockedBalanceAsync(), Is.LessThan(unlockedBalance1));
            Assert.That(await wallet.GetBalanceAsync(), Is.EqualTo(balance1 - tx.GetOutgoingAmount() - tx.Fee!.Value));
            Assert.That(await wallet.GetTxsAsync(new MoneroTxQuery { IsConfirmed = false }), Is.Not.Empty);

            // the balance of the recipient
            await recipient.SyncAsync();
            Assert.That(await recipient.GetBalanceAsync(), Is.EqualTo(amount));
        }
        finally
        {
            await CloseWallet(recipient);
        }
    }

    // Can send to multiple addresses in a transaction
    [Test, Category(Relays)]
    public async Task TestSendToMultiple()
    {
        await TestSendToMultiple(5, 3, false);
    }

    // Can send to multiple addresses in split transactions
    [Test, Category(Relays)]
    public async Task TestSendToMultipleSplit()
    {
        await TestSendToMultiple(3, 15, true);
    }

    // Can subtract the fee from the destinations
    [Test, Category(Relays)]
    public async Task TestSubtractFeeFrom()
    {
        await TestSendToMultiple(5, 3, false, true);
    }

    private static async Task TestSendToMultiple(int numAccounts, int numSubaddressesPerAccount, bool canSplit, bool subtractFee = false)
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 5;

        // create the accounts and the subaddresses that are missing
        while ((await wallet.GetAccountsAsync()).Count < numAccounts) await wallet.CreateAccountAsync();
        var destinations = new List<MoneroDestination>();
        for (uint accountIdx = 0; accountIdx < numAccounts; accountIdx++)
        {
            while ((await wallet.GetSubaddressesAsync(accountIdx)).Count < numSubaddressesPerAccount) await wallet.CreateSubaddressAsync(accountIdx);
            var subaddresses = await wallet.GetSubaddressesAsync(accountIdx);
            destinations.AddRange(subaddresses.Take(numSubaddressesPerAccount).Select(subaddress => new MoneroDestination(subaddress.Address!, amount)));
        }

        // the sender has the funds for all of them
        var total = amount * (ulong)destinations.Count;
        await PrepareToSend(wallet, total + MaxFee * 3);

        var config = new MoneroTxConfig { AccountIndex = 0, Destinations = destinations, CanSplit = canSplit, Relay = true };
        if (subtractFee) config.SubtractFeeFrom = [.. Enumerable.Range(0, destinations.Count).Select(i => (uint)i)];

        var txs = await wallet.CreateTxsAsync(config);

        Assert.That(txs, Is.Not.Empty);
        if (!canSplit) Assert.That(txs, Has.Count.EqualTo(1));
        foreach (var tx in txs)
        {
            TestRelayedTx(tx);
            Assert.That(tx.OutgoingTransfer!.Destinations!.Count, Is.GreaterThan(0));
        }

        // all the destinations are paid, in the sum of the txs
        var paid = txs.SelectMany(tx => tx.OutgoingTransfer!.Destinations!).Sum(destination => (decimal)destination.Amount!.Value);
        if (subtractFee)
        {
            var fees = txs.Sum(tx => (decimal)tx.Fee!.Value);
            Assert.That(paid, Is.EqualTo((decimal)total - fees), "The fee was not subtracted from the destinations");
        }
        else
        {
            Assert.That(paid, Is.EqualTo((decimal)total));
        }

        await ConfirmAndUnlock(wallet);
    }

    // Can send to an address in a transaction
    [Test, Category(Relays)]
    public async Task TestSend()
    {
        await TestSendToSingle(false);
    }

    // Can send to an address in split transactions
    [Test, Category(Relays)]
    public async Task TestSendSplit()
    {
        await TestSendToSingle(true);
    }

    // Can create a transaction, and relay it later
    [Test, Category(Relays)]
    public async Task TestCreateThenRelay()
    {
        await TestSendToSingle(false, false);
    }

    // Can create split transactions, and relay them later
    [Test, Category(Relays)]
    public async Task TestCreateThenRelaySplit()
    {
        await TestSendToSingle(true, false);
    }

    private static async Task TestSendToSingle(bool canSplit, bool relay = true)
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount * 2);
        var address = WalletTestUtils.GetExternalWalletAddress();

        var config = new MoneroTxConfig(address, amount) { AccountIndex = 0, CanSplit = canSplit, Relay = relay };
        var txs = canSplit ? await wallet.CreateTxsAsync(config) : [await wallet.CreateTxAsync(config)];
        Assert.That(txs, Is.Not.Empty);

        if (relay)
        {
            foreach (var tx in txs) TestRelayedTx(tx);
        }
        else
        {
            // the txs were not relayed: they have their metadata, and they are not in the pool
            foreach (var tx in txs)
            {
                Assert.That(tx.IsRelayed, Is.False);
                Assert.That(tx.Metadata, Is.Not.Null.And.Not.Empty);
                Assert.That(await wallet.GetTxAsync(tx.Hash!), Is.Null.Or.Property(nameof(MoneroTxWallet.InTxPool)).Not.EqualTo(true));
            }

            // relay them
            List<string> hashes;
            if (txs.Count == 1)
            {
                hashes = [await wallet.RelayTxAsync(txs[0])];
            }
            else
            {
                hashes = await wallet.RelayTxsAsync(txs);
            }

            Assert.That(hashes, Is.EqualTo(txs.Select(tx => tx.Hash)));
            foreach (var hash in hashes)
            {
                var relayed = await wallet.GetTxAsync(hash);
                Assert.That(relayed, Is.Not.Null);
                Assert.That(relayed!.InTxPool, Is.True);
            }
        }

        await ConfirmAndUnlock(wallet);
    }

    // Can relay a transaction by its metadata
    [Test, Category(Relays)]
    public async Task TestRelayTxByMetadata()
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount * 2);

        var tx = await wallet.CreateTxAsync(new MoneroTxConfig(WalletTestUtils.GetExternalWalletAddress(), amount) { AccountIndex = 0, Relay = false });
        var hash = await wallet.RelayTxAsync(tx.Metadata!);

        Assert.That(hash, Is.EqualTo(tx.Hash));
        Assert.That((await wallet.GetTxAsync(hash))!.InTxPool, Is.True);

        await ConfirmAndUnlock(wallet);
    }

    // Can sweep an output identified by its key image
    [Test, Category(Relays)]
    public async Task TestSweepOutput()
    {
        var wallet = await GetWallet();
        await PrepareToSend(wallet, MaxFee * 3);

        // an output that is unspent, unlocked and not frozen
        var outputs = await wallet.GetOutputsAsync(new MoneroOutputQuery
        {
            IsSpent = false,
            IsFrozen = false,
            TxQuery = new MoneroTxQuery { IsLocked = false },
        });
        Assert.That(outputs, Is.Not.Empty);
        var output = outputs[0];

        var config = new MoneroTxConfig(await wallet.GetPrimaryAddressAsync()) { KeyImage = output.KeyImage!.Hex, Relay = true };
        var tx = await wallet.SweepOutputAsync(config);

        TestRelayedTx(tx);
        Assert.That(tx.Inputs!.Select(input => input.KeyImage!.Hex), Does.Contain(output.KeyImage.Hex));

        await ConfirmAndUnlock(wallet);
    }

    // Can sweep the dust, and there is none
    [Test, Category(Relays)]
    public async Task TestSweepDustNoRelay()
    {
        var wallet = await GetWallet();

        var txs = await wallet.SweepDustAsync();

        Assert.That(txs, Is.Empty, "There is no dust in the wallet");
    }

    // Can sweep the unlocked funds of a subaddress that is not the primary one
    [Test, Category(Relays)]
    public async Task TestSweepUnlocked()
    {
        var wallet = await GetWallet();
        await PrepareToSend(wallet, MaxFee * 3);
        var subaddress = await WalletTestUtils.SelectSubaddressWithMinBalance(wallet, MaxFee * 3);
        Assert.That(subaddress, Is.Not.Null, "No subaddress has the balance to sweep");

        var config = new MoneroTxConfig(WalletTestUtils.GetExternalWalletAddress())
        {
            AccountIndex = subaddress!.AccountIndex,
            SubaddressIndices = [subaddress.Index!.Value],
            Relay = true,
        };
        var txs = await wallet.SweepUnlockedAsync(config);

        Assert.That(txs, Is.Not.Empty);
        foreach (var tx in txs) TestRelayedTx(tx);
        Assert.That(await wallet.GetUnlockedBalanceAsync(subaddress.AccountIndex, subaddress.Index), Is.EqualTo(0UL));

        await ConfirmAndUnlock(wallet);
    }

    // Can get the key, prove and check an outgoing transaction
    [Test, Category(Relays)]
    public async Task TestCheckTxKeyAndProofs()
    {
        var wallet = await GetWallet();
        var amount = MaxFee * 3;
        await PrepareToSend(wallet, amount * 2);
        var address = WalletTestUtils.GetExternalWalletAddress();
        var recipient = await TestUtils.CreateWalletRpc();
        string recipientAddress;
        try
        {
            recipientAddress = await recipient.GetPrimaryAddressAsync();
        }
        finally
        {
            await CloseWallet(recipient);
        }

        var tx = await wallet.CreateTxAsync(new MoneroTxConfig(recipientAddress, amount) { AccountIndex = 0, Relay = true });
        await ConfirmAndUnlock(wallet, 2);
        var hash = tx.Hash!;

        // the key of the tx
        var key = await wallet.GetTxKeyAsync(hash);
        Assert.That(key, Is.Not.Null.And.Not.Empty);
        var check = await wallet.CheckTxKeyAsync(hash, key, recipientAddress);
        Assert.That(check.IsGood, Is.True);
        Assert.That(check.ReceivedAmount, Is.EqualTo(amount));
        Assert.That(check.NumConfirmations, Is.GreaterThan(0UL));

        // an invalid hash, key and address
        WalletUtils.TestInvalidTxHashError(Assert.ThrowsAsync<MoneroError>(() => wallet.CheckTxKeyAsync("invalid_tx_hash", key, recipientAddress)));
        WalletUtils.TestInvalidTxKeyError(Assert.ThrowsAsync<MoneroError>(() => wallet.CheckTxKeyAsync(hash, "invalid_tx_key", recipientAddress)));
        WalletUtils.TestInvalidAddressError(Assert.ThrowsAsync<MoneroError>(() => wallet.CheckTxKeyAsync(hash, key, "invalid_tx_address")));

        // the proof of the tx
        const string message = "this transaction is mine";
        var proof = await wallet.GetTxProofAsync(hash, recipientAddress, message);
        Assert.That(proof, Does.StartWith("OutProof"));
        check = await wallet.CheckTxProofAsync(hash, recipientAddress, message, proof);
        Assert.That(check.IsGood, Is.True);
        Assert.That(check.ReceivedAmount, Is.EqualTo(amount));

        // a proof of another message and of another address are not good
        check = await wallet.CheckTxProofAsync(hash, recipientAddress, "another message", proof);
        Assert.That(check.IsGood, Is.False);
        check = await wallet.CheckTxProofAsync(hash, address, message, proof);
        Assert.That(check.IsGood, Is.False);

        // the proof of the spend of the tx
        var spendProof = await wallet.GetSpendProofAsync(hash, message);
        Assert.That(spendProof, Does.StartWith("SpendProof"));
        Assert.That(await wallet.CheckSpendProofAsync(hash, message, spendProof), Is.True);
        Assert.That(await wallet.CheckSpendProofAsync(hash, "another message", spendProof), Is.False);
        WalletUtils.TestSignatureHeaderError(Assert.ThrowsAsync<MoneroError>(() => wallet.CheckSpendProofAsync(hash, message, "invalid signature")));
    }

    // Can prove the reserve of the wallet and of an account
    [Test, Category(Relays)]
    public async Task TestReserveProofs()
    {
        var wallet = await GetWallet();
        await PrepareToSend(wallet, MaxFee);
        var address = await wallet.GetPrimaryAddressAsync();
        const string message = "This is my message to be signed with the reserve proof.";

        // the proof of the whole wallet
        var signature = await wallet.GetReserveProofWalletAsync(message);
        var check = await wallet.CheckReserveProofAsync(address, message, signature);
        Assert.That(check.IsGood, Is.True);
        Assert.That(check.TotalAmount, Is.EqualTo(await wallet.GetBalanceAsync()));

        // the proof of an account
        var accountBalance = await wallet.GetBalanceAsync(0);
        signature = await wallet.GetReserveProofAccountAsync(0, accountBalance, message);
        check = await wallet.CheckReserveProofAsync(address, message, signature);
        Assert.That(check.IsGood, Is.True);
        Assert.That(check.TotalAmount, Is.GreaterThanOrEqualTo(accountBalance));

        // another message, another address, and an amount that the account doesn't have
        check = await wallet.CheckReserveProofAsync(address, "another message", signature);
        Assert.That(check.IsGood, Is.False);
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetReserveProofAccountAsync(0, accountBalance + 1, message));
    }

    // Can set the notes of transactions
    [Test, Category(NonRelays)]
    public async Task TestSetTxNote()
    {
        var wallet = await GetWallet();
        var txs = await wallet.GetTxsAsync();
        Assert.That(txs, Is.Not.Empty);
        var hash = txs[0].Hash!;
        var uuid = WalletUtils.GetRandomString();

        await wallet.SetTxNoteAsync(hash, uuid);

        Assert.That(await wallet.GetTxNoteAsync(hash), Is.EqualTo(uuid));
    }

    // Can set the notes of many transactions
    [Test, Category(NonRelays)]
    public async Task TestSetTxNotes()
    {
        var wallet = await GetWallet();
        var txs = await wallet.GetTxsAsync();
        Assert.That(txs, Has.Count.GreaterThan(3));
        var hashes = txs.Take(3).Select(tx => tx.Hash!).ToList();
        var notes = hashes.Select((_, i) => $"{WalletUtils.GetRandomString()} {i}").ToList();

        await wallet.SetTxNotesAsync(hashes, notes);

        Assert.That(await wallet.GetTxNotesAsync(hashes), Is.EqualTo(notes));
        Assert.ThrowsAsync<ArgumentException>(() => wallet.SetTxNotesAsync(hashes, notes.Take(2).ToList()));
    }

    // Can get the transactions of the wallet
    [Test, Category(NonRelays)]
    public async Task TestGetTxsWallet()
    {
        var wallet = await GetWallet();

        var txs = await wallet.GetTxsAsync();

        Assert.That(txs, Is.Not.Empty, "The wallet has no txs to test");
        foreach (var tx in txs)
        {
            Assert.That(tx.Hash, Has.Length.EqualTo(64));
            Assert.That(tx.IsFailed, Is.Not.True);
            Assert.That(tx.IsIncoming == true || tx.IsOutgoing == true, Is.True);
            if (tx.IsConfirmed == true)
            {
                Assert.That(tx.Block, Is.Not.Null);
                Assert.That(tx.GetHeight(), Is.GreaterThan(0UL));
                Assert.That(tx.InTxPool, Is.Not.True);
            }

            if (tx.IsIncoming == true) Assert.That(tx.IncomingTransfers, Is.Not.Empty);
            if (tx.IsOutgoing == true) Assert.That(tx.OutgoingTransfer, Is.Not.Null);
        }
    }

    // Can get the transactions by their hashes
    [Test, Category(NonRelays)]
    public async Task TestGetTxsByHash()
    {
        var wallet = await GetWallet();
        var txs = await wallet.GetTxsAsync();
        Assert.That(txs, Has.Count.GreaterThan(1));
        txs = [.. txs.OrderBy(_ => Random.Shared.Next()).Take(10)];

        // by a hash
        var fetched = await wallet.GetTxAsync(txs[0].Hash!);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Hash, Is.EqualTo(txs[0].Hash));

        // by two hashes
        var fetchedTxs = await wallet.GetTxsAsync([txs[0].Hash!, txs[1].Hash!]);
        Assert.That(fetchedTxs.Select(tx => tx.Hash), Is.EqualTo(new[] { txs[0].Hash, txs[1].Hash }));

        // by the hashes in the order they are asked
        var hashes = txs.Select(tx => tx.Hash!).ToList();
        fetchedTxs = await wallet.GetTxsAsync(hashes);
        Assert.That(fetchedTxs.Select(tx => tx.Hash), Is.EqualTo(hashes));

        // with a hash that the wallet doesn't have
        hashes.Add("d01ede9cde813b2a693069b640c4b99c5adbdb49fbbd8da2c16c8087d0c3e320");
        fetchedTxs = await wallet.GetTxsAsync(hashes);
        Assert.That(fetchedTxs.Select(tx => tx.Hash), Is.EqualTo(hashes.Take(hashes.Count - 1)));
    }

    // Can get the transactions of a query
    [Test, Category(NonRelays)]
    public async Task TestGetTxsWithQuery()
    {
        var wallet = await GetWallet();
        var all = await wallet.GetTxsAsync();

        // confirmed and incoming
        var confirmed = await wallet.GetTxsAsync(new MoneroTxQuery { IsConfirmed = true });
        Assert.That(confirmed, Is.Not.Empty);
        Assert.That(confirmed.All(tx => tx.IsConfirmed == true), Is.True);
        var incoming = await wallet.GetTxsAsync(new MoneroTxQuery { IsIncoming = true });
        Assert.That(incoming.All(tx => tx.IsIncoming == true), Is.True);
        var outgoing = await wallet.GetTxsAsync(new MoneroTxQuery { IsOutgoing = true });
        Assert.That(outgoing.All(tx => tx.IsOutgoing == true), Is.True);

        // a height, and a range of heights
        var height = confirmed[0].GetHeight()!.Value;
        var atHeight = await wallet.GetTxsAsync(new MoneroTxQuery { Height = height });
        Assert.That(atHeight, Is.Not.Empty);
        Assert.That(atHeight.All(tx => tx.GetHeight() == height), Is.True);
        var inRange = await wallet.GetTxsAsync(new MoneroTxQuery { MinHeight = height, MaxHeight = height + 5 });
        Assert.That(inRange, Is.Not.Empty);
        Assert.That(inRange.All(tx => tx.GetHeight() >= height && tx.GetHeight() <= height + 5), Is.True);

        // the txs of a query are some of all the txs
        var hashes = all.Select(tx => tx.Hash).ToHashSet();
        Assert.That(confirmed.All(tx => hashes.Contains(tx.Hash)), Is.True);
    }

    // Can get the transfers of the wallet
    [Test, Category(NonRelays)]
    public async Task TestGetTransfers()
    {
        var wallet = await GetWallet();

        var transfers = await wallet.GetTransfersAsync();

        Assert.That(transfers, Is.Not.Empty);
        foreach (var transfer in transfers)
        {
            Assert.That(transfer.Tx, Is.Not.Null);
            Assert.That(transfer.Amount, Is.Not.Null);
            Assert.That(transfer.AccountIndex, Is.Not.Null);
            Assert.That(transfer.GetIsIncoming() ^ transfer.GetIsOutgoing(), Is.True);
        }

        // the transfers of an account, and of a subaddress
        var ofAccount = await wallet.GetTransfersAsync(0);
        Assert.That(ofAccount.All(transfer => transfer.AccountIndex == 0), Is.True);
        var ofSubaddress = await wallet.GetTransfersAsync(0, 1);
        Assert.That(ofSubaddress.Count, Is.LessThanOrEqualTo(ofAccount.Count));

        // the incoming and the outgoing transfers
        var incoming = await wallet.GetTransfersAsync(new MoneroTransferQuery { IsIncoming = true });
        Assert.That(incoming.All(transfer => transfer.GetIsIncoming() == true), Is.True);
        var outgoing = await wallet.GetTransfersAsync(new MoneroTransferQuery { IsIncoming = false });
        Assert.That(outgoing.All(transfer => transfer.GetIsOutgoing() == true), Is.True);
        Assert.That(incoming.Count + outgoing.Count, Is.EqualTo(transfers.Count));
    }

    // Can get the outputs of the wallet
    [Test, Category(NonRelays)]
    public async Task TestGetOutputs()
    {
        var wallet = await GetWallet();

        var outputs = await wallet.GetOutputsAsync();

        Assert.That(outputs, Is.Not.Empty);
        foreach (var output in outputs)
        {
            Assert.That(output.AccountIndex, Is.Not.Null);
            Assert.That(output.SubaddressIndex, Is.Not.Null);
            Assert.That(output.Index, Is.Not.Null);
            Assert.That(output.Amount, Is.GreaterThan(0UL));
            Assert.That(output.KeyImage, Is.Not.Null);
            Assert.That(output.KeyImage!.Hex, Is.Not.Null.And.Not.Empty);
            Assert.That(output.IsSpent, Is.Not.Null);
            Assert.That(output.IsFrozen, Is.Not.Null);
            Assert.That(output.GetTxWallet(), Is.Not.Null);
        }

        // the unspent outputs of an account
        var unspent = await wallet.GetOutputsAsync(new MoneroOutputQuery { IsSpent = false, AccountIndex = 0 });
        Assert.That(unspent, Is.Not.Empty);
        Assert.That(unspent.All(output => output.IsSpent == false && output.AccountIndex == 0), Is.True);
    }

    // Can freeze and thaw outputs
    [Test, Category(NonRelays)]
    public async Task TestFreezeOutputs()
    {
        var wallet = await GetWallet();

        // an output that is available
        var query = new MoneroOutputQuery { IsSpent = false, IsFrozen = false, TxQuery = new MoneroTxQuery { IsLocked = false } };
        var outputs = await wallet.GetOutputsAsync(query);
        Assert.That(outputs, Is.Not.Empty);
        Assert.That(outputs.All(output => output.IsFrozen == false), Is.True);
        var keyImage = outputs[0].KeyImage!.Hex!;
        Assert.That(outputs[0].GetTxWallet()!.IsLocked, Is.False);
        Assert.That(outputs[0].IsSpent, Is.False);

        // freeze the output
        var unlockedBalance = await wallet.GetUnlockedBalanceAsync();
        await wallet.FreezeOutputAsync(keyImage);
        Assert.That(await wallet.IsOutputFrozenAsync(keyImage), Is.True);
        Assert.That(await wallet.GetUnlockedBalanceAsync(), Is.LessThan(unlockedBalance));

        // the output is in the frozen outputs, and not in the available ones
        var frozen = await wallet.GetOutputsAsync(new MoneroOutputQuery { IsFrozen = true });
        Assert.That(frozen.Any(output => output.KeyImage!.Hex == keyImage), Is.True);
        var available = await wallet.GetOutputsAsync(query);
        Assert.That(available.Any(output => output.KeyImage!.Hex == keyImage), Is.False);

        // thaw the output
        await wallet.ThawOutputAsync(keyImage);
        Assert.That(await wallet.IsOutputFrozenAsync(keyImage), Is.False);
        Assert.That(await wallet.GetUnlockedBalanceAsync(), Is.EqualTo(unlockedBalance));

        // a key image that the wallet doesn't know
        Assert.ThrowsAsync<MoneroError>(() => wallet.FreezeOutputAsync("aa".PadRight(64, 'a')));
    }
}
