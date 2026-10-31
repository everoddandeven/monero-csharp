using Monero.Common;
using Monero.Daemon.Model;
using Monero.Native;
using Monero.Wallet.Model;

namespace Monero.Wallet;

/// <summary>
/// Implements a <see cref="MoneroWallet"/> using <see href="https://docs.getmonero.org/rpc-library/wallet-rpc/">monero-wallet-rpc</see>, through monero-c.
/// The server opens the wallet, so it is created or opened after the connection. Dispose it to release the native handle: the wallet stays
/// open on the server unless it is closed with <see cref="MoneroWallet.CloseAsync"/> first.
/// <para>
/// monero-wallet-rpc doesn't have everything of the interface. These throw a <see cref="MoneroError"/>: <c>GetNetworkTypeAsync</c>,
/// <c>GetSeedLanguageAsync</c>, <c>GetPublicViewKeyAsync</c>, <c>GetPublicSpendKeyAsync</c>, <c>GetRestoreHeightAsync</c>,
/// <c>GetHeightByDateAsync</c>, <c>GetDaemonHeightAsync</c>, <c>GetDaemonMaxPeerHeightAsync</c>, <c>IsDaemonSyncedAsync</c>,
/// <c>IsDaemonTrustedAsync</c> and <c>IsSyncedAsync</c>, and so does <c>SyncAsync</c> with a listener, since the server can't report
/// the progress of a sync.
/// </para>
/// </summary>
public class MoneroWalletRpc : MoneroWallet, IAsyncDisposable, IDisposable
{
    private readonly WalletHandle _handle;
    private readonly NativeWorker _worker = new("MoneroWalletRpc");
    private readonly object _lock = new();
    private readonly List<MoneroWalletListener> _listeners = [];
    private readonly List<TaskCompletionSource<ulong>> _waiters = [];
    private readonly MoneroWalletListener _waiterListener;
    private WalletEvents? _events;
    private bool _nativeListenerAdded;
    private bool _disposed;

    /// <summary>
    /// Initialize a wallet that connects with the given RPC connection of the server. No wallet is open until
    /// <see cref="OpenWalletAsync(string, string, CancellationToken)"/> or <see cref="CreateWalletAsync"/>.
    /// </summary>
    /// <param name="rpc">the RPC connection of the monero-wallet-rpc server.</param>
    /// <exception cref="MoneroError">the native library is not found, or it can't make the connection.</exception>
    public MoneroWalletRpc(MoneroRpcConnection rpc)
    {
        ArgumentNullException.ThrowIfNull(rpc);
        Check.EnsureLibrary();

        _waiterListener = new WaiterListener(this);

        Check.Ok(NativeMethods.monero_rpc_connection_create(MoneroJson.Serialize(rpc), out var connection));
        using (connection)
        {
            Check.Ok(NativeMethods.monero_wallet_rpc_connect(connection, out _handle));
        }
    }

    /// <summary>
    /// Initialize a wallet that connects to the monero-wallet-rpc server at the given URI.
    /// </summary>
    /// <param name="uri">the URI of the server.</param>
    /// <param name="username">the username to authenticate with, or an empty string for none.</param>
    /// <param name="password">the password to authenticate with, or an empty string for none.</param>
    /// <param name="proxyUri">the URI of a proxy to connect through, or an empty string for none.</param>
    /// <param name="zmqUri">the ZMQ URI of the server, or an empty string for none.</param>
    /// <param name="timeoutMs">the time to wait for a response of the server, in milliseconds, or <c>null</c> for the default of 20000.</param>
    /// <exception cref="MoneroError">the native library is not found, or it can't make the connection.</exception>
    public MoneroWalletRpc(string uri, string username = "", string password = "", string proxyUri = "", string zmqUri = "", uint? timeoutMs = null)
        : this(new MoneroRpcConnection(uri, NullIfEmpty(username), NullIfEmpty(password))
        {
            ProxyUri = NullIfEmpty(proxyUri),
            ZmqUri = NullIfEmpty(zmqUri),
            TimeoutMs = timeoutMs,
        })
    {
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    // server

    /// <summary>
    /// Get the connection to the monero-wallet-rpc server, as it is now: a copy, with the last known state of the server.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the connection to the server.</returns>
    public Task<MoneroRpcConnection> GetRpcConnectionAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroRpcConnection>(NativeMethods.monero_wallet_rpc_get_connection(h, out var json), json), ct);

    /// <summary>
    /// Indicates if the client is connected to the server: it answered the last request, and it accepted the credentials.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the client is connected to the server, <c>false</c> otherwise.</returns>
    public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
    {
        var connection = await GetRpcConnectionAsync(ct).ConfigureAwait(false);

        return connection.IsOnline == true && connection.IsAuthenticated != false;
    }

    /// <summary>
    /// Create a wallet on the server and open it. A config with a seed restores the wallet from the seed, a config with keys restores it from
    /// the keys, and a config with neither creates a random wallet. The server has its own network, so the config has no network type.
    /// </summary>
    /// <param name="config">configures the wallet to create, with the name of the wallet on the server as its path.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public Task CreateWalletAsync(MoneroWalletConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Run(h => Check.Ok(NativeMethods.monero_wallet_rpc_create_wallet(h, MoneroJson.Serialize(config))), ct);
    }

    /// <summary>
    /// Open a wallet on the server.
    /// </summary>
    /// <param name="name">the name of the wallet on the server.</param>
    /// <param name="password">the password of the wallet, or an empty string for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public Task OpenWalletAsync(string name, string password = "", CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return Run(h => Check.Ok(NativeMethods.monero_wallet_rpc_open_wallet(h, name, password)), ct);
    }

    /// <summary>
    /// Open a wallet on the server.
    /// </summary>
    /// <param name="config">configures the wallet to open, with the name of the wallet on the server as its path.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public Task OpenWalletAsync(MoneroWalletConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrEmpty(config.Path)) throw new MoneroError("The name of the wallet is needed: set the path of the config");

        return OpenWalletAsync(config.Path, config.Password ?? "", ct);
    }

    /// <summary>
    /// Save and close the current wallet and stop the server.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public Task StopAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_rpc_stop(h)), ct);

    /// <summary>
    /// Get the languages that the server supports for the seed of a wallet.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the names of the languages.</returns>
    public Task<List<string>> GetSeedLanguagesAsync(CancellationToken ct = default) =>
        Run(h => Check.Strings(NativeMethods.monero_wallet_rpc_get_seed_languages(h, out var json), json), ct);

    /// <summary>
    /// Set the period between polls of the server for changes of the wallet.
    /// </summary>
    /// <param name="periodMs">the poll period in milliseconds, from 1 to 2147483647.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public Task SetPollPeriodInMsAsync(ulong periodMs, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_rpc_set_poll_period(h, periodMs)), ct);

    // listeners

    /// <inheritdoc />
    public override void AddListener(MoneroWalletListener listener)
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
    public override void RemoveListener(MoneroWalletListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_lock)
        {
            if (_disposed || !_listeners.Remove(listener)) return;
            UpdateNativeListener();
        }
    }

    /// <inheritdoc />
    public override List<MoneroWalletListener> GetListeners()
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

    // the wallet polls while a native listener is registered, so there is one while a listener or a waiter exists
    private void UpdateNativeListener()
    {
        var needed = _listeners.Count + _waiters.Count > 0;

        if (needed && !_nativeListenerAdded)
        {
            _events ??= new WalletEvents(OnEvent);
            Check.Ok(NativeMethods.monero_wallet_add_listener(_handle, _events.Handle));
            _nativeListenerAdded = true;
        }
        else if (!needed && _nativeListenerAdded)
        {
            Check.Ok(NativeMethods.monero_wallet_remove_listener(_handle, _events!.Handle));
            _nativeListenerAdded = false;
        }
    }

    // runs on the thread pool, one event after the other
    private void OnEvent(Action<MoneroWalletListener> call)
    {
        MoneroWalletListener[] listeners;

        lock (_lock)
        {
            listeners = [.. _listeners, _waiterListener];
        }

        foreach (var listener in listeners)
        {
            try
            {
                call(listener);
            }
            catch
            {
                // a listener that throws doesn't stop the others
            }
        }
    }

    // completes the waiters of the next block
    private sealed class WaiterListener(MoneroWalletRpc wallet) : MoneroWalletListener
    {
        public override void OnNewBlock(ulong height)
        {
            TaskCompletionSource<ulong>[] waiters;

            lock (wallet._lock)
            {
                waiters = [.. wallet._waiters];
                wallet._waiters.Clear();
            }

            foreach (var waiter in waiters) waiter.TrySetResult(height);

            if (waiters.Length > 0)
            {
                lock (wallet._lock)
                {
                    if (!wallet._disposed) wallet.UpdateNativeListener();
                }
            }
        }
    }

    /// <summary>
    /// Wait for the next block to be added to the chain: the wallet gets it from the server when it polls it.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the height of the next block that was added to the chain.</returns>
    public override async Task<ulong> WaitForNextBlockAsync(CancellationToken ct = default)
    {
        var waiter = new TaskCompletionSource<ulong>(TaskCreationOptions.RunContinuationsAsynchronously);

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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    // worker

    private Task<T> Run<T>(Func<WalletHandle, T> call, CancellationToken ct) => _worker.RunAsync(() => call(_handle), ct);

    private Task Run(Action<WalletHandle> call, CancellationToken ct) => _worker.RunAsync(() => call(_handle), ct);

    private static string Required(string? text) => text ?? throw new MoneroError("The wallet returned no value");

    // general

    /// <inheritdoc />
    public override Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroVersion>(NativeMethods.monero_wallet_get_version(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> GetPathAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_path(h, out var path), path)), ct);

    /// <inheritdoc />
    public override Task<MoneroNetworkType> GetNetworkTypeAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_network_type(h, out var networkType));
            return (MoneroNetworkType)networkType;
        }, ct);

    /// <inheritdoc />
    public override Task<bool> IsViewOnlyAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_view_only(h, out var viewOnly));
            return viewOnly;
        }, ct);

    /// <inheritdoc />
    public override Task<bool> IsClosedAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_closed(h, out var closed));
            return closed;
        }, ct);

    /// <inheritdoc />
    public override Task<string> GetSeedAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Secret(NativeMethods.monero_wallet_get_seed(h, out var seed), seed)), ct);

    /// <inheritdoc />
    public override Task<string> GetSeedLanguageAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_seed_language(h, out var language), language)), ct);

    /// <inheritdoc />
    public override Task<string> GetPrivateViewKeyAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Secret(NativeMethods.monero_wallet_get_private_view_key(h, out var key), key)), ct);

    /// <inheritdoc />
    public override Task<string> GetPrivateSpendKeyAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Secret(NativeMethods.monero_wallet_get_private_spend_key(h, out var key), key)), ct);

    /// <inheritdoc />
    public override Task<string> GetPublicViewKeyAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_public_view_key(h, out var key), key)), ct);

    /// <inheritdoc />
    public override Task<string> GetPublicSpendKeyAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_public_spend_key(h, out var key), key)), ct);

    /// <inheritdoc />
    public override Task<string> GetPrimaryAddressAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_primary_address(h, out var address), address)), ct);

    /// <inheritdoc />
    public override Task<string> GetAddressAsync(uint accountIdx, uint subaddressIdx, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_address(h, accountIdx, subaddressIdx, out var address), address)), ct);

    /// <inheritdoc />
    public override Task<MoneroSubaddress> GetAddressIndexAsync(string address, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroSubaddress>(NativeMethods.monero_wallet_get_address_index(h, address, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroIntegratedAddress> GetIntegratedAddressAsync(string? standardAddress = null, string? paymentId = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroIntegratedAddress>(NativeMethods.monero_wallet_get_integrated_address(h, standardAddress, paymentId, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroIntegratedAddress> DecodeIntegratedAddressAsync(string integratedAddress, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroIntegratedAddress>(NativeMethods.monero_wallet_decode_integrated_address(h, integratedAddress, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string?> GetAttributeAsync(string key, CancellationToken ct = default) =>
        Run(h =>
        {
            var result = NativeMethods.monero_wallet_get_attribute(h, key, out var found, out var value);
            var text = Check.Text(result, value);
            return found ? text ?? "" : null;
        }, ct);

    /// <inheritdoc />
    public override Task SetAttributeAsync(string key, string value, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_set_attribute(h, key, value)), ct);

    /// <inheritdoc />
    public override Task<MoneroTxPriority> GetDefaultFeePriorityAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_default_fee_priority(h, out var priority));
            return (MoneroTxPriority)priority;
        }, ct);

    /// <inheritdoc />
    public override Task SaveAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_save(h)), ct);

    /// <inheritdoc />
    public override Task MoveToAsync(string path, string password, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_move_to(h, path, password)), ct);

    /// <inheritdoc />
    public override Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_change_password(h, oldPassword, newPassword)), ct);

    /// <inheritdoc />
    public override Task CloseAsync(bool save = false, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_close(h, save)), ct);

    // daemon connection and sync

    /// <inheritdoc />
    public override Task<MoneroRpcConnection?> GetDaemonConnectionAsync(CancellationToken ct = default) =>
        Run(h => Check.JsonOrNull<MoneroRpcConnection>(NativeMethods.monero_wallet_get_daemon_connection(h, out var json), json), ct);

    /// <summary>
    /// Set the wallet's daemon connection.
    /// </summary>
    /// <param name="connection">manages connection configuration, or <c>null</c> to disconnect the wallet from its daemon.</param>
    /// <param name="isTrusted">indicates if the daemon is trusted, or <c>null</c> to trust it only if its address is local.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public override Task SetDaemonConnectionAsync(MoneroRpcConnection? connection, bool? isTrusted = null, CancellationToken ct = default)
    {
        // an empty uri disconnects the wallet
        connection ??= new MoneroRpcConnection("");

        return Run(h =>
        {
            unsafe
            {
                var trusted = (byte)(isTrusted == true ? 1 : 0);

                Check.Ok(NativeMethods.monero_wallet_set_daemon_connection(h, connection.Uri ?? "", connection.Username, connection.Password,
                    connection.ProxyUri, isTrusted.HasValue ? &trusted : null, connection.SslVerify ?? true));
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task<bool> IsConnectedToDaemonAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_connected_to_daemon(h, out var connected));
            return connected;
        }, ct);

    /// <inheritdoc />
    public override Task<bool> IsDaemonSyncedAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_daemon_synced(h, out var synced));
            return synced;
        }, ct);

    /// <inheritdoc />
    public override Task<bool> IsDaemonTrustedAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_daemon_trusted(h, out var trusted));
            return trusted;
        }, ct);

    /// <inheritdoc />
    public override Task<bool> IsSyncedAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_synced(h, out var synced));
            return synced;
        }, ct);

    /// <inheritdoc />
    public override Task<ulong> GetHeightAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_height(h, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task<ulong> GetDaemonHeightAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_daemon_height(h, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task<ulong> GetDaemonMaxPeerHeightAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_daemon_max_peer_height(h, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task<ulong> GetHeightByDateAsync(int year, int month, int day, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, ushort.MaxValue);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 31);

        return Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_height_by_date(h, (ushort)year, (byte)month, (byte)day, out var height));
            return height;
        }, ct);
    }

    /// <inheritdoc />
    public override Task<ulong> GetRestoreHeightAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_get_restore_height(h, out var height));
            return height;
        }, ct);

    /// <inheritdoc />
    public override Task SetRestoreHeightAsync(ulong restoreHeight, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_set_restore_height(h, restoreHeight)), ct);

    /// <inheritdoc />
    public override async Task<MoneroSyncResult> SyncAsync(ulong? startHeight = null, MoneroWalletListener? listener = null, CancellationToken ct = default)
    {
        // the notifications of this sync go to its listener only, and the call returns after they were delivered
        WalletEvents? events = listener == null ? null : new WalletEvents(call => call(listener));

        try
        {
            return await Run(h =>
            {
                unsafe
                {
                    var height = startHeight.GetValueOrDefault();
                    var start = startHeight.HasValue ? &height : null;

                    var result = events == null
                        ? NativeMethods.monero_wallet_sync_without_listener(h, start, IntPtr.Zero, out var json)
                        : NativeMethods.monero_wallet_sync(h, start, events.Handle, out json);

                    return Check.Json<MoneroSyncResult>(result, json);
                }
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            if (events != null)
            {
                await events.CompleteAsync().ConfigureAwait(false);
                events.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public override Task StartSyncingAsync(ulong syncPeriodInMs = 10000, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_start_syncing(h, syncPeriodInMs)), ct);

    /// <inheritdoc />
    public override Task StopSyncingAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_stop_syncing(h)), ct);

    /// <inheritdoc />
    public override Task ScanTxsAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            Check.Ok(NativeMethods.monero_wallet_scan_txs(h, hashes.Pointer, hashes.Count));
        }, ct);
    }

    /// <inheritdoc />
    public override Task RescanSpentAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_rescan_spent(h)), ct);

    /// <inheritdoc />
    public override Task RescanBlockchainAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_rescan_blockchain(h)), ct);

    /// <inheritdoc />
    public override Task StartMiningAsync(ulong? numThreads = null, bool? backgroundMining = null, bool? ignoreBattery = null, CancellationToken ct = default) =>
        Run(h =>
        {
            unsafe
            {
                var threads = numThreads.GetValueOrDefault();
                var background = (byte)(backgroundMining == true ? 1 : 0);
                var battery = (byte)(ignoreBattery == true ? 1 : 0);

                Check.Ok(NativeMethods.monero_wallet_start_mining(h, numThreads.HasValue ? &threads : null,
                    backgroundMining.HasValue ? &background : null, ignoreBattery.HasValue ? &battery : null));
            }
        }, ct);

    /// <inheritdoc />
    public override Task StopMiningAsync(CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_stop_mining(h)), ct);

    // balances

    /// <inheritdoc />
    public override Task<ulong> GetBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default)
    {
        if (accountIdx == null && subaddressIdx != null) throw new ArgumentException("A subaddress needs its account", nameof(subaddressIdx));

        return Run(h =>
        {
            ulong balance;
            if (accountIdx == null) Check.Ok(NativeMethods.monero_wallet_get_balance(h, out balance));
            else if (subaddressIdx == null) Check.Ok(NativeMethods.monero_wallet_get_account_balance(h, accountIdx.Value, out balance));
            else Check.Ok(NativeMethods.monero_wallet_get_subaddress_balance(h, accountIdx.Value, subaddressIdx.Value, out balance));

            return balance;
        }, ct);
    }

    /// <inheritdoc />
    public override Task<ulong> GetUnlockedBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default)
    {
        if (accountIdx == null && subaddressIdx != null) throw new ArgumentException("A subaddress needs its account", nameof(subaddressIdx));

        return Run(h =>
        {
            ulong balance;
            if (accountIdx == null) Check.Ok(NativeMethods.monero_wallet_get_unlocked_balance(h, out balance));
            else if (subaddressIdx == null) Check.Ok(NativeMethods.monero_wallet_get_account_unlocked_balance(h, accountIdx.Value, out balance));
            else Check.Ok(NativeMethods.monero_wallet_get_subaddress_unlocked_balance(h, accountIdx.Value, subaddressIdx.Value, out balance));

            return balance;
        }, ct);
    }

    // accounts and subaddresses

    /// <inheritdoc />
    public override Task<List<MoneroAccount>> GetAccountsAsync(bool includeSubaddresses = false, string? tag = null, CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroAccount>>(NativeMethods.monero_wallet_rpc_get_accounts(h, includeSubaddresses, tag, false, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroAccount> GetAccountAsync(uint accountIdx, bool includeSubaddresses = false, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroAccount>(NativeMethods.monero_wallet_rpc_get_account(h, accountIdx, includeSubaddresses, false, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroAccount> CreateAccountAsync(string? label = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroAccount>(NativeMethods.monero_wallet_create_account(h, label, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<MoneroAccountTag>> GetAccountTagsAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<List<MoneroAccountTag>>(NativeMethods.monero_wallet_get_account_tags(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task TagAccountsAsync(string tag, IReadOnlyList<uint> accountIndices, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accountIndices);
        var indices = accountIndices.ToArray();

        return Run(h =>
        {
            unsafe
            {
                fixed (uint* ptr = indices)
                {
                    Check.Ok(NativeMethods.monero_wallet_tag_accounts(h, tag, ptr, (nuint)indices.Length));
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task UntagAccountsAsync(IReadOnlyList<uint> accountIndices, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accountIndices);
        var indices = accountIndices.ToArray();

        return Run(h =>
        {
            unsafe
            {
                fixed (uint* ptr = indices)
                {
                    Check.Ok(NativeMethods.monero_wallet_untag_accounts(h, ptr, (nuint)indices.Length));
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task SetAccountTagLabelAsync(string tag, string label, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_set_account_tag_label(h, tag, label)), ct);

    /// <inheritdoc />
    public override Task<List<MoneroSubaddress>> GetSubaddressesAsync(uint accountIdx, IReadOnlyList<uint>? subaddressIndices = null, CancellationToken ct = default)
    {
        var indices = subaddressIndices?.ToArray() ?? [];

        return Run(h =>
        {
            unsafe
            {
                fixed (uint* ptr = indices)
                {
                    return Check.Json<List<MoneroSubaddress>>(
                        NativeMethods.monero_wallet_rpc_get_subaddresses(h, accountIdx, indices.Length == 0 ? null : ptr, (nuint)indices.Length, false, out var json), json);
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task<MoneroSubaddress> CreateSubaddressAsync(uint accountIdx, string? label = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroSubaddress>(NativeMethods.monero_wallet_create_subaddress(h, accountIdx, label, out var json), json), ct);

    /// <inheritdoc />
    public override Task SetSubaddressLabelAsync(uint accountIdx, uint subaddressIdx, string? label = null, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_set_subaddress_label(h, accountIdx, subaddressIdx, label)), ct);

    // transactions, transfers and outputs

    /// <inheritdoc />
    public override Task<List<MoneroTxWallet>> GetTxsAsync(MoneroTxQuery? query = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroWalletBlocksResult>(NativeMethods.monero_wallet_get_txs(h, query == null ? null : MoneroJson.Serialize(query), out var json), json).GetTxs(), ct);

    /// <inheritdoc />
    public override Task<List<MoneroTransfer>> GetTransfersAsync(MoneroTransferQuery? query = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroWalletBlocksResult>(NativeMethods.monero_wallet_get_transfers(h, query == null ? null : MoneroJson.Serialize(query), out var json), json).GetTransfers(), ct);

    /// <inheritdoc />
    public override Task<List<MoneroOutputWallet>> GetOutputsAsync(MoneroOutputQuery? query = null, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroWalletBlocksResult>(NativeMethods.monero_wallet_get_outputs(h, query == null ? null : MoneroJson.Serialize(query), out var json), json).GetOutputs(), ct);

    /// <inheritdoc />
    public override Task<string> ExportOutputsAsync(bool all = false, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_export_outputs(h, all, out var hex), hex)), ct);

    /// <inheritdoc />
    public override Task<int> ImportOutputsAsync(string outputsHex, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_import_outputs(h, outputsHex, out var numImported));
            return numImported;
        }, ct);

    /// <inheritdoc />
    public override Task<MoneroKeyImageExportResult> ExportKeyImagesAsync(bool all = false, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroKeyImageExportResult>(NativeMethods.monero_wallet_export_key_images(h, all, out var json), json), ct);

    /// <inheritdoc />
    public override Task<MoneroKeyImageImportResult> ImportKeyImagesAsync(IReadOnlyList<MoneroKeyImage> keyImages, ulong offset = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keyImages);
        var json = MoneroJson.Serialize(keyImages);

        return Run(h => Check.Json<MoneroKeyImageImportResult>(NativeMethods.monero_wallet_import_key_images(h, json, offset, out var result), result), ct);
    }

    /// <inheritdoc />
    public override Task FreezeOutputAsync(string keyImage, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_freeze_output(h, keyImage)), ct);

    /// <inheritdoc />
    public override Task ThawOutputAsync(string keyImage, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_thaw_output(h, keyImage)), ct);

    /// <inheritdoc />
    public override Task<bool> IsOutputFrozenAsync(string keyImage, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_output_frozen(h, keyImage, out var frozen));
            return frozen;
        }, ct);

    // creating, relaying and signing transactions

    private static MoneroTxSet FirstTxSet(MoneroTxSetsResult result) =>
        result.TxSets is { Count: > 0 } ? result.TxSets[0] : throw new MoneroError("The wallet returned no transaction set");

    private static MoneroTxWallet FirstTx(MoneroTxSetsResult result) =>
        result.GetTxs() is { Count: > 0 } txs ? txs[0] : throw new MoneroError("The wallet returned no transaction");

    /// <inheritdoc />
    public override Task<MoneroTxWallet> CreateTxAsync(MoneroTxConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = MoneroJson.Serialize(config);

        return Run(h => FirstTx(Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_create_tx(h, json, out var result), result)), ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroTxWallet>> CreateTxsAsync(MoneroTxConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = MoneroJson.Serialize(config);

        return Run(h => Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_create_txs(h, json, out var result), result).GetTxs(), ct);
    }

    /// <inheritdoc />
    public override Task<MoneroTxWallet> SweepOutputAsync(MoneroTxConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = MoneroJson.Serialize(config);

        return Run(h => FirstTx(Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_sweep_output(h, json, out var result), result)), ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroTxWallet>> SweepUnlockedAsync(MoneroTxConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = MoneroJson.Serialize(config);

        return Run(h => Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_sweep_unlocked(h, json, out var result), result).GetTxs(), ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroTxWallet>> SweepDustAsync(bool relay = false, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_sweep_dust(h, relay, out var result), result).GetTxs(), ct);

    /// <inheritdoc />
    public override Task<string> RelayTxAsync(string txMetadata, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_relay_tx(h, txMetadata, out var hash), hash)), ct);

    /// <inheritdoc />
    public override Task<string> RelayTxAsync(MoneroTxWallet tx, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tx);
        var json = MoneroJson.Serialize(tx);

        return Run(h => Required(Check.Text(NativeMethods.monero_wallet_relay_tx_json(h, json, out var hash), hash)), ct);
    }

    /// <inheritdoc />
    public override Task<List<string>> RelayTxsAsync(IReadOnlyList<string> txMetadatas, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txMetadatas);

        return Run(h =>
        {
            using var metadatas = new NativeStringArray(txMetadatas);
            return Check.Strings(NativeMethods.monero_wallet_relay_txs(h, metadatas.Pointer, metadatas.Count, out var json), json);
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<string>> RelayTxsAsync(IReadOnlyList<MoneroTxWallet> txs, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txs);
        var txsJson = MoneroJson.Serialize(txs);

        return Run(h => Check.Strings(NativeMethods.monero_wallet_relay_txs_json(h, txsJson, out var json), json), ct);
    }

    /// <inheritdoc />
    public override Task<MoneroTxSet> DescribeTxSetAsync(MoneroTxSet txSet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txSet);
        var txSetJson = MoneroJson.Serialize(txSet);

        return Run(h => FirstTxSet(Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_describe_tx_set(h, txSetJson, out var json), json)), ct);
    }

    /// <inheritdoc />
    public override Task<MoneroTxSet> SignTxsAsync(string unsignedTxHex, CancellationToken ct = default) =>
        Run(h => FirstTxSet(Check.Json<MoneroTxSetsResult>(NativeMethods.monero_wallet_sign_txs(h, unsignedTxHex, out var json), json)), ct);

    /// <inheritdoc />
    public override Task<List<string>> SubmitTxsAsync(string signedTxHex, CancellationToken ct = default) =>
        Run(h => Check.Strings(NativeMethods.monero_wallet_submit_txs(h, signedTxHex, out var json), json), ct);

    // notes, payment uris and signatures

    /// <inheritdoc />
    public override Task<string> GetTxNoteAsync(string txHash, CancellationToken ct = default) =>
        Run(h => Check.Text(NativeMethods.monero_wallet_get_tx_note(h, txHash, out var note), note) ?? "", ct);

    /// <inheritdoc />
    public override Task<List<string>> GetTxNotesAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            return Check.Strings(NativeMethods.monero_wallet_get_tx_notes(h, hashes.Pointer, hashes.Count, out var json), json);
        }, ct);
    }

    /// <inheritdoc />
    public override Task SetTxNoteAsync(string txHash, string note, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_set_tx_note(h, txHash, note)), ct);

    /// <inheritdoc />
    public override Task SetTxNotesAsync(IReadOnlyList<string> txHashes, IReadOnlyList<string> notes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);
        ArgumentNullException.ThrowIfNull(notes);
        if (txHashes.Count != notes.Count) throw new ArgumentException("There must be a note for each hash", nameof(notes));

        return Run(h =>
        {
            using var hashes = new NativeStringArray(txHashes);
            using var texts = new NativeStringArray(notes);
            Check.Ok(NativeMethods.monero_wallet_set_tx_notes(h, hashes.Pointer, texts.Pointer, hashes.Count));
        }, ct);
    }

    /// <inheritdoc />
    public override Task<List<MoneroAddressBookEntry>> GetAddressBookEntriesAsync(IReadOnlyList<ulong>? indices = null, CancellationToken ct = default)
    {
        var array = indices?.ToArray() ?? [];

        return Run(h =>
        {
            unsafe
            {
                fixed (ulong* ptr = array)
                {
                    return Check.Json<List<MoneroAddressBookEntry>>(
                        NativeMethods.monero_wallet_get_address_book_entries(h, array.Length == 0 ? null : ptr, (nuint)array.Length, out var json), json);
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public override Task<ulong> AddAddressBookEntryAsync(string address, string? description = null, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_add_address_book_entry(h, address, description, out var index));
            return index;
        }, ct);

    /// <inheritdoc />
    public override Task EditAddressBookEntryAsync(ulong index, string? address, string? description, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_edit_address_book_entry(h, index, address, description)), ct);

    /// <inheritdoc />
    public override Task DeleteAddressBookEntryAsync(ulong index, CancellationToken ct = default) =>
        Run(h => Check.Ok(NativeMethods.monero_wallet_delete_address_book_entry(h, index)), ct);

    /// <inheritdoc />
    public override Task<string> GetPaymentUriAsync(MoneroTxConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = MoneroJson.Serialize(config);

        return Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_payment_uri(h, json, out var uri), uri)), ct);
    }

    /// <inheritdoc />
    public override Task<MoneroTxConfig> ParsePaymentUriAsync(string uri, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroTxConfig>(NativeMethods.monero_wallet_parse_payment_uri(h, uri, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> SignMessageAsync(string message, MoneroMessageSignatureType signatureType = MoneroMessageSignatureType.SignWithSpendKey, uint accountIdx = 0, uint subaddressIdx = 0, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_sign_message(h, message, (int)signatureType, accountIdx, subaddressIdx, out var signature), signature)), ct);

    /// <inheritdoc />
    public override Task<MoneroMessageSignatureResult> VerifyMessageAsync(string message, string address, string signature, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMessageSignatureResult>(NativeMethods.monero_wallet_verify_message(h, message, address, signature, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> GetTxKeyAsync(string txHash, CancellationToken ct = default) =>
        Run(h => Required(Check.Secret(NativeMethods.monero_wallet_get_tx_key(h, txHash, out var key), key)), ct);

    /// <inheritdoc />
    public override Task<MoneroCheckTx> CheckTxKeyAsync(string txHash, string txKey, string address, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroCheckTx>(NativeMethods.monero_wallet_check_tx_key(h, txHash, txKey, address, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> GetTxProofAsync(string txHash, string address, string? message = null, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_tx_proof(h, txHash, address, message, out var signature), signature)), ct);

    /// <inheritdoc />
    public override Task<MoneroCheckTx> CheckTxProofAsync(string txHash, string address, string? message, string signature, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroCheckTx>(NativeMethods.monero_wallet_check_tx_proof(h, txHash, address, message, signature, out var json), json), ct);

    /// <inheritdoc />
    public override Task<string> GetSpendProofAsync(string txHash, string? message = null, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_spend_proof(h, txHash, message, out var signature), signature)), ct);

    /// <inheritdoc />
    public override Task<bool> CheckSpendProofAsync(string txHash, string? message, string signature, CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_check_spend_proof(h, txHash, message, signature, out var good));
            return good;
        }, ct);

    /// <inheritdoc />
    public override Task<string> GetReserveProofWalletAsync(string? message = null, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_reserve_proof_wallet(h, message, out var signature), signature)), ct);

    /// <inheritdoc />
    public override Task<string> GetReserveProofAccountAsync(uint accountIdx, ulong amount, string? message = null, CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_get_reserve_proof_account(h, accountIdx, amount, message, out var signature), signature)), ct);

    /// <inheritdoc />
    public override Task<MoneroCheckReserve> CheckReserveProofAsync(string address, string? message, string signature, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroCheckReserve>(NativeMethods.monero_wallet_check_reserve_proof(h, address, message, signature, out var json), json), ct);

    // multisig

    /// <inheritdoc />
    public override Task<bool> IsMultisigAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_multisig(h, out var multisig));
            return multisig;
        }, ct);

    /// <inheritdoc />
    public override Task<MoneroMultisigInfo> GetMultisigInfoAsync(CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMultisigInfo>(NativeMethods.monero_wallet_get_multisig_info(h, out var json), json), ct);

    /// <inheritdoc />
    public override Task<bool> IsMultisigImportNeededAsync(CancellationToken ct = default) =>
        Run(h =>
        {
            Check.Ok(NativeMethods.monero_wallet_is_multisig_import_needed(h, out var needed));
            return needed;
        }, ct);

    /// <inheritdoc />
    public override Task<string> PrepareMultisigAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_prepare_multisig(h, out var hex), hex)), ct);

    /// <inheritdoc />
    public override Task<string> MakeMultisigAsync(IReadOnlyList<string> multisigHexes, int threshold, string password, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(multisigHexes);

        return Run(h =>
        {
            using var hexes = new NativeStringArray(multisigHexes);
            return Check.Text(NativeMethods.monero_wallet_make_multisig(h, hexes.Pointer, hexes.Count, threshold, password, out var hex), hex) ?? "";
        }, ct);
    }

    /// <inheritdoc />
    public override Task<MoneroMultisigInitResult> ExchangeMultisigKeysAsync(IReadOnlyList<string> multisigHexes, string password, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(multisigHexes);

        return Run(h =>
        {
            using var hexes = new NativeStringArray(multisigHexes);
            return Check.Json<MoneroMultisigInitResult>(NativeMethods.monero_wallet_exchange_multisig_keys(h, hexes.Pointer, hexes.Count, password, out var json), json);
        }, ct);
    }

    /// <inheritdoc />
    public override Task<string> ExportMultisigHexAsync(CancellationToken ct = default) =>
        Run(h => Required(Check.Text(NativeMethods.monero_wallet_export_multisig_hex(h, out var hex), hex)), ct);

    /// <inheritdoc />
    public override Task<int> ImportMultisigHexAsync(IReadOnlyList<string> multisigHexes, bool refreshAfterImport = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(multisigHexes);

        return Run(h =>
        {
            using var hexes = new NativeStringArray(multisigHexes);
            Check.Ok(NativeMethods.monero_wallet_import_multisig_hex(h, hexes.Pointer, hexes.Count, refreshAfterImport, out var numImported));
            return numImported;
        }, ct);
    }

    /// <inheritdoc />
    public override Task<MoneroMultisigSignResult> SignMultisigTxHexAsync(string multisigTxHex, CancellationToken ct = default) =>
        Run(h => Check.Json<MoneroMultisigSignResult>(NativeMethods.monero_wallet_sign_multisig_tx_hex(h, multisigTxHex, out var json), json), ct);

    /// <inheritdoc />
    public override Task<List<string>> SubmitMultisigTxHexAsync(string signedMultisigTxHex, CancellationToken ct = default) =>
        Run(h => Check.Strings(NativeMethods.monero_wallet_submit_multisig_tx_hex(h, signedMultisigTxHex, out var json), json), ct);

    // closing

    /// <summary>
    /// Release the native wallet. The calls that were in the queue run first, and the listeners are unregistered. The wallet stays open on the
    /// server unless it was closed.
    /// </summary>
    /// <returns>a task that ends when the wallet is released.</returns>
    public async ValueTask DisposeAsync()
    {
        TaskCompletionSource<ulong>[] waiters;

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
                    NativeMethods.monero_wallet_remove_listener(_handle, _events!.Handle);
                }
                catch (Exception)
                {
                    // the wallet is going away anyway
                }
            }
        }).ConfigureAwait(false);

        if (_events != null)
        {
            await _events.CompleteAsync().ConfigureAwait(false);
            _events.Dispose();
        }

        _worker.Dispose();
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Release the native wallet, and wait for it.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
