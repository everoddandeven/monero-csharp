using Monero.Common;
using Monero.Daemon;
using Monero.IntegrationTests.Utils;
using Monero.Wallet;
using Monero.Wallet.Common;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Monero.IntegrationTests;

[TraitDiscoverer("Monero.IntegrationTests.PriorityDiscoverer", "Monero.IntegrationTests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute, ITraitAttribute
{
    public TestPriorityAttribute(int priority) => Priority = priority;
    public int Priority { get; }
}

public class PriorityDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var priority = traitAttribute.GetConstructorArguments().First().ToString();
        yield return new KeyValuePair<string, string>("Priority", priority);
    }
}

public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc =>
        {
            var priority = tc.Traits.GetValueOrDefault("Priority")?.FirstOrDefault();
            return priority != null ? int.Parse(priority) : 3;
        });
    }
}

[TestCaseOrderer("Monero.IntegrationTests.PriorityOrderer", "Monero.IntegrationTests")]
public class TestMoneroWalletRpc
{
    // test constants
    private static readonly bool TestNonRelays = true;
    private static readonly ulong AmountRequiredAu = MoneroUtils.XmrToAtomicUnits(1);
    private static readonly bool Funded;
    private readonly MoneroDaemonRpc daemon; // daemon instance to test
    private static readonly ulong MinBlockchainHeight = 640;
    private static readonly ulong SendMaxDiff = 60;
    private static readonly ulong SendDivisor = 10;

    // instance variables
    private readonly MoneroWalletRpc wallet; // wallet instance to test

    public TestMoneroWalletRpc()
    {
        daemon = TestUtils.GetDaemonRpc();
        wallet = TestUtils.GetWalletRpcSync();
    }

    private MoneroWalletRpc CreateWalletSync(MoneroWalletConfig? config)
    {
        return (MoneroWalletRpc)CreateWallet(config).GetAwaiter().GetResult();
    }

    private async Task<List<MoneroTxWallet>> CreateTxs(IMoneroWallet wallet, MoneroTxConfig? config)
    {
        if (config == null)
        {
            throw new MoneroError("Musto provide valid tx config");
        }

        for (int i = 0; i < 5; i++)
        {
            try
            {
                return await wallet.CreateTxs(config);
            }
            catch (MoneroRpcError e)
            {
                if (e.Message.Contains("not enough money"))
                {
                    Thread.Sleep(5000);
                    continue;
                }

                throw;
            }
        }

        throw new MoneroError("Could not fund test wallet, please re-run tests");
    }

    [Fact, TestPriority(0)]
    public async Task TestFundWallet()
    {
        while (await daemon.GetHeight() < MinBlockchainHeight)
        {
            // wait for blockchain height
            Thread.Sleep(5000);
        }

        MoneroWalletRpc miningWallet = (MoneroWalletRpc)await CreateWallet(new MoneroWalletConfig().SetSeed(TestUtils.MINING_SEED));
        MoneroTxConfig txConfig = await GetFundTxConfig();
        List<MoneroTxWallet> txs = await CreateTxs(miningWallet, txConfig);
        await CloseWallet(miningWallet);
        // wait for confirmations
        Thread.Sleep(10000);
        await wallet.Sync();
        Assert.True(await wallet.GetBalance() > 0);
    }

    private async Task<MoneroTxConfig> GetFundTxConfig()
    {
        List<MoneroDestination> destinations = await this.GetFundDestinations();
        MoneroTxConfig config = new();

        config.SetDestinations(destinations);
        config.SetAccountIndex(0);
        config.SetCanSplit(true);

        return config;
    }

    private async Task<List<MoneroDestination>> GetFundDestinations()
    {
        List<MoneroDestination> dests = [];

        for (uint accountIndex = 0; accountIndex < 2; accountIndex++)
        {
            if (accountIndex > 0)
            {
                await wallet.CreateAccount();
            }

            for (uint subaddressIndex = 0; subaddressIndex < 50; subaddressIndex++)
            {
                MoneroSubaddress subaddress = await wallet.CreateSubaddress(accountIndex);

                MoneroDestination dest = new();
                dest.SetAddress(subaddress.GetAddress());
                dest.SetAmount(MoneroUtils.XmrToAtomicUnits(1));
                dests.Add(dest);
            }
        }

        return dests;
    }

    private async Task<IMoneroWallet> OpenWallet(MoneroWalletConfig? config)
    {
        MoneroWalletRpc ret;
        // assign defaults
        if (config == null)
        {
            config = new MoneroWalletConfig();
        }

        if (config.GetPassword() == null)
        {
            config.SetPassword(TestUtils.WALLET_PASSWORD);
        }

        if (config.GetServer() == null)
        {
            config.SetServer(daemon.GetRpcConnection());
        }

        // create a client connected to an internal monero-wallet-rpc process
        MoneroWalletRpc moneroWalletRpc = await TestUtils.GetCreateWallet();

        // open wallet
        try
        {
            await moneroWalletRpc.OpenWallet(config);
            await moneroWalletRpc.SetDaemonConnection(await moneroWalletRpc.GetDaemonConnection(), true,
                null); // set daemon as trusted
            if (await moneroWalletRpc.IsConnectedToDaemon())
            {
                await moneroWalletRpc.StartSyncing((ulong)TestUtils.SYNC_PERIOD_IN_MS);
            }

            ret = moneroWalletRpc;
        }
        catch (MoneroError)
        {
            try { TestUtils.StopWalletRpcProcess(moneroWalletRpc); }
            catch (Exception e2) { throw new Exception(e2.Message); }

            throw;
        }

        return ret;
    }

    private async Task<IMoneroWallet> CreateWallet(MoneroWalletConfig? config)
    {
        // assign defaults
        if (config == null)
        {
            config = new MoneroWalletConfig();
        }
        bool random = config.GetSeed() == null && config.GetPrimaryAddress() == null;

        if (config.GetPath() == null)
        {
            config.SetPath(GenUtils.GetGuid());
        }

        if (config.GetPassword() == null)
        {
            config.SetPassword(TestUtils.WALLET_PASSWORD);
        }

        if (config.GetRestoreHeight() == null && !random)
        {
            config.SetRestoreHeight(0);
        }

        if (config.GetServer() == null)
        {
            config.SetServer(daemon.GetRpcConnection());
        }

        // create client connected to xmr_wallet_2 container
        MoneroWalletRpc walletRpc = await TestUtils.GetCreateWallet();

        // create wallet
        try
        {
            await walletRpc.CreateWallet(config);
            await walletRpc.SetDaemonConnection(await walletRpc.GetDaemonConnection(), true, null); // set daemon as trusted
            if (await walletRpc.IsConnectedToDaemon())
            {
                await walletRpc.StartSyncing((ulong)TestUtils.SYNC_PERIOD_IN_MS);
            }
            return walletRpc;
        }
        catch (MoneroError e)
        {
            if (!e.Message.ToLower().Contains("already exists"))
            {
                try { await CloseWallet(walletRpc, false); } catch (Exception e2) { throw new Exception("An error occurred while stopping monero wallet rpc process", e2); }
            }
            throw;
        }
    }

    private async Task CloseWallet(IMoneroWallet walletInstance) { await CloseWallet(walletInstance, false); }

    private async Task CloseWallet(IMoneroWallet walletInstance, bool save)
    {
        MoneroWalletRpc walletRpc = (MoneroWalletRpc)walletInstance;
        string walletPath = await walletRpc.GetPath();

        if (string.IsNullOrEmpty(walletPath))
        {
            return;
        }

        await walletRpc.Close(save);
    }

    private Task<List<string>> GetSeedLanguages()
    {
        throw new NotImplementedException();
    }

    private async Task TestWallet(Func<Task> action, IMoneroWallet moneroWallet)
    {
        await TestWallet(action, moneroWallet, false);
    }

    private async Task TestWallet(Func<Task> action, IMoneroWallet moneroWallet, bool checkSeed)
    {
        Exception? e2 = null;
        try
        {
            await action();

            if (checkSeed)
            {
                MoneroUtils.ValidateMnemonic(await moneroWallet.GetSeed());
            }
        }
        catch (Exception e)
        {
            e2 = e;
        }

        await CloseWallet(moneroWallet);

        if (e2 != null)
        {
            throw e2;
        }
    }

    // Can create a random wallet
    [Fact, TestPriority(2)]
    public async Task TestCreateWalletRandom()
    {
        Assert.True(TestNonRelays);
        Exception? e1 = null; // emulating Java "finally" but compatible with other languages
        try
        {
            // create random wallet
            IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig());
            string path = await moneroWallet.GetPath();

            await TestWallet(async () =>
            {
                MoneroUtils.ValidateAddress(await moneroWallet.GetPrimaryAddress());
                MoneroUtils.ValidatePrivateViewKey(await moneroWallet.GetPrivateViewKey());
                MoneroUtils.ValidatePrivateSpendKey(await moneroWallet.GetPrivateSpendKey());
                MoneroUtils.ValidateMnemonic(await moneroWallet.GetSeed());
            }, moneroWallet);

            // attempt to create wallet at same path
            try
            {
                await CreateWallet(new MoneroWalletConfig().SetPath(path));
                throw new Exception("Should have thrown error");
            }
            catch (Exception e)
            {
                Assert.True("Wallet already exists: " + path == e.Message);
            }

            // attempt to create wallet with unknown language
            try
            {
                await CreateWallet(new MoneroWalletConfig().SetLanguage("english")); // TODO: support lowercase?
                throw new Exception("Should have thrown error");
            }
            catch (Exception e)
            {
                Assert.True("Unknown language: english" == e.Message);
            }
        }
        catch (Exception e)
        {
            e1 = e;
        }

        if (e1 != null)
        {
            throw new Exception(e1.Message);
        }
    }

    // Can create a wallet from a seed.
    [Fact, TestPriority(2)]
    public async Task TestCreateWalletFromSeed()
    {
        Assert.True(TestNonRelays);
        Exception? e1 = null; // emulating Java "finally" but compatible with other languages
        try
        {
            // save for comparison
            string primaryAddress = await wallet.GetPrimaryAddress();
            string privateViewKey = await wallet.GetPrivateViewKey();
            string privateSpendKey = await wallet.GetPrivateSpendKey();

            // recreate the test wallet from seed
            IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig().SetSeed(TestUtils.SEED)
                .SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
            string path = await moneroWallet.GetPath();

            await TestWallet(async () =>
            {
                Assert.True(primaryAddress == await moneroWallet.GetPrimaryAddress());
                Assert.True(privateViewKey == await moneroWallet.GetPrivateViewKey());
                Assert.True(privateSpendKey == await moneroWallet.GetPrivateSpendKey());
                Assert.True(TestUtils.SEED == await moneroWallet.GetSeed());
            }, moneroWallet);

            // attempt to create a wallet with two missing words
            try
            {
                string invalidMnemonic =
                    "memoir desk algebra inbound innocent unplugs fully okay five inflamed giant factual ritual toyed topic snake unhappy guarded tweezers haunted inundate giant";
                await CreateWallet(new MoneroWalletConfig().SetSeed(invalidMnemonic)
                    .SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
            }
            catch (Exception e)
            {
                Assert.True("Invalid mnemonic" == e.Message);
            }

            // attempt to create a wallet at the same path
            try
            {
                await CreateWallet(new MoneroWalletConfig().SetPath(path));
                throw new Exception("Should have thrown error");
            }
            catch (Exception e)
            {
                Assert.True("Wallet already exists: " + path == e.Message);
            }
        }
        catch (Exception e)
        {
            e1 = e;
        }

        if (e1 != null)
        {
            throw new Exception(e1.Message);
        }
    }

    // Can create a wallet from a seed with a seed offset
    [Fact, TestPriority(2)]
    public async Task TestCreateWalletFromSeedWithOffset()
    {
        Assert.True(TestNonRelays);
        Exception? e1 = null; // emulating Java "finally" but compatible with other languages
        try
        {
            // create a test wallet with offset
            IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig().SetSeed(TestUtils.SEED)
                .SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT).SetSeedOffset("my secret offset!"));

            await TestWallet(async () =>
            {
                MoneroUtils.ValidateMnemonic(await moneroWallet.GetSeed());
                Assert.True(TestUtils.SEED != await moneroWallet.GetSeed());
                MoneroUtils.ValidateAddress(await moneroWallet.GetPrimaryAddress());
                Assert.True(TestUtils.ADDRESS != await moneroWallet.GetPrimaryAddress());
            }, moneroWallet);
        }
        catch (Exception e)
        {
            e1 = e;
        }

        if (e1 != null)
        {
            throw new Exception(e1.Message);
        }
    }

    // Can create a wallet from keys
    [Fact, TestPriority(2)]
    public async Task TestCreateWalletFromKeys()
    {
        Assert.True(TestNonRelays);
        Exception? e1 = null; // emulating Java "finally" but compatible with other languages
        try
        {
            // save for comparison
            string primaryAddress = await wallet.GetPrimaryAddress();
            string privateViewKey = await wallet.GetPrivateViewKey();
            string privateSpendKey = await wallet.GetPrivateSpendKey();

            // recreate the test wallet from keys
            IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig().SetPrimaryAddress(primaryAddress)
                .SetPrivateViewKey(privateViewKey).SetPrivateSpendKey(privateSpendKey)
                .SetRestoreHeight(await daemon.GetHeight()));
            string path = await moneroWallet.GetPath();

            Func<Task> action = async () =>
            {
                Assert.True(primaryAddress == await moneroWallet.GetPrimaryAddress());
                Assert.True(privateViewKey == await moneroWallet.GetPrivateViewKey());
                Assert.True(privateSpendKey == await moneroWallet.GetPrivateSpendKey());
                if (TestUtils.TESTS_INCONTAINER)
                {
                    Assert.True(await moneroWallet.IsConnectedToDaemon(),
                        "Wallet created from keys is not connected to authenticated daemon");
                }

            };

            await TestWallet(action, moneroWallet, false);

            // attempt to create wallet at same path
            try
            {
                await CreateWallet(new MoneroWalletConfig().SetPath(path));
                throw new Exception("Should have thrown error");
            }
            catch (Exception e)
            {
                Assert.True("Wallet already exists: " + path == e.Message);
            }
        }
        catch (Exception e)
        {
            e1 = e;
        }

        if (e1 != null)
        {
            throw new Exception(e1.Message);
        }
    }

    // Can create wallets with subaddress lookahead
    [Fact(Skip = "monero-wallet-rpc does not support creating wallets with subaddress lookahead over rpc"), TestPriority(2)]
    public async Task TestSubaddressLookahead()
    {
        Assert.True(TestNonRelays);
        Exception? e1 = null; // emulating Java "finally" but compatible with other languages
        IMoneroWallet? receiver = null;
        try
        {
            // create wallet with high subaddress lookahead
            receiver = await CreateWallet(
                new MoneroWalletConfig().SetAccountLookahead(1).SetSubaddressLookahead(100000));

            // transfer funds to subaddress with high index
            await wallet.CreateTx(new MoneroTxConfig()
                .SetAccountIndex(0)
                .AddDestination((await receiver.GetSubaddress(0, 85000)).GetAddress()!, TestUtils.MAX_FEE)
                .SetRelay(true));

            // observe unconfirmed funds
            Thread.Sleep(1000);
            await receiver.Sync();
            Assert.True((await receiver.GetBalance()).CompareTo(0) > 0);
        }
        catch (Exception e)
        {
            e1 = e;
        }

        if (receiver != null)
        {
            await CloseWallet(receiver);
        }

        if (e1 != null)
        {
            throw new Exception(e1.Message);
        }
    }

    // Can get the wallet's version
    [Fact, TestPriority(2)]
    public async Task TestGetVersion()
    {
        Assert.True(TestNonRelays);
        MoneroVersion version = await wallet.GetVersion();
        Assert.NotNull(version.GetNumber());
        Assert.True(version.GetNumber() > 0);
        Assert.NotNull(version.IsRelease());
    }

    // Can get the wallet's path
    [Fact, TestPriority(2)]
    public async Task TestGetPath()
    {
        Assert.True(TestNonRelays);

        // create a random wallet
        IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig());

        // set a random attribute
        string uuid = Guid.NewGuid().ToString();
        await moneroWallet.SetAttribute("uuid", uuid);

        // record the wallet's path, then save and close
        string path = await moneroWallet.GetPath();
        await CloseWallet(moneroWallet, true);

        // re-open the wallet using its path
        moneroWallet = await OpenWallet(new MoneroWalletConfig().SetPath(path));

        // test the attribute
        Assert.True(uuid == await moneroWallet.GetAttribute("uuid"));
        await CloseWallet(moneroWallet);
    }

    // Can set the daemon connection
    [Fact, TestPriority(2)]
    public async Task TestSetDaemonConnection()
    {
        if (!TestUtils.TESTS_INCONTAINER)
        {
            MoneroUtils.Log(0, "Skipping TestSetDaemonConnection due docker address mismatch");
            return;
        }
        // create random wallet with default daemon connection
        IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig());
        Assert.True(
            new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME,
                TestUtils.DAEMON_RPC_PASSWORD).Equals(await moneroWallet.GetDaemonConnection()));
        Assert.True(await moneroWallet.IsConnectedToDaemon()); // uses default localhost connection

        // set empty server uri
        await moneroWallet.SetDaemonConnection("");
        Assert.Null(await moneroWallet.GetDaemonConnection());
        Assert.False(await moneroWallet.IsConnectedToDaemon());

        // set offline server uri
        await moneroWallet.SetDaemonConnection(TestUtils.OFFLINE_SERVER_URI);
        Assert.True(
            new MoneroRpcConnection(TestUtils.OFFLINE_SERVER_URI, "", "").Equals(
                await moneroWallet.GetDaemonConnection()));
        Assert.False(await moneroWallet.IsConnectedToDaemon());

        // set daemon with wrong credentials
        await moneroWallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI, "wronguser", "wrongpass");
        Assert.True(
            new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, "wronguser", "wrongpass").Equals(
                await moneroWallet.GetDaemonConnection()));
        if (string.IsNullOrEmpty(TestUtils.DAEMON_RPC_USERNAME))
        {
            Assert.True(await moneroWallet
                .IsConnectedToDaemon()); // TODO: monerod without authentication works with bad credentials?
        }
        else
        {
            Assert.False(await moneroWallet.IsConnectedToDaemon());
        }

        // set daemon with authentication
        await moneroWallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME,
            TestUtils.DAEMON_RPC_PASSWORD);
        Assert.True(
            new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME,
                TestUtils.DAEMON_RPC_PASSWORD).Equals(await moneroWallet.GetDaemonConnection()));
        Assert.True(await moneroWallet.IsConnectedToDaemon());

        // nullify daemon connection
        await moneroWallet.SetDaemonConnection((MoneroRpcConnection?)null);
        Assert.Null(await moneroWallet.GetDaemonConnection());
        await moneroWallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI);
        Assert.True(new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI).Equals(await moneroWallet.GetDaemonConnection()));
        await moneroWallet.SetDaemonConnection((MoneroRpcConnection?)null);
        Assert.Null(await moneroWallet.GetDaemonConnection());

        // set daemon uri to non-daemon
        await moneroWallet.SetDaemonConnection("www.Getmonero.org");
        Assert.True(new MoneroRpcConnection("www.Getmonero.org").Equals(await moneroWallet.GetDaemonConnection()));
        Assert.False(await moneroWallet.IsConnectedToDaemon());

        // set daemon to invalid uri
        await moneroWallet.SetDaemonConnection("abc123");
        Assert.False(await moneroWallet.IsConnectedToDaemon());

        // attempt to sync
        try
        {
            await moneroWallet.Sync();
            throw new Exception("Exception expected");
        }
        catch (MoneroError e)
        {
            Assert.Equal("Wallet is not connected to daemon", e.Message);
        }
        finally
        {
            await CloseWallet(moneroWallet);
        }
    }

    // Can get the seed
    [Fact, TestPriority(2)]
    public async Task TestGetSeed()
    {
        Assert.True(TestNonRelays);
        string seed = await wallet.GetSeed();
        MoneroUtils.ValidateMnemonic(seed);
        Assert.True(TestUtils.SEED == seed);
    }

    // Can get the language of the seed
    [Fact(Skip = "monero-wallet-rpc does not support getting seed language")]
    public async Task TestGetSeedLanguage()
    {
        Assert.True(TestNonRelays);
        string language = await wallet.GetSeedLanguage();
        Assert.True(IMoneroWallet.DefaultLanguage == language);
    }

    // Can get a list of supported languages for the seed
    [Fact(Skip = "monero-wallet-rpc does not support getting seed languages")]
    public async Task TestGetSeedLanguages()
    {
        Assert.True(TestNonRelays);
        List<string> languages = await GetSeedLanguages();
        Assert.True(languages.Count > 0);
        foreach (string language in languages)
        {
            Assert.True(language.Length > 0);
        }
    }

    // Can get the private view key
    [Fact, TestPriority(2)]
    public async Task TestGetPrivateViewKey()
    {
        Assert.True(TestNonRelays);
        string privateViewKey = await wallet.GetPrivateViewKey();
        MoneroUtils.ValidatePrivateViewKey(privateViewKey);
    }

    // Can get the private spend key
    [Fact, TestPriority(2)]
    public async Task TestGetPrivateSpendKey()
    {
        Assert.True(TestNonRelays);
        string privateSpendKey = await wallet.GetPrivateSpendKey();
        MoneroUtils.ValidatePrivateSpendKey(privateSpendKey);
    }

    // Can get the public view key
    [Fact(Skip = "Enable after monero-project fix (https://github.com/monero-project/monero/pull/9364)"), TestPriority(2)]
    public async Task TestGetPublicViewKey()
    {
        Assert.True(TestNonRelays);
        string publicViewKey = await wallet.GetPublicViewKey();
        MoneroUtils.ValidatePrivateSpendKey(publicViewKey);
    }

    // Can get the public view key
    [Fact(Skip = "Enable after monero-project fix (https://github.com/monero-project/monero/pull/9364)"), TestPriority(2)]
    public async Task TestGetPublicSpendKey()
    {
        Assert.True(TestNonRelays);
        string publicSpendKey = await wallet.GetPublicSpendKey();
        MoneroUtils.ValidatePrivateSpendKey(publicSpendKey);
    }

    // Can get the primary address
    [Fact, TestPriority(2)]
    public async Task TestGetPrimaryAddress()
    {
        Assert.True(TestNonRelays);
        string primaryAddress = await wallet.GetPrimaryAddress();
        MoneroUtils.ValidateAddress(primaryAddress);
        Assert.True(await wallet.GetAddress(0, 0) == primaryAddress);
    }

    // Can get the address of a subaddress at a specified account and subaddress index
    [Fact, TestPriority(2)]
    public async Task TestGetSubaddressAddress()
    {
        Assert.True(TestNonRelays);
        Assert.True(await wallet.GetPrimaryAddress() == (await wallet.GetSubaddress(0, 0)).GetAddress());
        foreach (MoneroAccount account in await wallet.GetAccounts(true))
        {
            foreach (MoneroSubaddress subaddress in account.GetSubaddresses()!)
            {
                Assert.True(subaddress.GetAddress() ==
                            await wallet.GetAddress((uint)account.GetIndex()!, (uint)subaddress.GetIndex()!));
            }
        }
    }

    // Can get addresses out of range of used accounts and subaddresses
    [Fact, TestPriority(2)]
    public async Task TestGetSubaddressAddressOutOfRange()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts(true);
        int accountIdx = accounts.Count - 1;
        MoneroAccount account = accounts[accountIdx];
        List<MoneroSubaddress>? subaddresses = account.GetSubaddresses();

        if (subaddresses == null)
        {
            throw new MoneroError("Subaddresses is null");
        }

        int subaddressIdx = subaddresses.Count;
        string? address = await wallet.GetAddress((uint)accountIdx, (uint)subaddressIdx);
        Assert.Null(address);
    }


    // Can get the current height that the wallet is synchronized to
    [Fact, TestPriority(2)]
    public async Task TestGetHeight()
    {
        Assert.True(TestNonRelays);
        ulong lastHeight = await wallet.GetHeight();

        await daemon.WaitForNextBlockHeader();

        ulong currentHeight = await wallet.GetHeight();

        Assert.True(currentHeight > lastHeight);
    }

    // Can create a new account without a label
    [Fact, TestPriority(2)]
    public async Task TestCreateAccountWithoutLabel()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accountsBefore = await wallet.GetAccounts();
        MoneroAccount createdAccount = await wallet.CreateAccount();
        TestAccount(createdAccount);
        Assert.Equal(accountsBefore.Count, (await wallet.GetAccounts()).Count - 1);
    }

    // Can create a new account with a label
    [Fact, TestPriority(2)]
    public async Task TestCreateAccountWithLabel()
    {
        Assert.True(TestNonRelays);

        // create account with label
        List<MoneroAccount> accountsBefore = await wallet.GetAccounts();
        string label = Guid.NewGuid().ToString();
        MoneroAccount createdAccount = await wallet.CreateAccount(label);
        TestAccount(createdAccount);
        Assert.Equal(accountsBefore.Count, (await wallet.GetAccounts()).Count - 1);

        uint? createdAccountIndex = createdAccount.GetIndex();

        if (createdAccountIndex == null)
        {
            throw new MoneroError("Created account index is null");
        }

        Assert.Equal(label, (await wallet.GetSubaddress((uint)createdAccountIndex, 0)).GetLabel());

        // fetch and test account
        createdAccount = await wallet.GetAccount((uint)createdAccountIndex);
        TestAccount(createdAccount);

        // create account with same label
        createdAccount = await wallet.CreateAccount(label);
        TestAccount(createdAccount);
        createdAccountIndex = createdAccount.GetIndex();

        if (createdAccountIndex == null)
        {
            throw new MoneroError("Created account index is null");
        }
        Assert.Equal(accountsBefore.Count, (await wallet.GetAccounts()).Count - 2);
        Assert.Equal(label, (await wallet.GetSubaddress((uint)createdAccountIndex, 0)).GetLabel());

        // fetch and test account
        createdAccount = await wallet.GetAccount((uint)createdAccountIndex);
        TestAccount(createdAccount);
    }

    // Can get accounts without subaddresses
    [Fact, TestPriority(2)]
    public async Task TestGetAccountsWithoutSubaddresses()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        Assert.False(accounts.Count == 0);
        foreach (MoneroAccount account in accounts)
        {
            TestAccount(account);
            Assert.Null(account.GetSubaddresses());
        }
    }

    // Can get accounts with subaddresses
    [Fact, TestPriority(2)]
    public async Task TestGetAccountsWithSubaddresses()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts(true);
        Assert.False(accounts.Count == 0);
        foreach (MoneroAccount account in accounts)
        {
            TestAccount(account);
            List<MoneroSubaddress> subaddresses = account.GetSubaddresses() ?? [];
            Assert.False(subaddresses.Count == 0);
        }
    }

    // Can get an account at a specified index
    [Fact, TestPriority(2)]
    public async Task TestGetAccount()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        Assert.False(accounts.Count == 0);
        foreach (MoneroAccount account in accounts)
        {
            TestAccount(account);

            uint? accountIdx = account.GetIndex();

            if (accountIdx == null)
            {
                throw new Exception("Account index is null");
            }

            // test without subaddresses
            MoneroAccount retrieved = await wallet.GetAccount((uint)accountIdx);
            Assert.Null(retrieved.GetSubaddresses());

            // test with subaddresses
            retrieved = await wallet.GetAccount((uint)accountIdx, true);
            List<MoneroSubaddress>? subaddresses = retrieved.GetSubaddresses();
            Assert.NotNull(subaddresses);
            Assert.False(subaddresses.Count == 0);
        }
    }

    // Can set account labels
    [Fact, TestPriority(2)]
    public async Task TestSetAccountLabel()
    {
        // create account
        if ((await wallet.GetAccounts()).Count < 2)
        {
            await wallet.CreateAccount();
        }

        // set account label
        string label = GenUtils.GetGuid();
        await wallet.SetAccountLabel(1, label);
        Assert.Equal(label, (await wallet.GetSubaddress(1, 0)).GetLabel());
    }

    // Can create a subaddress with and without a label
    [Fact, TestPriority(2)]
    public async Task TestCreateSubaddress()
    {
        Assert.True(TestNonRelays);

        // create subaddresses across accounts
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        if (accounts.Count < 2)
        {
            await wallet.CreateAccount();
        }
        accounts = await wallet.GetAccounts();
        Assert.True(accounts.Count > 1);
        for (uint accountIdx = 0; accountIdx < 2; accountIdx++)
        {
            // create subaddress with no label
            List<MoneroSubaddress> subaddresses = await wallet.GetSubaddresses(accountIdx);
            MoneroSubaddress subaddress = await wallet.CreateSubaddress(accountIdx);
            Assert.Null(subaddress.GetLabel());
            TestSubaddress(subaddress);
            List<MoneroSubaddress> subaddressesNew = await wallet.GetSubaddresses(accountIdx);
            Assert.Equal(subaddressesNew.Count - 1, subaddresses.Count);
            Assert.True(subaddress.Equals(subaddressesNew[subaddressesNew.Count - 1]));

            // create subaddress with label
            subaddresses = await wallet.GetSubaddresses(accountIdx);
            string uuid = GenUtils.GetGuid();
            subaddress = await wallet.CreateSubaddress(accountIdx, uuid);
            Assert.Equal(uuid, subaddress.GetLabel());
            TestSubaddress(subaddress);
            subaddressesNew = await wallet.GetSubaddresses(accountIdx);
            Assert.Equal(subaddresses.Count, subaddressesNew.Count - 1);
            Assert.True(subaddress.Equals(subaddressesNew[subaddressesNew.Count - 1]));
        }
    }

    // Can get subaddresses at a specified account index
    [Fact, TestPriority(2)]
    public async Task TestGetSubaddresses()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        Assert.False(accounts.Count == 0);
        foreach (MoneroAccount account in accounts)
        {
            uint? accountIndex = account.GetIndex();

            if (accountIndex == null)
            {
                throw new MoneroError("Account index is null");
            }

            List<MoneroSubaddress> subaddresses = await wallet.GetSubaddresses((uint)accountIndex);
            Assert.False(subaddresses.Count == 0);
            foreach (MoneroSubaddress subaddress in subaddresses)
            {
                TestSubaddress(subaddress);
                Assert.Equal(account.GetIndex(), subaddress.GetAccountIndex());
            }
        }
    }

    // Can get subaddresses at specified account and subaddress indices
    [Fact, TestPriority(2)]
    public async Task TestGetSubaddressesByIndices()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        Assert.False(accounts.Count == 0);
        foreach (MoneroAccount account in accounts)
        {
            uint? accountIndex = account.GetIndex();
            if (accountIndex == null)
            {
                throw new MoneroError("Account index is null");
            }
            // get subaddresses
            List<MoneroSubaddress> subaddresses = await wallet.GetSubaddresses((uint)accountIndex);
            Assert.True(subaddresses.Count > 0);
            // remove a subaddress for query if possible
            if (subaddresses.Count > 1)
            {
                subaddresses.RemoveAt(0);
            }
            // get subaddress indices
            List<uint> subaddressIndices = new();
            foreach (MoneroSubaddress subaddress in subaddresses)
            {
                uint? subaddressIndex = subaddress.GetIndex();

                if (subaddressIndex == null)
                {
                    throw new MoneroError("Subaddress index is null");
                }

                subaddressIndices.Add((uint)subaddressIndex);
            }

            Assert.True(subaddressIndices.Count > 0);
            // fetch subaddresses by indices
            List<MoneroSubaddress> fetchedSubaddresses = await wallet.GetSubaddresses((uint)accountIndex, subaddressIndices);

            // original subaddresses (minus one removed if applicable) is equal to fetched subaddresses
            int i = 0;

            foreach (MoneroSubaddress subaddr in subaddresses)
            {
                Assert.True(subaddr.Equals(fetchedSubaddresses[i]));
                i++;
            }
        }
    }

    // Can get a subaddress at a specified account and subaddress index
    [Fact, TestPriority(2)]
    public async Task TestGetSubaddressByIndex()
    {
        Assert.True(TestNonRelays);
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        Assert.True(accounts.Count > 0);
        foreach (MoneroAccount account in accounts)
        {
            uint? accountIdx = account.GetIndex();

            if (accountIdx == null)
            {
                throw new MoneroError("Account index is null");
            }

            List<MoneroSubaddress> subaddresses = await wallet.GetSubaddresses((uint)accountIdx);
            Assert.True(subaddresses.Count > 0);

            foreach (MoneroSubaddress subaddress in subaddresses)
            {
                TestSubaddress(subaddress);
                uint? subaddressIdx = subaddress.GetIndex();
                if (subaddressIdx == null) { throw new MoneroError("Subaddress index is null"); }
                Assert.True(subaddress.Equals(await wallet.GetSubaddress((uint)accountIdx, (uint)subaddressIdx)));
                Assert.True(subaddress.Equals((await wallet.GetSubaddresses((uint)accountIdx, [(uint)subaddressIdx])).First())); // test plural call with single subaddr number
            }
        }
    }

    // Can set subaddress labels
    [Fact, TestPriority(2)]
    public async Task TestSetSubaddressLabel()
    {

        // create subaddresses
        while ((await wallet.GetSubaddresses(0)).Count < 3)
        {
            await wallet.CreateSubaddress(0);
        }

        uint subaddressesCount = (uint)(await wallet.GetSubaddresses(0)).Count;
        // set subaddress labels
        for (uint subaddressIdx = 0; subaddressIdx < subaddressesCount; subaddressIdx++)
        {
            string label = GenUtils.GetGuid();
            await wallet.SetSubaddressLabel(0, subaddressIdx, label);
            Assert.Equal(label, (await wallet.GetSubaddress(0, subaddressIdx)).GetLabel());
        }
    }

    // Can sync (without progress)
    [Fact, TestPriority(2)]
    public async Task TestSyncWithoutProgress()
    {
        Assert.True(TestNonRelays);
        ulong numBlocks = 100;
        ulong chainHeight = await daemon.GetHeight();
        Assert.True(chainHeight >= numBlocks);
        MoneroSyncResult result = await wallet.Sync(chainHeight - numBlocks);  // sync end of chain
        Assert.True(result.GetNumBlocksFetched() >= 0);
        Assert.NotNull(result.GetReceivedMoney());
    }

    // Can get the locked and unlocked balances of the wallet, accounts, and subaddresses
    [Fact, TestPriority(2)]
    public async Task TestGetAllBalances()
    {
        Assert.True(TestNonRelays);

        // fetch accounts with all info as reference
        List<MoneroAccount> accounts = await wallet.GetAccounts(true);

        // test that balances add up between accounts and wallet
        ulong accountsBalance = 0;
        ulong accountsUnlockedBalance = 0;
        foreach (MoneroAccount account in accounts)
        {
            accountsBalance += account.GetBalance();
            accountsUnlockedBalance += account.GetUnlockedBalance();

            // test that balances add up between subaddresses and accounts
            ulong subaddressesBalance = 0;
            ulong subaddressesUnlockedBalance = 0;
            foreach (MoneroSubaddress subaddress in account.GetSubaddresses()!)
            {
                subaddressesBalance += subaddress.GetBalance() ?? 0;
                subaddressesUnlockedBalance += subaddress.GetUnlockedBalance() ?? 0;

                // test that balances are consistent with getAccounts() call
                Assert.Equal((await wallet.GetBalance(subaddress.GetAccountIndex(), subaddress.GetIndex())).ToString(), subaddress.GetBalance().ToString());
                Assert.Equal((await wallet.GetUnlockedBalance(subaddress.GetAccountIndex(), subaddress.GetIndex())).ToString(), subaddress.GetUnlockedBalance().ToString());
            }

            Assert.Equal((await wallet.GetBalance(account.GetIndex())).ToString(), subaddressesBalance.ToString());
            Assert.Equal((await wallet.GetUnlockedBalance(account.GetIndex())).ToString(), subaddressesUnlockedBalance.ToString());
        }

        TestUtils.TestUnsignedBigInteger(accountsBalance);
        TestUtils.TestUnsignedBigInteger(accountsUnlockedBalance);
        Assert.Equal((await wallet.GetBalance()).ToString(), accountsBalance.ToString());
        Assert.Equal((await wallet.GetUnlockedBalance()).ToString(), accountsUnlockedBalance.ToString());
    }

    // Can save and close the wallet in a single call
    [Fact, TestPriority(2)]
    public async Task TestSaveAndClose()
    {
        Assert.True(TestNonRelays);

        // create random wallet
        string password = "";
        IMoneroWallet moneroWallet = await CreateWallet(new MoneroWalletConfig().SetPassword(password));
        string path = await moneroWallet.GetPath();

        // set an attribute
        string uuid = GenUtils.GetGuid();
        await moneroWallet.SetAttribute("id", uuid);

        // close the wallet without saving
        await CloseWallet(moneroWallet);

        // re-open the wallet and ensure attribute was not saved
        moneroWallet = await OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(password));
        Assert.Null(await moneroWallet.GetAttribute("id"));

        // set the attribute and close with saving
        await moneroWallet.SetAttribute("id", uuid);
        await CloseWallet(moneroWallet, true);

        // re-open the wallet and ensure attribute was saved
        moneroWallet = await OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(password));
        Assert.Equal(uuid, await moneroWallet.GetAttribute("id"));
        await CloseWallet(moneroWallet);
    }

    // Can get transfers in the wallet, accounts, and subaddresses
    [Fact, TestPriority(2)]
    public virtual async Task TestGetTransfers()
    {
        // get all transfers
        await GetAndTestTransfers(wallet, null, null, true);

        // get transfers by account index
        bool nonDefaultIncoming = false;
        foreach (MoneroAccount account in await wallet.GetAccounts(true))
        {
            List<MoneroTransfer> accountTransfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(account.GetIndex()), null, null);
            foreach (MoneroTransfer transfer in accountTransfers)
            {
                Assert.Equal(transfer.GetAccountIndex(), account.GetIndex());
            }

            // get transfers by subaddress index
            List<MoneroTransfer> subaddressTransfers = new List<MoneroTransfer>();
            foreach (MoneroSubaddress subaddress in account.GetSubaddresses()!)
            {
                List<MoneroTransfer> _transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(subaddress.GetAccountIndex()).SetSubaddressIndex(subaddress.GetIndex()), null, null);
                foreach (MoneroTransfer transfer in _transfers)
                {

                    // test account and subaddress indices
                    Assert.Equal(subaddress.GetAccountIndex(), transfer.GetAccountIndex());
                    if (transfer.IsIncoming() == true)
                    {
                        MoneroIncomingTransfer inTransfer = (MoneroIncomingTransfer)transfer;
                        Assert.Equal(subaddress.GetIndex(), inTransfer.GetSubaddressIndex());
                        if (transfer.GetAccountIndex() != 0 && inTransfer.GetSubaddressIndex() != 0)
                        {
                            nonDefaultIncoming = true;
                        }
                    }
                    else
                    {
                        MoneroOutgoingTransfer outTransfer = (MoneroOutgoingTransfer)transfer;
                        Assert.Contains((uint)subaddress.GetIndex()!, outTransfer.GetSubaddressIndices()!);
                        if (transfer.GetAccountIndex() != 0)
                        {
                            foreach (int subaddrIdx in outTransfer.GetSubaddressIndices()!)
                            {
                                if (subaddrIdx > 0)
                                {
                                    nonDefaultIncoming = true;
                                    break;
                                }
                            }
                        }
                    }

                    // don't add duplicates TODO monero-wallet-rpc: duplicate outgoing transfers returned foreach different subaddress indices, way to return outgoing subaddress indices?
                    bool found = false;
                    foreach (MoneroTransfer subaddressTransfer in subaddressTransfers)
                    {
                        if (transfer.ToString().Equals(subaddressTransfer.ToString()) && transfer.GetTx()!.GetHash()!.Equals(subaddressTransfer.GetTx()!.GetHash()))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        subaddressTransfers.Add(transfer);
                    }
                }
            }
            Assert.Equal(accountTransfers.Count, subaddressTransfers.Count);

            // collect unique subaddress indices
            HashSet<uint> subaddressIndices = new HashSet<uint>();
            foreach (MoneroTransfer transfer in subaddressTransfers)
            {
                if (transfer.IsIncoming() == true)
                {
                    subaddressIndices.Add((uint)((MoneroIncomingTransfer)transfer).GetSubaddressIndex()!);
                }
                else
                {
                    foreach (var subIdx in ((MoneroOutgoingTransfer)transfer).GetSubaddressIndices()!)
                    {
                        subaddressIndices.Add(subIdx);
                    }
                }
            }

            // get and test transfers by subaddress indices
            List<MoneroTransfer> transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(account.GetIndex()).SetSubaddressIndices(new List<uint>(subaddressIndices)), null, null);
            Assert.Equal(subaddressTransfers.Count, transfers.Count); // TODO monero-wallet-rpc: these may not be equal because outgoing transfers are always from subaddress 0 (#5171) and/or incoming transfers from/to same account are occluded (#4500)
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(transfer.GetAccountIndex(), account.GetIndex());
                if (transfer.IsIncoming() == true)
                {
                    Assert.Contains((uint)((MoneroIncomingTransfer)transfer).GetSubaddressIndex()!, subaddressIndices);
                }
                else
                {
                    HashSet<uint> intersections = new HashSet<uint>(subaddressIndices);
                    intersections.IntersectWith(((MoneroOutgoingTransfer)transfer).GetSubaddressIndices()!);
                    Assert.True(intersections.Count > 0, "Subaddresses must overlap");
                }
            }
        }

        // ensure transfer found with non-zero account and subaddress indices
        Assert.True(nonDefaultIncoming, "No transfers found in non-default account and subaddress; run send-to-multiple tests");
    }

    // Can get transfers with additional configuration
    [Fact, TestPriority(2)]
    public virtual async Task TestGetTransfersWithQuery()
    {
        // get incoming transfers
        List<MoneroTransfer> transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetIsIncoming(true), null, true);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.True(transfer.IsIncoming());
        }

        // get outgoing transfers
        transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetIsOutgoing(true), null, true);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.True(transfer.IsOutgoing());
        }

        // get confirmed transfers to account 0
        transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(0).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)), null, true);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.Equal(0, (int)transfer.GetAccountIndex()!);
            Assert.True(transfer.GetTx()!.IsConfirmed());
        }

        // get confirmed transfers to [1, 2]
        transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(1).SetSubaddressIndex(2).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)), null, true);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.Equal(1, (int)transfer.GetAccountIndex()!);
            if (transfer.IsIncoming() == true)
            {
                Assert.Equal(2, (int)((MoneroIncomingTransfer)transfer).GetSubaddressIndex()!);
            }
            else
            {
                Assert.Contains((uint)2, ((MoneroOutgoingTransfer)transfer).GetSubaddressIndices()!);
            }
            Assert.True(transfer.GetTx()!.IsConfirmed());
        }

        // get transfers in the _tx pool
        transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetInTxPool(true)), null, null);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.Equal(true, transfer.GetTx()!.InTxPool());
        }

        // get random transactions
        List<MoneroTxWallet> txs = await GetRandomTransactions(wallet, null, 3, 5);

        // get transfers with a _tx hash
        List<string> txHashes = new List<string>();
        foreach (MoneroTxWallet tx in txs)
        {
            txHashes.Add(tx.GetHash()!);
            transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHash(tx.GetHash())), null, true);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(tx.GetHash(), transfer.GetTx()!.GetHash());
            }
        }

        // get transfers with _tx hashes
        transfers = await GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHashes(txHashes)), null, true);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.Contains(transfer.GetTx()!.GetHash()!, txHashes);
        }

        // TODO: test that transfers with the same txHash have the same _tx reference

        // TODO: test transfers destinations

        // get transfers with pre-built query that are confirmed and have outgoing destinations
        MoneroTransferQuery transferQuery = new MoneroTransferQuery();
        transferQuery.SetIsOutgoing(true);
        transferQuery.SetHasDestinations(true);
        transferQuery.SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true));
        transfers = await GetAndTestTransfers(wallet, transferQuery, null, null);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.Equal(true, transfer.IsOutgoing());
            Assert.True(((MoneroOutgoingTransfer)transfer).GetDestinations()!.Count > 0);
            Assert.Equal(true, transfer.GetTx()!.IsConfirmed());
        }

        // get incoming transfers to account 0 which has outgoing transfers (i.e. originated from the same wallet)
        transfers = await wallet.GetTransfers(new MoneroTransferQuery().SetAccountIndex(1).SetIsIncoming(true).SetTxQuery(new MoneroTxQuery().SetIsOutgoing(true)));
        Assert.False(transfers.Count == 0);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.True(transfer.IsIncoming());
            Assert.Equal(1, (int)transfer.GetAccountIndex()!);
            Assert.True(transfer.GetTx()!.IsOutgoing());
            Assert.Null(transfer.GetTx()!.GetOutgoingTransfer());
        }

        // get incoming transfers to a specific address
        string? subaddress = await wallet.GetAddress(1, 0);
        transfers = await wallet.GetTransfers(new MoneroTransferQuery().SetIsIncoming(true).SetAddress(subaddress));
        Assert.True(transfers.Count > 0);
        foreach (MoneroTransfer transfer in transfers)
        {
            Assert.True(transfer is MoneroIncomingTransfer);
            Assert.True(1 == (uint)transfer.GetAccountIndex()!);
            Assert.True(0 == (uint)((MoneroIncomingTransfer)transfer).GetSubaddressIndex()!);
            Assert.Equal(subaddress, ((MoneroIncomingTransfer)transfer).GetAddress());
        }
    }

    // Validates inputs when getting transfers
    [Fact, TestPriority(2)]
    public async Task TestValidateInputsGetTransfers()
    {
        // test with invalid hash
        List<MoneroTransfer> transfers = await wallet.GetTransfers(new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHash("invalid_id")));
        Assert.Empty(transfers);

        // test invalid hash in collection
        List<MoneroTxWallet> randomTxs = await GetRandomTransactions(wallet, null, 3, 5);
        transfers = await wallet.GetTransfers(new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHashes([randomTxs[0].GetHash()!, "invalid_id"])));
        Assert.True(transfers.Count > 0);
        MoneroTxWallet? tx = transfers[0].GetTx();
        foreach (MoneroTransfer transfer in transfers) Assert.True(tx == transfer.GetTx());

        // test unused subaddress indices
        transfers = await wallet.GetTransfers(new MoneroTransferQuery().SetAccountIndex(0).SetSubaddressIndices([1234907]));
        Assert.True(transfers.Count == 0);
    }

    // Can get incoming and outgoing transfers using convenience methods
    [Fact, TestPriority(2)]
    public async Task TestGetIncomingOutgoingTransfers()
    {
        // get incoming transfers
        List<MoneroIncomingTransfer> inTransfers = await wallet.GetIncomingTransfers();
        Assert.False(inTransfers.Count == 0);
        foreach (MoneroIncomingTransfer transfer in inTransfers)
        {
            Assert.True(transfer.IsIncoming());
            TestTransfer(transfer, null);
        }

        // get incoming transfers with query
        ulong? amount = inTransfers[0].GetAmount();
        uint? accountIdx = inTransfers[0].GetAccountIndex();
        uint? subaddressIdx = inTransfers[0].GetSubaddressIndex();
        inTransfers = await wallet.GetIncomingTransfers(new MoneroTransferQuery().SetAmount(amount).SetAccountIndex(accountIdx).SetSubaddressIndex(subaddressIdx).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)));
        Assert.False(inTransfers.Count == 0);
        foreach (MoneroIncomingTransfer transfer in inTransfers)
        {
            Assert.True(transfer.IsIncoming());
            Assert.Equal(amount, transfer.GetAmount());
            Assert.Equal(accountIdx, (uint)transfer.GetAccountIndex()!);
            Assert.Equal(subaddressIdx, (uint)transfer.GetSubaddressIndex()!);
            TestTransfer(transfer, null);
        }

        // get incoming transfers with contradictory query
        try
        {
            inTransfers = await wallet.GetIncomingTransfers(new MoneroTransferQuery().SetIsIncoming(false));
        }
        catch (MoneroError e)
        {
            Assert.Equal("Transfer query contradicts getting incoming transfers", e.Message);
        }

        // get outgoing transfers
        List<MoneroOutgoingTransfer> outTransfers = await wallet.GetOutgoingTransfers();
        Assert.False(outTransfers.Count == 0);
        foreach (MoneroOutgoingTransfer transfer in outTransfers)
        {
            Assert.True(transfer.IsOutgoing());
            TestTransfer(transfer, null);
        }


        // get outgoing transfers with query
        accountIdx = outTransfers[0].GetAccountIndex();
        subaddressIdx = outTransfers[0].GetSubaddressIndices()![0];
        outTransfers = await wallet.GetOutgoingTransfers(new MoneroTransferQuery().SetAccountIndex(accountIdx).SetSubaddressIndex(subaddressIdx));
        Assert.False(outTransfers.Count == 0);
        foreach (MoneroOutgoingTransfer transfer in outTransfers)
        {
            Assert.True(transfer.IsOutgoing());
            Assert.Equal(accountIdx, (uint)transfer.GetAccountIndex()!);
            Assert.Contains((uint)subaddressIdx, transfer.GetSubaddressIndices());
            TestTransfer(transfer, null);
        }

        // get outgoing transfers with contradictory query
        try
        {
            outTransfers = await wallet.GetOutgoingTransfers(new MoneroTransferQuery().SetIsOutgoing(false));
        }
        catch (MoneroError e)
        {
            Assert.Equal("Transfer query contradicts getting outgoing transfers", e.Message);
        }
    }

    // Can send to multiple addresses in a single transaction
    [Fact, TestPriority(1)]
    public async Task TestSendToMultiple()
    {
        await SendToMultiple(5, 3, false);
    }

    // Can send to multiple addresses in split transactions
    [Fact, TestPriority(1)]
    public async Task TestSendToMultipleSplit()
    {
        await SendToMultiple(3, 15, true);
    }

    #region Test Utils

    private static void TestSubaddress(MoneroSubaddress? subaddress)
    {
        Assert.NotNull(subaddress);
        Assert.True(subaddress.GetAccountIndex() >= 0);
        Assert.True(subaddress.GetIndex() >= 0);
        Assert.NotNull(subaddress.GetAddress());
        string? label = subaddress.GetLabel();
        Assert.True(label == null || label.Length != 0);
        TestUtils.TestUnsignedBigInteger(subaddress.GetBalance());
        TestUtils.TestUnsignedBigInteger(subaddress.GetUnlockedBalance());
        Assert.True(subaddress.GetNumUnspentOutputs() >= 0);
        Assert.NotNull(subaddress.IsUsed());

        if (subaddress.GetBalance() > 0)
        {
            Assert.True(subaddress.IsUsed());
        }

        Assert.True(subaddress.GetNumBlocksToUnlock() >= 0);
    }

    private static void TestAccount(MoneroAccount? account)
    {
        // test account
        Assert.NotNull(account);
        uint? accountIndex = account.GetIndex();
        Assert.NotNull(accountIndex);
        MoneroUtils.ValidateAddress(account.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);
        TestUtils.TestUnsignedBigInteger(account.GetBalance());
        TestUtils.TestUnsignedBigInteger(account.GetUnlockedBalance());

        // if given, test subaddresses and that their balances add up to account balances
        if (account.GetSubaddresses() != null)
        {
            ulong balance = 0;
            ulong unlockedBalance = 0;
            List<MoneroSubaddress> subaddresses = account.GetSubaddresses() ?? [];

            uint i = 0;

            foreach (MoneroSubaddress subaddress in subaddresses)
            {
                TestSubaddress(subaddress);
                Assert.Equal(accountIndex, subaddress.GetAccountIndex());
                Assert.Equal(i, subaddress.GetIndex());
                balance += subaddress.GetBalance() ?? 0;
                unlockedBalance += subaddress.GetUnlockedBalance() ?? 0;
                i++;
            }

            Assert.True(balance.Equals(account.GetBalance()), "Subaddress balances " + balance + " != account " + accountIndex + " balance " + account.GetBalance());
            Assert.True(unlockedBalance.Equals(account.GetUnlockedBalance()), "Subaddress unlocked balances " + unlockedBalance + " != account " + accountIndex + " unlocked balance " + account.GetUnlockedBalance());
        }

        // tag must be undefined or non-empty
        string? tag = account.GetTag();
        Assert.True(tag == null || tag.Length > 0);
    }

    protected virtual void TestTxsWallet(List<MoneroTxWallet> txs, TxContext ctx)
    {
        // test each transaction
        Assert.True(txs.Count > 0);
        foreach (MoneroTxWallet tx in txs)
        {
            TestTxWallet(tx, ctx);
        }

        // test destinations across transactions
        if (ctx.Config != null && ctx.Config.GetDestinations() != null)
        {
            int destinationIdx = 0;
            bool subtractFeeFromDestinations = ctx.Config.GetSubtractFeeFrom() != null && ctx.Config.GetSubtractFeeFrom()!.Count > 0;
            foreach (MoneroTxWallet tx in txs)
            {
                // TODO: remove this after >18.3.1 when amounts_by_dest_list is official
                if (tx.GetOutgoingTransfer()!.GetDestinations() == null)
                {
                    Console.WriteLine("Tx missing destinations");
                    return;
                }

                ulong amountDiff = 0;
                foreach (MoneroDestination destination in tx.GetOutgoingTransfer()!.GetDestinations()!)
                {
                    MoneroDestination ctxDestination = ctx.Config.GetDestinations()![destinationIdx];
                    Assert.Equal(ctxDestination.GetAddress(), destination.GetAddress());
                    if (subtractFeeFromDestinations)
                    {
                        amountDiff += ctxDestination.GetAmount() - destination.GetAmount() ?? 0;
                    }
                    else
                    {
                        Assert.Equal(ctxDestination.GetAmount(), destination.GetAmount());
                    }
                    destinationIdx++;
                }

                if (subtractFeeFromDestinations)
                {
                    Assert.Equal(amountDiff, tx.GetFee());
                }
            }

            Assert.Equal(destinationIdx, ctx.Config.GetDestinations()!.Count);
        }
    }

    protected virtual void TestTxWallet(MoneroTxWallet? tx)
    {
        TestTxWallet(tx, null);
    }

    protected virtual void TestTxWallet(MoneroTxWallet? tx, TxContext? txCtx)
    {
        // validate / sanitize inputs
        TxContext ctx = new(txCtx);
        ctx.Wallet = null;  // TODO: re-enable

        if (tx == null)
        {
            throw new MoneroError("Tx is null");
        }

        if (ctx.IsSendResponse == null || ctx.Config == null)
        {
            if (ctx.IsSendResponse != null)
            {
                throw new Exception("if either sendRequest or isSendResponse is defined, they must both be defined");
            }

            if (ctx.Config != null)
            {
                throw new Exception("if either sendRequest or isSendResponse is defined, they must both be defined");
            }
        }

        // test common field types
        Assert.NotNull(tx.GetHash());
        Assert.NotNull(tx.IsConfirmed());
        Assert.NotNull(tx.IsMinerTx());
        Assert.NotNull(tx.IsFailed());
        Assert.NotNull(tx.IsRelayed());
        Assert.NotNull(tx.InTxPool());
        Assert.NotNull(tx.IsLocked());
        TestUtils.TestUnsignedBigInteger(tx.GetFee());
        if (tx.GetPaymentId() != null)
        {
            Assert.NotEqual(MoneroTx.DefaultPaymentId, tx.GetPaymentId()); // default payment id converted to null
        }
        if (tx.GetNote() != null)
        {
            Assert.True(tx.GetNote()!.Length > 0);  // empty notes converted to undefined
        }
        Assert.True(tx.GetUnlockTime() >= 0);
        Assert.Null(tx.GetSize());   // TODO monero-wallet-rpc: add tx_size to get_transfers and get_transfer_by_txid
        Assert.Null(tx.GetReceivedTimestamp());  // TODO monero-wallet-rpc: return received timestamp (asked to file issue if wanted)

        // test send _tx
        if (ctx.IsSendResponse == true)
        {
            Assert.True(tx.GetWeight() > 0);
            Assert.NotNull(tx.GetInputs());
            Assert.True(tx.GetInputs()!.Count > 0);
            foreach (MoneroOutput input in tx.GetInputs()!)
            {
                Assert.True(input.GetTx() == tx);
            }
        }
        else
        {
            Assert.Null(tx.GetWeight());
            Assert.Null(tx.GetInputs());
        }

        // test confirmed
        if (tx.IsConfirmed() == true)
        {
            Assert.NotNull(tx.GetBlock());
            Assert.Contains(tx, tx.GetBlock()!.GetTxs()!);
            Assert.True(tx.GetBlock()!.GetHeight() > 0);
            Assert.True(tx.GetBlock()!.GetTimestamp() > 0);
            Assert.Equal(true, tx.GetRelay());
            Assert.Equal(true, tx.IsRelayed());
            Assert.Equal(false, tx.IsFailed());
            Assert.Equal(false, tx.InTxPool());
            Assert.Equal(false, tx.IsDoubleSpendSeen());
            Assert.True(tx.GetNumConfirmations() > 0);
        }
        else
        {
            Assert.Null(tx.GetBlock());
            Assert.True(tx.GetNumConfirmations() == 0);
        }

        // test in _tx pool
        if (tx.InTxPool() == true)
        {
            Assert.Equal(false, tx.IsConfirmed());
            Assert.Equal(true, tx.GetRelay());
            Assert.Equal(true, tx.IsRelayed());
            Assert.Equal(false, tx.IsDoubleSpendSeen()); // TODO: test double spend attempt
            Assert.Equal(true, tx.IsLocked());

            // these should be initialized unless a response from sending
            if (ctx.IsSendResponse != true)
            {
                //Assert.True(_tx.GetReceivedTimestamp() > 0);  // TODO: re-enable when received timestamp returned in wallet rpc
            }
        }
        else
        {
            Assert.Null(tx.GetLastRelayedTimestamp());
        }

        // test miner _tx
        if (tx.IsMinerTx() == true)
        {
            Assert.True(tx.GetFee() == 0);
            Assert.True(tx.GetIncomingTransfers()!.Count > 0);
        }

        // test failed  // TODO: what else to test associated with failed
        if (tx.IsFailed() == true)
        {
            Assert.True(tx.GetOutgoingTransfer() is MoneroTransfer);
            //Assert.True(_tx.GetReceivedTimestamp() > 0);  // TODO: re-enable when received timestamp returned in wallet rpc
        }
        else
        {
            if (tx.IsRelayed() == true)
            {
                Assert.Equal(tx.IsDoubleSpendSeen(), false);
            }
            else
            {
                Assert.Equal(false, tx.GetRelay());
                Assert.Equal(false, tx.IsRelayed());
                Assert.Null(tx.IsDoubleSpendSeen());
            }
        }
        Assert.Null(tx.GetLastFailedHeight());
        Assert.Null(tx.GetLastFailedHash());

        // received time only for _tx pool or failed txs
        if (tx.GetReceivedTimestamp() != null)
        {
            Assert.True(tx.InTxPool() == true || tx.IsFailed() == true);
        }

        // test relayed _tx
        if (tx.IsRelayed() == true)
        {
            Assert.Equal(tx.GetRelay(), true);
        }

        if (!tx.GetRelay() == true)
        {
            Assert.True(!tx.IsRelayed());
        }

        // test outgoing transfer per configuration
        if (ctx.HasOutgoingTransfer == false)
        {
            Assert.Null(tx.GetOutgoingTransfer());
        }

        if (ctx.HasDestinations == true)
        {
            Assert.True(tx.GetOutgoingTransfer()!.GetDestinations()!.Count > 0);
        }

        // test outgoing transfer
        if (tx.GetOutgoingTransfer() != null)
        {
            Assert.True(tx.IsOutgoing());
            TestTransfer(tx.GetOutgoingTransfer(), ctx);
            if (ctx.IsSweepResponse == true)
            {
                Assert.Single(tx.GetOutgoingTransfer()!.GetDestinations()!);
            }

            // TODO: handle special cases
        }
        else
        {
            Assert.True(tx.GetIncomingTransfers()!.Count > 0);
            Assert.Null(tx.GetOutgoingAmount());
            Assert.Null(tx.GetOutgoingTransfer());
            Assert.Null(tx.GetRingSize());
            Assert.Null(tx.GetFullHex());
            Assert.Null(tx.GetMetadata());
            Assert.Null(tx.GetKey());
        }

        // test incoming transfers
        if (tx.GetIncomingTransfers() != null)
        {
            Assert.True(tx.IsIncoming());
            Assert.True(tx.GetIncomingTransfers()!.Count > 0);
            TestUtils.TestUnsignedBigInteger(tx.GetIncomingAmount());
            Assert.Equal(tx.IsFailed(), false);

            // test each transfer and collect transfer sum
            ulong transferSum = 0;
            foreach (MoneroIncomingTransfer transfer in tx.GetIncomingTransfers()!)
            {
                TestTransfer(transfer, ctx);
                transferSum += transfer.GetAmount() ?? 0;
                if (ctx.Wallet != null)
                {
                    Assert.Equal(ctx.Wallet.GetAddress((uint)transfer.GetAccountIndex()!, (uint)transfer.GetSubaddressIndex()!).GetAwaiter().GetResult(), transfer.GetAddress());
                }

                // TODO special case: transfer amount of 0
            }

            // incoming transfers add up to incoming _tx amount
            Assert.Equal(tx.GetIncomingAmount(), transferSum);
        }
        else
        {
            Assert.NotNull(tx.GetOutgoingTransfer());
            Assert.Null(tx.GetIncomingAmount());
            Assert.Null(tx.GetIncomingTransfers());
        }

        // test _tx results from send or relay
        if (ctx.IsSendResponse == true)
        {

            // test _tx set
            Assert.NotNull(tx.GetTxSet());
            bool found = false;
            foreach (MoneroTxWallet aTx in tx.GetTxSet()!.GetTxs()!)
            {
                if (aTx == tx)
                {
                    found = true;
                    break;
                }
            }
            if (ctx.IsCopy == true)
            {
                Assert.False(found); // copy will not have back the reference from _tx set
            }
            else
            {
                Assert.True(found);
            }

            // test common attributes
            MoneroTxConfig? config = ctx.Config;

            if (config == null)
            {
                throw new MoneroError("Config is null");
            }

            Assert.Equal(false, tx.IsConfirmed());
            TestTransfer(tx.GetOutgoingTransfer(), ctx);
            Assert.Equal(MoneroUtils.RingSize, (uint)tx.GetRingSize()!);
            Assert.True(0 == tx.GetUnlockTime());
            Assert.Null(tx.GetBlock());
            Assert.True(tx.GetKey()!.Length > 0);
            Assert.NotNull(tx.GetFullHex());
            Assert.True(tx.GetFullHex()!.Length > 0);
            Assert.NotNull(tx.GetMetadata());
            Assert.Null(tx.GetReceivedTimestamp());
            Assert.True(tx.IsLocked());

            // test locked state
            if (tx.GetUnlockTime() == 0)
            {
                Assert.Equal(tx.IsConfirmed(), !tx.IsLocked());
            }
            else
            {
                Assert.Equal(true, tx.IsLocked());
            }
            foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
            {
                Assert.Equal(tx.IsLocked(), output.IsLocked());
            }

            // test destinations of sent _tx
            if (tx.GetOutgoingTransfer()!.GetDestinations() != null)
            {
                Assert.NotNull(tx.GetOutgoingTransfer()!.GetDestinations());
                Assert.True(tx.GetOutgoingTransfer()!.GetDestinations()!.Count > 0);
                bool subtractFeeFromDestinations = ctx.Config != null && ctx.Config.GetSubtractFeeFrom() != null && ctx.Config.GetSubtractFeeFrom()!.Count > 0;
                if (ctx.IsSweepResponse == true)
                {
                    Assert.True(1 == config.GetDestinations()!.Count);
                    Assert.Null(config.GetDestinations()![0].GetAmount());
                    if (!subtractFeeFromDestinations)
                    {
                        Assert.Equal(tx.GetOutgoingTransfer()!.GetAmount().ToString(), tx.GetOutgoingTransfer()!.GetDestinations()![0].GetAmount().ToString());
                    }
                }
            }

            // test relayed txs
            if (config.GetRelay() == true)
            {
                Assert.Equal(true, tx.InTxPool());
                Assert.Equal(true, tx.GetRelay());
                Assert.Equal(true, tx.IsRelayed());
                Assert.True(tx.GetLastRelayedTimestamp() > 0);
                Assert.Equal(false, tx.IsDoubleSpendSeen());
            }

            // test non-relayed txs
            else
            {
                Assert.Equal(false, tx.InTxPool());
                Assert.Equal(false, tx.GetRelay());
                Assert.Equal(false, tx.IsRelayed());
                Assert.Null(tx.GetLastRelayedTimestamp());
                Assert.Null(tx.IsDoubleSpendSeen());
            }
        }

        // test _tx result query
        else
        {
            Assert.Null(tx.GetTxSet());  // _tx set only initialized on send responses
            Assert.Null(tx.GetRingSize());
            Assert.Null(tx.GetKey());
            Assert.Null(tx.GetFullHex());
            Assert.Null(tx.GetMetadata());
            Assert.Null(tx.GetLastRelayedTimestamp());
        }

        // test inputs
        if (tx.IsOutgoing() == true && ctx.IsSendResponse == true)
        {
            Assert.NotNull(tx.GetInputs());
            Assert.True(tx.GetInputs()!.Count > 0);
        }

        if (tx.GetInputs() != null)
        {
            foreach (MoneroOutputWallet input in tx.GetInputsWallet())
            {
                TestInputWallet(input);
            }
        }

        // test outputs
        if (tx.IsIncoming() == true && ctx.IncludeOutputs == true)
        {
            if (tx.IsConfirmed() == true)
            {
                Assert.NotNull(tx.GetOutputs());
                Assert.True(tx.GetOutputs()!.Count > 0);
            }
            else
            {
                Assert.Null(tx.GetOutputs());
            }
        }

        if (tx.GetOutputs() != null)
        {
            foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
            {
                TestOutputWallet(output);
            }
        }

        // test deep copy
        if (ctx.IsCopy != true)
        {
            TestTxWalletCopy(tx, ctx);
        }
    }

    private static void TestTransfer(MoneroTransfer? transfer, TxContext? ctx)
    {
        if (transfer == null)
        {
            throw new MoneroError("Transfer is null");
        }

        if (ctx == null)
        {
            ctx = new TxContext();
        }
        TestUtils.TestUnsignedBigInteger(transfer.GetAmount());
        if (ctx.IsSweepOutputResponse != true)
        {
            Assert.True(transfer.GetAccountIndex() >= 0);
        }

        if (transfer.IsIncoming() == true)
        {
            TestIncomingTransfer((MoneroIncomingTransfer?)transfer);
        }
        else
        {
            TestOutgoingTransfer((MoneroOutgoingTransfer?)transfer, ctx);
        }

        // transfer and _tx reference each other
        MoneroTxWallet? transferTx = transfer.GetTx();

        if (transferTx == null)
        {
            throw new MoneroError("Transfer transaction is null");
        }

        if (!transfer.Equals(transferTx.GetOutgoingTransfer()))
        {
            List<MoneroIncomingTransfer>? incomingTransfers = transferTx.GetIncomingTransfers();

            if (incomingTransfers == null)
            {
                throw new MoneroError("No incoming transfers found");
            }
            Assert.True(incomingTransfers.Contains(transfer), "Transaction does not reference given transfer");
        }
    }

    private static void TestIncomingTransfer(MoneroIncomingTransfer? transfer)
    {
        if (transfer == null)
        {
            throw new MoneroError("Incoming transfer is null");
        }

        Assert.True(transfer.IsIncoming());
        Assert.False(transfer.IsOutgoing());
        Assert.NotNull(transfer.GetAddress());
        Assert.True(transfer.GetSubaddressIndex() >= 0);
        Assert.True(transfer.GetNumSuggestedConfirmations() > 0);
    }

    private static void TestOutgoingTransfer(MoneroOutgoingTransfer? transfer, TxContext? ctx)
    {
        if (ctx == null)
        {
            throw new MoneroError("Tx context is null");
        }

        if (transfer == null)
        {
            throw new MoneroError("Outgoing transfer is null");
        }

        Assert.False(transfer.IsIncoming());
        Assert.True(transfer.IsOutgoing());
        if (ctx.IsSendResponse != true)
        {
            Assert.NotNull(transfer.GetSubaddressIndices());
        }
        if (transfer.GetSubaddressIndices() != null)
        {
            Assert.True(transfer.GetSubaddressIndices()!.Count >= 1);
        }
        if (transfer.GetAddresses() != null)
        {
            Assert.Equal(transfer.GetSubaddressIndices()!.Count, transfer.GetAddresses()!.Count);
            foreach (string address in transfer.GetAddresses()!)
            {
                Assert.NotNull(address);
            }
        }

        // test destinations sum to outgoing amount
        List<MoneroDestination>? destinations = transfer.GetDestinations();
        if (destinations != null)
        {
            Assert.True(destinations.Count > 0);
            ulong sum = 0;
            foreach (MoneroDestination destination in destinations)
            {
                TestDestination(destination);
                sum += destination.GetAmount() ?? 0;
            }

            Assert.Equal(sum, transfer.GetAmount());
        }
    }

    private static void TestDestination(MoneroDestination? destination)
    {
        if (destination == null)
        {
            throw new MoneroError("Destination is null");
        }
        MoneroUtils.ValidateAddress(destination.GetAddress(), TestUtils.NETWORK_TYPE);
        TestUtils.TestUnsignedBigInteger(destination.GetAmount(), true);
    }

    private static void TestInputWallet(MoneroOutputWallet? input)
    {
        if (input == null)
        {
            throw new MoneroError("Input is null");
        }

        MoneroKeyImage? keyImage = input.GetKeyImage();

        if (keyImage == null)
        {
            throw new MoneroError("Input key image is null");
        }

        string? keyImageHex = keyImage.GetHex();

        if (keyImageHex == null)
        {
            throw new MoneroError("Input key image hex is null");
        }

        Assert.True(keyImageHex.Length > 0);
        Assert.Null(input.GetAmount()); // must get info separately
    }

    private static void TestOutputWallet(MoneroOutputWallet? output)
    {
        if (output == null)
        {
            throw new MoneroError("Output is null");
        }
        Assert.True(output.GetAccountIndex() >= 0);
        Assert.True(output.GetSubaddressIndex() >= 0);
        Assert.True(output.GetIndex() >= 0);
        Assert.NotNull(output.IsSpent());
        Assert.NotNull(output.IsLocked());
        Assert.NotNull(output.IsFrozen());
        MoneroKeyImage? keyImage = output.GetKeyImage();

        if (keyImage == null)
        {
            throw new MoneroError("Input key image is null");
        }

        string? keyImageHex = keyImage.GetHex();

        if (keyImageHex == null)
        {
            throw new MoneroError("Input key image hex is null");
        }

        Assert.True(keyImageHex.Length > 0);
        TestUtils.TestUnsignedBigInteger(output.GetAmount(), true);

        // output has circular reference to its transaction which has some initialized fields
        MoneroTxWallet? tx = output.GetTx();
        if (tx == null)
        {
            throw new MoneroError("Tx is null");
        }

        List<MoneroOutput> outputs = tx.GetOutputs() ?? [];

        Assert.Contains(output, outputs);
        Assert.NotNull(tx.GetHash());
        Assert.NotNull(tx.IsLocked());
        Assert.Equal(true, tx.IsConfirmed());  // TODO monero-wallet-rpc: possible to get unconfirmed outputs?
        Assert.Equal(true, tx.IsRelayed());
        Assert.Equal(false, tx.IsFailed());
        Assert.True(tx.GetHeight() > 0);

        // test copying
        MoneroOutputWallet copy = output.Clone();
        Assert.True(copy != output);
        Assert.Equal(output.ToString(), copy.ToString());
        Assert.Null(copy.GetTx());  // TODO: should output copy do deep copy of _tx so models are graph instead of tree?  Would need to work out circular references
    }

    private void TestTxWalletCopy(MoneroTxWallet? tx, TxContext? txCtx)
    {
        if (tx == null)
        {
            throw new MoneroError("Cannot copy null tx");
        }

        // copy _tx and Assert. deep equality
        MoneroTxWallet copy = tx.Clone();
        Assert.True(copy is MoneroTxWallet);
        Assert.True(copy.Equals(tx));

        // test different references
        if (tx.GetOutgoingTransfer() != null)
        {
            Assert.True(tx.GetOutgoingTransfer() != copy.GetOutgoingTransfer());
            Assert.True(tx.GetOutgoingTransfer()!.GetTx() != copy.GetOutgoingTransfer()!.GetTx());
            if (tx.GetOutgoingTransfer()!.GetDestinations() != null)
            {
                Assert.True(tx.GetOutgoingTransfer()!.GetDestinations() != copy.GetOutgoingTransfer()!.GetDestinations());
                for (int i = 0; i < tx.GetOutgoingTransfer()!.GetDestinations()!.Count; i++)
                {
                    Assert.True(copy.GetOutgoingTransfer()!.GetDestinations()![i].Equals(tx.GetOutgoingTransfer()!.GetDestinations()![i]));
                    Assert.True(tx.GetOutgoingTransfer()!.GetDestinations()![i] != copy.GetOutgoingTransfer()!.GetDestinations()![i]);
                }
            }
        }
        if (tx.GetIncomingTransfers() != null)
        {
            for (int i = 0; i < tx.GetIncomingTransfers()!.Count; i++)
            {
                Assert.True(copy.GetIncomingTransfers()![i].Equals(tx.GetIncomingTransfers()![i]));
                Assert.True(tx.GetIncomingTransfers()![i] != copy.GetIncomingTransfers()![i]);
            }
        }
        if (tx.GetInputs() != null)
        {
            for (int i = 0; i < tx.GetInputs()!.Count; i++)
            {
                Assert.True(copy.GetInputs()![i].Equals(tx.GetInputs()![i]));
                Assert.True(tx.GetInputs()![i] != copy.GetInputs()![i]);
            }
        }
        if (tx.GetOutputs() != null)
        {
            for (int i = 0; i < tx.GetOutputs()!.Count; i++)
            {
                Assert.True(copy.GetOutputs()![i].Equals(tx.GetOutputs()![i]));
                Assert.True(tx.GetOutputs()![i] != copy.GetOutputs()![i]);
            }
        }

        // test copied _tx
        TxContext ctx = new(txCtx);
        ctx.IsCopy = true;
        if (tx.GetBlock() != null)
        {
            copy.SetBlock(tx.GetBlock()!.Clone().SetTxs([copy])); // copy block for testing
        }
        TestTxWallet(copy, ctx);

        // test merging with copy
        MoneroTxWallet merged = copy.Merge(copy.Clone());
        Assert.Equal(merged.ToString(), tx.ToString());
    }

    private async Task<List<MoneroTransfer>> GetAndTestTransfers(IMoneroWallet wallet, MoneroTransferQuery? query, TxContext? ctx, bool? isExpected)
    {
        MoneroTransferQuery? copy = null;
        if (query != null)
        {
            copy = query.Clone();
        }

        List<MoneroTransfer> transfers = await wallet.GetTransfers(query);
        if (isExpected == false)
        {
            Assert.True(0 == transfers.Count);
        }

        if (isExpected == true)
        {
            Assert.True(transfers.Count > 0, "Transfers were expected but not found; run send tests?");
        }

        if (ctx == null)
        {
            ctx = new TxContext();
        }

        ctx.Wallet = wallet;
        foreach (MoneroTransfer transfer in transfers)
        {
            TestTxWallet(transfer.GetTx(), ctx);
        }

        if (query != null)
        {
            Assert.NotNull(copy);
            Assert.True(copy.Equals(query));
        }
        return transfers;
    }

    private static async Task<List<MoneroTxWallet>> GetRandomTransactions(IMoneroWallet wallet, MoneroTxQuery? txQuery, int? minTxs, int? maxTxs)
    {
        List<MoneroTxWallet> txs = await wallet.GetTxs(txQuery);

        if (minTxs != null)
        {
            Assert.True(txs.Count >= minTxs, txs.Count + "/" + minTxs + " transactions found with the query");
        }

        GenUtils.Shuffle(txs);

        if (maxTxs == null)
        {
            return txs;
        }
        return txs.GetRange(0, Math.Min((int)maxTxs, txs.Count));
    }

    private async Task SendToMultiple(uint numAccounts, uint numSubaddressesPerAccount, bool canSplit, ulong? sendAmountPerSubaddress = null, bool subtractFeeFromDestinations = false)
    {
        await TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);

        // compute the minimum account unlocked balance needed in order to fulfill the config
        ulong? minAccountAmount = null;
        uint totalSubaddresses = numAccounts * numSubaddressesPerAccount;
        if (sendAmountPerSubaddress != null) minAccountAmount = (totalSubaddresses * sendAmountPerSubaddress) + TestUtils.MAX_FEE; // min account amount must cover the total amount being sent plus the tx fee = numAddresses * (amtPerSubaddress + fee)
        else minAccountAmount = (TestUtils.MAX_FEE * totalSubaddresses * SendDivisor) + TestUtils.MAX_FEE; // account balance must be more than fee * numAddresses * divisor + fee so each destination amount is at least a fee's worth (so dust is not sent)

        // send funds from first account with sufficient unlocked funds
        MoneroAccount? srcAccount = null;
        bool hasBalance = false;
        foreach (MoneroAccount walletAccount in await wallet.GetAccounts())
        {
            if (walletAccount.GetBalance()! > minAccountAmount) hasBalance = true;
            if (walletAccount.GetUnlockedBalance()! > minAccountAmount)
            {
                srcAccount = walletAccount;
                break;
            }
        }
        Assert.True(hasBalance, "Wallet does not have enough balance; load '" + TestUtils.WALLET_NAME + "' with XMR in order to test sending");
        if (srcAccount == null) throw new Exception("Wallet is waiting on unlocked funds");
        ulong balance = (ulong)srcAccount.GetBalance()!;
        ulong unlockedBalance = (ulong)srcAccount.GetUnlockedBalance()!;

        // get amount to send total and per subaddress
        ulong? sendAmount;
        if (sendAmountPerSubaddress == null)
        {
            sendAmount = TestUtils.MAX_FEE * 5 * totalSubaddresses;
            sendAmountPerSubaddress = (sendAmount / (totalSubaddresses));
        }
        else
        {
            sendAmount = sendAmountPerSubaddress * totalSubaddresses;
        }

        // create minimum number of accounts
        List<MoneroAccount> accounts = await wallet.GetAccounts();
        for (int i = 0; i < numAccounts - accounts.Count; i++)
        {
            await wallet.CreateAccount();
        }

        // create minimum number of subaddresses per account and collect destination addresses
        List<string> destinationAddresses = new List<string>();
        for (uint i = 0; i < numAccounts; i++)
        {
            List<MoneroSubaddress> subaddresses = await wallet.GetSubaddresses(i);
            for (int j = 0; j < numSubaddressesPerAccount - subaddresses.Count; j++)
            {
                await wallet.CreateSubaddress(i);
            }
            subaddresses = await wallet.GetSubaddresses(i);
            Assert.True(subaddresses.Count >= numSubaddressesPerAccount);
            for (int j = 0; j < numSubaddressesPerAccount; j++)
            {
                destinationAddresses.Add(subaddresses[j].GetAddress()!);
            }
        }

        // build tx config
        MoneroTxConfig config = new MoneroTxConfig();
        config.SetAccountIndex(srcAccount.GetIndex());
        config.SetSubaddressIndices(null); // test assigning null
        config.SetDestinations(new List<MoneroDestination>());
        config.SetRelay(true);
        config.SetCanSplit(canSplit);
        config.SetPriority(MoneroTxPriority.Normal);
        List<uint> subtractFeeFrom = new List<uint>();
        for (uint i = 0; i < destinationAddresses.Count; i++)
        {
            config.GetDestinations()!.Add(new MoneroDestination(destinationAddresses[(int)i], sendAmountPerSubaddress));
            subtractFeeFrom.Add(i);
        }
        if (subtractFeeFromDestinations) config.SetSubtractFeeFrom(subtractFeeFrom);

        MoneroTxConfig configCopy = config.Clone();

        // send tx(s) with config
        List<MoneroTxWallet>? txs = null;
        try
        {
            txs = await wallet.CreateTxs(config);
        }
        catch (MoneroError e)
        {

            // test error applying subtractFromFee with split txs
            if (subtractFeeFromDestinations && txs == null)
            {
                if (!e.Message.Equals("subtractfeefrom transfers cannot be split over multiple transactions yet")) throw;
                return;
            }

            throw;
        }

        if (!canSplit)
        {
            Assert.True(1 == txs.Count);
        }

        // test that config is unchanged
        Assert.True(configCopy != config);
        Assert.True(configCopy.Equals(config));

        // test that wallet balance decreased
        MoneroAccount account = await wallet.GetAccount((uint)srcAccount.GetIndex()!);
        Assert.True(account.GetBalance()! < balance);
        Assert.True(account.GetUnlockedBalance()! < unlockedBalance);

        // build test context
        config.SetCanSplit(canSplit);
        TxContext ctx = new TxContext();
        ctx.Wallet = wallet;
        ctx.Config = config;
        ctx.IsSendResponse = true;

        // test each transaction
        Assert.True(txs.Count > 0);
        ulong feeSum = 0;
        ulong outgoingSum = 0;
        TestTxsWallet(txs, ctx);
        foreach (MoneroTxWallet tx in txs)
        {
            feeSum += ((ulong)tx.GetFee()!);
            outgoingSum = outgoingSum + ((ulong)tx.GetOutgoingAmount()!);
            if (tx.GetOutgoingTransfer() != null && tx.GetOutgoingTransfer()!.GetDestinations() != null)
            {
                ulong destinationSum = 0;
                foreach (MoneroDestination destination in tx.GetOutgoingTransfer()!.GetDestinations()!)
                {
                    TestDestination(destination);
                    Assert.Contains(destination.GetAddress()!, destinationAddresses);
                    destinationSum = destinationSum + ((ulong)destination.GetAmount()!);
                }
                Assert.True(tx.GetOutgoingAmount().Equals(destinationSum));  // Assert. that transfers sum up to tx amount
            }
        }

        // Assert. that outgoing amounts sum up to the amount sent within a small margin
        if (sendAmount - (subtractFeeFromDestinations ? feeSum : 0) - outgoingSum > SendMaxDiff)
        { // send amounts may be slightly different
            Assert.Fail("Actual send amount is too different from requested send amount: " + sendAmount + " - " + (subtractFeeFromDestinations ? feeSum : 0) + " - " + outgoingSum + " = " + (sendAmount - (subtractFeeFromDestinations ? feeSum : 0) - outgoingSum));
        }
    }

    #endregion
}