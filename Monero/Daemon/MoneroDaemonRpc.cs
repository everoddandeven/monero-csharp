using Monero.Common;
using Monero.Daemon.Model;
using Monero.Native;

namespace Monero.Daemon;

/// <summary>
/// Implements a <see cref="MoneroDaemon"/> using <see href="https://docs.getmonero.org/rpc-library/monerod-rpc/">monerod-rpc</see>, through monero-c.
/// Dispose it to release the native handle.
/// </summary>
public class MoneroDaemonRpc : MoneroDaemon, IAsyncDisposable, IDisposable
{
    private readonly DaemonHandle _handle;
    private readonly NativeWorker _worker = new("MoneroDaemonRpc");
    private readonly object _lock = new();
    private readonly List<MoneroDaemonListener> _listeners = [];
    private readonly List<TaskCompletionSource<MoneroBlockHeader>> _waiters = [];
    private DaemonEvents? _events;
    private bool _nativeListenerAdded;
    private bool _disposed;

    /// <summary>
    /// Initialize a daemon that connects with the given RPC connection.
    /// </summary>
    /// <param name="rpc">the RPC connection of the daemon.</param>
    /// <exception cref="MoneroError">the native library is not found, or it can't make the connection.</exception>
    public MoneroDaemonRpc(MoneroRpcConnection rpc)
    {
        ArgumentNullException.ThrowIfNull(rpc);
        Check.EnsureLibrary();

        Check.Ok(NativeMethods.monero_rpc_connection_create(MoneroJson.Serialize(rpc), out var connection));
        using (connection)
        {
            Check.Ok(NativeMethods.monero_daemon_connect_with(connection, out _handle));
        }
    }

    /// <summary>
    /// Initialize a daemon that connects to the given URI.
    /// </summary>
    /// <param name="uri">the daemon RPC URI.</param>
    /// <param name="username">the username to authenticate with, or an empty string for none.</param>
    /// <param name="password">the password to authenticate with, or an empty string for none.</param>
    /// <param name="proxyUri">the URI of a proxy to connect through, or an empty string for none.</param>
    /// <param name="zmqUri">the ZMQ URI of the daemon, or an empty string for none.</param>
    /// <param name="timeoutMs">the time to wait for a response of the server, in milliseconds, or <c>null</c> for the default of 20000.</param>
    /// <exception cref="MoneroError">the native library is not found, or it can't make the connection.</exception>
    public MoneroDaemonRpc(string uri, string username = "", string password = "", string proxyUri = "", string zmqUri = "", uint? timeoutMs = null)
        : this(new MoneroRpcConnection(uri, NullIfEmpty(username), NullIfEmpty(password))
        {
            ProxyUri = NullIfEmpty(proxyUri),
            ZmqUri = NullIfEmpty(zmqUri),
            TimeoutMs = timeoutMs,
        })
    {
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    // connection

    /// <summary>
    /// Get the daemon's RPC connection, as it is now: a copy, with the last known state of the server.
    /// </summary>
    /// <returns>the daemon's RPC connection.</returns>
    public MoneroRpcConnection GetRpcConnection()
    {
        return Check.Json<MoneroRpcConnection>(NativeMethods.monero_daemon_get_rpc_connection(_handle, out var json), json);
    }

    /// <summary>
    /// Indicates if the client is connected to the daemon via RPC: the server answered the last request, and it accepted the credentials.
    /// </summary>
    /// <returns><c>true</c> if the client is connected to the daemon, <c>false</c> otherwise.</returns>
    public bool IsConnected()
    {
        var connection = GetRpcConnection();
        return connection.IsOnline == true && connection.IsAuthenticated != false;
    }

    /// <summary>
    /// Set the period between polls of the daemon for block notifications.
    /// </summary>
    /// <param name="periodMs">the poll period in milliseconds, from 1 to 2147483647.</param>
    public void SetPollPeriodInMs(ulong periodMs)
    {
        Check.Ok(NativeMethods.monero_daemon_set_poll_period(_handle, periodMs));
    }

    // listeners

    /// <inheritdoc />
    public override void AddListener(MoneroDaemonListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_lock)
        {
            ThrowIfDisposed();
            if (_listeners.Contains(listener)) return;

            _listeners.Add(listener);
            try
            {
                UpdateNativeListener();
            }
            catch
            {
                _listeners.Remove(listener);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public override void RemoveListener(MoneroDaemonListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_lock)
        {
            if (_disposed || !_listeners.Remove(listener)) return;
            UpdateNativeListener();
        }
    }

    /// <inheritdoc />
    public override List<MoneroDaemonListener> GetListeners()
    {
        lock (_lock)
        {
            return [.. _listeners];
        }
    }

    /// <inheritdoc />
    public override void RemoveListeners()
    {
        lock (_lock)
        {
            if (_disposed) return;

            _listeners.Clear();
            UpdateNativeListener();
        }
    }

    // the daemon polls while a native listener is registered, so there is one while a listener or a waiter exists
    private void UpdateNativeListener()
    {
        var needed = _listeners.Count + _waiters.Count > 0;

        if (needed && !_nativeListenerAdded)
        {
            _events ??= new DaemonEvents(OnBlockHeader);
            Check.Ok(NativeMethods.monero_daemon_add_listener(_handle, _events.Handle));
            _nativeListenerAdded = true;
        }
        else if (!needed && _nativeListenerAdded)
        {
            Check.Ok(NativeMethods.monero_daemon_remove_listener(_handle, _events!.Handle));
            _nativeListenerAdded = false;
        }
    }

    // runs on the thread pool, one header after the other
    private void OnBlockHeader(MoneroBlockHeader header)
    {
        MoneroDaemonListener[] listeners;
        TaskCompletionSource<MoneroBlockHeader>[] waiters;

        lock (_lock)
        {
            listeners = [.. _listeners];
            waiters = [.. _waiters];
            _waiters.Clear();
        }

        foreach (var listener in listeners)
        {
            try
            {
                listener.OnBlockHeader(header);
            }
            catch
            {
                // a listener that throws doesn't stop the others
            }
        }

        foreach (var waiter in waiters) waiter.TrySetResult(header);

        if (waiters.Length > 0)
        {
            lock (_lock)
            {
                if (!_disposed) UpdateNativeListener();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    // worker

    private Task<T> Run<T>(Func<DaemonHandle, T> call, CancellationToken ct) => _worker.RunAsync(() => call(_handle), ct);

    private Task Run(Action<DaemonHandle> call, CancellationToken ct) => _worker.RunAsync(() => call(_handle), ct);

    // the txs and the hexes of a query by hashes come in the order of the blocks: sort them by the hashes that were asked for
    private static List<MoneroTx> SortByHashes(List<MoneroTx> txs, IReadOnlyList<string> hashes)
    {
        var position = new Dictionary<string, int>();
        for (var i = 0; i < hashes.Count; i++) position.TryAdd(hashes[i], i);

        return [.. txs.OrderBy(tx => tx.Hash != null && position.TryGetValue(tx.Hash, out var i) ? i : int.MaxValue)];
    }

    // general

    /// <inheritdoc />
    public override Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroVersion>(NativeMethods.monero_daemon_get_version(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<bool> IsTrustedAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_is_trusted(h, out var trusted));
            return trusted;
        }, ct);

    /// <inheritdoc />
    public override Task<ulong> GetHeightAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_get_height(h, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task<MoneroDaemonInfo> GetInfoAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroDaemonInfo>(NativeMethods.monero_daemon_get_info(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroDaemonSyncInfo> GetSyncInfoAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroDaemonSyncInfo>(NativeMethods.monero_daemon_get_sync_info(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroDaemonNetworkStats> GetNetworkStatsAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroDaemonNetworkStats>(NativeMethods.monero_daemon_get_network_stats(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroHardForkInfo> GetHardForkInfoAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroHardForkInfo>(NativeMethods.monero_daemon_get_hard_fork_info(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroAltChain>> GetAltChainsAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroAltChain>>(NativeMethods.monero_daemon_get_alt_chains(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<string>> GetAltBlockHashesAsync(CancellationToken ct = default) =>
        Run(h => Check.Strings(NativeMethods.monero_daemon_get_alt_block_hashes(h, out var json), json), ct);

    /// <summary>
    /// Get the header of the next block added to the chain.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the header of the next block added to the chain.</returns>
    public override async Task<MoneroBlockHeader> WaitForNextBlockHeaderAsync(CancellationToken ct = default)
    {
        var waiter = new TaskCompletionSource<MoneroBlockHeader>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            ThrowIfDisposed();
            _waiters.Add(waiter);
            try
            {
                UpdateNativeListener();
            }
            catch
            {
                _waiters.Remove(waiter);
                throw;
            }
        }

        await using var registration = ct.Register(() =>
        {
            waiter.TrySetCanceled(ct);
            lock (_lock)
            {
                if (_waiters.Remove(waiter) && !_disposed) UpdateNativeListener();
            }
        }).ConfigureAwait(false);

        return await waiter.Task.ConfigureAwait(false);
    }

    // blocks

    /// <inheritdoc />
    public override Task<string> GetBlockHashAsync(ulong height, CancellationToken ct = default) =>
        Run(h => Check.Text(NativeMethods.monero_daemon_get_block_hash(h, height, out var hash), hash) ?? throw new MoneroError("The daemon returned no hash"), ct);

    /// <inheritdoc />
    public override Task<MoneroBlockHeader> GetLastBlockHeaderAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlockHeader>(NativeMethods.monero_daemon_get_last_block_header(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroBlockHeader> GetBlockHeaderByHashAsync(string hash, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlockHeader>(NativeMethods.monero_daemon_get_block_header_by_hash(h, hash, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroBlockHeader> GetBlockHeaderByHeightAsync(ulong height, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlockHeader>(NativeMethods.monero_daemon_get_block_header_by_height(h, height, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroBlockHeader>> GetBlockHeadersByRangeAsync(ulong startHeight, ulong endHeight, CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroBlockHeader>>(NativeMethods.monero_daemon_get_block_headers_by_range(h, startHeight, endHeight, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroBlock> GetBlockByHashAsync(string hash, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlock>(NativeMethods.monero_daemon_get_block_by_hash(h, hash, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroBlock> GetBlockByHeightAsync(ulong height, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlock>(NativeMethods.monero_daemon_get_block_by_height(h, height, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroGetBlocksByHashResult> GetBlocksByHashAsync(IReadOnlyList<string> blockHashes, ulong startHeight, bool prune, ulong maxBlockCount = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(blockHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(blockHashes);
            return Check.Json<MoneroGetBlocksByHashResult>(
                NativeMethods.monero_daemon_get_blocks_by_hash(h, hashes.Pointer, hashes.Count, startHeight, prune, maxBlockCount, out var json), json);
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroBlock>> GetBlocksByHeightAsync(IReadOnlyList<ulong> heights, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(heights);
        var array = heights.ToArray();

        return Run(h =>
        {
            unsafe
            {
                fixed (ulong* ptr = array)
                {
                    return Check.Json<List<MoneroBlock>>(NativeMethods.monero_daemon_get_blocks_by_height(h, ptr, (nuint)array.Length, out var json), json);
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroBlock>> GetBlocksByRangeAsync(ulong? startHeight, ulong? endHeight, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var start = startHeight.GetValueOrDefault();
                var end = endHeight.GetValueOrDefault();
                return Check.Json<List<MoneroBlock>>(
                    NativeMethods.monero_daemon_get_blocks_by_range(h, startHeight.HasValue ? &start : null, endHeight.HasValue ? &end : null, out var json), json);
            }
        }, ct);

    /// <inheritdoc />
    public override Task<List<MoneroBlock>> GetBlocksByRangeChunkedAsync(ulong? startHeight, ulong? endHeight, ulong? maxChunkSize = null, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var start = startHeight.GetValueOrDefault();
                var end = endHeight.GetValueOrDefault();
                var chunk = maxChunkSize.GetValueOrDefault();
                return Check.Json<List<MoneroBlock>>(
                    NativeMethods.monero_daemon_get_blocks_by_range_chunked(h, startHeight.HasValue ? &start : null, endHeight.HasValue ? &end : null,
                        maxChunkSize.HasValue ? &chunk : null, out var json), json);
            }
        }, ct);

    /// <inheritdoc />
    public override Task<MoneroGetBlockHashesResult> GetBlockHashesAsync(IReadOnlyList<string> blockHashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(blockHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(blockHashes);
            return Check.Json<MoneroGetBlockHashesResult>(NativeMethods.monero_daemon_get_block_hashes(h, hashes.Pointer, hashes.Count, out var json), json);
        }, ct);
    }

    // transactions

    /// <inheritdoc />
    public override Task<List<MoneroTx>> GetTxsAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            var result = Check.Json<MoneroBlocksResult>(NativeMethods.monero_daemon_get_txs(h, hashes.Pointer, hashes.Count, prune, out var json), json);
            return SortByHashes(result.GetTxs(), txHashes);
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<string>> GetTxHexesAsync(IReadOnlyList<string> txHashes, bool prune = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            return Check.Strings(NativeMethods.monero_daemon_get_tx_hexes(h, hashes.Pointer, hashes.Count, prune, out var json), json);
        }, ct);
    }

    /// <inheritdoc />
    public override Task<MoneroSubmitTxResult> SubmitTxHexAsync(string txHex, bool doNotRelay = false, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroSubmitTxResult>(NativeMethods.monero_daemon_submit_tx_hex(h, txHex, doNotRelay, out var json), json), ct);

    /// <inheritdoc />
    public override Task RelayTxByHashAsync(string txHash, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_relay_tx_by_hash(h, txHash)), ct);

    /// <inheritdoc />
    public override Task RelayTxsByHashAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            Check.Ok(NativeMethods.monero_daemon_relay_txs_by_hash(h, hashes.Pointer, hashes.Count));
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroTx>> GetTxPoolAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBlocksResult>(NativeMethods.monero_daemon_get_tx_pool(h, out var json), json).GetTxs(), ct);

    /// <inheritdoc />
    public override Task<List<string>> GetTxPoolHashesAsync(CancellationToken ct = default) =>
        Run(h => Check.Strings(NativeMethods.monero_daemon_get_tx_pool_hashes(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroTxBacklogEntry>> GetTxPoolBacklogAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroTxBacklogEntry>>(NativeMethods.monero_daemon_get_tx_pool_backlog(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroTxPoolStats> GetTxPoolStatsAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroTxPoolStats>(NativeMethods.monero_daemon_get_tx_pool_stats(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task FlushTxPoolAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_flush_tx_pool(h)), ct);

    /// <inheritdoc />
    public override Task FlushTxPoolAsync(string hash, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_flush_tx_pool_hash(h, hash)), ct);

    /// <inheritdoc />
    public override Task FlushTxPoolAsync(IReadOnlyList<string> hashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(hashes);

        return Run(h =>
        {
            using var native = new NativeStringArray(hashes);
            Check.Ok(NativeMethods.monero_daemon_flush_tx_pool_hashes(h, native.Pointer, native.Count));
        }, ct);
    }

    // key images and outputs

    /// <inheritdoc />
    public override Task<List<MoneroKeyImageSpentStatus>> GetKeyImageSpentStatusesAsync(IReadOnlyList<string> keyImages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keyImages);

        return Run(h =>
        {
            using var images = new NativeStringArray(keyImages);
            var statuses = Check.Array<int>(
                NativeMethods.monero_daemon_get_key_image_spent_statuses(h, images.Pointer, images.Count, out var ptr, out var count), ptr, count);
            return statuses.Select(status => (MoneroKeyImageSpentStatus)status).ToList();
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<ulong>> GetOutputIndicesAsync(string txHash, CancellationToken ct = default) =>
        Run(h => Check.Array<ulong>(NativeMethods.monero_daemon_get_output_indices(h, txHash, out var ptr, out var count), ptr, count), ct);

    /// <inheritdoc />
    public override Task<List<MoneroOutput>> GetOutputsAsync(IReadOnlyList<MoneroOutput> outputs, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        var request = MoneroJson.Serialize(outputs);

        return Run(h => Check.Json<List<MoneroOutput>>(NativeMethods.monero_daemon_get_outputs(h, request, out var json), json), ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroOutputHistogramEntry>> GetOutputHistogramAsync(IReadOnlyList<ulong> amounts, int? minCount = null, int? maxCount = null, bool? isUnlocked = null, int? recentCutoff = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        var array = amounts.ToArray();

        return Run(h =>
        {
            unsafe
            {
                var min = minCount.GetValueOrDefault();
                var max = maxCount.GetValueOrDefault();
                var unlocked = (byte)(isUnlocked == true ? 1 : 0);
                var cutoff = recentCutoff.GetValueOrDefault();

                fixed (ulong* ptr = array)
                {
                    return Check.Json<List<MoneroOutputHistogramEntry>>(
                        NativeMethods.monero_daemon_get_output_histogram(h, ptr, (nuint)array.Length, minCount.HasValue ? &min : null, maxCount.HasValue ? &max : null,
                            isUnlocked.HasValue ? &unlocked : null, recentCutoff.HasValue ? &cutoff : null, out var json), json);
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroOutputDistributionEntry>> GetOutputDistributionAsync(IReadOnlyList<ulong> amounts, bool? isCumulative = null, ulong? startHeight = null, ulong? endHeight = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        var array = amounts.ToArray();

        return Run(h =>
        {
            unsafe
            {
                var cumulative = (byte)(isCumulative == true ? 1 : 0);
                var start = startHeight.GetValueOrDefault();
                var end = endHeight.GetValueOrDefault();

                fixed (ulong* ptr = array)
                {
                    return Check.Json<List<MoneroOutputDistributionEntry>>(
                        NativeMethods.monero_daemon_get_output_distribution(h, ptr, (nuint)array.Length, isCumulative.HasValue ? &cumulative : null,
                            startHeight.HasValue ? &start : null, endHeight.HasValue ? &end : null, out var json), json);
                }
            }
        }, ct);
    }

    // fees

    /// <inheritdoc />
    public override Task<MoneroFeeEstimate> GetFeeEstimateAsync(ulong graceBlocks = 0, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroFeeEstimate>(NativeMethods.monero_daemon_get_fee_estimate(h, graceBlocks, out var json), json), ct);

    // mining

    /// <inheritdoc />
    public override Task<MoneroBlockTemplate> GetBlockTemplateAsync(string walletAddress, int? reserveSize = null, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var reserve = reserveSize.GetValueOrDefault();
                return Check.Json<MoneroBlockTemplate>(
                    NativeMethods.monero_daemon_get_block_template(h, walletAddress, reserveSize.HasValue ? &reserve : null, out var json), json);
            }
        }, ct);

    /// <inheritdoc />
    public override Task<MoneroMinerData> GetMinerDataAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMinerData>(NativeMethods.monero_daemon_get_miner_data(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> CalculatePowAsync(uint majorVersion, ulong height, string blockBlob, string seedHash, CancellationToken ct = default) =>
        Run(h => Check.Text(NativeMethods.monero_daemon_calculate_pow(h, majorVersion, height, blockBlob, seedHash, out var hash), hash)
                 ?? throw new MoneroError("The daemon returned no hash"), ct);

    /// <inheritdoc />
    public override Task<MoneroAddAuxiliaryPowResult> AddAuxiliaryPowAsync(string blockTemplateBlob, IReadOnlyList<MoneroAuxiliaryPow> auxPow, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auxPow);
        var request = MoneroJson.Serialize(auxPow);

        return Run(h => Check.Json<MoneroAddAuxiliaryPowResult>(NativeMethods.monero_daemon_add_auxiliary_pow(h, blockTemplateBlob, request, out var json), json), ct);
    }

    /// <inheritdoc />
    public override Task<MoneroMinerTxSum> GetMinerTxSumAsync(ulong height, ulong numBlocks, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMinerTxSum>(NativeMethods.monero_daemon_get_miner_tx_sum(h, height, numBlocks, out var json), json), ct);

    /// <inheritdoc />
    public override Task StartMiningAsync(string address, ulong? numThreads = null, bool? isBackground = null, bool? ignoreBattery = null, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var threads = numThreads.GetValueOrDefault();
                var background = (byte)(isBackground == true ? 1 : 0);
                var battery = (byte)(ignoreBattery == true ? 1 : 0);

                Check.Ok(NativeMethods.monero_daemon_start_mining(h, address, numThreads.HasValue ? &threads : null,
                    isBackground.HasValue ? &background : null, ignoreBattery.HasValue ? &battery : null));
            }
        }, ct);

    /// <inheritdoc />
    public override Task StopMiningAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_stop_mining(h)), ct);

    /// <inheritdoc />
    public override Task<MoneroMiningStatus> GetMiningStatusAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMiningStatus>(NativeMethods.monero_daemon_get_mining_status(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroGenerateBlocksResult> GenerateBlocksAsync(string walletAddress, ulong numBlocks, string? prevBlockHash = null, uint? startingNonce = null, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var nonce = startingNonce.GetValueOrDefault();
                return Check.Json<MoneroGenerateBlocksResult>(
                    NativeMethods.monero_daemon_generate_blocks(h, walletAddress, numBlocks, prevBlockHash, startingNonce.HasValue ? &nonce : null, out var json), json);
            }
        }, ct);

    /// <inheritdoc />
    public override Task SubmitBlockAsync(string blockBlob, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_submit_block(h, blockBlob)), ct);

    /// <inheritdoc />
    public override Task SubmitBlocksAsync(IReadOnlyList<string> blockBlobs, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(blockBlobs);

        return Run(h =>
        {
            using var blobs = new NativeStringArray(blockBlobs);
            Check.Ok(NativeMethods.monero_daemon_submit_blocks(h, blobs.Pointer, blobs.Count));
        }, ct);
    }

    // peers and bandwidth

    /// <inheritdoc />
    public override Task<List<MoneroPeer>> GetPeersAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroPeer>>(NativeMethods.monero_daemon_get_peers(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroPeer>> GetKnownPeersAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroPeer>>(NativeMethods.monero_daemon_get_known_peers(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroPeer>> GetPublicPeersAsync(bool includeOffline = false, CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroPeer>>(NativeMethods.monero_daemon_get_public_peers(h, includeOffline, out var json), json), ct);

    /// <inheritdoc />
    public override Task SetOutgoingPeerLimitAsync(int limit, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_set_outgoing_peer_limit(h, limit)), ct);

    /// <inheritdoc />
    public override Task SetIncomingPeerLimitAsync(int limit, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_set_incoming_peer_limit(h, limit)), ct);

    /// <inheritdoc />
    public override Task<List<MoneroBan>> GetPeerBansAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroBan>>(NativeMethods.monero_daemon_get_peer_bans(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroBan> GetPeerBanAsync(string address, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroBan>(NativeMethods.monero_daemon_get_peer_ban(h, address, out var json), json), ct);

    /// <inheritdoc />
    public override Task SetPeerBansAsync(IReadOnlyList<MoneroBan> bans, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bans);
        var request = MoneroJson.Serialize(bans);

        return Run(h => Check.Ok(NativeMethods.monero_daemon_set_peer_bans(h, request)), ct);
    }

    /// <inheritdoc />
    public override Task SetPeerBanAsync(MoneroBan ban, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ban);
        var request = MoneroJson.Serialize(ban);

        return Run(h => Check.Ok(NativeMethods.monero_daemon_set_peer_ban(h, request)), ct);
    }

    /// <inheritdoc />
    public override Task<int> GetDownloadLimitAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_get_download_limit(h, out var limit));
            return limit;
        }, ct);

    /// <inheritdoc />
    public override Task<int> SetDownloadLimitAsync(int limit, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_set_download_limit(h, limit, out var newLimit));
            return newLimit;
        }, ct);

    /// <inheritdoc />
    public override Task<int> ResetDownloadLimitAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_reset_download_limit(h, out var limit));
            return limit;
        }, ct);

    /// <inheritdoc />
    public override Task<int> GetUploadLimitAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_get_upload_limit(h, out var limit));
            return limit;
        }, ct);

    /// <inheritdoc />
    public override Task<int> SetUploadLimitAsync(int limit, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_set_upload_limit(h, limit, out var newLimit));
            return newLimit;
        }, ct);

    /// <inheritdoc />
    public override Task<int> ResetUploadLimitAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_reset_upload_limit(h, out var limit));
            return limit;
        }, ct);

    // blockchain maintenance

    /// <inheritdoc />
    public override Task<MoneroPruneResult> PruneBlockchainAsync(bool check, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroPruneResult>(NativeMethods.monero_daemon_prune_blockchain(h, check, out var json), json), ct);

    /// <inheritdoc />
    public override Task SaveBlockchainAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_save_blockchain(h)), ct);

    /// <inheritdoc />
    public override Task<ulong> PopBlocksAsync(ulong numBlocks, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_daemon_pop_blocks(h, numBlocks, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task FlushCacheAsync(bool badBlocks = false, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_flush_cache(h, badBlocks)), ct);

    // configuration, logs and updates

    /// <inheritdoc />
    public override Task SetBootstrapDaemonAsync(string address, string username = "", string password = "", string proxy = "", CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_set_bootstrap_daemon(h, address, username, password, proxy)), ct);

    /// <inheritdoc />
    public override Task RemoveBootstrapDaemonAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_remove_bootstrap_daemon(h)), ct);

    /// <inheritdoc />
    public override Task SetLogHashRateAsync(bool isVisible, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_set_log_hash_rate(h, isVisible)), ct);

    /// <inheritdoc />
    public override Task SetLogLevelAsync(int level, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_set_log_level(h, level)), ct);

    /// <inheritdoc />
    public override Task<string> SetLogCategoriesAsync(string categories = "", CancellationToken ct = default) =>
        Run(h => Check.Text(NativeMethods.monero_daemon_set_log_categories(h, categories, out var result), result) ?? "", ct);

    /// <inheritdoc />
    public override Task<MoneroDaemonUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroDaemonUpdateCheckResult>(NativeMethods.monero_daemon_check_for_update(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroDaemonUpdateDownloadResult> DownloadUpdateAsync(string path = "", CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroDaemonUpdateDownloadResult>(NativeMethods.monero_daemon_download_update(h, path, out var json), json), ct);

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_daemon_stop(h)), ct);

    // closing

    /// <summary>
    /// Release the native daemon. The calls that were in the queue run first, and the listeners are unregistered.
    /// </summary>
    /// <returns>a task that ends when the daemon is released.</returns>
    public async ValueTask DisposeAsync()
    {
        TaskCompletionSource<MoneroBlockHeader>[] waiters;

        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            waiters = [.. _waiters];
            _waiters.Clear();
            _listeners.Clear();
        }

        foreach (var waiter in waiters) waiter.TrySetException(new ObjectDisposedException(GetType().Name));

        // the calls that were in the queue run first, then the listener goes, then the handle
        await _worker.RunAsync(() =>
        {
            if (_nativeListenerAdded)
            {
                try
                {
                    NativeMethods.monero_daemon_remove_listener(_handle, _events!.Handle);
                }
                catch (Exception)
                {
                    // the daemon is going away anyway
                }
            }
        }).ConfigureAwait(false);

        _events?.Dispose();
        _worker.Dispose();
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Release the native daemon, and wait for it.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
