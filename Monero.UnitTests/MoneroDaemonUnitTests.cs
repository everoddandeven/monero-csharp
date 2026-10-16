using System.Reflection;
using Monero.Daemon;
using Monero.Daemon.Model;

namespace Monero.UnitTests;

/// <summary>
/// Unit tests of <see cref="MoneroDaemon"/>. They run without monero-c, a node or the network.
/// </summary>
[TestFixture]
public class MoneroDaemonUnitTests
{
    // the methods of the daemon, in snake_case
    private static readonly string[] Methods =
    [
        "add_auxiliary_pow",
        "add_listener",
        "calculate_pow",
        "check_for_update",
        "download_update",
        "flush_cache",
        "flush_tx_pool",
        "generate_blocks",
        "get_alt_block_hashes",
        "get_alt_chains",
        "get_block_by_hash",
        "get_block_by_height",
        "get_block_hash",
        "get_block_hashes",
        "get_block_header_by_hash",
        "get_block_header_by_height",
        "get_block_headers_by_range",
        "get_block_template",
        "get_blocks_by_hash",
        "get_blocks_by_height",
        "get_blocks_by_range",
        "get_blocks_by_range_chunked",
        "get_download_limit",
        "get_fee_estimate",
        "get_hard_fork_info",
        "get_height",
        "get_info",
        "get_key_image_spent_status",
        "get_key_image_spent_statuses",
        "get_known_peers",
        "get_last_block_header",
        "get_listeners",
        "get_miner_data",
        "get_miner_tx_sum",
        "get_mining_status",
        "get_network_stats",
        "get_output_distribution",
        "get_output_histogram",
        "get_output_indices",
        "get_outputs",
        "get_peer_ban",
        "get_peer_bans",
        "get_peers",
        "get_public_peers",
        "get_sync_info",
        "get_tx",
        "get_tx_hex",
        "get_tx_hexes",
        "get_tx_pool",
        "get_tx_pool_backlog",
        "get_tx_pool_hashes",
        "get_tx_pool_stats",
        "get_txs",
        "get_upload_limit",
        "get_version",
        "is_trusted",
        "pop_blocks",
        "prune_blockchain",
        "relay_tx_by_hash",
        "relay_txs_by_hash",
        "remove_bootstrap_daemon",
        "remove_listener",
        "remove_listeners",
        "reset_download_limit",
        "reset_upload_limit",
        "save_blockchain",
        "set_bootstrap_daemon",
        "set_download_limit",
        "set_incoming_peer_limit",
        "set_log_categories",
        "set_log_hash_rate",
        "set_log_level",
        "set_outgoing_peer_limit",
        "set_peer_ban",
        "set_peer_bans",
        "set_upload_limit",
        "start_mining",
        "stop",
        "stop_mining",
        "submit_block",
        "submit_blocks",
        "submit_tx_hex",
        "wait_for_next_block_header"
    ];

    // not async: they work on the list of listeners
    private static readonly string[] SyncMethods = ["add_listener", "remove_listener", "get_listeners", "remove_listeners"];

    // implemented with another method of the daemon
    private static readonly string[] DefaultMethods = ["get_tx", "get_tx_hex", "get_key_image_spent_status"];

    private static string CSharpName(string snakeName)
    {
        var name = string.Concat(snakeName.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return SyncMethods.Contains(snakeName) ? name : name + "Async";
    }

    private static MethodInfo[] PublicMethods() => typeof(MoneroDaemon)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Test]
    public void IsAbstract()
    {
        Assert.That(typeof(MoneroDaemon).IsAbstract, Is.True);
    }

    [TestCaseSource(nameof(Methods))]
    public void HasTheMethod(string name)
    {
        var methods = PublicMethods().Where(method => method.Name == CSharpName(name)).ToArray();

        Assert.That(methods, Is.Not.Empty, $"{name} is not in MoneroDaemon");
        if (DefaultMethods.Contains(name))
        {
            Assert.That(methods.All(method => method is { IsVirtual: true, IsAbstract: false }), Is.True, $"{name} has a default");
        }
        else
        {
            Assert.That(methods.All(method => method.IsAbstract), Is.True, $"{name} is abstract");
        }
    }

    [Test]
    public void HasNoUnexpectedMethod()
    {
        var expected = Methods.Select(CSharpName).ToHashSet();

        var extra = PublicMethods().Select(method => method.Name).Where(name => !expected.Contains(name)).Distinct().ToList();

        Assert.That(extra, Is.Empty);
    }

    [Test]
    public void FlushTxPoolHasThreeOverloads()
    {
        var parameters = PublicMethods()
            .Where(method => method.Name == nameof(MoneroDaemon.FlushTxPoolAsync))
            .Select(method => method.GetParameters().Select(parameter => parameter.ParameterType).First())
            .ToHashSet();

        Assert.That(parameters, Is.EquivalentTo(new[] { typeof(CancellationToken), typeof(string), typeof(IReadOnlyList<string>) }));
    }

    [Test]
    public void AsyncMethodsTakeACancellationTokenAtTheEnd()
    {
        foreach (var method in PublicMethods().Where(method => method.Name.EndsWith("Async")))
        {
            var last = method.GetParameters().Last();

            Assert.That(last.ParameterType, Is.EqualTo(typeof(CancellationToken)), method.Name);
            Assert.That(last.HasDefaultValue, Is.True, method.Name);
            Assert.That(typeof(Task).IsAssignableFrom(method.ReturnType), Is.True, method.Name);
        }
    }

    // the defaults

    [Test]
    public async Task GetTxAsksForTheOneHashAndReturnsTheFirst()
    {
        var daemon = new RecordingDaemon();
        daemon.Txs.Add(new MoneroTx { Hash = "aa" });
        daemon.Txs.Add(new MoneroTx { Hash = "bb" });

        var tx = await daemon.GetTxAsync("aa", true);

        Assert.That(tx!.Hash, Is.EqualTo("aa"));
        Assert.That(daemon.RequestedHashes, Is.EqualTo(new[] { "aa" }));
        Assert.That(daemon.RequestedPrune, Is.True);
    }

    [Test]
    public async Task GetTxIsNullWhenTheDaemonFindsNothing()
    {
        var daemon = new RecordingDaemon();

        var tx = await daemon.GetTxAsync("aa");

        Assert.That(tx, Is.Null);
        Assert.That(daemon.RequestedPrune, Is.False);
    }

    [Test]
    public async Task GetTxHexAsksForTheOneHashAndReturnsTheFirst()
    {
        var daemon = new RecordingDaemon();
        daemon.Hexes.Add("0102");
        daemon.Hexes.Add("0304");

        var hex = await daemon.GetTxHexAsync("aa", true);

        Assert.That(hex, Is.EqualTo("0102"));
        Assert.That(daemon.RequestedHashes, Is.EqualTo(new[] { "aa" }));
        Assert.That(daemon.RequestedPrune, Is.True);
    }

    [Test]
    public async Task GetTxHexIsNullWhenTheDaemonFindsNothing()
    {
        var daemon = new RecordingDaemon();

        Assert.That(await daemon.GetTxHexAsync("aa"), Is.Null);
    }

    [Test]
    public async Task GetKeyImageSpentStatusAsksForTheOneKeyImageAndReturnsTheFirst()
    {
        var daemon = new RecordingDaemon();
        daemon.Statuses.Add(MoneroKeyImageSpentStatus.TxPool);
        daemon.Statuses.Add(MoneroKeyImageSpentStatus.Confirmed);

        var status = await daemon.GetKeyImageSpentStatusAsync("ki");

        Assert.That(status, Is.EqualTo(MoneroKeyImageSpentStatus.TxPool));
        Assert.That(daemon.RequestedHashes, Is.EqualTo(new[] { "ki" }));
    }

    [Test]
    public void GetKeyImageSpentStatusThrowsWhenTheDaemonReturnsNoStatus()
    {
        var daemon = new RecordingDaemon();

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => daemon.GetKeyImageSpentStatusAsync("ki"));

        Assert.That(error!.Message, Is.EqualTo("Could not get key image spent status"));
    }

    [Test]
    public void TheDefaultsPassTheCancellationTokenOn()
    {
        var daemon = new RecordingDaemon();
        using var source = new CancellationTokenSource();

        Assert.DoesNotThrowAsync(() => daemon.GetTxAsync("aa", ct: source.Token));
        Assert.DoesNotThrowAsync(() => daemon.GetTxHexAsync("aa", ct: source.Token));
        Assert.That(daemon.Tokens, Is.All.EqualTo(source.Token));
        Assert.That(daemon.Tokens, Has.Count.EqualTo(2));
    }

    [Test]
    public void ADerivedClassCanOverrideADefault()
    {
        var daemon = new OverridingDaemon();

        Assert.That(daemon.GetTxAsync("aa").Result!.Hash, Is.EqualTo("overridden"));
    }

    /// <summary>
    /// A daemon that answers the three bulk methods that the defaults use, and records how it was asked.
    /// </summary>
    private class RecordingDaemon : StubDaemon
    {
        public List<MoneroTx> Txs { get; } = [];
        public List<string> Hexes { get; } = [];
        public List<MoneroKeyImageSpentStatus> Statuses { get; } = [];
        public IReadOnlyList<string>? RequestedHashes { get; private set; }
        public bool? RequestedPrune { get; private set; }
        public List<CancellationToken> Tokens { get; } = [];

        public override Task<List<MoneroTx>> GetTxsAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default)
        {
            RequestedHashes = txHashes;
            RequestedPrune = prune;
            Tokens.Add(ct);
            return Task.FromResult(Txs);
        }

        public override Task<List<string>> GetTxHexesAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default)
        {
            RequestedHashes = txHashes;
            RequestedPrune = prune;
            Tokens.Add(ct);
            return Task.FromResult(Hexes);
        }

        public override Task<List<MoneroKeyImageSpentStatus>> GetKeyImageSpentStatusesAsync(IReadOnlyList<string> keyImages, CancellationToken ct = default)
        {
            RequestedHashes = keyImages;
            return Task.FromResult(Statuses);
        }
    }

    private class OverridingDaemon : StubDaemon
    {
        public override Task<MoneroTx?> GetTxAsync(string txHash, bool prune = false, CancellationToken ct = default)
        {
            return Task.FromResult<MoneroTx?>(new MoneroTx { Hash = "overridden" });
        }
    }

    /// <summary>
    /// A daemon that implements every abstract method of <see cref="MoneroDaemon"/> by throwing.
    /// </summary>
    private abstract class StubDaemon : MoneroDaemon
    {
    public override void AddListener(MoneroDaemonListener listener) => throw new NotSupportedException();
    public override void RemoveListener(MoneroDaemonListener listener) => throw new NotSupportedException();
    public override List<MoneroDaemonListener> GetListeners() => throw new NotSupportedException();
    public override void RemoveListeners() => throw new NotSupportedException();
    public override Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<bool> IsTrustedAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<ulong> GetHeightAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroDaemonInfo> GetInfoAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroDaemonSyncInfo> GetSyncInfoAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroDaemonNetworkStats> GetNetworkStatsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroHardForkInfo> GetHardForkInfoAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroAltChain>> GetAltChainsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<string>> GetAltBlockHashesAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlockHeader> WaitForNextBlockHeaderAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<string> GetBlockHashAsync(ulong height, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlockHeader> GetLastBlockHeaderAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlockHeader> GetBlockHeaderByHashAsync(string hash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlockHeader> GetBlockHeaderByHeightAsync(ulong height, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroBlockHeader>> GetBlockHeadersByRangeAsync(ulong startHeight, ulong endHeight, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlock> GetBlockByHashAsync(string hash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlock> GetBlockByHeightAsync(ulong height, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroGetBlocksByHashResult> GetBlocksByHashAsync(IReadOnlyList<string> blockHashes, ulong startHeight, bool prune, ulong maxBlockCount = 0, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroBlock>> GetBlocksByHeightAsync(IReadOnlyList<ulong> heights, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroBlock>> GetBlocksByRangeAsync(ulong? startHeight, ulong? endHeight, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroBlock>> GetBlocksByRangeChunkedAsync(ulong? startHeight, ulong? endHeight, ulong? maxChunkSize = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroGetBlockHashesResult> GetBlockHashesAsync(IReadOnlyList<string> blockHashes, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroTx>> GetTxsAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<string>> GetTxHexesAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroSubmitTxResult> SubmitTxHexAsync(string txHex, bool doNotRelay = false, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task RelayTxByHashAsync(string txHash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task RelayTxsByHashAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroTx>> GetTxPoolAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<string>> GetTxPoolHashesAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroTxBacklogEntry>> GetTxPoolBacklogAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroTxPoolStats> GetTxPoolStatsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task FlushTxPoolAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task FlushTxPoolAsync(string hash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task FlushTxPoolAsync(IReadOnlyList<string> hashes, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroKeyImageSpentStatus>> GetKeyImageSpentStatusesAsync(IReadOnlyList<string> keyImages, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<ulong>> GetOutputIndicesAsync(string txHash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroOutput>> GetOutputsAsync(IReadOnlyList<MoneroOutput> outputs, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroOutputHistogramEntry>> GetOutputHistogramAsync(IReadOnlyList<ulong> amounts, int? minCount = null, int? maxCount = null, bool? isUnlocked = null, int? recentCutoff = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroOutputDistributionEntry>> GetOutputDistributionAsync(IReadOnlyList<ulong> amounts, bool? isCumulative = null, ulong? startHeight = null, ulong? endHeight = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroFeeEstimate> GetFeeEstimateAsync(ulong graceBlocks = 0, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBlockTemplate> GetBlockTemplateAsync(string walletAddress, int? reserveSize = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroMinerData> GetMinerDataAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<string> CalculatePowAsync(uint majorVersion, ulong height, string blockBlob, string seedHash, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroAddAuxiliaryPowResult> AddAuxiliaryPowAsync(string blockTemplateBlob, IReadOnlyList<MoneroAuxiliaryPow> auxPow, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroMinerTxSum> GetMinerTxSumAsync(ulong height, ulong numBlocks, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task StartMiningAsync(string address, ulong? numThreads = null, bool? isBackground = null, bool? ignoreBattery = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task StopMiningAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroMiningStatus> GetMiningStatusAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroGenerateBlocksResult> GenerateBlocksAsync(string walletAddress, ulong numBlocks, string? prevBlockHash = null, uint? startingNonce = null, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SubmitBlockAsync(string blockBlob, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SubmitBlocksAsync(IReadOnlyList<string> blockBlobs, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroPeer>> GetPeersAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroPeer>> GetKnownPeersAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroPeer>> GetPublicPeersAsync(bool includeOffline = false, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetOutgoingPeerLimitAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetIncomingPeerLimitAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<List<MoneroBan>> GetPeerBansAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroBan> GetPeerBanAsync(string address, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetPeerBansAsync(IReadOnlyList<MoneroBan> bans, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetPeerBanAsync(MoneroBan ban, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> GetDownloadLimitAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> SetDownloadLimitAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> ResetDownloadLimitAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> GetUploadLimitAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> SetUploadLimitAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<int> ResetUploadLimitAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroPruneResult> PruneBlockchainAsync(bool check, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SaveBlockchainAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<ulong> PopBlocksAsync(ulong numBlocks, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task FlushCacheAsync(bool badBlocks = false, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetBootstrapDaemonAsync(string address, string username = "", string password = "", string proxy = "", CancellationToken ct = default) => throw new NotSupportedException();
    public override Task RemoveBootstrapDaemonAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetLogHashRateAsync(bool isVisible, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task SetLogLevelAsync(int level, CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<string> SetLogCategoriesAsync(string categories = "", CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroDaemonUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public override Task<MoneroDaemonUpdateDownloadResult> DownloadUpdateAsync(string path = "", CancellationToken ct = default) => throw new NotSupportedException();
    public override Task StopAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
