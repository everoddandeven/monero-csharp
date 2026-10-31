using System.Runtime.InteropServices;

namespace Monero.Native;

/// <summary>Declarations of the functions of monero-c. No logic: results, memory and JSON are handled by the layers above.</summary>
internal static unsafe partial class NativeMethods
{
    private const string Library = "monero_c";

    // utils

    [LibraryImport(Library)]
    public static partial IntPtr monero_last_error();

    [LibraryImport(Library)]
    public static partial void monero_utils_free(IntPtr ptr);

    [LibraryImport(Library)]
    public static partial int monero_utils_get_abi_version(out uint major, out uint minor, out uint patch);

    [LibraryImport(Library)]
    public static partial void monero_utils_free_secret(IntPtr secret);

    // rpc connection

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_rpc_connection_create(string connectionJson, out RpcConnectionHandle connection);

    [LibraryImport(Library)]
    public static partial void monero_rpc_connection_free(IntPtr connection);

    // daemon: connection and listeners

    [LibraryImport(Library)]
    public static partial int monero_daemon_connect_with(RpcConnectionHandle connection, out DaemonHandle daemon);

    [LibraryImport(Library)]
    public static partial void monero_daemon_free(IntPtr daemon);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_rpc_connection(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_poll_period(DaemonHandle daemon, ulong periodMs);

    [LibraryImport(Library)]
    public static partial int monero_daemon_listener_create(in DaemonListenerCallbacks callbacks, out DaemonListenerHandle listener);

    [LibraryImport(Library)]
    public static partial void monero_daemon_listener_free(IntPtr listener);

    [LibraryImport(Library)]
    public static partial int monero_daemon_add_listener(DaemonHandle daemon, DaemonListenerHandle listener);

    [LibraryImport(Library)]
    public static partial int monero_daemon_remove_listener(DaemonHandle daemon, DaemonListenerHandle listener);

    // daemon: general

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_version(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_is_trusted(DaemonHandle daemon, [MarshalAs(UnmanagedType.U1)] out bool trusted);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_height(DaemonHandle daemon, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_block_hash(DaemonHandle daemon, ulong height, out IntPtr hash);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_info(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_sync_info(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_network_stats(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_hard_fork_info(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_alt_chains(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_alt_block_hashes(DaemonHandle daemon, out IntPtr json);

    // daemon: mining

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_block_template(DaemonHandle daemon, string walletAddress, int* reserveSize, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_miner_data(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_calculate_pow(DaemonHandle daemon, uint majorVersion, ulong height, string blockBlob, string seedHash, out IntPtr powHash);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_add_auxiliary_pow(DaemonHandle daemon, string blockTemplateBlob, string auxPowJson, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_mining_status(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_start_mining(DaemonHandle daemon, string address, ulong* numThreads, byte* isBackground, byte* ignoreBattery);

    [LibraryImport(Library)]
    public static partial int monero_daemon_stop_mining(DaemonHandle daemon);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_generate_blocks(DaemonHandle daemon, string walletAddress, ulong numBlocks, string? prevBlockHash, uint* startingNonce, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_submit_block(DaemonHandle daemon, string blockBlob);

    [LibraryImport(Library)]
    public static partial int monero_daemon_submit_blocks(DaemonHandle daemon, IntPtr blockBlobs, nuint numBlockBlobs);

    // daemon: blocks

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_last_block_header(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_block_header_by_hash(DaemonHandle daemon, string blockHash, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_block_header_by_height(DaemonHandle daemon, ulong height, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_block_headers_by_range(DaemonHandle daemon, ulong startHeight, ulong endHeight, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_block_by_hash(DaemonHandle daemon, string blockHash, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_blocks_by_hash(DaemonHandle daemon, IntPtr blockHashes, nuint numBlockHashes, ulong startHeight, [MarshalAs(UnmanagedType.U1)] bool prune, ulong maxBlockCount, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_block_by_height(DaemonHandle daemon, ulong height, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_blocks_by_height(DaemonHandle daemon, ulong* heights, nuint numHeights, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_blocks_by_range(DaemonHandle daemon, ulong* startHeight, ulong* endHeight, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_blocks_by_range_chunked(DaemonHandle daemon, ulong* startHeight, ulong* endHeight, ulong* maxChunkSize, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_block_hashes(DaemonHandle daemon, IntPtr blockHashes, nuint numBlockHashes, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_wait_for_next_block_header(DaemonHandle daemon, out IntPtr json);

    // daemon: transactions

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_txs(DaemonHandle daemon, IntPtr txHashes, nuint numTxHashes, [MarshalAs(UnmanagedType.U1)] bool prune, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_tx_hexes(DaemonHandle daemon, IntPtr txHashes, nuint numTxHashes, [MarshalAs(UnmanagedType.U1)] bool prune, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_miner_tx_sum(DaemonHandle daemon, ulong height, ulong numBlocks, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_fee_estimate(DaemonHandle daemon, ulong graceBlocks, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_submit_tx_hex(DaemonHandle daemon, string txHex, [MarshalAs(UnmanagedType.U1)] bool doNotRelay, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_relay_tx_by_hash(DaemonHandle daemon, string txHash);

    [LibraryImport(Library)]
    public static partial int monero_daemon_relay_txs_by_hash(DaemonHandle daemon, IntPtr txHashes, nuint numTxHashes);

    // daemon: tx pool

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_tx_pool(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_tx_pool_hashes(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_tx_pool_backlog(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_tx_pool_stats(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_flush_tx_pool(DaemonHandle daemon);

    [LibraryImport(Library)]
    public static partial int monero_daemon_flush_tx_pool_hashes(DaemonHandle daemon, IntPtr txHashes, nuint numTxHashes);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_flush_tx_pool_hash(DaemonHandle daemon, string txHash);

    // daemon: key images and outputs

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_key_image_spent_statuses(DaemonHandle daemon, IntPtr keyImages, nuint numKeyImages, out IntPtr statuses, out nuint count);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_output_indices(DaemonHandle daemon, string txHash, out IntPtr indices, out nuint count);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_outputs(DaemonHandle daemon, string outputsJson, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_output_histogram(DaemonHandle daemon, ulong* amounts, nuint numAmounts, int* minCount, int* maxCount, byte* isUnlocked, int* recentCutoff, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_output_distribution(DaemonHandle daemon, ulong* amounts, nuint numAmounts, byte* isCumulative, ulong* startHeight, ulong* endHeight, out IntPtr json);

    // daemon: bandwidth

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_download_limit(DaemonHandle daemon, out int limit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_download_limit(DaemonHandle daemon, int limit, out int newLimit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_reset_download_limit(DaemonHandle daemon, out int limit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_upload_limit(DaemonHandle daemon, out int limit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_upload_limit(DaemonHandle daemon, int limit, out int newLimit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_reset_upload_limit(DaemonHandle daemon, out int limit);

    // daemon: peers

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_peers(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_known_peers(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_public_peers(DaemonHandle daemon, [MarshalAs(UnmanagedType.U1)] bool includeOffline, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_outgoing_peer_limit(DaemonHandle daemon, int limit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_incoming_peer_limit(DaemonHandle daemon, int limit);

    [LibraryImport(Library)]
    public static partial int monero_daemon_get_peer_bans(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_set_peer_bans(DaemonHandle daemon, string bansJson);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_set_peer_ban(DaemonHandle daemon, string banJson);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_get_peer_ban(DaemonHandle daemon, string address, out IntPtr json);

    // daemon: admin

    [LibraryImport(Library)]
    public static partial int monero_daemon_prune_blockchain(DaemonHandle daemon, [MarshalAs(UnmanagedType.U1)] bool check, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_save_blockchain(DaemonHandle daemon);

    [LibraryImport(Library)]
    public static partial int monero_daemon_pop_blocks(DaemonHandle daemon, ulong numBlocks, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_daemon_flush_cache(DaemonHandle daemon, [MarshalAs(UnmanagedType.U1)] bool badBlocks);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_set_bootstrap_daemon(DaemonHandle daemon, string address, string username, string password, string proxy);

    [LibraryImport(Library)]
    public static partial int monero_daemon_remove_bootstrap_daemon(DaemonHandle daemon);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_log_hash_rate(DaemonHandle daemon, [MarshalAs(UnmanagedType.U1)] bool isVisible);

    [LibraryImport(Library)]
    public static partial int monero_daemon_set_log_level(DaemonHandle daemon, int level);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_set_log_categories(DaemonHandle daemon, string categories, out IntPtr outCategories);

    [LibraryImport(Library)]
    public static partial int monero_daemon_check_for_update(DaemonHandle daemon, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_daemon_download_update(DaemonHandle daemon, string path, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_daemon_stop(DaemonHandle daemon);
}

/// <summary>The callbacks of a daemon listener, in the order of <c>monero_daemon_listener_callbacks</c> of monero-c.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DaemonListenerCallbacks
{
    /// <summary>The size of this struct, which monero-c needs to read a struct that a program built with another version of the header. Without it the listener is not created.</summary>
    public uint StructSize;

    public void* UserData;
    public delegate* unmanaged[Cdecl]<void*, byte*, void> OnBlockHeader;
}
