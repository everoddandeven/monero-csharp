using System.Runtime.InteropServices;

namespace Monero.Native;

// the functions of monero_c_wallet.h that a wallet on a monero-wallet-rpc server uses. Strings that can be NULL are string?, and the
// optional values are pointers, null when they are not set
internal static unsafe partial class NativeMethods
{
    // listener

    [LibraryImport(Library)]
    public static partial int monero_wallet_listener_create(in WalletListenerCallbacks callbacks, out WalletListenerHandle listener);

    [LibraryImport(Library)]
    public static partial void monero_wallet_listener_free(IntPtr listener);

    [LibraryImport(Library)]
    public static partial int monero_wallet_add_listener(WalletHandle wallet, WalletListenerHandle listener);

    [LibraryImport(Library)]
    public static partial int monero_wallet_remove_listener(WalletHandle wallet, WalletListenerHandle listener);

    [LibraryImport(Library)]
    public static partial int monero_wallet_remove_listeners(WalletHandle wallet);

    // lifecycle

    [LibraryImport(Library)]
    public static partial void monero_wallet_free(IntPtr wallet);

    [LibraryImport(Library)]
    public static partial int monero_wallet_save(WalletHandle wallet);

    [LibraryImport(Library)]
    public static partial int monero_wallet_close(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] bool save);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_closed(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool closed);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_move_to(WalletHandle wallet, string path, string? password);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_change_password(WalletHandle wallet, string? oldPassword, string? newPassword);

    // wallet RPC

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_connect(RpcConnectionHandle connection, out WalletHandle wallet);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_rpc_open_wallet(WalletHandle wallet, string name, string? walletPassword);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_rpc_create_wallet(WalletHandle wallet, string configJson);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_stop(WalletHandle wallet);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_get_seed_languages(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_get_connection(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_set_poll_period(WalletHandle wallet, ulong periodMs);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_get_balances(WalletHandle wallet, uint* accountIdx, uint* subaddressIdx, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_get_account(WalletHandle wallet, uint accountIdx, [MarshalAs(UnmanagedType.U1)] bool includeSubaddresses, [MarshalAs(UnmanagedType.U1)] bool skipBalances, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_rpc_get_accounts(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] bool includeSubaddresses, string? tag, [MarshalAs(UnmanagedType.U1)] bool skipBalances, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rpc_get_subaddresses(WalletHandle wallet, uint accountIdx, uint* subaddressIndices, nuint numSubaddressIndices, [MarshalAs(UnmanagedType.U1)] bool skipBalances, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_rpc_set_daemon_connection(WalletHandle wallet, string connectionJson, [MarshalAs(UnmanagedType.U1)] bool isTrusted, string? sslOptionsJson);

    // keys, network and version

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_network_type(WalletHandle wallet, out int networkType);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_view_only(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool viewOnly);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_multisig(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool multisig);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_seed(WalletHandle wallet, out IntPtr seed);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_seed_language(WalletHandle wallet, out IntPtr language);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_primary_address(WalletHandle wallet, out IntPtr address);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_private_view_key(WalletHandle wallet, out IntPtr key);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_private_spend_key(WalletHandle wallet, out IntPtr key);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_public_view_key(WalletHandle wallet, out IntPtr key);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_public_spend_key(WalletHandle wallet, out IntPtr key);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_path(WalletHandle wallet, out IntPtr path);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_version(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_default_fee_priority(WalletHandle wallet, out int priority);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_attribute(WalletHandle wallet, string key, [MarshalAs(UnmanagedType.U1)] out bool found, out IntPtr value);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_set_attribute(WalletHandle wallet, string key, string value);

    // heights and balances

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_height(WalletHandle wallet, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_restore_height(WalletHandle wallet, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_wallet_set_restore_height(WalletHandle wallet, ulong restoreHeight);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_height_by_date(WalletHandle wallet, ushort year, byte month, byte day, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_balance(WalletHandle wallet, out ulong balance);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_unlocked_balance(WalletHandle wallet, out ulong balance);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_account_balance(WalletHandle wallet, uint accountIdx, out ulong balance);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_subaddress_balance(WalletHandle wallet, uint accountIdx, uint subaddressIdx, out ulong balance);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_account_unlocked_balance(WalletHandle wallet, uint accountIdx, out ulong balance);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_subaddress_unlocked_balance(WalletHandle wallet, uint accountIdx, uint subaddressIdx, out ulong balance);

    // addresses, accounts and subaddresses

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_address(WalletHandle wallet, uint accountIdx, uint subaddressIdx, out IntPtr address);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_integrated_address(WalletHandle wallet, string? standardAddress, string? paymentId, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_decode_integrated_address(WalletHandle wallet, string integratedAddress, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_address_index(WalletHandle wallet, string address, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_create_account(WalletHandle wallet, string? label, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_create_subaddress(WalletHandle wallet, uint accountIdx, string? label, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_set_subaddress_label(WalletHandle wallet, uint accountIdx, uint subaddressIdx, string? label);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_account_tags(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_set_account_tag_label(WalletHandle wallet, string tag, string label);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_tag_accounts(WalletHandle wallet, string tag, uint* accountIndices, nuint numAccountIndices);

    [LibraryImport(Library)]
    public static partial int monero_wallet_untag_accounts(WalletHandle wallet, uint* accountIndices, nuint numAccountIndices);

    // address book

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_add_address_book_entry(WalletHandle wallet, string address, string? description, out ulong index);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_edit_address_book_entry(WalletHandle wallet, ulong index, string? address, string? description);

    [LibraryImport(Library)]
    public static partial int monero_wallet_delete_address_book_entry(WalletHandle wallet, ulong index);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_address_book_entries(WalletHandle wallet, ulong* indices, nuint numIndices, out IntPtr json);

    // messages

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_sign_message(WalletHandle wallet, string message, int signatureType, uint accountIdx, uint subaddressIdx, out IntPtr signature);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_verify_message(WalletHandle wallet, string message, string address, string signature, out IntPtr json);

    // key images and outputs

    [LibraryImport(Library)]
    public static partial int monero_wallet_export_key_images(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] bool all, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_import_key_images(WalletHandle wallet, string keyImagesJson, ulong offset, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_export_outputs(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] bool all, out IntPtr hex);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_import_outputs(WalletHandle wallet, string outputsHex, out int numImported);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_freeze_output(WalletHandle wallet, string keyImage);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_thaw_output(WalletHandle wallet, string keyImage);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_is_output_frozen(WalletHandle wallet, string keyImage, [MarshalAs(UnmanagedType.U1)] out bool frozen);

    // transaction notes

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_tx_note(WalletHandle wallet, string txHash, out IntPtr note);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_set_tx_note(WalletHandle wallet, string txHash, string note);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_tx_notes(WalletHandle wallet, IntPtr txHashes, nuint numTxHashes, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_set_tx_notes(WalletHandle wallet, IntPtr txHashes, IntPtr notes, nuint numTxHashes);

    // daemon

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_set_daemon_connection(WalletHandle wallet, string uri, string? username, string? password, string? proxyUri, byte* isTrusted, [MarshalAs(UnmanagedType.U1)] bool sslVerify);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_daemon_connection(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_daemon_trusted(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool trusted);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_connected_to_daemon(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool connected);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_daemon_synced(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool synced);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_daemon_height(WalletHandle wallet, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_daemon_max_peer_height(WalletHandle wallet, out ulong height);

    // sync

    [LibraryImport(Library)]
    public static partial int monero_wallet_sync(WalletHandle wallet, ulong* startHeight, WalletListenerHandle listener, out IntPtr json);

    // a SafeHandle can't be null, so the sync without a listener passes a null pointer
    [LibraryImport(Library, EntryPoint = "monero_wallet_sync")]
    public static partial int monero_wallet_sync_without_listener(WalletHandle wallet, ulong* startHeight, IntPtr listener, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_start_syncing(WalletHandle wallet, ulong syncPeriodMs);

    [LibraryImport(Library)]
    public static partial int monero_wallet_stop_syncing(WalletHandle wallet);

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_synced(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool synced);

    [LibraryImport(Library)]
    public static partial int monero_wallet_wait_for_next_block(WalletHandle wallet, out ulong height);

    [LibraryImport(Library)]
    public static partial int monero_wallet_scan_txs(WalletHandle wallet, IntPtr txHashes, nuint numTxHashes);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rescan_blockchain(WalletHandle wallet);

    [LibraryImport(Library)]
    public static partial int monero_wallet_rescan_spent(WalletHandle wallet);

    // mining

    [LibraryImport(Library)]
    public static partial int monero_wallet_start_mining(WalletHandle wallet, ulong* numThreads, byte* backgroundMining, byte* ignoreBattery);

    [LibraryImport(Library)]
    public static partial int monero_wallet_stop_mining(WalletHandle wallet);

    // transactions

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_txs(WalletHandle wallet, string? queryJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_transfers(WalletHandle wallet, string? queryJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_outputs(WalletHandle wallet, string? queryJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_create_tx(WalletHandle wallet, string configJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_create_txs(WalletHandle wallet, string configJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_relay_tx(WalletHandle wallet, string txMetadata, out IntPtr hash);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_relay_tx_json(WalletHandle wallet, string txJson, out IntPtr hash);

    [LibraryImport(Library)]
    public static partial int monero_wallet_relay_txs(WalletHandle wallet, IntPtr txMetadatas, nuint numTxMetadatas, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_relay_txs_json(WalletHandle wallet, string txsJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_submit_txs(WalletHandle wallet, string signedTxHex, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_sign_txs(WalletHandle wallet, string unsignedTxHex, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_describe_tx_set(WalletHandle wallet, string txSetJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_sweep_output(WalletHandle wallet, string configJson, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_sweep_dust(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] bool relay, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_sweep_unlocked(WalletHandle wallet, string configJson, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_payment_uri(WalletHandle wallet, string configJson, out IntPtr uri);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_parse_payment_uri(WalletHandle wallet, string uri, out IntPtr json);

    // proofs

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_tx_key(WalletHandle wallet, string txHash, out IntPtr key);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_check_tx_key(WalletHandle wallet, string txHash, string txKey, string address, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_tx_proof(WalletHandle wallet, string txHash, string address, string? message, out IntPtr signature);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_check_tx_proof(WalletHandle wallet, string txHash, string address, string? message, string signature, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_spend_proof(WalletHandle wallet, string txHash, string? message, out IntPtr signature);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_check_spend_proof(WalletHandle wallet, string txHash, string? message, string signature, [MarshalAs(UnmanagedType.U1)] out bool good);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_reserve_proof_wallet(WalletHandle wallet, string? message, out IntPtr signature);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_get_reserve_proof_account(WalletHandle wallet, uint accountIdx, ulong amount, string? message, out IntPtr signature);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_check_reserve_proof(WalletHandle wallet, string address, string? message, string signature, out IntPtr json);

    // multisig

    [LibraryImport(Library)]
    public static partial int monero_wallet_is_multisig_import_needed(WalletHandle wallet, [MarshalAs(UnmanagedType.U1)] out bool needed);

    [LibraryImport(Library)]
    public static partial int monero_wallet_get_multisig_info(WalletHandle wallet, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_prepare_multisig(WalletHandle wallet, out IntPtr multisigHex);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_make_multisig(WalletHandle wallet, IntPtr multisigHexes, nuint numMultisigHexes, int threshold, string password, out IntPtr multisigHex);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_exchange_multisig_keys(WalletHandle wallet, IntPtr multisigHexes, nuint numMultisigHexes, string password, out IntPtr json);

    [LibraryImport(Library)]
    public static partial int monero_wallet_export_multisig_hex(WalletHandle wallet, out IntPtr multisigHex);

    [LibraryImport(Library)]
    public static partial int monero_wallet_import_multisig_hex(WalletHandle wallet, IntPtr multisigHexes, nuint numMultisigHexes, [MarshalAs(UnmanagedType.U1)] bool refreshAfterImport, out int numImported);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_sign_multisig_tx_hex(WalletHandle wallet, string multisigTxHex, out IntPtr json);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int monero_wallet_submit_multisig_tx_hex(WalletHandle wallet, string signedMultisigTxHex, out IntPtr json);
}

/// <summary>The callbacks of a wallet listener, in the order of <c>monero_wallet_listener_callbacks</c> of monero-c.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WalletListenerCallbacks
{
    public void* UserData;
    public delegate* unmanaged[Cdecl]<void*, ulong, ulong, ulong, double, byte*, void> OnSyncProgress;
    public delegate* unmanaged[Cdecl]<void*, ulong, void> OnNewBlock;
    public delegate* unmanaged[Cdecl]<void*, ulong, ulong, void> OnBalancesChanged;
    public delegate* unmanaged[Cdecl]<void*, byte*, void> OnOutputReceived;
    public delegate* unmanaged[Cdecl]<void*, byte*, void> OnOutputSpent;
}
