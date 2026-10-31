using System.Text.Json;
using Monero.Common;
using Monero.Daemon;
using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Test utilities and constants, configured by <c>Config/config.json</c>. The daemon URIs can be overridden with the environment variables
/// <c>MONERO_DAEMON_RPC_URI</c> and <c>MONERO_MINING_DAEMON_RPC_URI</c>.
/// </summary>
public static class TestUtils
{
    private static readonly object Lock = new();
    private static MoneroDaemonRpc? _daemonRpc;
    private static MoneroDaemonRpc? _miningDaemon;
    private static MoneroWalletRpc? _walletRpc;
    private static MoneroWalletRpc? _miningWalletRpc;

    static TestUtils()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "config.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var general = root.GetProperty("general");
        var daemon = root.GetProperty("daemon");

        TestNonRelays = general.GetProperty("testNonRelays").GetBoolean();
        TestRelays = general.GetProperty("testRelays").GetBoolean();
        TestNotifications = general.GetProperty("testNotifications").GetBoolean();
        LiteMode = general.GetProperty("liteMode").GetBoolean();
        TestResets = general.GetProperty("testResets").GetBoolean();
        AutoConnectTimeoutMs = general.GetProperty("autoConnectTimeoutMs").GetInt32();
        LogLevel = general.GetProperty("logLevel").GetInt32();
        LogCategories = general.GetProperty("logCategories").GetString()!;

        var networkType = general.GetProperty("networkType").GetString()!.ToLowerInvariant();
        Regtest = networkType is "regtest" or "reg";
        MinBlockHeight = Regtest ? 120UL : 0UL;

        DaemonLogLevel = daemon.GetProperty("logLevel").GetInt32();
        DaemonPollPeriodInMs = daemon.GetProperty("pollPeriodInMs").GetUInt64();
        DaemonRpcUri = Environment.GetEnvironmentVariable("MONERO_DAEMON_RPC_URI") ?? daemon.GetProperty("rpcUri").GetString()!;
        MiningDaemonRpcUri = Environment.GetEnvironmentVariable("MONERO_MINING_DAEMON_RPC_URI") ?? daemon.GetProperty("miningRpcUri").GetString()!;
        DaemonRpcUsername = daemon.GetProperty("rpcUsername").GetString()!;
        DaemonRpcPassword = daemon.GetProperty("rpcPassword").GetString()!;
        DaemonRpcZmqUri = daemon.GetProperty("zmqUri").GetString()!;

        var wallet = root.GetProperty("wallet");
        var miningWallet = root.GetProperty("miningWallet");

        Address = wallet.GetProperty("address").GetString()!;
        WalletName = wallet.GetProperty("name").GetString()!;
        WalletPassword = wallet.GetProperty("password").GetString()!;
        Language = wallet.GetProperty("language").GetString()!;
        Seed = wallet.GetProperty("seed").GetString()!;
        PrivateViewKey = wallet.GetProperty("privateViewKey").GetString()!;
        PrivateSpendKey = wallet.GetProperty("privateSpendKey").GetString()!;
        PublicViewKey = wallet.GetProperty("publicViewKey").GetString()!;
        PublicSpendKey = wallet.GetProperty("publicSpendKey").GetString()!;
        SyncPeriodInMs = wallet.GetProperty("syncPeriodInMs").GetUInt64();
        WalletRpcUris = [.. wallet.GetProperty("rpcUris").EnumerateArray().Select(uri => uri.GetString()!)];
        WalletRpcUsername = wallet.GetProperty("rpcUsername").GetString()!;
        WalletRpcPassword = wallet.GetProperty("rpcPassword").GetString()!;

        MiningAddress = miningWallet.GetProperty("address").GetString()!;
        MiningSeed = miningWallet.GetProperty("seed").GetString()!;
        MiningWalletName = miningWallet.GetProperty("name").GetString()!;
    }

    /// <summary>Indicates if the tests that don't relay transactions are enabled.</summary>
    public static bool TestNonRelays { get; }

    /// <summary>Indicates if the tests that relay transactions are enabled.</summary>
    public static bool TestRelays { get; }

    /// <summary>Indicates if the tests of notifications are enabled.</summary>
    public static bool TestNotifications { get; }

    /// <summary>Indicates if the tests run in light mode.</summary>
    public static bool LiteMode { get; }

    /// <summary>Indicates if the tests that reset state are enabled.</summary>
    public static bool TestResets { get; }

    /// <summary>Default connection timeout in milliseconds.</summary>
    public static int AutoConnectTimeoutMs { get; }

    /// <summary>Monero core internal log level.</summary>
    public static int LogLevel { get; }

    /// <summary>Monero core internal log categories.</summary>
    public static string LogCategories { get; }

    /// <summary>Indicates if the tests run on a fake chain.</summary>
    public static bool Regtest { get; }

    /// <summary>Minimum blockchain height for the tests.</summary>
    public static ulong MinBlockHeight { get; }

    /// <summary>Daemon RPC log level.</summary>
    public static int DaemonLogLevel { get; }

    /// <summary>Period between daemon polls for block notifications, in milliseconds.</summary>
    public static ulong DaemonPollPeriodInMs { get; }

    /// <summary>Monero daemon RPC URI.</summary>
    public static string DaemonRpcUri { get; }

    /// <summary>URI of the daemon that mines.</summary>
    public static string MiningDaemonRpcUri { get; }

    /// <summary>Monero daemon RPC username.</summary>
    public static string DaemonRpcUsername { get; }

    /// <summary>Monero daemon RPC password.</summary>
    public static string DaemonRpcPassword { get; }

    /// <summary>Monero daemon ZMQ URI.</summary>
    public static string DaemonRpcZmqUri { get; }

    /// <summary>Test wallet primary address.</summary>
    public static string Address { get; }

    /// <summary>Mining wallet primary address.</summary>
    public static string MiningAddress { get; }

    /// <summary>Mining wallet seed.</summary>
    public static string MiningSeed { get; }

    /// <summary>Name of the mining wallet on its monero-wallet-rpc server.</summary>
    public static string MiningWalletName { get; }

    /// <summary>Name of the test wallet on its monero-wallet-rpc server.</summary>
    public static string WalletName { get; }

    /// <summary>Password of the test wallets.</summary>
    public static string WalletPassword { get; }

    /// <summary>Seed language of the test wallet.</summary>
    public static string Language { get; }

    /// <summary>Test wallet seed.</summary>
    public static string Seed { get; }

    /// <summary>Test wallet private view key.</summary>
    public static string PrivateViewKey { get; }

    /// <summary>Test wallet private spend key.</summary>
    public static string PrivateSpendKey { get; }

    /// <summary>Test wallet public view key.</summary>
    public static string PublicViewKey { get; }

    /// <summary>Test wallet public spend key.</summary>
    public static string PublicSpendKey { get; }

    /// <summary>Period between background syncs of the test wallets, in milliseconds.</summary>
    public static ulong SyncPeriodInMs { get; }

    /// <summary>URIs of the monero-wallet-rpc servers: the first is for the test wallet, the second for the mining wallet and the third for the wallets that tests create.</summary>
    public static IReadOnlyList<string> WalletRpcUris { get; }

    /// <summary>Username of the monero-wallet-rpc servers.</summary>
    public static string WalletRpcUsername { get; }

    /// <summary>Password of the monero-wallet-rpc servers.</summary>
    public static string WalletRpcPassword { get; }

    /// <summary>Dummy server URI to remain offline.</summary>
    public const string OfflineServerUri = "offline_server_uri";

    /// <summary>
    /// Get the test daemon RPC. It is shared by the tests.
    /// </summary>
    public static MoneroDaemonRpc GetDaemonRpc()
    {
        lock (Lock)
        {
            return _daemonRpc ??= new MoneroDaemonRpc(DaemonRpcUri, DaemonRpcUsername, DaemonRpcPassword);
        }
    }

    /// <summary>
    /// Get the daemon used for mining. It is shared by the tests.
    /// </summary>
    public static MoneroDaemonRpc GetMiningDaemon()
    {
        lock (Lock)
        {
            return _miningDaemon ??= new MoneroDaemonRpc(GetMiningDaemonRpcConnection());
        }
    }

    /// <summary>
    /// Get the RPC connection of the daemon used for mining.
    /// </summary>
    public static MoneroRpcConnection GetMiningDaemonRpcConnection() => new(MiningDaemonRpcUri, DaemonRpcUsername, DaemonRpcPassword);

    /// <summary>
    /// Get a new RPC connection to the test daemon.
    /// </summary>
    public static MoneroRpcConnection GetDaemonRpcConnection() => new(DaemonRpcUri, DaemonRpcUsername, DaemonRpcPassword);
    /// <summary>
    /// Get the RPC connection to the monero-wallet-rpc server with the given index.
    /// </summary>
    /// <param name="index">0 is the server of the test wallet, 1 the one of the mining wallet and 2 the one of the wallets that tests create.</param>
    public static MoneroRpcConnection GetWalletRpcConnection(int index = 0) => new(WalletRpcUris[index], WalletRpcUsername, WalletRpcPassword);

    /// <summary>
    /// Get the test wallet, funded and synced. It is shared by the tests.
    /// </summary>
    public static async Task<MoneroWalletRpc> GetWalletRpc()
    {
        var wallet = GetOrCreate(ref _walletRpc, () => new MoneroWalletRpc(GetWalletRpcConnection(0)));
        await OpenOrCreateWallet(wallet, WalletName, new MoneroWalletConfig { Seed = Seed, RestoreHeight = 0 });

        Assert.That(await wallet.GetSeedAsync(), Is.EqualTo(Seed));
        Assert.That(await wallet.GetPrimaryAddressAsync(), Is.EqualTo(Address));

        await wallet.SyncAsync();
        await wallet.SaveAsync();
        await wallet.StartSyncingAsync(SyncPeriodInMs);

        return wallet;
    }

    /// <summary>
    /// Get the mining wallet, which has the rewards of the blocks that are mined for the tests. It is shared by the tests.
    /// </summary>
    public static async Task<MoneroWalletRpc> GetMiningWalletRpc()
    {
        var wallet = GetOrCreate(ref _miningWalletRpc, () => new MoneroWalletRpc(GetWalletRpcConnection(1)));
        await OpenOrCreateWallet(wallet, MiningWalletName, new MoneroWalletConfig { Seed = MiningSeed, RestoreHeight = 0 });

        Assert.That(await wallet.GetSeedAsync(), Is.EqualTo(MiningSeed));
        Assert.That(await wallet.GetPrimaryAddressAsync(), Is.EqualTo(MiningAddress));

        await wallet.SyncAsync();
        await wallet.SaveAsync();

        return wallet;
    }

    // the daemon of the monero-wallet-rpc servers, as the servers reach it in the network of the compose file. The wallet manager of the Python
    // tests gives it to the wallets it creates or opens, because the client knows no daemon of a wallet that was not given one
    private const string WalletDaemonRpcUri = "http://node_2:18081";

    private static MoneroRpcConnection GetWalletDaemonRpcConnection() => new(WalletDaemonRpcUri, DaemonRpcUsername, DaemonRpcPassword);

    // as the wallet manager of the Python tests does after it creates or opens a wallet on a server: the wallet keeps its daemon and trusts it,
    // and if it is connected the wallet syncs in the background, which also sets the period of its poller. Without it a new wallet is refreshed
    // by the server when the server pleases, and polled by the client every 20 seconds
    private static async Task ConnectAndSync(MoneroWalletRpc wallet)
    {
        await wallet.SetDaemonConnectionAsync(await wallet.GetDaemonConnectionAsync(), true);
        if (await wallet.IsConnectedToDaemonAsync()) await wallet.StartSyncingAsync(SyncPeriodInMs);
    }

    // the server may have no wallet open, and then it has nothing to stop
    private static async Task StopSyncing(MoneroWalletRpc wallet)
    {
        try
        {
            await wallet.StopSyncingAsync();
        }
        catch (MoneroError)
        {
            // nothing to stop
        }
    }

    /// <summary>
    /// Create a wallet on the third server. The server has one wallet open at a time, so a wallet that was created before is not usable
    /// after: dispose it first.
    /// </summary>
    /// <param name="config">configures the wallet, which has a random name if its path is not set.</param>
    public static async Task<MoneroWalletRpc> CreateWalletRpc(MoneroWalletConfig? config = null)
    {
        var wallet = new MoneroWalletRpc(GetWalletRpcConnection(2));
        try
        {
            config = config == null ? new MoneroWalletConfig() : config.Copy();
            config.Path ??= Guid.NewGuid().ToString("N");
            config.Password ??= WalletPassword;
            config.Server ??= GetWalletDaemonRpcConnection();
            await StopSyncing(wallet);
            await wallet.CreateWalletAsync(config);
            await ConnectAndSync(wallet);

            return wallet;
        }
        catch
        {
            await wallet.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Open a wallet on the third server.
    /// </summary>
    /// <param name="config">has the name of the wallet as its path and its password.</param>
    public static async Task<MoneroWalletRpc> OpenWalletRpc(MoneroWalletConfig config)
    {
        var wallet = new MoneroWalletRpc(GetWalletRpcConnection(2));
        try
        {
            config = config.Copy();
            config.Password ??= WalletPassword;
            config.Server ??= GetWalletDaemonRpcConnection();
            await StopSyncing(wallet);
            await wallet.OpenWalletAsync(config);
            await ConnectAndSync(wallet);

            return wallet;
        }
        catch
        {
            await wallet.DisposeAsync();
            throw;
        }
    }

    // the wallet is opened if the server has it, and created from the config otherwise
    private static async Task OpenOrCreateWallet(MoneroWalletRpc wallet, string name, MoneroWalletConfig config)
    {
        try
        {
            await wallet.OpenWalletAsync(name, WalletPassword);
        }
        catch (MoneroError)
        {
            config = config.Copy();
            config.Path = name;
            config.Password = WalletPassword;
            await wallet.CreateWalletAsync(config);
        }
    }

    private static T GetOrCreate<T>(ref T? field, Func<T> create) where T : class
    {
        lock (Lock)
        {
            return field ??= create();
        }
    }
}
