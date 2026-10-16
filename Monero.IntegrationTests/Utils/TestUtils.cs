using System.Text.Json;
using Monero.Common;
using Monero.Daemon;

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

        Address = root.GetProperty("wallet").GetProperty("address").GetString()!;
        MiningAddress = root.GetProperty("miningWallet").GetProperty("address").GetString()!;
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
}
