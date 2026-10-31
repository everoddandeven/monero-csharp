using Monero.Common;
using Monero.IntegrationTests.Utils;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests;

public partial class MoneroWalletRpcIntegrationTests
{
    // Can get the version of the wallet
    [Test, Category(NonRelays)]
    public async Task TestGetVersion()
    {
        var wallet = await GetWallet();

        var version = await wallet.GetVersionAsync();

        Assert.That(version.Number, Is.GreaterThan(0u));
        Assert.That(version.IsRelease, Is.Not.Null);
    }

    // Can set the daemon connection
    [Test, Category(NonRelays)]
    public async Task TestSetDaemonConnection()
    {
        // create a random wallet, which has the daemon of the server
        var wallet = await TestUtils.CreateWalletRpc();
        try
        {
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.True);

            // set an empty server uri
            await wallet.SetDaemonConnectionAsync("");
            Assert.That(await wallet.GetDaemonConnectionAsync(), Is.Null);
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.False);

            // set the uri of a server that is offline
            await wallet.SetDaemonConnectionAsync(TestUtils.OfflineServerUri);
            Assert.That((await wallet.GetDaemonConnectionAsync())!.Uri, Is.EqualTo(TestUtils.OfflineServerUri));
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.False);

            // set the daemon with wrong credentials
            var daemonUri = "http://node_2:18081";
            await wallet.SetDaemonConnectionAsync(daemonUri, "wronguser", "wrongpass");
            var connection = await wallet.GetDaemonConnectionAsync();
            Assert.That(connection!.Uri, Is.EqualTo(daemonUri));
            Assert.That(connection.Username, Is.EqualTo("wronguser"));
            Assert.That(connection.Password, Is.EqualTo("wrongpass"));
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.False);

            // set the daemon with its credentials
            await wallet.SetDaemonConnectionAsync(daemonUri, TestUtils.DaemonRpcUsername, TestUtils.DaemonRpcPassword);
            connection = await wallet.GetDaemonConnectionAsync();
            Assert.That(connection!.Uri, Is.EqualTo(daemonUri));
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.True);

            // disconnect the wallet
            await wallet.SetDaemonConnectionAsync((MoneroRpcConnection?)null);
            Assert.That(await wallet.GetDaemonConnectionAsync(), Is.Null);

            // set the uri of an invalid server
            await wallet.SetDaemonConnectionAsync("abc123");
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.False);

            // attempt to sync
            var error = Assert.ThrowsAsync<MoneroError>(() => wallet.SyncAsync());
            Assert.That(error!.Message, Does.Contain(WalletUtils.NotConnectedError));
        }
        finally
        {
            await CloseWallet(wallet);
        }
    }

    // Can get the seed
    [Test, Category(NonRelays)]
    public async Task TestGetSeed()
    {
        var wallet = await GetWallet();

        var seed = await wallet.GetSeedAsync();

        WalletUtils.ValidateMnemonic(seed);
        Assert.That(seed, Is.EqualTo(TestUtils.Seed));
    }

    // monero-wallet-rpc can't get the language of the seed
    [Test, Category(NonRelays)]
    public async Task TestGetSeedLanguage()
    {
        var wallet = await GetWallet();

        Assert.ThrowsAsync<MoneroError>(() => wallet.GetSeedLanguageAsync());
    }

    // Can get the private view key
    [Test, Category(NonRelays)]
    public async Task TestGetPrivateViewKey()
    {
        var wallet = await GetWallet();

        var key = await wallet.GetPrivateViewKeyAsync();

        WalletUtils.ValidateKey(key);
        Assert.That(key, Is.EqualTo(TestUtils.PrivateViewKey));
    }

    // Can get the private spend key
    [Test, Category(NonRelays)]
    public async Task TestGetPrivateSpendKey()
    {
        var wallet = await GetWallet();

        var key = await wallet.GetPrivateSpendKeyAsync();

        WalletUtils.ValidateKey(key);
        Assert.That(key, Is.EqualTo(TestUtils.PrivateSpendKey));
    }

    // monero-wallet-rpc can't get the public keys
    [Test, Category(NonRelays)]
    public async Task TestGetPublicKeys()
    {
        var wallet = await GetWallet();

        Assert.ThrowsAsync<MoneroError>(() => wallet.GetPublicViewKeyAsync());
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetPublicSpendKeyAsync());
    }

    // Can get the primary address
    [Test, Category(NonRelays)]
    public async Task TestGetPrimaryAddress()
    {
        var wallet = await GetWallet();

        var primaryAddress = await wallet.GetPrimaryAddressAsync();

        WalletUtils.ValidateAddress(primaryAddress);
        Assert.That(primaryAddress, Is.EqualTo(TestUtils.Address));
        Assert.That(await wallet.GetAddressAsync(0, 0), Is.EqualTo(primaryAddress));
    }

    // Can get the address of a subaddress at an account index and a subaddress index
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddressAddress()
    {
        var wallet = await GetWallet();

        Assert.That(await wallet.GetPrimaryAddressAsync(), Is.EqualTo(await wallet.GetAddressAsync(0, 0)));
        foreach (var account in await wallet.GetAccountsAsync(true))
        {
            foreach (var subaddress in account.Subaddresses!)
            {
                Assert.That(subaddress.Address, Is.EqualTo(await wallet.GetAddressAsync(account.Index!.Value, subaddress.Index!.Value)));
            }
        }
    }

    // Can get the account index and the subaddress index of an address
    [Test, Category(NonRelays)]
    public async Task TestGetAddressIndices()
    {
        var wallet = await GetWallet();

        // get the last subaddress
        var accounts = await wallet.GetAccountsAsync(true);
        var accountIdx = (uint)(accounts.Count - 1);
        var subaddressIdx = (uint)(accounts[^1].Subaddresses!.Count - 1);
        var address = await wallet.GetAddressAsync(accountIdx, subaddressIdx);
        Assert.That(address, Is.Not.Null.And.Not.Empty);

        var subaddress = await wallet.GetAddressIndexAsync(address);
        Assert.That(subaddress.AccountIndex, Is.EqualTo(accountIdx));
        Assert.That(subaddress.Index, Is.EqualTo(subaddressIdx));

        // an address that is valid and is not of the wallet
        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetAddressIndexAsync(WalletTestUtils.GetExternalWalletAddress()));
        Assert.That(error!.Message, Is.EqualTo("Address doesn't belong to the wallet"));

        // an invalid address
        error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetAddressIndexAsync("this is definitely not an address"));
        WalletUtils.TestInvalidAddressError(error);
    }

    // Can get an integrated address
    [Test, Category(NonRelays)]
    public async Task TestGetIntegratedAddress()
    {
        var wallet = await GetWallet();
        var address = await wallet.GetPrimaryAddressAsync();

        // a valid payment id
        const string paymentId = "03284e41c342f036";
        var integratedAddress = await wallet.GetIntegratedAddressAsync("", paymentId);
        Assert.That(integratedAddress.StandardAddress, Is.EqualTo(address));
        Assert.That(integratedAddress.PaymentId, Is.EqualTo(paymentId));

        // no payment id, which generates a new one
        integratedAddress = await wallet.GetIntegratedAddressAsync();
        Assert.That(integratedAddress.StandardAddress, Is.EqualTo(address));
        Assert.That(integratedAddress.PaymentId, Is.Not.Null.And.Not.Empty);

        // the primary address
        integratedAddress = await wallet.GetIntegratedAddressAsync(address, paymentId);
        Assert.That(integratedAddress.StandardAddress, Is.EqualTo(address));
        Assert.That(integratedAddress.PaymentId, Is.EqualTo(paymentId));

        // a subaddress
        if ((await wallet.GetSubaddressesAsync(0)).Count < 2) await wallet.CreateSubaddressAsync(0);
        var subaddress = await wallet.GetSubaddressAsync(0, 1);
        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetIntegratedAddressAsync(subaddress.Address));
        Assert.That(error!.Message, Is.EqualTo("Subaddress shouldn't be used"));

        // an invalid payment id
        const string invalidPaymentId = "invalid_payment_id_123456";
        error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetIntegratedAddressAsync("", invalidPaymentId));
        Assert.That(error!.Message, Is.EqualTo($"Invalid payment ID: {invalidPaymentId}"));
    }

    // Can decode an integrated address
    [Test, Category(NonRelays)]
    public async Task TestDecodeIntegratedAddress()
    {
        var wallet = await GetWallet();

        var integratedAddress = await wallet.GetIntegratedAddressAsync("", "03284e41c342f036");
        var decodedAddress = await wallet.DecodeIntegratedAddressAsync(integratedAddress.IntegratedAddress!);
        AssertUtils.AssertEquals(integratedAddress, decodedAddress);

        // an invalid address
        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.DecodeIntegratedAddressAsync("bad address"));
        WalletUtils.TestInvalidAddressError(error);

        // an invalid payment id
        error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetIntegratedAddressAsync(TestUtils.Address, "invalid payment id"));
        Assert.That(error!.Message, Is.EqualTo("Invalid payment ID: invalid payment id"));
    }

    // Can sync without progress
    [Test, Category(NonRelays)]
    public async Task TestSyncWithoutProgress()
    {
        var wallet = await GetWallet();
        const ulong numBlocks = 100;
        var chainHeight = await Daemon.GetHeightAsync();
        Assert.That(chainHeight, Is.GreaterThanOrEqualTo(numBlocks));

        var result = await wallet.SyncAsync(chainHeight - numBlocks); // sync the end of the chain

        Assert.That(result.NumBlocksFetched, Is.GreaterThanOrEqualTo(0UL));
        Assert.That(result.ReceivedMoney, Is.Not.Null);
    }

    // Can get the height that the wallet is synchronized to
    [Test, Category(NonRelays)]
    public async Task TestGetHeight()
    {
        var wallet = await GetWallet();

        Assert.That(await wallet.GetHeightAsync(), Is.GreaterThan(0UL));
    }

    // monero-wallet-rpc can't get the heights of the daemon, or the height of a date
    [Test, Category(NonRelays)]
    public async Task TestNotSupportedHeights()
    {
        var wallet = await GetWallet();

        Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.True, "Wallet is not connected to daemon");
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetDaemonHeightAsync());
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetDaemonMaxPeerHeightAsync());
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetHeightByDateAsync(2025, 1, 1));
    }

    // Can't get a height by an invalid date
    [Test, Category(NonRelays)]
    public async Task TestGetHeightByInvalidDate()
    {
        var wallet = await GetWallet();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => wallet.GetHeightByDateAsync(2025, 13, 1));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => wallet.GetHeightByDateAsync(2025, 1, 0));
    }

    // Can get the locked and unlocked balances of the wallet, the accounts and the subaddresses
    [Test, Category(NonRelays)]
    public async Task TestGetAllBalances()
    {
        var wallet = await GetWallet();

        // fetch the accounts with all their info as the reference
        var accounts = await wallet.GetAccountsAsync(true);

        // the balances add up between the accounts and the wallet
        ulong accountsBalance = 0;
        ulong accountsUnlockedBalance = 0;
        foreach (var account in accounts)
        {
            accountsBalance += account.Balance!.Value;
            accountsUnlockedBalance += account.UnlockedBalance!.Value;

            // the balances add up between the subaddresses and the account
            ulong subaddressesBalance = 0;
            ulong subaddressesUnlockedBalance = 0;
            foreach (var subaddress in account.Subaddresses!)
            {
                subaddressesBalance += subaddress.Balance!.Value;
                subaddressesUnlockedBalance += subaddress.UnlockedBalance!.Value;

                // the balances are consistent with the call to get the accounts
                Assert.That(await wallet.GetBalanceAsync(subaddress.AccountIndex, subaddress.Index), Is.EqualTo(subaddress.Balance));
                Assert.That(await wallet.GetUnlockedBalanceAsync(subaddress.AccountIndex, subaddress.Index), Is.EqualTo(subaddress.UnlockedBalance));
            }

            Assert.That(await wallet.GetBalanceAsync(account.Index), Is.EqualTo(subaddressesBalance));
            Assert.That(await wallet.GetUnlockedBalanceAsync(account.Index), Is.EqualTo(subaddressesUnlockedBalance));
        }

        GenUtils.TestUnsignedBigInteger(accountsBalance);
        GenUtils.TestUnsignedBigInteger(accountsUnlockedBalance);
        Assert.That(await wallet.GetBalanceAsync(), Is.EqualTo(accountsBalance));
        Assert.That(await wallet.GetUnlockedBalanceAsync(), Is.EqualTo(accountsUnlockedBalance));
    }

    // Can't get the balance of a subaddress without its account
    [Test, Category(NonRelays)]
    public async Task TestGetBalanceOfASubaddressNeedsTheAccount()
    {
        var wallet = await GetWallet();

        Assert.ThrowsAsync<ArgumentException>(() => wallet.GetBalanceAsync(null, 0));
        Assert.ThrowsAsync<ArgumentException>(() => wallet.GetUnlockedBalanceAsync(null, 0));
    }

    // Can get the accounts without subaddresses
    [Test, Category(NonRelays)]
    public async Task TestGetAccountsWithoutSubaddresses()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync();

        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            WalletUtils.TestAccount(account);
            Assert.That(account.Subaddresses ?? [], Is.Empty);
        }
    }

    // Can get the accounts with subaddresses
    [Test, Category(NonRelays)]
    public async Task TestGetAccountsWithSubaddresses()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync(true);

        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            WalletUtils.TestAccount(account);
            Assert.That(account.Subaddresses, Is.Not.Empty);
        }
    }

    // Can get an account at an index
    [Test, Category(NonRelays)]
    public async Task TestGetAccount()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync();
        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            WalletUtils.TestAccount(account);

            // without subaddresses
            var retrieved = await wallet.GetAccountAsync(account.Index!.Value);
            Assert.That(retrieved.Subaddresses ?? [], Is.Empty);

            // with subaddresses
            retrieved = await wallet.GetAccountAsync(account.Index!.Value, true);
            Assert.That(retrieved.Subaddresses, Is.Not.Empty);
        }
    }

    // Can create an account without a label
    [Test, Category(NonRelays)]
    public async Task TestCreateAccountWithoutLabel()
    {
        var wallet = await GetWallet();
        var accountsBefore = await wallet.GetAccountsAsync();

        var createdAccount = await wallet.CreateAccountAsync();

        WalletUtils.TestAccount(createdAccount);
        Assert.That(await wallet.GetAccountsAsync(), Has.Count.EqualTo(accountsBefore.Count + 1));
    }

    // Can create an account with a label
    [Test, Category(NonRelays)]
    public async Task TestCreateAccountWithLabel()
    {
        var wallet = await GetWallet();
        var accountsBefore = await wallet.GetAccountsAsync();
        var label = WalletUtils.GetRandomString();

        var createdAccount = await wallet.CreateAccountAsync(label);

        WalletUtils.TestAccount(createdAccount);
        Assert.That(await wallet.GetAccountsAsync(), Has.Count.EqualTo(accountsBefore.Count + 1));
        Assert.That((await wallet.GetSubaddressAsync(createdAccount.Index!.Value, 0)).Label, Is.EqualTo(label));

        // fetch the account
        createdAccount = await wallet.GetAccountAsync(createdAccount.Index!.Value);
        WalletUtils.TestAccount(createdAccount);

        // create an account with the same label
        createdAccount = await wallet.CreateAccountAsync(label);
        WalletUtils.TestAccount(createdAccount);
        Assert.That(await wallet.GetAccountsAsync(), Has.Count.EqualTo(accountsBefore.Count + 2));
        Assert.That((await wallet.GetSubaddressAsync(createdAccount.Index!.Value, 0)).Label, Is.EqualTo(label));
    }

    // Can get the subaddresses of an account
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddresses()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync();
        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            var subaddresses = await wallet.GetSubaddressesAsync(account.Index!.Value);

            Assert.That(subaddresses, Is.Not.Empty);
            foreach (var subaddress in subaddresses)
            {
                WalletUtils.TestSubaddress(subaddress);
                Assert.That(subaddress.AccountIndex, Is.EqualTo(account.Index));
            }
        }
    }

    // Can get the subaddresses of an account by their indices
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddressesByIndices()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync();
        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            var subaddresses = await wallet.GetSubaddressesAsync(account.Index!.Value);
            Assert.That(subaddresses, Is.Not.Empty);

            // leave out the first subaddress, if there are more
            if (subaddresses.Count > 1) subaddresses = [.. subaddresses.Skip(1)];

            var indices = subaddresses.Select(subaddress => subaddress.Index!.Value).ToList();
            var fetched = await wallet.GetSubaddressesAsync(account.Index!.Value, indices);

            Assert.That(MoneroJson.Serialize(fetched), Is.EqualTo(MoneroJson.Serialize(subaddresses)));
        }
    }

    // Can get a subaddress by its indices
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddressByIndex()
    {
        var wallet = await GetWallet();

        var accounts = await wallet.GetAccountsAsync();
        Assert.That(accounts, Is.Not.Empty);
        foreach (var account in accounts)
        {
            var subaddresses = await wallet.GetSubaddressesAsync(account.Index!.Value);
            Assert.That(subaddresses, Is.Not.Empty);
            foreach (var subaddress in subaddresses)
            {
                WalletUtils.TestSubaddress(subaddress);
                AssertUtils.AssertEquals(subaddress, await wallet.GetSubaddressAsync(account.Index!.Value, subaddress.Index!.Value));

                // the plural call with one index
                AssertUtils.AssertEquals(subaddress, (await wallet.GetSubaddressesAsync(account.Index!.Value, [subaddress.Index!.Value]))[0]);
            }
        }
    }

    // Can create a subaddress with and without a label
    [Test, Category(NonRelays)]
    public async Task TestCreateSubaddress()
    {
        var wallet = await GetWallet();
        if ((await wallet.GetAccountsAsync()).Count < 2) await wallet.CreateAccountAsync();

        // create subaddresses across accounts
        for (uint accountIdx = 0; accountIdx < 2; accountIdx++)
        {
            // without a label
            var subaddresses = await wallet.GetSubaddressesAsync(accountIdx);
            var subaddress = await wallet.CreateSubaddressAsync(accountIdx);
            Assert.That(subaddress.Label, Is.Null.Or.Empty);
            WalletUtils.TestSubaddress(subaddress);
            var subaddressesNew = await wallet.GetSubaddressesAsync(accountIdx);
            Assert.That(subaddressesNew, Has.Count.EqualTo(subaddresses.Count + 1));
            AssertUtils.AssertEquals(subaddress, subaddressesNew[^1]);

            // with a label
            subaddresses = await wallet.GetSubaddressesAsync(accountIdx);
            var uuid = WalletUtils.GetRandomString();
            subaddress = await wallet.CreateSubaddressAsync(accountIdx, uuid);
            Assert.That(subaddress.Label, Is.EqualTo(uuid));
            WalletUtils.TestSubaddress(subaddress);
            subaddressesNew = await wallet.GetSubaddressesAsync(accountIdx);
            Assert.That(subaddressesNew, Has.Count.EqualTo(subaddresses.Count + 1));
            AssertUtils.AssertEquals(subaddress, subaddressesNew[^1]);
        }
    }

    // Can set the labels of subaddresses
    [Test, Category(NonRelays)]
    public async Task TestSetSubaddressLabel()
    {
        var wallet = await GetWallet();
        while ((await wallet.GetSubaddressesAsync(0)).Count < 3) await wallet.CreateSubaddressAsync(0);

        var count = (await wallet.GetSubaddressesAsync(0)).Count;
        for (uint subaddressIdx = 0; subaddressIdx < count; subaddressIdx++)
        {
            var label = WalletUtils.GetRandomString();

            await wallet.SetSubaddressLabelAsync(0, subaddressIdx, label);

            Assert.That((await wallet.GetSubaddressAsync(0, subaddressIdx)).Label, Is.EqualTo(label));
        }
    }

    // Can sign and verify messages
    [Test, Category(NonRelays)]
    public async Task TestSignAndVerifyMessages()
    {
        var wallet = await GetWallet();
        const string message = "This is a super important message which needs to be signed and verified.";
        (uint Account, uint Index)[] subaddresses = [(0, 0), (0, 1), (1, 0)];

        foreach (var (accountIdx, idx) in subaddresses)
        {
            foreach (var type in new[] { MoneroMessageSignatureType.SignWithSpendKey, MoneroMessageSignatureType.SignWithViewKey })
            {
                // sign and verify the message
                var signature = await wallet.SignMessageAsync(message, type, accountIdx, idx);
                var result = await wallet.VerifyMessageAsync(message, await wallet.GetAddressAsync(accountIdx, idx), signature);
                WalletUtils.TestMessageSignatureResult(result, true);
                Assert.That(result.SignatureType, Is.EqualTo(type));

                // verify the message with an incorrect address
                result = await wallet.VerifyMessageAsync(message, await wallet.GetAddressAsync(0, 2), signature);
                WalletUtils.TestMessageSignatureResult(result, false);

                // verify the message with an invalid address
                result = await wallet.VerifyMessageAsync(message, "invalid address", signature);
                WalletUtils.TestMessageSignatureResult(result, false);

                // verify the message with an external address
                result = await wallet.VerifyMessageAsync(message, WalletTestUtils.GetExternalWalletAddress(), signature);
                WalletUtils.TestMessageSignatureResult(result, false);
            }
        }
    }

    // Has an address book
    [Test, Category(NonRelays)]
    public async Task TestAddressBook()
    {
        var wallet = await GetWallet();

        // the initial state
        var entries = await wallet.GetAddressBookEntriesAsync();
        var numEntriesStart = entries.Count;
        foreach (var entry in entries) WalletUtils.TestAddressBookEntry(entry);

        // add standard addresses
        const int numEntries = 5;
        var address = WalletTestUtils.GetExternalWalletAddress();
        var indices = new List<ulong>();
        for (var i = 0; i < numEntries; i++) indices.Add(await wallet.AddAddressBookEntryAsync(address, "hi there!"));

        entries = await wallet.GetAddressBookEntriesAsync();
        Assert.That(entries, Has.Count.EqualTo(numEntriesStart + numEntries));
        foreach (var idx in indices)
        {
            var entry = entries.SingleOrDefault(e => e.Index == idx);
            Assert.That(entry, Is.Not.Null, $"Index {idx} is not in the address book");
            WalletUtils.TestAddressBookEntry(entry);
            Assert.That(entry!.Address, Is.EqualTo(address));
            Assert.That(entry.Description, Is.EqualTo("hi there!"));
        }

        // edit each entry
        foreach (var idx in indices) await wallet.EditAddressBookEntryAsync(idx, null, "hello there!!");

        entries = await wallet.GetAddressBookEntriesAsync(indices);
        Assert.That(entries, Has.Count.EqualTo(numEntries));
        Assert.That(entries.All(entry => entry.Description == "hello there!!"), Is.True);

        // delete the entries at the starting index
        for (var i = 0; i < indices.Count; i++) await wallet.DeleteAddressBookEntryAsync(indices[0]);
        Assert.That(await wallet.GetAddressBookEntriesAsync(), Has.Count.EqualTo(numEntriesStart));

        // add integrated addresses, with a payment id less one character
        const string paymentId = "03284e41c342f03";
        indices = [];
        var integratedAddresses = new Dictionary<ulong, MoneroIntegratedAddress>();
        var descriptions = new Dictionary<ulong, string>();
        for (var i = 0; i < numEntries; i++)
        {
            // a unique integrated address
            var integratedAddress = await wallet.GetIntegratedAddressAsync("", $"{paymentId}{i}");
            var uuid = WalletUtils.GetRandomString();
            var idx = await wallet.AddAddressBookEntryAsync(integratedAddress.IntegratedAddress!, uuid);
            indices.Add(idx);
            integratedAddresses[idx] = integratedAddress;
            descriptions[idx] = uuid;
        }

        entries = await wallet.GetAddressBookEntriesAsync();
        Assert.That(entries, Has.Count.EqualTo(numEntriesStart + numEntries));
        foreach (var idx in indices)
        {
            var entry = entries.SingleOrDefault(e => e.Index == idx);
            Assert.That(entry, Is.Not.Null, $"Index {idx} is not in the address book");
            WalletUtils.TestAddressBookEntry(entry);
            Assert.That(entry!.Description, Is.EqualTo(descriptions[idx]));
            Assert.That(entry.Address, Is.EqualTo(integratedAddresses[idx].IntegratedAddress));
            Assert.That(entry.PaymentId, Is.Null);
        }

        // delete the entries at the starting index
        for (var i = 0; i < indices.Count; i++) await wallet.DeleteAddressBookEntryAsync(indices[0]);
        Assert.That(await wallet.GetAddressBookEntriesAsync(), Has.Count.EqualTo(numEntriesStart));
    }

    // Can get and set arbitrary attributes
    [Test, Category(NonRelays)]
    public async Task TestSetAttributes()
    {
        var wallet = await GetWallet();

        // set the attributes
        var attributes = new Dictionary<string, string>();
        for (var i = 0; i < 5; i++)
        {
            var key = $"attr{i}";
            var value = WalletUtils.GetRandomString();
            attributes[key] = value;
            await wallet.SetAttributeAsync(key, value);
        }

        // test the attributes
        foreach (var (key, value) in attributes) Assert.That(await wallet.GetAttributeAsync(key), Is.EqualTo(value));

        // an attribute that is not set
        Assert.That(await wallet.GetAttributeAsync("unset_key"), Is.Null);
    }

    // Can convert between a tx config and a payment URI
    [Test, Category(NonRelays)]
    public async Task TestGetPaymentUri()
    {
        var wallet = await GetWallet();

        // an address and an amount
        var config1 = new MoneroTxConfig(await wallet.GetAddressAsync(0, 0), 0);
        var uri = await wallet.GetPaymentUriAsync(config1);
        var config2 = await wallet.ParsePaymentUriAsync(uri);
        AssertUtils.AssertEquals(config1, config2);

        // a subaddress and all the fields
        var subaddress = await wallet.GetSubaddressAsync(0, 1);
        config1.Destinations![0].Address = subaddress.Address;
        config1.Destinations[0].Amount = 425000000000;
        config1.RecipientName = "John Doe";
        config1.Note = "OMZG XMR FTW";
        uri = await wallet.GetPaymentUriAsync(config1);
        config2 = await wallet.ParsePaymentUriAsync(uri);
        AssertUtils.AssertEquals(config1, config2);

        // no address
        var address = config1.Destinations[0].Address;
        config1.Destinations[0].Address = null;
        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetPaymentUriAsync(config1));
        Assert.That(error!.Message, Does.Contain("Cannot make URI from supplied parameters"));
        config1.Destinations[0].Address = address;

        // a standalone payment id
        config1.PaymentId = "03284e41c342f03603284e41c342f03603284e41c342f03603284e41c342f036";
        error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetPaymentUriAsync(config1));
        Assert.That(error!.Message, Does.Contain("Cannot make URI from supplied parameters"));
    }

    // Can start and stop mining
    [Test, Category(NonRelays)]
    public async Task TestMining()
    {
        var wallet = await GetWallet();
        var status = await Daemon.GetMiningStatusAsync();
        if (status.IsActive == true) await wallet.StopMiningAsync();

        await wallet.StartMiningAsync(1, false, true);
        await wallet.StopMiningAsync();
    }

    // Can change the password of a wallet
    [Test, Category(NonRelays)]
    public async Task TestChangePassword()
    {
        // create a random wallet
        var config = new MoneroWalletConfig { Password = TestUtils.WalletPassword };
        var wallet = await TestUtils.CreateWalletRpc(config);
        var path = await wallet.GetPathAsync();

        // change the password
        const string newPassword = "";
        await wallet.ChangePasswordAsync(TestUtils.WalletPassword, newPassword);

        // close the wallet without saving
        await CloseWallet(wallet);

        // the old password doesn't work: the change is saved at once
        var error = Assert.ThrowsAsync<MoneroError>(() => TestUtils.OpenWalletRpc(new MoneroWalletConfig { Path = path, Password = TestUtils.WalletPassword }));
        Assert.That(error!.Message.ToLowerInvariant(), Does.Contain("failed to open wallet").Or.Contain("invalid password"));

        // open the wallet with the new password
        wallet = await TestUtils.OpenWalletRpc(new MoneroWalletConfig { Path = path, Password = newPassword });
        try
        {
            // change the password with an incorrect password
            error = Assert.ThrowsAsync<MoneroError>(() => wallet.ChangePasswordAsync("badpassword", newPassword));
            Assert.That(error!.Message, Is.EqualTo("Invalid original password."));
        }
        finally
        {
            await CloseWallet(wallet, true);
        }

        // open the wallet again
        wallet = await TestUtils.OpenWalletRpc(new MoneroWalletConfig { Path = path, Password = newPassword });
        await CloseWallet(wallet);
    }

    // Can save and close a wallet in a call
    [Test, Category(NonRelays)]
    public async Task TestSaveAndClose()
    {
        // create a random wallet
        const string password = "";
        var wallet = await TestUtils.CreateWalletRpc(new MoneroWalletConfig { Password = password });
        var path = await wallet.GetPathAsync();

        // set an attribute
        var uuid = WalletUtils.GetRandomString();
        await wallet.SetAttributeAsync("id", uuid);

        // close the wallet without saving
        await CloseWallet(wallet);

        // open the wallet and check that the attribute was not saved
        var config = new MoneroWalletConfig { Path = path, Password = password };
        wallet = await TestUtils.OpenWalletRpc(config);
        Assert.That(await wallet.GetAttributeAsync("id"), Is.Null);

        // set the attribute and close the wallet with saving
        await wallet.SetAttributeAsync("id", uuid);
        await CloseWallet(wallet, true);

        // open the wallet and check that the attribute was saved
        wallet = await TestUtils.OpenWalletRpc(config);
        try
        {
            Assert.That(await wallet.GetAttributeAsync("id"), Is.EqualTo(uuid));
        }
        finally
        {
            await CloseWallet(wallet);
        }
    }

    // Can get the default priority of the fee
    [Test, Category(NonRelays)]
    public async Task TestGetDefaultFeePriority()
    {
        var wallet = await GetWallet();

        Assert.That(await wallet.GetDefaultFeePriorityAsync(), Is.Not.EqualTo(MoneroTxPriority.Default));
    }

    // Can tell if the import of multisig info is needed
    [Test, Category(NonRelays)]
    public async Task TestIsMultisigNeeded()
    {
        var wallet = await GetWallet();

        Assert.That(await wallet.IsMultisigAsync(), Is.False);
        Assert.That(await wallet.IsMultisigImportNeededAsync(), Is.False, "Expected a wallet that is not multisig");
    }

    // Can tag accounts and get the accounts of a tag
    [Test, Category(NonRelays)]
    public async Task TestAccountTags()
    {
        var wallet = await GetWallet();
        while ((await wallet.GetAccountsAsync()).Count < 4) await wallet.CreateAccountAsync();

        // tag some of the accounts
        var tag = new MoneroAccountTag($"my_tag_{WalletUtils.GetRandomString()}", "my tag label", [0, 1]);
        await wallet.TagAccountsAsync(tag.Tag!, tag.AccountIndices!);

        // get the accounts of the tag
        var tagged = await wallet.GetAccountsAsync(false, tag.Tag);
        Assert.That(tagged.Select(account => account.Index), Is.EqualTo(new uint?[] { 0, 1 }));
        Assert.That(tagged.All(account => account.Tag == tag.Tag), Is.True);

        // set the label of the tag
        await wallet.SetAccountTagLabelAsync(tag.Tag!, tag.Label!);

        // the new tag is in the tags
        var tags = await wallet.GetAccountTagsAsync();
        Assert.That(tags.Any(t => t.Tag == tag.Tag), Is.True, $"Could not find the tag {tag.Tag}");

        // tag an account again
        var tag2 = new MoneroAccountTag($"my_tag_{WalletUtils.GetRandomString()}", "my tag label 2", [1]);
        await wallet.TagAccountsAsync(tag2.Tag!, tag2.AccountIndices!);
        var tagged2 = await wallet.GetAccountsAsync(false, tag2.Tag);
        Assert.That(tagged2, Has.Count.EqualTo(1));
        Assert.That(tagged2[0].Index, Is.EqualTo(1u));
        Assert.That(tagged2[0].Tag, Is.EqualTo(tag2.Tag));

        // the first tag applies to one account now
        tagged = await wallet.GetAccountsAsync(false, tag.Tag);
        Assert.That(tagged, Has.Count.EqualTo(1));
        Assert.That(tagged[0].Index, Is.EqualTo(0u));
        Assert.That(tagged[0].Tag, Is.EqualTo(tag.Tag));

        // untag the accounts
        await wallet.UntagAccountsAsync([0, 1]);
        Assert.That(await wallet.GetAccountTagsAsync(), Is.Empty);
        Assert.ThrowsAsync<MoneroError>(() => wallet.GetAccountsAsync(false, tag.Tag));
    }

    // Can export the outputs
    [Test, Category(NonRelays)]
    public async Task TestExportOutputs()
    {
        var wallet = await GetWallet();

        var outputsHex = await wallet.ExportOutputsAsync(true);
        Assert.That(outputsHex, Is.Not.Empty);

        // the wallet exports the outputs since the last export by default
        var outputsHexSinceLastExport = await wallet.ExportOutputsAsync();
        Assert.That((await wallet.ExportOutputsAsync(true)).Length, Is.GreaterThanOrEqualTo(outputsHexSinceLastExport.Length));
    }

    // Can import outputs in hex format
    [Test, Category(NonRelays)]
    public async Task TestImportOutputs()
    {
        var wallet = await GetWallet();

        // export outputs hex
        var outputsHex = await wallet.ExportOutputsAsync();

        // import outputs hex
        if (outputsHex.Length > 0)
        {
            var numImported = await wallet.ImportOutputsAsync(outputsHex);
            Assert.That(numImported, Is.GreaterThanOrEqualTo(0));
        }
    }

    // Can export the key images
    [Test, Category(NonRelays)]
    public async Task TestExportKeyImages()
    {
        var wallet = await GetWallet();

        var result = await wallet.ExportKeyImagesAsync(true);

        Assert.That(result.KeyImages, Is.Not.Empty, "No signed key images in the wallet");
        foreach (var image in result.KeyImages!)
        {
            Assert.That(image.Hex, Is.Not.Null.And.Not.Empty);
            Assert.That(image.Signature, Is.Not.Null.And.Not.Empty);
        }
    }
}
