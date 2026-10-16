using Monero.Daemon.Model;

namespace Monero.Daemon;

/// <summary>
/// Monero daemon interface. Methods that talk to the node are async.
/// </summary>
public abstract class MoneroDaemon
{
    // listeners

    /// <summary>
    /// Register a listener to receive daemon notifications.
    /// </summary>
    /// <param name="listener">the listener to register.</param>
    public abstract void AddListener(MoneroDaemonListener listener);

    /// <summary>
    /// Unregister a listener to receive daemon notifications.
    /// </summary>
    /// <param name="listener">a previously registered listener to be unregistered.</param>
    public abstract void RemoveListener(MoneroDaemonListener listener);

    /// <summary>
    /// Get the listeners registered with the daemon.
    /// </summary>
    /// <returns>the registered listeners.</returns>
    public abstract List<MoneroDaemonListener> GetListeners();

    /// <summary>
    /// Unregister all listeners registered with the daemon.
    /// </summary>
    public abstract void RemoveListeners();

    // node, version and chain

    /// <summary>
    /// Gets the version of the daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the version of the daemon.</returns>
    public abstract Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the daemon is trusted or untrusted.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the daemon is trusted, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsTrustedAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the number of blocks in the longest chain known to the node.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the number of blocks.</returns>
    public abstract Task<ulong> GetHeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Get general information about the state of the node and the network.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>general information about the node and network.</returns>
    public abstract Task<MoneroDaemonInfo> GetInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Get synchronization information.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the sync information.</returns>
    public abstract Task<MoneroDaemonSyncInfo> GetSyncInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Get network (bandwidth) statistics since the daemon started.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's network statistics.</returns>
    public abstract Task<MoneroDaemonNetworkStats> GetNetworkStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Look up information regarding hard fork voting and readiness.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hard fork information.</returns>
    public abstract Task<MoneroHardForkInfo> GetHardForkInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Get alternative chains seen by the node.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the alternative chains seen by the node.</returns>
    public abstract Task<List<MoneroAltChain>> GetAltChainsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get known block hashes which are not on the main chain.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the known block hashes which are not on the main chain.</returns>
    public abstract Task<List<string>> GetAltBlockHashesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the header of the next block added to the chain.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the header of the next block added to the chain.</returns>
    public abstract Task<MoneroBlockHeader> WaitForNextBlockHeaderAsync(CancellationToken ct = default);

    // blocks

    /// <summary>
    /// Get a block's hash by its height.
    /// </summary>
    /// <param name="height">is the height of the block hash to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block's hash at the given height.</returns>
    public abstract Task<string> GetBlockHashAsync(ulong height, CancellationToken ct = default);

    /// <summary>
    /// Get the last block's header.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the last block's header.</returns>
    public abstract Task<MoneroBlockHeader> GetLastBlockHeaderAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a block header by its hash.
    /// </summary>
    /// <param name="hash">is the hash of the block to get the header of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block's header.</returns>
    public abstract Task<MoneroBlockHeader> GetBlockHeaderByHashAsync(string hash, CancellationToken ct = default);

    /// <summary>
    /// Get a block header by its height.
    /// </summary>
    /// <param name="height">is the height of the block to get the header of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block's header.</returns>
    public abstract Task<MoneroBlockHeader> GetBlockHeaderByHeightAsync(ulong height, CancellationToken ct = default);

    /// <summary>
    /// Get block headers for the given range.
    /// </summary>
    /// <param name="startHeight">is the start height lower bound inclusive.</param>
    /// <param name="endHeight">is the end height upper bound inclusive.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block headers in the given range.</returns>
    public abstract Task<List<MoneroBlockHeader>> GetBlockHeadersByRangeAsync(ulong startHeight, ulong endHeight, CancellationToken ct = default);

    /// <summary>
    /// Get a block by hash.
    /// </summary>
    /// <param name="hash">is the hash of the block to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block with the given hash.</returns>
    public abstract Task<MoneroBlock> GetBlockByHashAsync(string hash, CancellationToken ct = default);

    /// <summary>
    /// Get a block by height.
    /// </summary>
    /// <param name="height">is the height of the block to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block at the given height.</returns>
    public abstract Task<MoneroBlock> GetBlockByHeightAsync(ulong height, CancellationToken ct = default);

    /// <summary>
    /// Get blocks by hash.
    /// </summary>
    /// <param name="blockHashes">a short chain history; first 10 block hashes go sequential, next go in pow(2,n) offset, like 2, 4, 8, 16, 32, 64
    /// and so on, and the last one is always the genesis block.</param>
    /// <param name="startHeight">the start height to resume from; when non-zero the daemon skips the search for the last common block and uses it as-is.</param>
    /// <param name="prune">whether returned blocks should be pruned.</param>
    /// <param name="maxBlockCount">caps how many blocks the daemon returns in one call (0 leaves it to the daemon's own default limit).</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the retrieved blocks plus the daemon's current chain height.</returns>
    public abstract Task<MoneroGetBlocksByHashResult> GetBlocksByHashAsync(IReadOnlyList<string> blockHashes, ulong startHeight, bool prune, ulong maxBlockCount = 0, CancellationToken ct = default);

    /// <summary>
    /// Get blocks at the given heights.
    /// </summary>
    /// <param name="heights">are the heights of the blocks to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blocks at the given heights.</returns>
    public abstract Task<List<MoneroBlock>> GetBlocksByHeightAsync(IReadOnlyList<ulong> heights, CancellationToken ct = default);

    /// <summary>
    /// Get blocks in the given height range.
    /// </summary>
    /// <param name="startHeight">is the start height lower bound inclusive, or <c>null</c> for the first block.</param>
    /// <param name="endHeight">is the end height upper bound inclusive, or <c>null</c> for the last block.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blocks in the given height range.</returns>
    public abstract Task<List<MoneroBlock>> GetBlocksByRangeAsync(ulong? startHeight, ulong? endHeight, CancellationToken ct = default);

    /// <summary>
    /// Get blocks in the given height range as chunked requests so that each request is not too big.
    /// </summary>
    /// <param name="startHeight">is the start height lower bound inclusive, or <c>null</c> for the first block.</param>
    /// <param name="endHeight">is the end height upper bound inclusive, or <c>null</c> for the last block.</param>
    /// <param name="maxChunkSize">is the maximum chunk size in any one request, or <c>null</c> for the default of 3,000,000 bytes.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blocks in the given height range.</returns>
    public abstract Task<List<MoneroBlock>> GetBlocksByRangeChunkedAsync(ulong? startHeight, ulong? endHeight, ulong? maxChunkSize = null, CancellationToken ct = default);

    /// <summary>
    /// Get block hashes as a binary request to the daemon.
    /// </summary>
    /// <param name="blockHashes">a short chain history; first 10 block hashes go sequential, next go in pow(2,n) offset, like 2, 4, 8, 16, 32, 64
    /// and so on, and the last one is always the genesis block, which is required or the request fails.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the requested hashes plus their start height and the daemon's current chain height.</returns>
    public abstract Task<MoneroGetBlockHashesResult> GetBlockHashesAsync(IReadOnlyList<string> blockHashes, CancellationToken ct = default);

    // transactions

    /// <summary>
    /// Get a transaction by hash.
    /// The default asks <see cref="GetTxsAsync"/> for the one hash, and returns the first transaction that comes back.
    /// </summary>
    /// <param name="txHash">is the hash of the transaction to get.</param>
    /// <param name="prune">specifies if the returned tx should be pruned.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction with the given hash, or <c>null</c> if not found.</returns>
    public virtual async Task<MoneroTx?> GetTxAsync(string txHash, bool prune = false, CancellationToken ct = default)
    {
        var txs = await GetTxsAsync([txHash], prune, ct).ConfigureAwait(false);
        return txs.Count > 0 ? txs[0] : null;
    }

    /// <summary>
    /// Get transactions by hashes.
    /// </summary>
    /// <param name="txHashes">are hashes of transactions to get.</param>
    /// <param name="prune">specifies if the returned txs should be pruned.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the found transactions with the given hashes.</returns>
    public abstract Task<List<MoneroTx>> GetTxsAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default);

    /// <summary>
    /// Get a transaction hex by hash.
    /// The default asks <see cref="GetTxHexesAsync"/> for the one hash, and returns the first hex that comes back.
    /// </summary>
    /// <param name="txHash">is the hash of the transaction to get hex from.</param>
    /// <param name="prune">specifies if the returned tx hex should be pruned.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx hex with the given hash, or <c>null</c> if not found.</returns>
    public virtual async Task<string?> GetTxHexAsync(string txHash, bool prune = false, CancellationToken ct = default)
    {
        var hexes = await GetTxHexesAsync([txHash], prune, ct).ConfigureAwait(false);
        return hexes.Count > 0 ? hexes[0] : null;
    }

    /// <summary>
    /// Get transaction hexes by hashes.
    /// </summary>
    /// <param name="txHashes">are hashes of transactions to get hexes from.</param>
    /// <param name="prune">specifies if the returned tx hexes should be pruned.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx hexes.</returns>
    public abstract Task<List<string>> GetTxHexesAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default);

    /// <summary>
    /// Submits a transaction to the daemon's pool.
    /// </summary>
    /// <param name="txHex">is the raw transaction hex to submit.</param>
    /// <param name="doNotRelay">specifies if the transaction should be kept in the pool without relaying it to the network.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the submission results.</returns>
    public abstract Task<MoneroSubmitTxResult> SubmitTxHexAsync(string txHex, bool doNotRelay = false, CancellationToken ct = default);

    /// <summary>
    /// Relays a transaction by hash.
    /// </summary>
    /// <param name="txHash">identifies the transaction to relay.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task RelayTxByHashAsync(string txHash, CancellationToken ct = default);

    /// <summary>
    /// Relays transactions by hash.
    /// </summary>
    /// <param name="txHashes">identify the transactions to relay.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task RelayTxsByHashAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default);

    /// <summary>
    /// Get valid transactions seen by the node but not yet mined into a block, as well as spent key image information for the tx pool.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transactions in the transaction pool.</returns>
    public abstract Task<List<MoneroTx>> GetTxPoolAsync(CancellationToken ct = default);

    /// <summary>
    /// Get hashes of transactions in the transaction pool.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hashes of transactions in the transaction pool.</returns>
    public abstract Task<List<string>> GetTxPoolHashesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all transaction pool backlog.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction pool backlog entries.</returns>
    public abstract Task<List<MoneroTxBacklogEntry>> GetTxPoolBacklogAsync(CancellationToken ct = default);

    /// <summary>
    /// Get transaction pool statistics.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the statistics about the transaction pool.</returns>
    public abstract Task<MoneroTxPoolStats> GetTxPoolStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Flush all transactions from the tx pool.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task FlushTxPoolAsync(CancellationToken ct = default);

    /// <summary>
    /// Flush a transaction from the tx pool.
    /// </summary>
    /// <param name="hash">is the hash of the transaction to flush from the tx pool.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task FlushTxPoolAsync(string hash, CancellationToken ct = default);

    /// <summary>
    /// Flush transactions from the tx pool.
    /// </summary>
    /// <param name="hashes">are hashes of transactions to flush.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task FlushTxPoolAsync(IReadOnlyList<string> hashes, CancellationToken ct = default);

    // key images and outputs

    /// <summary>
    /// Get the spent status of the given key image.
    /// The default asks <see cref="GetKeyImageSpentStatusesAsync"/> for the one key image, and returns the first status that comes back.
    /// </summary>
    /// <param name="keyImage">is key image hex to get the status of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the status of the key image.</returns>
    /// <exception cref="InvalidOperationException">the daemon returned no status.</exception>
    public virtual async Task<MoneroKeyImageSpentStatus> GetKeyImageSpentStatusAsync(string keyImage, CancellationToken ct = default)
    {
        var statuses = await GetKeyImageSpentStatusesAsync([keyImage], ct).ConfigureAwait(false);
        if (statuses.Count == 0) throw new InvalidOperationException("Could not get key image spent status");
        return statuses[0];
    }

    /// <summary>
    /// Get the spent status of each given key image.
    /// </summary>
    /// <param name="keyImages">are hex key images to get the statuses of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the spent status for each key image.</returns>
    public abstract Task<List<MoneroKeyImageSpentStatus>> GetKeyImageSpentStatusesAsync(IReadOnlyList<string> keyImages, CancellationToken ct = default);

    /// <summary>
    /// Get the global output index of each output in a transaction.
    /// </summary>
    /// <param name="txHash">the hash of the transaction to get output indices for.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the global output index of each output in the transaction, in order.</returns>
    public abstract Task<List<ulong>> GetOutputIndicesAsync(string txHash, CancellationToken ct = default);

    /// <summary>
    /// Get outputs identified by a list of output amounts and indices as a binary request.
    /// </summary>
    /// <param name="outputs">identify each output by amount and index.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the identified outputs.</returns>
    public abstract Task<List<MoneroOutput>> GetOutputsAsync(IReadOnlyList<MoneroOutput> outputs, CancellationToken ct = default);

    /// <summary>
    /// Get a histogram of output amounts. For all amounts (possibly filtered by parameters), gives the number of outputs on the chain for that
    /// amount. RingCT outputs counts as 0 amount.
    /// </summary>
    /// <param name="amounts">are amounts of outputs to make the histogram with (atomic units).</param>
    /// <param name="minCount">filters amounts with at least this number of outputs.</param>
    /// <param name="maxCount">filters amounts with at most this number of outputs.</param>
    /// <param name="isUnlocked">makes a histogram with outputs with the specified lock state.</param>
    /// <param name="recentCutoff">excludes outputs from older than this timestamp.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the output histogram entries meeting the parameters.</returns>
    public abstract Task<List<MoneroOutputHistogramEntry>> GetOutputHistogramAsync(IReadOnlyList<ulong> amounts, int? minCount = null, int? maxCount = null, bool? isUnlocked = null, int? recentCutoff = null, CancellationToken ct = default);

    /// <summary>
    /// Creates an output distribution.
    /// </summary>
    /// <param name="amounts">are amounts of outputs to make the distribution with.</param>
    /// <param name="isCumulative">specifies if the results should be cumulative.</param>
    /// <param name="startHeight">is the start height lower bound inclusive.</param>
    /// <param name="endHeight">is the end height upper bound inclusive.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the output distribution entries meeting the parameters.</returns>
    public abstract Task<List<MoneroOutputDistributionEntry>> GetOutputDistributionAsync(IReadOnlyList<ulong> amounts, bool? isCumulative = null, ulong? startHeight = null, ulong? endHeight = null, CancellationToken ct = default);

    // fees

    /// <summary>
    /// Get mining fee estimates per kB.
    /// </summary>
    /// <param name="graceBlocks">number of blocks we want the fee estimate to be valid for.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the mining fee estimates per kB.</returns>
    public abstract Task<MoneroFeeEstimate> GetFeeEstimateAsync(ulong graceBlocks = 0, CancellationToken ct = default);

    // mining

    /// <summary>
    /// Get a block template for mining a new block.
    /// </summary>
    /// <param name="walletAddress">is the address of the wallet to receive miner transactions if block is successfully mined.</param>
    /// <param name="reserveSize">is the reserve size.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>a block template for mining a new block.</returns>
    public abstract Task<MoneroBlockTemplate> GetBlockTemplateAsync(string walletAddress, int? reserveSize = null, CancellationToken ct = default);

    /// <summary>
    /// Get the data needed to construct a block template for mining, e.g. for use by a pool that assembles its own block templates.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the current data for mining a new block.</returns>
    public abstract Task<MoneroMinerData> GetMinerDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Calculate the proof-of-work hash of a mined block.
    /// </summary>
    /// <param name="majorVersion">the block's major version.</param>
    /// <param name="height">the block's height.</param>
    /// <param name="blockBlob">the block's blob to hash.</param>
    /// <param name="seedHash">the seed hash used to select the RandomX dataset/cache.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the block's proof-of-work hash.</returns>
    public abstract Task<string> CalculatePowAsync(uint majorVersion, ulong height, string blockBlob, string seedHash, CancellationToken ct = default);

    /// <summary>
    /// Add auxiliary proof-of-work to a block template for merge mining.
    /// </summary>
    /// <param name="blockTemplateBlob">the block template blob to add auxiliary PoW to.</param>
    /// <param name="auxPow">identifies each auxiliary chain by id and its block's PoW hash.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the updated block template along with the (possibly reordered) auxiliary PoW.</returns>
    public abstract Task<MoneroAddAuxiliaryPowResult> AddAuxiliaryPowAsync(string blockTemplateBlob, IReadOnlyList<MoneroAuxiliaryPow> auxPow, CancellationToken ct = default);

    /// <summary>
    /// Gets the total emissions and fees from the genesis block to the current height.
    /// </summary>
    /// <param name="height">is the height to start computing the miner sum.</param>
    /// <param name="numBlocks">are the number of blocks to include in the sum.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the sum of emission and fees since the genesis block.</returns>
    public abstract Task<MoneroMinerTxSum> GetMinerTxSumAsync(ulong height, ulong numBlocks, CancellationToken ct = default);

    /// <summary>
    /// Start mining.
    /// </summary>
    /// <param name="address">is the address given miner rewards if the daemon mines a block.</param>
    /// <param name="numThreads">is the number of mining threads to run.</param>
    /// <param name="isBackground">specifies if the miner should run in the background or not.</param>
    /// <param name="ignoreBattery">specifies if the battery state (e.g. on laptop) should be ignored or not.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StartMiningAsync(string address, ulong? numThreads = null, bool? isBackground = null, bool? ignoreBattery = null, CancellationToken ct = default);

    /// <summary>
    /// Stop mining.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StopMiningAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the daemon's mining status.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's mining status.</returns>
    public abstract Task<MoneroMiningStatus> GetMiningStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Generate blocks to a wallet address (regtest only).
    /// </summary>
    /// <param name="walletAddress">is the address of the wallet to receive miner transactions if block is successfully mined.</param>
    /// <param name="numBlocks">is the number of blocks to generate.</param>
    /// <param name="prevBlockHash">is the hash of the previous block to build on top of, or <c>null</c> to build on the current tip.</param>
    /// <param name="startingNonce">is the starting nonce to use.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of generating blocks; the height is the one of the last block generated.</returns>
    public abstract Task<MoneroGenerateBlocksResult> GenerateBlocksAsync(string walletAddress, ulong numBlocks, string? prevBlockHash = null, uint? startingNonce = null, CancellationToken ct = default);

    /// <summary>
    /// Submit a mined block to the network.
    /// </summary>
    /// <param name="blockBlob">is the mined block to submit.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SubmitBlockAsync(string blockBlob, CancellationToken ct = default);

    /// <summary>
    /// Submit mined blocks to the network.
    /// </summary>
    /// <param name="blockBlobs">are the mined blocks to submit.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SubmitBlocksAsync(IReadOnlyList<string> blockBlobs, CancellationToken ct = default);

    // peers and bandwidth

    /// <summary>
    /// Get peers with active incoming or outgoing connections to the node.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's peers.</returns>
    public abstract Task<List<MoneroPeer>> GetPeersAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all known peers including their last known online status.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's known peers.</returns>
    public abstract Task<List<MoneroPeer>> GetKnownPeersAsync(CancellationToken ct = default);

    /// <summary>
    /// Get public nodes known to the daemon.
    /// </summary>
    /// <param name="includeOffline">specifies if offline nodes should be included.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's known public nodes.</returns>
    public abstract Task<List<MoneroPeer>> GetPublicPeersAsync(bool includeOffline = false, CancellationToken ct = default);

    /// <summary>
    /// Limit number of outgoing peers.
    /// </summary>
    /// <param name="limit">is the maximum number of outgoing peers.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetOutgoingPeerLimitAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Limit number of incoming peers.
    /// </summary>
    /// <param name="limit">is the maximum number of incoming peers.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetIncomingPeerLimitAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Get peer bans.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the entries about banned peers.</returns>
    public abstract Task<List<MoneroBan>> GetPeerBansAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the ban status of a peer node.
    /// </summary>
    /// <param name="address">the address of the peer node to check, e.g. "1.2.3.4" or "1.2.3.4:18080".</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the peer's ban status.</returns>
    public abstract Task<MoneroBan> GetPeerBanAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Ban peers nodes.
    /// </summary>
    /// <param name="bans">are bans to apply against peer nodes.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetPeerBansAsync(IReadOnlyList<MoneroBan> bans, CancellationToken ct = default);

    /// <summary>
    /// Ban a peer node.
    /// </summary>
    /// <param name="ban">contains information about a node to ban.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetPeerBanAsync(MoneroBan ban, CancellationToken ct = default);

    /// <summary>
    /// Get the download bandwidth limit.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the download bandwidth limit.</returns>
    public abstract Task<int> GetDownloadLimitAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the download bandwidth limit.
    /// </summary>
    /// <param name="limit">is the download limit to set (<c>-1</c> to reset to default).</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the new download limit after setting.</returns>
    public abstract Task<int> SetDownloadLimitAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Reset the download bandwidth limit.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the download bandwidth limit after resetting.</returns>
    public abstract Task<int> ResetDownloadLimitAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the upload bandwidth limit.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the upload bandwidth limit.</returns>
    public abstract Task<int> GetUploadLimitAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the upload bandwidth limit.
    /// </summary>
    /// <param name="limit">is the upload limit to set (<c>-1</c> to reset to default).</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the new upload limit after setting.</returns>
    public abstract Task<int> SetUploadLimitAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Reset the upload bandwidth limit.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the upload bandwidth limit after resetting.</returns>
    public abstract Task<int> ResetUploadLimitAsync(CancellationToken ct = default);

    // blockchain maintenance

    /// <summary>
    /// Prune the blockchain.
    /// </summary>
    /// <param name="check">specifies to check the pruning.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the prune result.</returns>
    public abstract Task<MoneroPruneResult> PruneBlockchainAsync(bool check, CancellationToken ct = default);

    /// <summary>
    /// Save (flush) the blockchain to disk.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SaveBlockchainAsync(CancellationToken ct = default);

    /// <summary>
    /// Pop (remove) blocks from the top of the blockchain.
    /// </summary>
    /// <param name="numBlocks">the number of blocks to pop.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blockchain height after popping the blocks.</returns>
    public abstract Task<ulong> PopBlocksAsync(ulong numBlocks, CancellationToken ct = default);

    /// <summary>
    /// Flush the daemon's invalid block and transaction caches.
    /// </summary>
    /// <param name="badBlocks">specifies to also flush the bad blocks cache.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task FlushCacheAsync(bool badBlocks = false, CancellationToken ct = default);

    // configuration, logs and updates

    /// <summary>
    /// Set the bootstrap daemon used by the daemon to serve requests while it is not fully synced, e.g. a remote node.
    /// </summary>
    /// <param name="address">the bootstrap daemon's address (host:port), "auto" to select a public node automatically, or an empty string to disable
    /// the bootstrap daemon.</param>
    /// <param name="username">the username to authenticate with the bootstrap daemon.</param>
    /// <param name="password">the password to authenticate with the bootstrap daemon.</param>
    /// <param name="proxy">the proxy used to reach the bootstrap daemon, e.g. a SOCKS proxy.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetBootstrapDaemonAsync(string address, string username = "", string password = "", string proxy = "", CancellationToken ct = default);

    /// <summary>
    /// Disable the bootstrap daemon so the daemon no longer falls back to it.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task RemoveBootstrapDaemonAsync(CancellationToken ct = default);

    /// <summary>
    /// Show or hide the mining hash rate in the daemon's console log.
    /// </summary>
    /// <param name="isVisible">specifies if the hash rate should be logged.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetLogHashRateAsync(bool isVisible, CancellationToken ct = default);

    /// <summary>
    /// Set the daemon's log level.
    /// </summary>
    /// <param name="level">the log level to set, from 0 (least verbose) to 4 (most verbose).</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetLogLevelAsync(int level, CancellationToken ct = default);

    /// <summary>
    /// Set the daemon's log categories.
    /// </summary>
    /// <param name="categories">the log categories to set, e.g. "*:WARNING,net.p2p:DEBUG" (an empty string resets categories to the default).</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the daemon's resulting log categories.</returns>
    public abstract Task<string> SetLogCategoriesAsync(string categories = "", CancellationToken ct = default);

    /// <summary>
    /// Check for update.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the update check.</returns>
    public abstract Task<MoneroDaemonUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>
    /// Download an update.
    /// </summary>
    /// <param name="path">the download path, or an empty string for the default one.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the update download.</returns>
    public abstract Task<MoneroDaemonUpdateDownloadResult> DownloadUpdateAsync(string path = "", CancellationToken ct = default);

    /// <summary>
    /// Safely disconnect and shut down the daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StopAsync(CancellationToken ct = default);
}
