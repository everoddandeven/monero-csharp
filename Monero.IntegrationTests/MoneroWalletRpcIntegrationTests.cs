using Monero.Common;
using Monero.Daemon;
using Monero.IntegrationTests.Utils;
using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests;

/// <summary>
/// Wallet RPC integration tests. They need the regtest network of <c>docker-compose.yml</c>, with its monero-wallet-rpc servers. The test wallet
/// is on the first server, the mining wallet on the second and the wallets that the tests create on the third.
/// </summary>
[TestFixture]
public partial class MoneroWalletRpcIntegrationTests : BaseTestClass
{
    private static MoneroDaemonRpc Daemon => TestUtils.GetDaemonRpc();

    private static Task<MoneroWalletRpc> GetWallet() => TestUtils.GetWalletRpc();

    protected override async Task BeforeAll()
    {
        // mine the blockchain until the minimum height
        await BlockchainUtils.SetupBlockchain();

        // fund the test wallet with the mined coins, and wait until the funds are unlocked
        var wallet = await GetWallet();
        var txs = await WalletTestUtils.FundWallet(wallet);
        if (txs.Count > 0)
        {
            // the mining node mines the txs once it has them in its pool
            await DaemonUtils.WaitForTxsInPool(MiningUtils.GetDaemon(), txs.Select(tx => tx.Hash!).ToList());

            // mine an output to the primary address, then the blocks that confirm and unlock the txs
            await MiningUtils.GenerateBlocks(await wallet.GetPrimaryAddressAsync(), 1);
            var height = await BlockchainUtils.WaitForBlocks(11);

            while (await wallet.GetHeightAsync() < height)
            {
                await wallet.SyncAsync();
                await Task.Delay((int)TestUtils.SyncPeriodInMs);
            }

            var hashes = txs.Select(tx => tx.Hash!).ToList();
            var funded = await wallet.GetTxsAsync(new MoneroTxQuery { Hashes = hashes });
            Assert.That(funded, Has.Count.EqualTo(txs.Count));
            Assert.That(funded.All(tx => tx.IsLocked == false), Is.True, "The funding txs are locked");
        }
    }

    protected override async Task AfterAll()
    {
        await MiningUtils.TryStopMining(Daemon);
    }

    // helpers

    private static MoneroWalletConfig NewConfig(string? path = null) => new() { Path = path ?? WalletUtils.GetRandomString() };

    // the wallet is closed, and the connection released
    private static async Task CloseWallet(MoneroWalletRpc wallet, bool save = false)
    {
        try
        {
            await wallet.CloseAsync(save);
        }
        finally
        {
            await wallet.DisposeAsync();
        }
    }

    // offline wallets

    // Can't use an offline wallet
    [Test, Category(NonRelays)]
    public async Task TestOfflineWallet()
    {
        await using var offline = new MoneroWalletRpc(TestUtils.OfflineServerUri, TestUtils.WalletRpcUsername, TestUtils.WalletPassword);

        var error = Assert.ThrowsAsync<MoneroError>(() => offline.IsViewOnlyAsync());
        WalletUtils.TestWalletIsNotConnectedError(error);

        error = Assert.ThrowsAsync<MoneroError>(() => offline.StopAsync());
        WalletUtils.TestWalletIsNotConnectedError(error);

        error = Assert.ThrowsAsync<MoneroError>(() => offline.RescanBlockchainAsync());
        WalletUtils.TestWalletIsNotConnectedError(error);

        Assert.That(await offline.IsClosedAsync(), Is.False); // TODO should throw a connection error
    }

    // Validates the config to create a wallet
    [Test, Category(NonRelays)]
    public async Task TestValidateInputsCreateWallet()
    {
        await using var offline = new MoneroWalletRpc(TestUtils.OfflineServerUri, TestUtils.WalletRpcUsername, TestUtils.WalletPassword);
        var config = new MoneroWalletConfig
        {
            Seed = TestUtils.Seed,
            PrimaryAddress = TestUtils.Address,
            PrivateSpendKey = TestUtils.PrivateSpendKey,
            PrivateViewKey = TestUtils.PrivateViewKey,
        };

        var error = Assert.ThrowsAsync<MoneroError>(() => offline.CreateWalletAsync(config));

        Assert.That(error!.Message, Is.EqualTo("Wallet can be initialized with a seed or keys but not both"));
    }

    // Does not support reporting the progress of a sync
    [Test, Category(NonRelays)]
    public async Task TestSyncProgress()
    {
        var wallet = await GetWallet();
        var listener = new MoneroWalletListener();
        const string message = "Monero Wallet RPC does not support reporting sync progress";

        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.SyncAsync(listener: listener));
        Assert.That(error!.Message, Is.EqualTo(message));

        error = Assert.ThrowsAsync<MoneroError>(() => wallet.SyncAsync(0, listener));
        Assert.That(error!.Message, Is.EqualTo(message));
    }

    // Can set the poll period for wallet notifications
    [Test, Category(NonRelays)]
    public async Task TestSetPollPeriodInMs()
    {
        var wallet = await GetWallet();

        try
        {
            await wallet.SetPollPeriodInMsAsync(1000);
        }
        finally
        {
            // restore the configured period so that the later tests poll at the expected rate
            await wallet.SetPollPeriodInMsAsync(TestUtils.SyncPeriodInMs);
        }
    }

    // Can get the connection to the server
    [Test, Category(NonRelays)]
    public async Task TestGetRpcConnection()
    {
        var wallet = await GetWallet();

        var connection = await wallet.GetRpcConnectionAsync();

        Assert.That(connection.Uri, Is.EqualTo(TestUtils.WalletRpcUris[0]));
        Assert.That(connection.Username, Is.EqualTo(TestUtils.WalletRpcUsername));
        Assert.That(await wallet.IsConnectedAsync(), Is.True);
    }

    // Can get the languages of the server
    [Test, Category(NonRelays)]
    public async Task TestGetSeedLanguages()
    {
        var wallet = await GetWallet();

        var languages = await wallet.GetSeedLanguagesAsync();

        Assert.That(languages, Is.Not.Empty);
        Assert.That(languages, Has.All.Not.Empty);
        Assert.That(languages, Does.Contain(TestUtils.Language));
    }

    // Can get a subaddress with the methods of monero-wallet-rpc
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddressRpc()
    {
        var wallet = await GetWallet();

        var subaddress = await wallet.GetSubaddressAsync(0, 0);

        WalletUtils.TestSubaddress(subaddress);
        Assert.That(subaddress.AccountIndex, Is.EqualTo(0u));
        Assert.That(subaddress.Index, Is.EqualTo(0u));

        var plural = await wallet.GetSubaddressesAsync(0, [0]);
        Assert.That(plural, Has.Count.EqualTo(1));
        AssertUtils.AssertEquals(subaddress, plural[0]);

        Assert.That(subaddress.Address, Is.EqualTo(await wallet.GetAddressAsync(0, 0)));
    }

    // Can get an address out of the range of the used subaddresses: the server has none
    [Test, Category(NonRelays)]
    public async Task TestGetSubaddressAddressOutOfRange()
    {
        var wallet = await GetWallet();
        var accounts = await wallet.GetAccountsAsync(true);
        var accountIdx = (uint)(accounts.Count - 1);
        var subaddressIdx = (uint)accounts[^1].Subaddresses!.Count;

        var address = await wallet.GetAddressAsync(accountIdx, subaddressIdx);

        Assert.That(address, Is.Null.Or.Empty);
    }

    // Can create a wallet with a randomly generated seed
    [Test, Category(NonRelays)]
    public async Task TestCreateWalletRandom()
    {
        // create a random wallet with the defaults
        var path = WalletUtils.GetRandomString();
        var wallet = await TestUtils.CreateWalletRpc(NewConfig(path));
        string seed;
        try
        {
            seed = await wallet.GetSeedAsync();
            WalletUtils.ValidateMnemonic(seed);
            Assert.That(seed, Is.Not.EqualTo(TestUtils.Seed));
            WalletUtils.ValidateAddress(await wallet.GetPrimaryAddressAsync());
            WalletUtils.ValidateKey(await wallet.GetPrivateViewKeyAsync());
            WalletUtils.ValidateKey(await wallet.GetPrivateSpendKeyAsync());

            // very quick because the restore height is the chain height
            await wallet.SyncAsync();
        }
        finally
        {
            await CloseWallet(wallet);
        }

        // create a random wallet with a language
        var config = NewConfig();
        config.Language = "Spanish";
        wallet = await TestUtils.CreateWalletRpc(config);
        try
        {
            WalletUtils.ValidateMnemonic(await wallet.GetSeedAsync());
            Assert.That(await wallet.GetSeedAsync(), Is.Not.EqualTo(seed));
            seed = await wallet.GetSeedAsync();
            WalletUtils.ValidateAddress(await wallet.GetPrimaryAddressAsync());

            // attempt to create a wallet that exists already
            var error = Assert.ThrowsAsync<MoneroError>(() => wallet.CreateWalletAsync(config));
            Assert.That(error!.Message, Is.EqualTo($"Wallet already exists: {config.Path}"));
            Assert.That(await wallet.GetSeedAsync(), Is.EqualTo(seed));
        }
        finally
        {
            await CloseWallet(wallet);
        }

        // attempt to create a wallet with an unknown language
        config = NewConfig();
        config.Language = "english";
        var unknown = Assert.ThrowsAsync<MoneroError>(() => TestUtils.CreateWalletRpc(config));
        Assert.That(unknown!.Message, Is.EqualTo("Unknown language: english"));
    }

    // Can create a wallet from a seed
    [Test, Category(NonRelays)]
    public async Task TestCreateWalletFromSeed()
    {
        // create a wallet with the seed and the defaults
        var config = NewConfig();
        config.Seed = TestUtils.Seed;
        config.RestoreHeight = 0;
        var wallet = await TestUtils.CreateWalletRpc(config);
        try
        {
            Assert.That(await wallet.GetSeedAsync(), Is.EqualTo(TestUtils.Seed));
            Assert.That(await wallet.GetPrimaryAddressAsync(), Is.EqualTo(TestUtils.Address));
            Assert.That(await wallet.GetPrivateViewKeyAsync(), Is.EqualTo(TestUtils.PrivateViewKey));
            Assert.That(await wallet.GetPrivateSpendKeyAsync(), Is.EqualTo(TestUtils.PrivateSpendKey));

            await wallet.SyncAsync();
            Assert.That(await wallet.GetHeightAsync(), Is.EqualTo(await Daemon.GetHeightAsync()));

            // the wallet is used
            var txs = await wallet.GetTxsAsync();
            Assert.That(txs, Is.Not.Empty, "Wallet is not used");
        }
        finally
        {
            await CloseWallet(wallet);
        }

        // attempt to create a wallet with two missing words
        config = NewConfig();
        config.Seed = string.Join(' ', TestUtils.Seed.Split(' ').Skip(2));
        config.RestoreHeight = 0;
        var invalid = Assert.ThrowsAsync<MoneroError>(() => TestUtils.CreateWalletRpc(config));
        Assert.That(invalid!.Message, Is.EqualTo("Invalid mnemonic"));
    }

    // Can create a wallet from a seed with an offset
    [Test, Category(NonRelays)]
    public async Task TestCreateWalletFromSeedWithOffset()
    {
        var config = NewConfig();
        config.Seed = TestUtils.Seed;
        config.RestoreHeight = 0;
        config.SeedOffset = "my secret offset!";
        config.Language = "German";
        config.SaveCurrent = false;
        var wallet = await TestUtils.CreateWalletRpc(config);
        try
        {
            WalletUtils.ValidateMnemonic(await wallet.GetSeedAsync());
            Assert.That(await wallet.GetSeedAsync(), Is.Not.EqualTo(TestUtils.Seed));
            Assert.That(await wallet.GetPrimaryAddressAsync(), Is.Not.EqualTo(TestUtils.Address));
            Assert.That(await wallet.IsViewOnlyAsync(), Is.False);

            await wallet.SyncAsync();
            Assert.That(await wallet.GetTxsAsync(), Is.Empty, "Wallet is used");
        }
        finally
        {
            await CloseWallet(wallet);
        }
    }

    // Can create a wallet from its keys
    [Test, Category(NonRelays)]
    public async Task TestCreateWalletFromKeys()
    {
        var config = NewConfig();
        config.PrimaryAddress = TestUtils.Address;
        config.PrivateViewKey = TestUtils.PrivateViewKey;
        config.PrivateSpendKey = TestUtils.PrivateSpendKey;
        config.RestoreHeight = await Daemon.GetHeightAsync();
        var wallet = await TestUtils.CreateWalletRpc(config);
        try
        {
            Assert.That(await wallet.GetPrimaryAddressAsync(), Is.EqualTo(TestUtils.Address));
            Assert.That(await wallet.GetPrivateViewKeyAsync(), Is.EqualTo(TestUtils.PrivateViewKey));
            Assert.That(await wallet.GetPrivateSpendKeyAsync(), Is.EqualTo(TestUtils.PrivateSpendKey));
            Assert.That(await wallet.IsConnectedToDaemonAsync(), Is.True, "Wallet created from keys is not connected to authenticated daemon");
        }
        finally
        {
            await CloseWallet(wallet);
        }

        // attempt to create a wallet that exists already
        var error = Assert.ThrowsAsync<MoneroError>(() => TestUtils.CreateWalletRpc(config));
        Assert.That(error!.Message, Is.EqualTo($"Wallet already exists: {config.Path}"));
    }

    // Can open wallets
    [Test, Category(NonRelays)]
    public async Task TestOpenWallet()
    {
        // create a test wallet
        var name = WalletUtils.GetRandomString();
        var wallet = await TestUtils.CreateWalletRpc(NewConfig(name));
        var seed = await wallet.GetSeedAsync();
        await CloseWallet(wallet, true);

        // open the test wallet
        wallet = await TestUtils.OpenWalletRpc(NewConfig(name));
        try
        {
            Assert.That(await wallet.GetSeedAsync(), Is.EqualTo(seed));
        }
        finally
        {
            await CloseWallet(wallet);
        }

        // attempt to open a wallet that doesn't exist
        Assert.ThrowsAsync<MoneroError>(() => TestUtils.OpenWalletRpc(NewConfig("btc_integrity")));
    }

    // Can get the path of a wallet, and open it with it
    [Test, Category(NonRelays)]
    public async Task TestGetPath()
    {
        // create a random wallet with an attribute
        var wallet = await TestUtils.CreateWalletRpc();
        var uuid = WalletUtils.GetRandomString();
        await wallet.SetAttributeAsync("uuid", uuid);

        // record the path, then save and close
        var path = await wallet.GetPathAsync();
        await CloseWallet(wallet, true);

        // open the wallet with its path
        wallet = await TestUtils.OpenWalletRpc(NewConfig(path));
        try
        {
            Assert.That(await wallet.GetAttributeAsync("uuid"), Is.EqualTo(uuid));
        }
        finally
        {
            await CloseWallet(wallet);
        }
    }

    // Can save the wallet
    [Test, Category(NonRelays)]
    public async Task TestSave()
    {
        var wallet = await GetWallet();

        await wallet.SaveAsync();
    }

    // Can close a wallet
    [Test, Category(NonRelays)]
    public async Task TestClose()
    {
        // create a test wallet
        var path = WalletUtils.GetRandomString();
        var wallet = await TestUtils.CreateWalletRpc(NewConfig(path));
        try
        {
            await wallet.SyncAsync();

            // close the wallet
            await wallet.CloseAsync();

            // attempt to use the wallet
            WalletUtils.TestNoWalletFileError(Assert.ThrowsAsync<MoneroError>(() => wallet.GetHeightAsync()));
            WalletUtils.TestNoWalletFileError(Assert.ThrowsAsync<MoneroError>(() => wallet.GetSeedAsync()));
            WalletUtils.TestNoWalletFileError(Assert.ThrowsAsync<MoneroError>(() => wallet.SyncAsync()));

            // open the wallet again
            await wallet.OpenWalletAsync(path, TestUtils.WalletPassword);
            await wallet.SyncAsync();
            Assert.That(await wallet.GetHeightAsync(), Is.EqualTo(await Daemon.GetHeightAsync()));
        }
        finally
        {
            await CloseWallet(wallet, true);
        }
    }

    // Can stop the server
    [Test, Category(NonRelays)]
    [Ignore("Disabled so that the server is not stopped")]
    public async Task TestStop()
    {
        var wallet = await GetWallet();

        await wallet.StopAsync();
    }
}
