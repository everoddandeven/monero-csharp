using Monero.Common;
using Monero.Daemon;
using Monero.Daemon.Model;
using Monero.IntegrationTests.Utils;

namespace Monero.IntegrationTests;

/// <summary>
/// Daemon RPC integration tests. They need the regtest network of <c>docker-compose.yml</c>.
/// </summary>
[TestFixture]
public class MoneroDaemonRpcIntegrationTests : BaseTestClass
{
    private static readonly BinaryBlockContext BinaryBlockCtx = new();

    private static MoneroDaemonRpc Daemon => TestUtils.GetDaemonRpc();

    protected override async Task BeforeAll()
    {
        // mine the blockchain until the minimum height
        await BlockchainUtils.SetupBlockchain();
    }

    // helpers

    /// <summary>
    /// The hashes of the confirmed txs to test: the ones of a wallet, if there are any, and the miner txs otherwise.
    /// </summary>
    private static async Task<(List<string> Hashes, bool AreMinerTxs)> GetTxHashesToTest()
    {
        var hashes = await DaemonUtils.GetConfirmedTxHashes(Daemon);
        if (hashes.Count > 0) return (hashes, false);

        return (await DaemonUtils.GetMinerTxHashes(Daemon), true);
    }

    /// <summary>
    /// Test a tx that the daemon returned.
    /// </summary>
    private static void TestConfirmedTx(MoneroTx? tx, bool isMinerTx, bool isPruned)
    {
        Assert.That(tx, Is.Not.Null);

        if (!isMinerTx)
        {
            TxUtils.TestTx(tx, new ModelTestContext { IsPruned = isPruned, IsConfirmed = true, FromGetTxPool = false });
            return;
        }

        // a miner tx has no fee, no inputs and no signatures, and it can't be tested like the others. The daemon doesn't say that it is a miner tx
        // when it is asked for it by hash: it has the outputs and the unlock time, and nothing else of the miner tx of a block
        Assert.That(tx!.Hash, Is.Not.Null.And.Length.EqualTo(64));
        Assert.That(tx.IsConfirmed, Is.True);
        Assert.That(tx.InTxPool, Is.False);
        Assert.That(tx.Block, Is.Not.Null);
        Assert.That(tx.Block!.Height, Is.GreaterThan(0UL));
        Assert.That(tx.Block.Txs, Does.Contain(tx));
        Assert.That(tx.Version, Is.Not.Null);
        Assert.That(tx.UnlockTime, Is.Not.Null);
        Assert.That(tx.Extra, Is.Not.Null.And.Not.Empty);
        Assert.That(tx.Outputs ?? [], Is.Not.Empty);
        Assert.That(tx.Inputs ?? [], Is.Empty);
        if (isPruned) Assert.That(tx.PrunedHex, Is.Not.Null);
    }

    // non relays tests

    // Test offline daemon connection
    [Test, Category(NonRelays)]
    public async Task TestOfflineDaemon()
    {
        // create connection to offline uri
        var offlineConnection = new MoneroRpcConnection(TestUtils.OfflineServerUri);

        // create daemon
        await using var daemon = new MoneroDaemonRpc(offlineConnection);

        // test daemon connection
        Assert.That(daemon.GetRpcConnection().Uri, Is.EqualTo(offlineConnection.Uri));
        Assert.That(daemon.IsConnected(), Is.False);

        // call to any daemon method should throw network error
        var error = Assert.ThrowsAsync<MoneroError>(() => daemon.GetHeightAsync());
        Assert.That(error!.Message, Is.EqualTo(RpcConnectionUtils.NetworkErrorMsg));

        error = Assert.ThrowsAsync<MoneroError>(() => daemon.StopAsync());
        Assert.That(error!.Message, Is.EqualTo("Network error: no response from offline_server_uri/stop_daemon (timeout: 180000ms)"));
    }

    // Can get the daemon's version
    [Test, Category(NonRelays)]
    public async Task TestGetVersion()
    {
        var version = await Daemon.GetVersionAsync();
        Assert.That(version.Number, Is.GreaterThan(0U));
        Assert.That(version.IsRelease, Is.Not.Null);
    }

    // Can indicate if it's trusted
    [Test, Category(NonRelays)]
    public async Task TestIsTrusted()
    {
        var isTrusted = await Daemon.IsTrustedAsync();
        TestContext.Progress.WriteLine($"Trusted daemon: {isTrusted}");
    }

    // Can get the blockchain height
    [Test, Category(NonRelays)]
    public async Task TestGetHeight()
    {
        var height = await Daemon.GetHeightAsync();
        Assert.That(height, Is.GreaterThan(0UL), "Height must be greater than 0");
    }

    // Can get a block hash by height
    [Test, Category(NonRelays)]
    public async Task TestGetBlockIdByHeight()
    {
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        var hash = await Daemon.GetBlockHashAsync(lastHeader.Height!.Value);
        Assert.That(hash, Has.Length.EqualTo(64), $"Invalid block hash '{hash}'");
    }

    // Can get a block template
    [Test, Category(NonRelays)]
    public async Task TestGetBlockTemplate()
    {
        var template = await Daemon.GetBlockTemplateAsync(TestUtils.Address, 2);
        BlockUtils.TestBlockTemplate(template);
    }

    // Can get the last block's header
    [Test, Category(NonRelays)]
    public async Task TestGetLastBlockHeader()
    {
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        BlockUtils.TestBlockHeader(lastHeader, true);
    }

    // Can get a block header by hash
    [Test, Category(NonRelays)]
    public async Task TestGetBlockHeaderByHash()
    {
        // retrieve last block by hash
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        var lastHeight = lastHeader.Height!.Value;
        var hash = await Daemon.GetBlockHashAsync(lastHeight);
        var header = await Daemon.GetBlockHeaderByHashAsync(hash);
        BlockUtils.TestBlockHeader(header, true);
        AssertUtils.AssertEquals(lastHeader, header);

        // retrieve by hash of previous to last block
        hash = await Daemon.GetBlockHashAsync(lastHeight - 1);
        header = await Daemon.GetBlockHeaderByHashAsync(hash);
        BlockUtils.TestBlockHeader(header, true);
        Assert.That(header.Height, Is.EqualTo(lastHeight - 1));
    }

    // Can get a block header by height
    [Test, Category(NonRelays)]
    public async Task TestGetBlockHeaderByHeight()
    {
        // retrieve by height of last block
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        var lastHeight = lastHeader.Height!.Value;
        var header = await Daemon.GetBlockHeaderByHeightAsync(lastHeight);
        BlockUtils.TestBlockHeader(header, true);
        AssertUtils.AssertEquals(lastHeader, header);

        // retrieve by height of previous to last block
        header = await Daemon.GetBlockHeaderByHeightAsync(lastHeight - 1);
        BlockUtils.TestBlockHeader(header, true);
        Assert.That(header.Height, Is.EqualTo(lastHeight - 1));
    }

    // Can get block headers by range
    // TODO: test start with no end, vice versa, inclusivity
    [Test, Category(NonRelays)]
    public async Task TestGetBlockHeadersByRange()
    {
        // determine start and end height based on number of blocks and how many blocks ago
        const ulong numBlocks = 100;
        const ulong numBlocksAgo = 100;
        var currentHeight = await Daemon.GetHeightAsync();
        var startHeight = currentHeight - numBlocksAgo;
        var endHeight = currentHeight - (numBlocksAgo - numBlocks) - 1;

        // fetch headers
        var headers = await Daemon.GetBlockHeadersByRangeAsync(startHeight, endHeight);

        // test headers
        Assert.That((ulong)headers.Count, Is.EqualTo(numBlocks));
        for (var i = 0; i < headers.Count; i++)
        {
            Assert.That(headers[i].Height, Is.EqualTo(startHeight + (ulong)i));
            BlockUtils.TestBlockHeader(headers[i], true);
        }
    }

    // Can get a block by hash
    [Test, Category(NonRelays)]
    public async Task TestGetBlockByHash()
    {
        // test config
        var ctx = new ModelTestContext { HasHex = true, HasTxs = false, HeaderIsFull = true };

        // retrieve by hash of last block
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        var lastHeight = lastHeader.Height!.Value;
        var hash = await Daemon.GetBlockHashAsync(lastHeight);
        var block = await Daemon.GetBlockByHashAsync(hash);
        BlockUtils.TestBlock(block, ctx);
        AssertUtils.AssertEquals(await Daemon.GetBlockByHeightAsync(block.Height!.Value), block);
        Assert.That(block.Txs ?? [], Is.Empty, $"No block tx expected, found: {block.Txs?.Count}");

        // retrieve by hash of previous to last block
        hash = await Daemon.GetBlockHashAsync(lastHeight - 1);
        block = await Daemon.GetBlockByHashAsync(hash);
        BlockUtils.TestBlock(block, ctx);
        AssertUtils.AssertEquals(await Daemon.GetBlockByHeightAsync(lastHeight - 1), block);
        Assert.That(block.Txs ?? [], Is.Empty, $"No block tx expected, found: {block.Txs?.Count}");
    }

    // Can get blocks by hash which includes transactions (binary)
    [Test, Category(NonRelays)]
    public async Task TestGetBlocksByHashBinary()
    {
        var chainHeight = await Daemon.GetHeightAsync();
        var genesisHash = await Daemon.GetBlockHashAsync(0);

        // a non-zero start height resumes from there and ignores the hash list
        var resumeHeight = chainHeight - 5;
        var result = await Daemon.GetBlocksByHashAsync([genesisHash], resumeHeight, false, 0);
        Assert.That(result.CurrentHeight, Is.GreaterThanOrEqualTo(chainHeight));
        Assert.That(result.Blocks, Is.Not.Null.And.Not.Empty);
        for (var i = 0; i < result.Blocks!.Count; i++)
        {
            Assert.That(result.Blocks[i].Height, Is.EqualTo(resumeHeight + (ulong)i));
            BlockUtils.TestBlock(result.Blocks[i], BinaryBlockCtx);
        }

        // a genesis-terminated history resumes from height 0, capped by max_block_count
        result = await Daemon.GetBlocksByHashAsync([genesisHash], 0, false, 10);
        Assert.That(result.Blocks, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Blocks!.Count, Is.LessThanOrEqualTo(10));
        Assert.That(result.Blocks[0].Height, Is.EqualTo(0UL));

        // noop when the first hash is already the tip
        var tip = await Daemon.GetLastBlockHeaderAsync();
        result = await Daemon.GetBlocksByHashAsync([tip.Hash!, genesisHash], 0, false, 0);
        Assert.That(result.Blocks ?? [], Is.Empty);
    }

    // Can get a block by height
    [Test, Category(NonRelays)]
    public async Task TestGetBlockByHeight()
    {
        // config for testing blocks
        var ctx = new ModelTestContext { HasHex = true, HeaderIsFull = true, HasTxs = false };

        // retrieve by height of last block
        var lastHeader = await Daemon.GetLastBlockHeaderAsync();
        var lastHeight = lastHeader.Height!.Value;
        var block = await Daemon.GetBlockByHeightAsync(lastHeight);
        BlockUtils.TestBlock(block, ctx);
        AssertUtils.AssertEquals(await Daemon.GetBlockByHeightAsync(block.Height!.Value), block);

        // retrieve by height of previous to last block
        block = await Daemon.GetBlockByHeightAsync(lastHeight - 1);
        BlockUtils.TestBlock(block, ctx);
        Assert.That(block.Height, Is.EqualTo(lastHeight - 1));
    }

    // Can get blocks by height which includes transactions (binary)
    [Test, Category(NonRelays)]
    public async Task TestGetBlocksByHeightBinary()
    {
        // set number of blocks to test
        const int numBlocks = 100;

        // select the last heights
        var currentHeight = await Daemon.GetHeightAsync();
        var endHeight = currentHeight - 1;
        var startHeight = endHeight - numBlocks;
        var heights = Enumerable.Range(0, numBlocks).Select(i => startHeight + (ulong)i).ToList();

        // fetch blocks
        var blocks = await Daemon.GetBlocksByHeightAsync(heights);

        // test blocks
        var txFound = false;
        Assert.That(blocks, Has.Count.EqualTo(numBlocks));
        for (var i = 0; i < heights.Count; i++)
        {
            if (blocks[i].Txs?.Count > 0) txFound = true;

            BlockUtils.TestBlock(blocks[i], BinaryBlockCtx);
            Assert.That(blocks[i].Height, Is.EqualTo(heights[i]));
        }

        // only a wallet can send transactions, so a fresh chain has only miner txs
        if (!txFound) TestContext.Progress.WriteLine("No transactions found to test: the blockchain has only miner transactions");
    }

    // Can get blocks by range in a single request
    [Test, Category(NonRelays)]
    public async Task TestGetBlocksByRange()
    {
        // get height range
        const ulong numBlocks = 100;
        const ulong numBlocksAgo = 102;
        var height = await Daemon.GetHeightAsync();
        var startHeight = height - numBlocksAgo;
        var endHeight = height - numBlocksAgo + numBlocks - 1;
        Assert.That(endHeight, Is.LessThan(height));

        // test known start and end heights
        await BlockUtils.TestGetBlocksRange(Daemon, startHeight, endHeight, height, false, BinaryBlockCtx);

        // test unspecified start
        await BlockUtils.TestGetBlocksRange(Daemon, null, numBlocks - 1, height, false, BinaryBlockCtx);

        // test unspecified end
        await BlockUtils.TestGetBlocksRange(Daemon, height - numBlocks - 1, null, height, false, BinaryBlockCtx);
    }

    // Test invalid blocks range
    [Test, Category(NonRelays)]
    public async Task TestGetBlocksByRangeInvalid()
    {
        var height = await Daemon.GetHeightAsync();
        var blocks = await Daemon.GetBlocksByRangeChunkedAsync(height, height - 10);
        Assert.That(blocks, Is.Empty, $"Expected empty blocks for invalid range request, got {blocks.Count}");
    }

    // Can get blocks by range using chunked requests
    [Test, Category(NonRelays)]
    public async Task TestGetBlocksByRangeChunked()
    {
        // get long height range
        var height = await Daemon.GetHeightAsync();
        var numBlocks = Math.Min(height - 2, 1440UL); // test up to ~2 days of blocks
        Assert.That(numBlocks, Is.GreaterThan(0UL));
        var startHeight = height - numBlocks;
        var endHeight = height - 1;

        // test known start and end heights
        await BlockUtils.TestGetBlocksRange(Daemon, startHeight, endHeight, height, true, BinaryBlockCtx);

        // test unspecified start
        await BlockUtils.TestGetBlocksRange(Daemon, null, numBlocks - 1, height, true, BinaryBlockCtx);

        // test unspecified end
        await BlockUtils.TestGetBlocksRange(Daemon, endHeight - numBlocks - 1, null, height, true, BinaryBlockCtx);
    }

    // Can get genesis block by range using chunked requests
    [Test, Category(NonRelays)]
    public async Task TestGetGenesisBlockByRangeChunked()
    {
        // get current chain height
        var chainHeight = await Daemon.GetHeightAsync();

        // test genesis
        await BlockUtils.TestGetBlocksRange(Daemon, 0, 0, chainHeight, true, BinaryBlockCtx);
    }

    // Can get block hashes (binary)
    [Test, Category(NonRelays)]
    public async Task TestGetBlockHashesBinary()
    {
        var chainHeight = await Daemon.GetHeightAsync();
        var genesisHash = await Daemon.GetBlockHashAsync(0);

        // a genesis-terminated history returns the whole chain from height 0
        var result = await Daemon.GetBlockHashesAsync([genesisHash]);
        Assert.That(result.StartHeight, Is.EqualTo(0UL));
        Assert.That(result.CurrentHeight, Is.GreaterThanOrEqualTo(chainHeight));
        Assert.That(result.Hashes, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Hashes![0], Is.EqualTo(genesisHash));
        Assert.That(result.Hashes, Is.All.Length.EqualTo(64));

        // only the tip comes back when it is already the first hash
        var tip = await Daemon.GetLastBlockHeaderAsync();
        result = await Daemon.GetBlockHashesAsync([tip.Hash!, genesisHash]);
        Assert.That(result.Hashes, Is.EqualTo(new[] { tip.Hash }));
    }

    // Can get a transaction by hash and without pruning
    [Test, Category(NonRelays)]
    public async Task TestGetTxByHash()
    {
        // fetch tx hashes to test
        var (txHashes, areMinerTxs) = await GetTxHashesToTest();
        Assert.That(txHashes, Is.Not.Empty);

        // fetch each tx by hash without pruning
        foreach (var txHash in txHashes)
        {
            var tx = await Daemon.GetTxAsync(txHash);
            TestConfirmedTx(tx, areMinerTxs, false);
            Assert.That(tx!.Hash, Is.EqualTo(txHash));
        }

        // fetch each tx by hash with pruning
        foreach (var txHash in txHashes)
        {
            var tx = await Daemon.GetTxAsync(txHash, true);
            TestConfirmedTx(tx, areMinerTxs, true);
        }

        // fetch invalid hash
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetTxAsync("invalid tx hash"));
        Assert.That(error!.Message, Is.EqualTo("Invalid transaction hash"));
    }

    // Can get transactions by hashes with and without pruning
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestGetTxsByHashes()
    {
        // fetch tx hashes to test
        var (txHashes, areMinerTxs) = await GetTxHashesToTest();
        Assert.That(txHashes, Is.Not.Empty, "No tx hashes found");

        // fetch each tx by hash without pruning
        var txs = await Daemon.GetTxsAsync(txHashes);
        Assert.That(txs, Has.Count.EqualTo(txHashes.Count));
        foreach (var tx in txs) TestConfirmedTx(tx, areMinerTxs, false);

        // the txs come in the order of the hashes
        Assert.That(txs.Select(tx => tx.Hash), Is.EqualTo(txHashes));

        // fetch each tx by hash with pruning
        txs = await Daemon.GetTxsAsync(txHashes, true);
        Assert.That(txs, Has.Count.EqualTo(txHashes.Count));
        foreach (var tx in txs) TestConfirmedTx(tx, areMinerTxs, true);

        // fetch missing hash
        var missingHash = new string('7', 64);
        Assert.That(await Daemon.GetTxAsync(missingHash), Is.Null);
        var numTxs = txs.Count;
        txs = await Daemon.GetTxsAsync([.. txHashes, missingHash]);
        Assert.That(txs, Has.Count.EqualTo(numTxs));

        // fetch invalid hash
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetTxsAsync(["invalid tx hash"]));
        Assert.That(error!.Message, Is.EqualTo("Invalid transaction hash"));
    }

    // Can get a transaction hex by hash with and without pruning
    [Test, Category(NonRelays)]
    public async Task TestGetTxHexByHash()
    {
        // fetch transaction hashes to test
        var (txHashes, _) = await GetTxHashesToTest();

        // fetch each tx hex by hash with and without pruning
        var hexes = new List<string?>();
        var hexesPruned = new List<string?>();
        foreach (var txHash in txHashes)
        {
            hexes.Add(await Daemon.GetTxHexAsync(txHash));
            hexesPruned.Add(await Daemon.GetTxHexAsync(txHash, true));
        }

        // test results
        Assert.That(hexes, Has.Count.EqualTo(txHashes.Count));
        Assert.That(hexesPruned, Has.Count.EqualTo(txHashes.Count));

        for (var i = 0; i < hexes.Count; i++)
        {
            Assert.That(hexes[i], Is.Not.Null);
            Assert.That(hexesPruned[i], Is.Not.Null.And.Not.Empty);
            // pruned hex is shorter
            Assert.That(hexes[i]!.Length, Is.GreaterThanOrEqualTo(hexesPruned[i]!.Length));
        }

        // fetch invalid hash
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetTxHexAsync("invalid tx hash"));
        Assert.That(error!.Message, Is.EqualTo("Invalid transaction hash"));
    }

    // Can get transaction hexes by hashes with and without pruning
    [Test, Category(NonRelays)]
    public async Task TestGetTxHexesByHashes()
    {
        // fetch transaction hashes to test
        var (txHashes, _) = await GetTxHashesToTest();

        // fetch tx hexes by hash with and without pruning
        var hexes = await Daemon.GetTxHexesAsync(txHashes);
        var hexesPruned = await Daemon.GetTxHexesAsync(txHashes, true);

        // test results
        Assert.That(hexes, Has.Count.EqualTo(txHashes.Count));
        Assert.That(hexesPruned, Has.Count.EqualTo(txHashes.Count));
        for (var i = 0; i < hexes.Count; i++)
        {
            Assert.That(hexes[i], Is.Not.Null);
            Assert.That(hexesPruned[i], Is.Not.Null.And.Not.Empty);
            // pruned hex is shorter
            Assert.That(hexes[i].Length, Is.GreaterThanOrEqualTo(hexesPruned[i].Length));
        }

        // fetch invalid hash
        txHashes.Add("invalid tx hash");
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetTxHexesAsync(txHashes));
        Assert.That(error!.Message, Is.EqualTo("Invalid transaction hash"));
    }

    // Can get the miner tx sum
    [Test, Category(NonRelays)]
    public async Task TestGetMinerTxSum()
    {
        var txSum = await Daemon.GetMinerTxSumAsync(0, Math.Min(5000, await Daemon.GetHeightAsync()));
        DaemonUtils.TestMinerTxSum(txSum);
    }

    // Can get fee estimate
    [Test, Category(NonRelays)]
    public async Task TestGetFeeEstimate()
    {
        var feeEstimate = await Daemon.GetFeeEstimateAsync();
        GenUtils.TestUnsignedBigInteger(feeEstimate.Fee, true);
        Assert.That(feeEstimate.Fees, Has.Count.EqualTo(4), "Expected 4 fees");
        foreach (var fee in feeEstimate.Fees!) GenUtils.TestUnsignedBigInteger(fee, true);
        GenUtils.TestUnsignedBigInteger(feeEstimate.QuantizationMask, true);
    }

    // Can get the transaction pool, which can be empty
    [Test, Category(NonRelays)]
    public async Task TestGetTxPool()
    {
        var txs = await Daemon.GetTxPoolAsync();
        var txHashes = await Daemon.GetTxPoolHashesAsync();
        var stats = await Daemon.GetTxPoolStatsAsync();

        var ctx = new ModelTestContext { IsPruned = false, IsConfirmed = false, FromGetTxPool = true };
        Assert.That(txs, Has.Count.EqualTo(txHashes.Count), $"Txs in pool {txs.Count} != Txs hashes in pool {txHashes.Count}");
        Assert.That(stats.NumTxs, Is.EqualTo(txs.Count));
        foreach (var tx in txs)
        {
            TxUtils.TestTx(tx, ctx);
            Assert.That(txHashes, Does.Contain(tx.Hash));
        }

        DaemonUtils.TestTxPoolStats(stats);
    }

    // Can flush all transactions from the pool
    [Test, Category(NonRelays)]
    public async Task TestFlushTxsFromPool()
    {
        await Daemon.FlushTxPoolAsync();
        Assert.That(await Daemon.GetTxPoolAsync(), Is.Empty);

        // flush what is not there
        await Daemon.FlushTxPoolAsync(new string('7', 64));
        await Daemon.FlushTxPoolAsync([new string('8', 64), new string('9', 64)]);
        Assert.That(await Daemon.GetTxPoolAsync(), Is.Empty);
    }

    // Can get the spent status of key images
    [Test, Category(NonRelays)]
    public async Task TestGetSpentStatusOfKeyImages()
    {
        // an image that no tx spent
        await DaemonUtils.TestSpentStatuses(Daemon, [new string('1', 64), new string('2', 64)], MoneroKeyImageSpentStatus.NotSpent);

        // collect key images of confirmed txs, which only a wallet sends
        var keyImages = new List<string>();
        foreach (var tx in await DaemonUtils.GetConfirmedTxs(Daemon, 10))
        {
            foreach (var input in tx.Inputs ?? []) keyImages.Add(input.KeyImage!.Hex!);
        }

        if (keyImages.Count == 0) TestContext.Progress.WriteLine("No confirmed txs to read key images from: the blockchain has only miner transactions");

        // key images are all spent
        await DaemonUtils.TestSpentStatuses(Daemon, keyImages, MoneroKeyImageSpentStatus.Confirmed);
    }

    // Can get an output histogram (binary)
    [Test, Category(NonRelays)]
    public async Task TestGetOutputHistogramBinary()
    {
        var entries = await Daemon.GetOutputHistogramAsync([]);
        Assert.That(entries, Is.Not.Empty);
        foreach (var entry in entries) OutputUtils.TestOutputHistogramEntry(entry);
    }

    // Can get an output distribution
    [Test, Category(NonRelays)]
    public async Task TestGetOutputDistribution()
    {
        ulong[] amounts = [0, 1, 10, 100, 1000, 10000, 100000, 1000000];
        var entries = await Daemon.GetOutputDistributionAsync(amounts);
        foreach (var entry in entries) OutputUtils.TestOutputDistributionEntry(entry);
    }

    // Can get general information
    [Test, Category(NonRelays)]
    public async Task TestGetGeneralInformation()
    {
        var info = await Daemon.GetInfoAsync();
        DaemonUtils.TestInfo(info);
        Assert.That(info.IsRegtest, Is.EqualTo(TestUtils.Regtest));
    }

    // Can get sync information
    [Test, Category(NonRelays)]
    public async Task TestGetSyncInformation()
    {
        var syncInfo = await Daemon.GetSyncInfoAsync();
        DaemonUtils.TestSyncInfo(syncInfo);
    }

    // Can get hard fork information
    [Test, Category(NonRelays)]
    public async Task TestGetHardForkInformation()
    {
        var hardForkInfo = await Daemon.GetHardForkInfoAsync();
        DaemonUtils.TestHardForkInfo(hardForkInfo);
    }

    // Can get alternative chains
    [Test, Category(NonRelays)]
    public async Task TestGetAlternativeChains()
    {
        var altChains = await Daemon.GetAltChainsAsync();
        if (altChains.Count == 0) TestContext.Progress.WriteLine("No alternative chain found!");

        foreach (var altChain in altChains) BlockUtils.TestAltChain(altChain);
    }

    // Can get alternative block hashes
    [Test, Category(NonRelays)]
    public async Task TestGetAlternativeBlockIds()
    {
        var altBlockIds = await Daemon.GetAltBlockHashesAsync();
        if (altBlockIds.Count == 0) TestContext.Progress.WriteLine("No alternative block ids found!");

        foreach (var altBlockId in altBlockIds) Assert.That(altBlockId, Has.Length.EqualTo(64));
    }

    // Can get, set, and reset a download bandwidth limit
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestSetDownloadBandwidth()
    {
        var initVal = await Daemon.GetDownloadLimitAsync();
        Assert.That(initVal, Is.GreaterThan(0));
        var setVal = initVal * 2;
        await Daemon.SetDownloadLimitAsync(setVal);
        Assert.That(await Daemon.GetDownloadLimitAsync(), Is.EqualTo(setVal));
        var resetVal = await Daemon.ResetDownloadLimitAsync();
        Assert.That(resetVal, Is.EqualTo(initVal));

        // test invalid limits
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.SetDownloadLimitAsync(0));
        Assert.That(error!.Message, Is.EqualTo("Download limit must be an integer greater than 0"));

        Assert.That(await Daemon.GetDownloadLimitAsync(), Is.EqualTo(initVal));
    }

    // Can get, set, and reset an upload bandwidth limit
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestSetUploadBandwidth()
    {
        var initVal = await Daemon.GetUploadLimitAsync();
        Assert.That(initVal, Is.GreaterThan(0));
        var setVal = initVal * 2;
        await Daemon.SetUploadLimitAsync(setVal);
        Assert.That(await Daemon.GetUploadLimitAsync(), Is.EqualTo(setVal));
        var resetVal = await Daemon.ResetUploadLimitAsync();
        Assert.That(resetVal, Is.EqualTo(initVal));

        // test invalid limits
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.SetUploadLimitAsync(0));
        Assert.That(error!.Message, Is.EqualTo("Upload limit must be an integer greater than 0"));

        Assert.That(await Daemon.GetUploadLimitAsync(), Is.EqualTo(initVal));
    }

    // Can get peers with active incoming or outgoing connections
    [Test, Category(NonRelays)]
    public async Task TestGetPeers()
    {
        var peers = await Daemon.GetPeersAsync();
        Assert.That(peers, Is.Not.Empty, "Daemon has no incoming or outgoing peers to test");

        var seen = new HashSet<(string, int)>();
        foreach (var peer in peers)
        {
            var key = (peer.Host!, peer.Port!.Value);
            Assert.That(seen.Add(key), Is.True, $"Duplicate peer found: {key}");
            Assert.That(peer.IsOnline, Is.True, "Expected active peer connection to be online");
            DaemonUtils.TestPeer(peer);
        }
    }

    // Can get all known peers which may be online or offline
    [Test, Category(NonRelays)]
    public async Task TestGetKnownPeers()
    {
        var peers = await Daemon.GetKnownPeersAsync();
        if (TestUtils.Regtest)
        {
            Assert.That(peers, Is.Empty, "Regtest daemon should not have known peers to test");
        }
        else
        {
            Assert.That(peers, Is.Not.Empty, "Daemon has no known peers to test");
        }

        foreach (var peer in peers) DaemonUtils.TestKnownPeer(peer, false);
    }

    // Can limit the number of outgoing peers
    [Test, Category(NonRelays)]
    public async Task TestSetOutgoingPeerLimit()
    {
        await Daemon.SetOutgoingPeerLimitAsync(0);
        await Daemon.SetOutgoingPeerLimitAsync(8);
        await Daemon.SetOutgoingPeerLimitAsync(10);
    }

    // Can limit the number of incoming peers
    [Test, Category(NonRelays)]
    public async Task TestSetIncomingPeerLimit()
    {
        await Daemon.SetIncomingPeerLimitAsync(0);
        await Daemon.SetIncomingPeerLimitAsync(8);
        await Daemon.SetIncomingPeerLimitAsync(10);
    }

    // Can notify listeners when a new block is added to the chain
    [Test, Category(NonRelays), Category(Notifications)]
    public async Task TestBlockListener()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        try
        {
            // start mining if possible to help push the network along
            try
            {
                await Daemon.StartMiningAsync(TestUtils.MiningAddress, 1, false, true);
            }
            catch (Exception e)
            {
                TestContext.Progress.WriteLine($"[!]: {e.Message}");
            }

            // register a listener
            var listener = new MoneroDaemonListener();
            Daemon.AddListener(listener);

            // wait for next block notification
            var header = await Daemon.WaitForNextBlockHeaderAsync(timeout.Token);
            Daemon.RemoveListener(listener); // unregister listener so daemon does not keep polling
            BlockUtils.TestBlockHeader(header, true);

            // test that listener was called with equivalent header
            AssertUtils.AssertEquals(header, listener.LastHeader);
        }
        finally
        {
            // stop mining
            try
            {
                await Daemon.StopMiningAsync();
            }
            catch (Exception e)
            {
                TestContext.Progress.WriteLine($"[!]: {e.Message}");
            }
        }
    }

    // Can ban a peer
    [Test, Category(NonRelays)]
    public async Task TestBanPeer()
    {
        // set ban
        const string host = "192.168.1.51";
        await Daemon.SetPeerBanAsync(new MoneroBan { Host = host, IsBanned = true, Seconds = 60 });

        // test ban
        var bans = await Daemon.GetPeerBansAsync();
        var found = false;
        foreach (var peerBan in bans)
        {
            DaemonUtils.TestBan(peerBan);
            if (host == peerBan.Host) found = true;
        }

        Assert.That(found, Is.True, $"Could not find peer ban {host}");
    }

    // Can ban peers
    [Test, Category(NonRelays)]
    public async Task TestBanPeers()
    {
        // set bans
        const string addr1 = "192.168.1.52";
        const string addr2 = "192.168.1.53";
        var ban1 = new MoneroBan { Host = addr1, IsBanned = true, Seconds = 60 };
        var ban2 = new MoneroBan { Host = addr2, IsBanned = true, Seconds = 60 };
        await Daemon.SetPeerBansAsync([ban1, ban2]);

        // test bans
        var bans = await Daemon.GetPeerBansAsync();
        var found1 = false;
        var found2 = false;
        foreach (var aBan in bans)
        {
            DaemonUtils.TestBan(aBan);
            if (addr1 == aBan.Host) found1 = true;
            if (addr2 == aBan.Host) found2 = true;
        }

        Assert.That(found1, Is.True, $"Could not find peer ban1 {addr1}");
        Assert.That(found2, Is.True, $"Could not find peer ban2 {addr2}");
    }

    // Can start and stop mining
    [Test, Category(NonRelays)]
    public async Task TestMining()
    {
        // stop mining at beginning of test
        await MiningUtils.TryStopMining(Daemon);

        // start mining to the address of the mining wallet
        await Daemon.StartMiningAsync(TestUtils.MiningAddress, 1, false, true);

        // stop mining
        await Daemon.StopMiningAsync();
    }

    // Can get mining status
    [Test, Category(NonRelays)]
    public async Task TestGetMiningStatus()
    {
        try
        {
            // stop mining at beginning of test
            await MiningUtils.TryStopMining(Daemon);

            // test status without mining
            var status = await Daemon.GetMiningStatusAsync();
            Assert.That(status.IsActive, Is.False);
            Assert.That(status.Address, Is.Null, $"Mining address is not null: {status.Address}");
            Assert.That(status.Speed, Is.EqualTo(0UL));
            Assert.That(status.NumThreads, Is.EqualTo(0));
            Assert.That(status.IsBackground, Is.Null);

            // test status with mining
            const int threadCount = 1;
            const bool isBackground = false;
            await Daemon.StartMiningAsync(TestUtils.MiningAddress, threadCount, isBackground, true);
            status = await Daemon.GetMiningStatusAsync();
            Assert.That(status.Speed, Is.Not.Null);
            Assert.That(status.IsActive, Is.True);
            Assert.That(status.Address, Is.EqualTo(TestUtils.MiningAddress));
            Assert.That(status.NumThreads, Is.EqualTo(threadCount));
            Assert.That(status.IsBackground, Is.EqualTo(isBackground));
        }
        finally
        {
            // stop mining at end of test
            await MiningUtils.TryStopMining(Daemon);
        }
    }

    // Can submit a mined block to the network
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestSubmitMinedBlock()
    {
        // get template to mine on
        var template = await Daemon.GetBlockTemplateAsync(TestUtils.Address);
        Assert.That(template.BlockTemplateBlob, Is.Not.Null);

        // TODO monero rpc: way to get mining nonce when found in order to submit?

        // try to submit block hashing blob without nonce
        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.SubmitBlockAsync(template.BlockTemplateBlob!));
        Assert.That(error!.Message, Is.EqualTo("Block not accepted"));
    }

    // Can prune the blockchain
    [Test, Category(NonRelays)]
    public async Task TestPruneBlockchain()
    {
        var result = await Daemon.PruneBlockchainAsync(true);

        if (result.IsPruned == true)
        {
            Assert.That(result.PruningSeed, Is.GreaterThan(0));
        }
        else
        {
            Assert.That(result.PruningSeed, Is.EqualTo(0));
        }
    }

    // Can check for an update
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestCheckForUpdate()
    {
        var result = await DaemonUtils.RequireUpdateServers(() => Daemon.CheckForUpdateAsync());
        DaemonUtils.TestUpdateCheckResult(result);
    }

    // Can download an update
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestDownloadUpdate()
    {
        // download to default path
        var result = await DaemonUtils.RequireUpdateServers(() => Daemon.DownloadUpdateAsync());
        DaemonUtils.TestUpdateDownloadResult(result);

        // download to defined path
        var path = $"test_download_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.tar.bz2";
        result = await Daemon.DownloadUpdateAsync(path);
        DaemonUtils.TestUpdateDownloadResult(result);

        // test invalid path
        // TODO monerod: an invalid path causes a 500 in daemon rpc rather than a clean error
        if (result.IsUpdateAvailable == true)
        {
            Assert.ThrowsAsync<MoneroError>(() => Daemon.DownloadUpdateAsync("./ohhai/there"));
        }
    }

    // Can be stopped
    [Test, Category(NonRelays), Ignore("test is disabled to not interfere with other tests")]
    public async Task TestStop()
    {
        // stop the daemon
        await Daemon.StopAsync();

        // give the daemon time to shut down
        await Task.Delay(5000);

        // try to interact with the stopped daemon
        Assert.ThrowsAsync<MoneroError>(() => Daemon.GetHeightAsync());
    }

    // Can get the data needed to mine a block
    [Test, Category(NonRelays)]
    public async Task TestGetMinerData()
    {
        var data = await Daemon.GetMinerDataAsync();
        DaemonUtils.TestMinerData(data);
    }

    // Can calculate a block's proof-of-work hash
    [Test, Category(NonRelays)]
    public async Task TestCalculatePow()
    {
        var data = await Daemon.GetMinerDataAsync();
        var template = await Daemon.GetBlockTemplateAsync(TestUtils.Address);
        Assert.That(template.BlockTemplateBlob, Is.Not.Null);

        var powHash = await Daemon.CalculatePowAsync(data.MajorVersion!.Value, data.Height!.Value, template.BlockTemplateBlob!, data.SeedHash!);
        Assert.That(powHash, Has.Length.EqualTo(64));

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.CalculatePowAsync(data.MajorVersion.Value, data.Height.Value, "", data.SeedHash!));
        Assert.That(error!.Message, Is.EqualTo("Must provide a block blob to hash"));
    }

    // Can add auxiliary proof-of-work to a block template
    [Test, Category(NonRelays)]
    public async Task TestAddAuxiliaryPow()
    {
        var template = await Daemon.GetBlockTemplateAsync(TestUtils.Address);
        Assert.That(template.BlockTemplateBlob, Is.Not.Null);

        var auxPow = new MoneroAuxiliaryPow { Id = new string('1', 64), Hash = new string('2', 64) };
        var result = await Daemon.AddAuxiliaryPowAsync(template.BlockTemplateBlob!, [auxPow]);
        Assert.That(result.BlockTemplateBlob, Is.Not.Null.And.Not.Empty);
        Assert.That(result.BlockHashingBlob, Is.Not.Null.And.Not.Empty);
        Assert.That(result.MerkleRoot, Is.Not.Null.And.Length.EqualTo(64));
        Assert.That(result.MerkleTreeDepth, Is.Not.Null);
        Assert.That(result.AuxPow, Has.Count.EqualTo(1));
        Assert.That(result.AuxPow![0].Id, Is.Not.Null.And.Length.EqualTo(64));
        Assert.That(result.AuxPow[0].Hash, Is.Not.Null.And.Length.EqualTo(64));

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.AddAuxiliaryPowAsync(template.BlockTemplateBlob!, []));
        Assert.That(error!.Message, Is.EqualTo("Must provide auxiliary proof of work to add"));
    }

    // Can get the global output indices of a transaction
    [Test, Category(NonRelays)]
    public async Task TestGetOutputIndices()
    {
        var (txHashes, _) = await GetTxHashesToTest();
        Assert.That(txHashes, Is.Not.Empty, "No confirmed txs found");

        foreach (var txHash in txHashes)
        {
            var indices = await Daemon.GetOutputIndicesAsync(txHash);
            Assert.That(indices, Is.Not.Empty);
        }

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetOutputIndicesAsync(""));
        Assert.That(error!.Message, Is.EqualTo("Must provide a transaction hash"));
    }

    // Can get outputs given a list of output amounts and indices (binary)
    [Test, Category(NonRelays)]
    public async Task TestGetOutputs()
    {
        var (txHashes, _) = await GetTxHashesToTest();
        Assert.That(txHashes, Is.Not.Empty, "No confirmed txs found");

        // identify each output by amount 0 (RingCT) and its global index
        var requested = new List<MoneroOutput>();
        foreach (var txHash in txHashes)
        {
            foreach (var index in await Daemon.GetOutputIndicesAsync(txHash))
            {
                requested.Add(new MoneroOutput { Amount = 0, Index = index });
            }
        }

        Assert.That(requested, Is.Not.Empty);

        var fetched = await Daemon.GetOutputsAsync(requested);
        Assert.That(fetched, Has.Count.EqualTo(requested.Count));

        for (var i = 0; i < fetched.Count; i++)
        {
            var output = fetched[i];
            Assert.That(output.Index, Is.EqualTo(requested[i].Index));
            Assert.That(output.StealthPublicKey, Is.Not.Null.And.Length.EqualTo(64));
            Assert.That(output.Mask, Is.Not.Null.And.Length.EqualTo(64));
            // the tx and the block height of the output are not in the JSON of this call
        }

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetOutputsAsync([]));
        Assert.That(error!.Message, Is.EqualTo("Must provide outputs to fetch"));
    }

    // Can get network (bandwidth) statistics
    [Test, Category(NonRelays)]
    public async Task TestGetNetworkStats()
    {
        var stats = await Daemon.GetNetworkStatsAsync();
        DaemonUtils.TestNetworkStats(stats);
    }

    // Can get public nodes known to the daemon
    [Test, Category(NonRelays)]
    public async Task TestGetPublicPeers()
    {
        var peers = await Daemon.GetPublicPeersAsync();
        if (TestUtils.Regtest) Assert.That(peers, Is.Empty);

        foreach (var peer in peers) DaemonUtils.TestKnownPeer(peer, false);
    }

    // Can query the ban status of a single peer
    [Test, Category(NonRelays)]
    public async Task TestGetPeerBan()
    {
        const string host = "192.168.1.60";
        await Daemon.SetPeerBanAsync(new MoneroBan { Host = host, IsBanned = true, Seconds = 60 });

        var banned = await Daemon.GetPeerBanAsync(host);
        Assert.That(banned.IsBanned, Is.True);
        Assert.That(banned.Seconds, Is.GreaterThan(0UL));

        Assert.That((await Daemon.GetPeerBanAsync("192.168.1.61")).IsBanned, Is.False);

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.GetPeerBanAsync(""));
        Assert.That(error!.Message, Is.EqualTo("Must provide an address to check the ban status of"));
    }

    // Can remove all listeners at once
    [Test, Category(NonRelays)]
    public void TestRemoveDaemonListeners()
    {
        var listeners = new[] { new DaemonNotificationCollector(Daemon), new DaemonNotificationCollector(Daemon) };
        foreach (var listener in listeners) Daemon.AddListener(listener);
        Assert.That(Daemon.GetListeners(), Has.Count.EqualTo(2));

        Daemon.RemoveListeners();
        Assert.That(Daemon.GetListeners(), Is.Empty);
    }

    // Can flush the blockchain to disk
    [Test, Category(NonRelays)]
    public async Task TestSaveBlockchain()
    {
        await Daemon.SaveBlockchainAsync();
    }

    // Can flush the daemon's invalid block and tx caches
    [Test, Category(NonRelays)]
    public async Task TestFlushCache()
    {
        await Daemon.FlushCacheAsync();
        await Daemon.FlushCacheAsync(true);
    }

    // Can disable the bootstrap daemon
    [Test, Category(NonRelays)]
    public async Task TestRemoveBootstrapDaemon()
    {
        await Daemon.RemoveBootstrapDaemonAsync();
    }

    // Can set the poll period for block notifications
    [Test, Category(NonRelays)]
    public void TestSetPollPeriodInMs()
    {
        try
        {
            Daemon.SetPollPeriodInMs(1000);
        }
        finally
        {
            // restore the configured period so later tests poll at the expected rate
            Daemon.SetPollPeriodInMs(TestUtils.DaemonPollPeriodInMs);
        }
    }

    // Can set the daemon's log level
    [Test, Category(NonRelays)]
    public async Task TestSetLogLevel()
    {
        try
        {
            for (var i = 0; i < 5; i++) await Daemon.SetLogLevelAsync(i);

            foreach (var level in new[] { -1, 5 })
            {
                var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.SetLogLevelAsync(level));
                Assert.That(error!.Message, Is.EqualTo("Log level must be an integer between 0 and 4"));
            }
        }
        finally
        {
            // restore the level from docker-compose so later tests keep full logs
            await Daemon.SetLogLevelAsync(TestUtils.DaemonLogLevel);
        }
    }

    // Can set the daemon's log categories
    [Test, Category(NonRelays)]
    public async Task TestSetLogCategories()
    {
        try
        {
            var categories = await Daemon.SetLogCategoriesAsync("*:WARNING");
            Assert.That(categories, Is.Not.Null);
        }
        finally
        {
            // re-applying the configured level also resets categories to its mapping
            await Daemon.SetLogLevelAsync(TestUtils.DaemonLogLevel);
        }
    }

    // Can toggle the mining hash rate in the daemon log
    [Test, Category(NonRelays)]
    public async Task TestSetLogHashRate()
    {
        try
        {
            await Daemon.SetLogHashRateAsync(true);
            await Daemon.SetLogHashRateAsync(false);
        }
        catch (Exception e)
        {
            // daemon rejects the call while not mining
            TestContext.Progress.WriteLine($"set_log_hash_rate: {e.Message}");
        }
    }

    // Can pop blocks from the top of the chain
    [Test, Category(NonRelays), Retry(5)]
    public async Task TestPopBlocks()
    {
        if (!TestUtils.Regtest) Assert.Ignore("REGTEST disabled");

        // stop mining so it doesn't race the pop, then restore the shared chain afterwards
        await MiningUtils.TryStopMining(Daemon);
        var height = await Daemon.GetHeightAsync();

        Assert.That(await Daemon.PopBlocksAsync(1), Is.EqualTo(height - 1));
        await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, 1, Daemon);
        Assert.That(await Daemon.GetHeightAsync(), Is.EqualTo(height));

        var error = Assert.ThrowsAsync<MoneroError>(() => Daemon.PopBlocksAsync(0));
        Assert.That(error!.Message, Is.EqualTo("Must provide a number of blocks to pop greater than 0"));
    }

    // TODO: relay tests and tx pool tests, they need a wallet to send transactions

    // notifications tests

    // Can start and stop listening
    [Test, Category(Notifications)]
    public async Task TestStartStopListening()
    {
        // add listener
        var listener = new DaemonNotificationCollector(Daemon);
        var autoRemoveListener = new DaemonNotificationCollector(Daemon, true);
        Daemon.AddListener(listener);
        Daemon.AddListener(autoRemoveListener);
        Assert.That(Daemon.GetListeners(), Has.Count.EqualTo(2));

        // mine blocks and collect notifications
        await BlockchainUtils.WaitForBlocks(10);

        // the daemon polls: wait for the notifications of the blocks
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(TestUtils.DaemonPollPeriodInMs * 3);
        while ((listener.NumBlockHeaders == 0 || autoRemoveListener.NumBlockHeaders == 0) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
        }

        // test notifications
        Assert.That(listener.Errors, Is.Empty);
        Assert.That(listener.NumBlockHashes, Is.GreaterThan(0), "Daemon listener didn't collect any block hash");
        Assert.That(listener.NumBlockHeaders, Is.GreaterThan(0), "Daemon listener didn't collect any block header");
        Assert.That(listener.NumBlockHeaders, Is.EqualTo(listener.NumBlockHashes), $"Count mismatch {listener.NumBlockHashes} != {listener.NumBlockHeaders}");

        // test autoremove listener
        Assert.That(autoRemoveListener.NumBlockHeaders, Is.EqualTo(1));
        Assert.That(autoRemoveListener.NumBlockHashes, Is.EqualTo(1));
        Assert.That(Daemon.GetListeners(), Has.Count.EqualTo(1));

        // remove listener
        Daemon.RemoveListener(listener);
        Assert.That(Daemon.GetListeners(), Is.Empty);
    }
}
