using Monero.Common;
using Monero.Daemon.Model;
using Monero.Wallet.Model;

namespace Monero.Wallet;

/// <summary>
/// Monero wallet interface. Methods that use the wallet are async. A wallet runs one call at a time: the calls queue in the order they were made.
/// </summary>
public abstract class MoneroWallet
{
    // listeners

    /// <summary>
    /// Register a listener to receive wallet notifications.
    /// </summary>
    /// <param name="listener">the listener to register.</param>
    public abstract void AddListener(MoneroWalletListener listener);

    /// <summary>
    /// Unregister a listener to receive wallet notifications.
    /// </summary>
    /// <param name="listener">a previously registered listener to be unregistered.</param>
    public abstract void RemoveListener(MoneroWalletListener listener);

    /// <summary>
    /// Get the listeners registered with the wallet.
    /// </summary>
    /// <returns>the registered listeners.</returns>
    public abstract List<MoneroWalletListener> GetListeners();

    /// <summary>
    /// Unregister all listeners registered with the wallet.
    /// </summary>
    public abstract void RemoveListeners();

    // general

    /// <summary>
    /// Get the version of the wallet.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the version of the wallet.</returns>
    public abstract Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's path.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the path the wallet can be opened with.</returns>
    public abstract Task<string> GetPathAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's network type.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the network type of the wallet.</returns>
    public abstract Task<MoneroNetworkType> GetNetworkTypeAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the wallet is view-only, meaning it does not have the private spend key and can therefore only observe incoming outputs.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the wallet is view-only, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsViewOnlyAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the wallet is closed or not.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the wallet is closed, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsClosedAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's mnemonic phrase or seed.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's mnemonic phrase or seed.</returns>
    public abstract Task<string> GetSeedAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the language of the wallet's mnemonic phrase or seed.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the language of the wallet's mnemonic phrase or seed.</returns>
    public abstract Task<string> GetSeedLanguageAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's private view key.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's private view key.</returns>
    public abstract Task<string> GetPrivateViewKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's private spend key.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's private spend key.</returns>
    public abstract Task<string> GetPrivateSpendKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's public view key.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's public view key.</returns>
    public abstract Task<string> GetPublicViewKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's public spend key.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's public spend key.</returns>
    public abstract Task<string> GetPublicSpendKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the wallet's primary address.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's primary address.</returns>
    public abstract Task<string> GetPrimaryAddressAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the address of a specific subaddress.
    /// </summary>
    /// <param name="accountIdx">specifies the account index of the address's subaddress.</param>
    /// <param name="subaddressIdx">specifies the subaddress index within the account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the receive address of the specified subaddress.</returns>
    public abstract Task<string> GetAddressAsync(uint accountIdx, uint subaddressIdx, CancellationToken ct = default);

    /// <summary>
    /// Get the account and subaddress index of the given address.
    /// </summary>
    /// <param name="address">is the address to get the account and subaddress index from.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the account and subaddress indices.</returns>
    public abstract Task<MoneroSubaddress> GetAddressIndexAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Get an integrated address based on the given standard address and payment ID.
    /// </summary>
    /// <param name="standardAddress">is the standard address to generate the integrated address from, or <c>null</c> for the wallet's primary address.</param>
    /// <param name="paymentId">is the payment ID to generate an integrated address from, or <c>null</c> for a random one.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the integrated address.</returns>
    public abstract Task<MoneroIntegratedAddress> GetIntegratedAddressAsync(string? standardAddress = null, string? paymentId = null, CancellationToken ct = default);

    /// <summary>
    /// Decode an integrated address to get its standard address and payment id.
    /// </summary>
    /// <param name="integratedAddress">is the integrated address to decode.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the decoded integrated address, including the standard address and payment id.</returns>
    public abstract Task<MoneroIntegratedAddress> DecodeIntegratedAddressAsync(string integratedAddress, CancellationToken ct = default);

    /// <summary>
    /// Get an attribute that the wallet stores with its files.
    /// </summary>
    /// <param name="key">is the attribute to get the value of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the attribute's value, or <c>null</c> if the attribute does not exist.</returns>
    public abstract Task<string?> GetAttributeAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Set an arbitrary attribute, stored with the wallet's files.
    /// </summary>
    /// <param name="key">is the attribute key.</param>
    /// <param name="value">is the attribute value.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetAttributeAsync(string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Get the default priority of the fee of the transactions.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the default priority.</returns>
    public abstract Task<MoneroTxPriority> GetDefaultFeePriorityAsync(CancellationToken ct = default);

    /// <summary>
    /// Save the wallet at its current path.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// Move the wallet from its current path to the given path.
    /// </summary>
    /// <param name="path">is the wallet's destination path.</param>
    /// <param name="password">is the password of the wallet files at the destination path.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task MoveToAsync(string path, string password, CancellationToken ct = default);

    /// <summary>
    /// Change the wallet password.
    /// </summary>
    /// <param name="oldPassword">is the wallet's old password.</param>
    /// <param name="newPassword">is the wallet's new password.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Close the wallet, saving its state first if requested.
    /// </summary>
    /// <param name="save">specifies if the wallet's state should be saved before closing.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task CloseAsync(bool save = false, CancellationToken ct = default);

    // daemon connection and sync

    /// <summary>
    /// Get the wallet's daemon connection.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's daemon connection, or <c>null</c> if it has none.</returns>
    public abstract Task<MoneroRpcConnection?> GetDaemonConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the wallet's daemon connection.
    /// </summary>
    /// <param name="connection">manages connection configuration, or <c>null</c> to disconnect the wallet from its daemon.</param>
    /// <param name="isTrusted">indicates if the daemon is trusted, or <c>null</c> to trust it only if its address is local.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetDaemonConnectionAsync(MoneroRpcConnection? connection, bool? isTrusted = null, CancellationToken ct = default);

    /// <summary>
    /// Set the wallet's daemon connection.
    /// </summary>
    /// <param name="uri">is the uri of the daemon to connect to, or an empty string to disconnect the wallet from its daemon.</param>
    /// <param name="username">is the username to authenticate with, or <c>null</c> for none.</param>
    /// <param name="password">is the password to authenticate with, or <c>null</c> for none.</param>
    /// <param name="proxyUri">is the uri of a proxy to connect through, or <c>null</c> for none.</param>
    /// <param name="isTrusted">indicates if the daemon is trusted, or <c>null</c> to trust it only if its address is local.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public virtual Task SetDaemonConnectionAsync(string uri, string? username = null, string? password = null, string? proxyUri = null, bool? isTrusted = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return SetDaemonConnectionAsync(new MoneroRpcConnection(uri, username, password) { ProxyUri = proxyUri }, isTrusted, ct);
    }

    /// <summary>
    /// Indicates if the wallet is connected to its daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the wallet is connected to its daemon, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsConnectedToDaemonAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the wallet's daemon is synced with the network.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the daemon is synced with the network, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsDaemonSyncedAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the wallet's daemon is trusted.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the daemon is trusted, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsDaemonTrustedAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the wallet is synced with the daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the wallet is synced with the daemon, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsSyncedAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the number of blocks the wallet has synced.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the height of the last block the wallet has synced.</returns>
    public abstract Task<ulong> GetHeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the blockchain's height.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blockchain's height.</returns>
    public abstract Task<ulong> GetDaemonHeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the maximum height of the peers the wallet's daemon is connected to.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the maximum height of the peers the wallet's daemon is connected to.</returns>
    public abstract Task<ulong> GetDaemonMaxPeerHeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the blockchain's height by date as a conservative estimate for scanning.
    /// </summary>
    /// <param name="year">year of the height to get.</param>
    /// <param name="month">month of the height to get as a number between 1 and 12.</param>
    /// <param name="day">day of the height to get as a number between 1 and 31.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the blockchain's approximate height at the given date.</returns>
    public abstract Task<ulong> GetHeightByDateAsync(int year, int month, int day, CancellationToken ct = default);

    /// <summary>
    /// Get the height of the first block that the wallet scans.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the height of the first block that the wallet scans.</returns>
    public abstract Task<ulong> GetRestoreHeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the height of the first block that the wallet scans.
    /// </summary>
    /// <param name="restoreHeight">is the height of the first block that the wallet scans.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetRestoreHeightAsync(ulong restoreHeight, CancellationToken ct = default);

    /// <summary>
    /// Synchronize the wallet with the daemon as a one-time synchronous process.
    /// </summary>
    /// <param name="startHeight">is the start height to sync from, or <c>null</c> to sync from the last synced block.</param>
    /// <param name="listener">receives notifications as the sync progresses, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the sync result.</returns>
    public abstract Task<MoneroSyncResult> SyncAsync(ulong? startHeight = null, MoneroWalletListener? listener = null, CancellationToken ct = default);

    /// <summary>
    /// Start background synchronizing with a maximum period between syncs.
    /// </summary>
    /// <param name="syncPeriodInMs">is the maximum period between syncs in milliseconds.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StartSyncingAsync(ulong syncPeriodInMs = 10000, CancellationToken ct = default);

    /// <summary>
    /// Stop synchronizing the wallet with the daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StopSyncingAsync(CancellationToken ct = default);

    /// <summary>
    /// Wait for the next block to be added to the chain. The wallet must be syncing in the background.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the height of the next block that was added to the chain.</returns>
    public abstract Task<ulong> WaitForNextBlockAsync(CancellationToken ct = default);

    /// <summary>
    /// Scan transactions by their hash.
    /// </summary>
    /// <param name="txHashes">are the hashes of the transactions to scan.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task ScanTxsAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default);

    /// <summary>
    /// Rescan the blockchain for spent outputs. It needs a trusted daemon.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task RescanSpentAsync(CancellationToken ct = default);

    /// <summary>
    /// Rescan the blockchain from scratch, losing any information which cannot be recovered from the blockchain itself.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task RescanBlockchainAsync(CancellationToken ct = default);

    /// <summary>
    /// Start mining.
    /// </summary>
    /// <param name="numThreads">is the number of threads created for mining, or <c>null</c> for the default.</param>
    /// <param name="backgroundMining">specifies if mining should occur in the background, or <c>null</c> for the default.</param>
    /// <param name="ignoreBattery">specifies if the battery should be ignored for mining, or <c>null</c> for the default.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StartMiningAsync(ulong? numThreads = null, bool? backgroundMining = null, bool? ignoreBattery = null, CancellationToken ct = default);

    /// <summary>
    /// Stop mining.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task StopMiningAsync(CancellationToken ct = default);

    // balances

    /// <summary>
    /// Get the balance of the wallet, an account or a subaddress.
    /// </summary>
    /// <param name="accountIdx">is the index of the account, or <c>null</c> for the whole wallet.</param>
    /// <param name="subaddressIdx">is the index of the subaddress within the account, or <c>null</c> for the whole account. It needs an account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the balance, in atomic units.</returns>
    public abstract Task<ulong> GetBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default);

    /// <summary>
    /// Get the unlocked balance of the wallet, an account or a subaddress.
    /// </summary>
    /// <param name="accountIdx">is the index of the account, or <c>null</c> for the whole wallet.</param>
    /// <param name="subaddressIdx">is the index of the subaddress within the account, or <c>null</c> for the whole account. It needs an account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the unlocked balance, in atomic units.</returns>
    public abstract Task<ulong> GetUnlockedBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default);

    // accounts and subaddresses

    /// <summary>
    /// Get the accounts of the wallet.
    /// </summary>
    /// <param name="includeSubaddresses">includes the subaddresses of each account if <c>true</c>.</param>
    /// <param name="tag">is the tag of the accounts to get, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the accounts.</returns>
    public abstract Task<List<MoneroAccount>> GetAccountsAsync(bool includeSubaddresses = false, string? tag = null, CancellationToken ct = default);

    /// <summary>
    /// Get an account.
    /// </summary>
    /// <param name="accountIdx">is the index of the account to get.</param>
    /// <param name="includeSubaddresses">includes the subaddresses of the account if <c>true</c>.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the account.</returns>
    public abstract Task<MoneroAccount> GetAccountAsync(uint accountIdx, bool includeSubaddresses = false, CancellationToken ct = default);

    /// <summary>
    /// Create a new account.
    /// </summary>
    /// <param name="label">is the label to apply to the new account, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created account.</returns>
    public abstract Task<MoneroAccount> CreateAccountAsync(string? label = null, CancellationToken ct = default);

    /// <summary>
    /// Get the tags of the accounts.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tags of the accounts.</returns>
    public abstract Task<List<MoneroAccountTag>> GetAccountTagsAsync(CancellationToken ct = default);

    /// <summary>
    /// Tag accounts.
    /// </summary>
    /// <param name="tag">is the tag to apply to the specified accounts.</param>
    /// <param name="accountIndices">are the indices of the accounts to tag.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task TagAccountsAsync(string tag, IReadOnlyList<uint> accountIndices, CancellationToken ct = default);

    /// <summary>
    /// Untag accounts.
    /// </summary>
    /// <param name="accountIndices">are the indices of the accounts to untag.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task UntagAccountsAsync(IReadOnlyList<uint> accountIndices, CancellationToken ct = default);

    /// <summary>
    /// Set the label of an account tag.
    /// </summary>
    /// <param name="tag">is the tag to set a label for.</param>
    /// <param name="label">is the label to set.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetAccountTagLabelAsync(string tag, string label, CancellationToken ct = default);

    /// <summary>
    /// Get the subaddresses in an account.
    /// </summary>
    /// <param name="accountIdx">specifies the account to get subaddresses within.</param>
    /// <param name="subaddressIndices">are the indices of the subaddresses to get, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the subaddresses.</returns>
    public abstract Task<List<MoneroSubaddress>> GetSubaddressesAsync(uint accountIdx, IReadOnlyList<uint>? subaddressIndices = null, CancellationToken ct = default);

    /// <summary>
    /// Get a subaddress.
    /// </summary>
    /// <param name="accountIdx">specifies the index of the subaddress's account.</param>
    /// <param name="subaddressIdx">specifies the index of the subaddress within the account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the subaddress.</returns>
    public virtual async Task<MoneroSubaddress> GetSubaddressAsync(uint accountIdx, uint subaddressIdx, CancellationToken ct = default)
    {
        var subaddresses = await GetSubaddressesAsync(accountIdx, [subaddressIdx], ct).ConfigureAwait(false);

        return subaddresses.Count == 1 ? subaddresses[0] : throw new MoneroError($"The subaddress {accountIdx},{subaddressIdx} is not found");
    }

    /// <summary>
    /// Create a subaddress within an account.
    /// </summary>
    /// <param name="accountIdx">specifies the index of the account to create the subaddress within.</param>
    /// <param name="label">is the label for the subaddress, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created subaddress.</returns>
    public abstract Task<MoneroSubaddress> CreateSubaddressAsync(uint accountIdx, string? label = null, CancellationToken ct = default);

    /// <summary>
    /// Set the label of a subaddress.
    /// </summary>
    /// <param name="accountIdx">specifies the index of the account with the subaddress.</param>
    /// <param name="subaddressIdx">specifies the index of the subaddress within the account.</param>
    /// <param name="label">is the label to set, or <c>null</c> to clear it.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetSubaddressLabelAsync(uint accountIdx, uint subaddressIdx, string? label = null, CancellationToken ct = default);

    // transactions, transfers and outputs

    /// <summary>
    /// Get wallet transactions. A transaction is the one of a block, in the pool, or relayed and failed.
    /// </summary>
    /// <param name="query">filters the transactions, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transactions that match the query.</returns>
    public abstract Task<List<MoneroTxWallet>> GetTxsAsync(MoneroTxQuery? query = null, CancellationToken ct = default);

    /// <summary>
    /// Get wallet transactions by their hash, in the order of the hashes.
    /// </summary>
    /// <param name="txHashes">are the hashes of the transactions to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transactions of the hashes that the wallet has.</returns>
    public virtual async Task<List<MoneroTxWallet>> GetTxsAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(txHashes);

        var txs = await GetTxsAsync(new MoneroTxQuery { Hashes = [.. txHashes] }, ct).ConfigureAwait(false);
        var position = new Dictionary<string, int>();
        for (var i = 0; i < txHashes.Count; i++) position.TryAdd(txHashes[i], i);

        return [.. txs.OrderBy(tx => tx.Hash != null && position.TryGetValue(tx.Hash, out var i) ? i : int.MaxValue)];
    }

    /// <summary>
    /// Get a wallet transaction by its hash.
    /// </summary>
    /// <param name="txHash">is the hash of the transaction to get.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction, or <c>null</c> if the wallet doesn't have it.</returns>
    public virtual async Task<MoneroTxWallet?> GetTxAsync(string txHash, CancellationToken ct = default)
    {
        var txs = await GetTxsAsync([txHash], ct).ConfigureAwait(false);

        return txs.Count == 0 ? null : txs[0];
    }

    /// <summary>
    /// Get incoming and outgoing transfers to and from this wallet.
    /// </summary>
    /// <param name="query">filters the transfers, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transfers that match the query.</returns>
    public abstract Task<List<MoneroTransfer>> GetTransfersAsync(MoneroTransferQuery? query = null, CancellationToken ct = default);

    /// <summary>
    /// Get incoming and outgoing transfers of an account, or of one of its subaddresses.
    /// </summary>
    /// <param name="accountIdx">is the index of the account.</param>
    /// <param name="subaddressIdx">is the index of the subaddress within the account, or <c>null</c> for the whole account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transfers of the account or of the subaddress.</returns>
    public virtual Task<List<MoneroTransfer>> GetTransfersAsync(uint accountIdx, uint? subaddressIdx = null, CancellationToken ct = default)
    {
        return GetTransfersAsync(new MoneroTransferQuery { AccountIndex = accountIdx, SubaddressIndex = subaddressIdx }, ct);
    }

    /// <summary>
    /// Get the outputs of the wallet.
    /// </summary>
    /// <param name="query">filters the outputs, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the outputs that match the query.</returns>
    public abstract Task<List<MoneroOutputWallet>> GetOutputsAsync(MoneroOutputQuery? query = null, CancellationToken ct = default);

    /// <summary>
    /// Export outputs in hex format.
    /// </summary>
    /// <param name="all">exports all the outputs if <c>true</c>, or only the ones that changed since the last export if <c>false</c>.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the outputs in hex format.</returns>
    public abstract Task<string> ExportOutputsAsync(bool all = false, CancellationToken ct = default);

    /// <summary>
    /// Import outputs in hex format.
    /// </summary>
    /// <param name="outputsHex">are outputs in hex format.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the number of outputs imported.</returns>
    public abstract Task<int> ImportOutputsAsync(string outputsHex, CancellationToken ct = default);

    /// <summary>
    /// Export signed key images.
    /// </summary>
    /// <param name="all">exports all key images if <c>true</c>, or only the ones that changed since the last export if <c>false</c>.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the wallet's signed key images.</returns>
    public abstract Task<MoneroKeyImageExportResult> ExportKeyImagesAsync(bool all = false, CancellationToken ct = default);

    /// <summary>
    /// Import signed key images and verify their spent status.
    /// </summary>
    /// <param name="keyImages">are images to import and verify.</param>
    /// <param name="offset">is the offset of the first key image among the wallet's outputs.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the results of the import.</returns>
    public abstract Task<MoneroKeyImageImportResult> ImportKeyImagesAsync(IReadOnlyList<MoneroKeyImage> keyImages, ulong offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Freeze an output so that it is not spent.
    /// </summary>
    /// <param name="keyImage">is the key image of the output to freeze.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task FreezeOutputAsync(string keyImage, CancellationToken ct = default);

    /// <summary>
    /// Thaw a frozen output so that it can be spent.
    /// </summary>
    /// <param name="keyImage">is the key image of the output to thaw.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task ThawOutputAsync(string keyImage, CancellationToken ct = default);

    /// <summary>
    /// Check if an output is frozen.
    /// </summary>
    /// <param name="keyImage">is the key image of the output to check.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the output is frozen, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsOutputFrozenAsync(string keyImage, CancellationToken ct = default);

    // creating, relaying and signing transactions

    /// <summary>
    /// Create a transaction to transfer funds from this wallet.
    /// </summary>
    /// <param name="config">configures the transaction to create.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created transaction.</returns>
    public abstract Task<MoneroTxWallet> CreateTxAsync(MoneroTxConfig config, CancellationToken ct = default);

    /// <summary>
    /// Create one or more transactions to transfer funds from this wallet.
    /// </summary>
    /// <param name="config">configures the transactions to create.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created transactions.</returns>
    public abstract Task<List<MoneroTxWallet>> CreateTxsAsync(MoneroTxConfig config, CancellationToken ct = default);

    /// <summary>
    /// Sweep an output with a given key image.
    /// </summary>
    /// <param name="config">configures the transaction to create.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created transaction.</returns>
    public abstract Task<MoneroTxWallet> SweepOutputAsync(MoneroTxConfig config, CancellationToken ct = default);

    /// <summary>
    /// Sweep all unlocked funds according to the given configuration.
    /// </summary>
    /// <param name="config">configures the transactions to create.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created transactions.</returns>
    public abstract Task<List<MoneroTxWallet>> SweepUnlockedAsync(MoneroTxConfig config, CancellationToken ct = default);

    /// <summary>
    /// Sweep all unmixable dust outputs back to the wallet to make them easier to spend and mix.
    /// </summary>
    /// <param name="relay">specifies if the resulting transaction should be relayed.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the created transactions.</returns>
    public abstract Task<List<MoneroTxWallet>> SweepDustAsync(bool relay = false, CancellationToken ct = default);

    /// <summary>
    /// Relay a previously created transaction.
    /// </summary>
    /// <param name="txMetadata">is the metadata of the transaction to relay.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hash of the relayed transaction.</returns>
    public abstract Task<string> RelayTxAsync(string txMetadata, CancellationToken ct = default);

    /// <summary>
    /// Relay a previously created transaction.
    /// </summary>
    /// <param name="tx">is the transaction to relay, with its metadata.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hash of the relayed transaction.</returns>
    public abstract Task<string> RelayTxAsync(MoneroTxWallet tx, CancellationToken ct = default);

    /// <summary>
    /// Relay previously created transactions.
    /// </summary>
    /// <param name="txMetadatas">are the metadata of the transactions to relay.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hashes of the relayed transactions.</returns>
    public abstract Task<List<string>> RelayTxsAsync(IReadOnlyList<string> txMetadatas, CancellationToken ct = default);

    /// <summary>
    /// Relay previously created transactions.
    /// </summary>
    /// <param name="txs">are the transactions to relay, with their metadata.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hashes of the relayed transactions.</returns>
    public abstract Task<List<string>> RelayTxsAsync(IReadOnlyList<MoneroTxWallet> txs, CancellationToken ct = default);

    /// <summary>
    /// Describe a tx set containing unsigned or multisig tx hex to a new tx set containing structured transactions.
    /// </summary>
    /// <param name="txSet">is a tx set containing unsigned or multisig tx hex.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx set with the structured transactions.</returns>
    public abstract Task<MoneroTxSet> DescribeTxSetAsync(MoneroTxSet txSet, CancellationToken ct = default);

    /// <summary>
    /// Describe unsigned tx hex as a tx set with structured transactions.
    /// </summary>
    /// <param name="unsignedTxHex">is unsigned tx hex from when the transactions were created.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx set with the structured transactions.</returns>
    public virtual Task<MoneroTxSet> DescribeUnsignedTxSetAsync(string unsignedTxHex, CancellationToken ct = default)
    {
        return DescribeTxSetAsync(new MoneroTxSet { UnsignedTxHex = unsignedTxHex }, ct);
    }

    /// <summary>
    /// Describe multisig tx hex as a tx set with structured transactions.
    /// </summary>
    /// <param name="multisigTxHex">is multisig tx hex from when the transactions were created.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx set with the structured transactions.</returns>
    public virtual Task<MoneroTxSet> DescribeMultisigTxSetAsync(string multisigTxHex, CancellationToken ct = default)
    {
        return DescribeTxSetAsync(new MoneroTxSet { MultisigTxHex = multisigTxHex }, ct);
    }

    /// <summary>
    /// Sign unsigned transactions from a view-only wallet.
    /// </summary>
    /// <param name="unsignedTxHex">is unsigned tx hex from when the transactions were created.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx set with the signed transactions and their hex.</returns>
    public abstract Task<MoneroTxSet> SignTxsAsync(string unsignedTxHex, CancellationToken ct = default);

    /// <summary>
    /// Submit signed transactions from a view-only wallet.
    /// </summary>
    /// <param name="signedTxHex">is signed tx hex from signing the transactions.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hashes of the submitted transactions.</returns>
    public abstract Task<List<string>> SubmitTxsAsync(string signedTxHex, CancellationToken ct = default);

    // notes, payment uris and signatures

    /// <summary>
    /// Get a transaction note.
    /// </summary>
    /// <param name="txHash">specifies the transaction to get the note of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the tx note.</returns>
    public abstract Task<string> GetTxNoteAsync(string txHash, CancellationToken ct = default);

    /// <summary>
    /// Get the notes of multiple transactions.
    /// </summary>
    /// <param name="txHashes">identify the transactions to get the notes of.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the notes, in the order of the hashes.</returns>
    public abstract Task<List<string>> GetTxNotesAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default);

    /// <summary>
    /// Set a note for a specific transaction.
    /// </summary>
    /// <param name="txHash">specifies the transaction.</param>
    /// <param name="note">specifies the note.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetTxNoteAsync(string txHash, string note, CancellationToken ct = default);

    /// <summary>
    /// Set notes for multiple transactions.
    /// </summary>
    /// <param name="txHashes">specify the transactions to set notes for.</param>
    /// <param name="notes">are the notes to set, one for each hash.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task SetTxNotesAsync(IReadOnlyList<string> txHashes, IReadOnlyList<string> notes, CancellationToken ct = default);

    /// <summary>
    /// Get the entries of the address book.
    /// </summary>
    /// <param name="indices">are the indices of the entries to get, or <c>null</c> for all of them.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the address book entries.</returns>
    public abstract Task<List<MoneroAddressBookEntry>> GetAddressBookEntriesAsync(IReadOnlyList<ulong>? indices = null, CancellationToken ct = default);

    /// <summary>
    /// Add an entry to the address book.
    /// </summary>
    /// <param name="address">is the address of the entry.</param>
    /// <param name="description">is the description of the entry, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the index of the added entry.</returns>
    public abstract Task<ulong> AddAddressBookEntryAsync(string address, string? description = null, CancellationToken ct = default);

    /// <summary>
    /// Edit an entry of the address book.
    /// </summary>
    /// <param name="index">is the index of the entry to edit.</param>
    /// <param name="address">is the new address of the entry, or <c>null</c> to leave it.</param>
    /// <param name="description">is the new description of the entry, or <c>null</c> to leave it.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task EditAddressBookEntryAsync(ulong index, string? address, string? description, CancellationToken ct = default);

    /// <summary>
    /// Delete an entry of the address book.
    /// </summary>
    /// <param name="index">is the index of the entry to delete.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    public abstract Task DeleteAddressBookEntryAsync(ulong index, CancellationToken ct = default);

    /// <summary>
    /// Get a payment URI from a transaction config.
    /// </summary>
    /// <param name="config">specifies configuration for a potential transaction.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the payment uri.</returns>
    public abstract Task<string> GetPaymentUriAsync(MoneroTxConfig config, CancellationToken ct = default);

    /// <summary>
    /// Parse a payment URI to a transaction config.
    /// </summary>
    /// <param name="uri">is the payment uri to parse.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction config.</returns>
    public abstract Task<MoneroTxConfig> ParsePaymentUriAsync(string uri, CancellationToken ct = default);

    /// <summary>
    /// Sign a message.
    /// </summary>
    /// <param name="message">is the message to sign.</param>
    /// <param name="signatureType">sign with the spend key or the view key.</param>
    /// <param name="accountIdx">is the index of the account whose key signs the message.</param>
    /// <param name="subaddressIdx">is the index of the subaddress within the account.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the signature.</returns>
    public abstract Task<string> SignMessageAsync(string message, MoneroMessageSignatureType signatureType = MoneroMessageSignatureType.SignWithSpendKey, uint accountIdx = 0, uint subaddressIdx = 0, CancellationToken ct = default);

    /// <summary>
    /// Verify a signature on a message.
    /// </summary>
    /// <param name="message">is the signed message.</param>
    /// <param name="address">is the signing address.</param>
    /// <param name="signature">is the signature.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the verification.</returns>
    public abstract Task<MoneroMessageSignatureResult> VerifyMessageAsync(string message, string address, string signature, CancellationToken ct = default);

    /// <summary>
    /// Get a transaction's secret key from its hash.
    /// </summary>
    /// <param name="txHash">is the transaction's hash.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction's secret key.</returns>
    public abstract Task<string> GetTxKeyAsync(string txHash, CancellationToken ct = default);

    /// <summary>
    /// Check a transaction in the blockchain with its secret key.
    /// </summary>
    /// <param name="txHash">specifies the transaction to check.</param>
    /// <param name="txKey">is the transaction's secret key.</param>
    /// <param name="address">is the destination public address of the transaction.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the check.</returns>
    public abstract Task<MoneroCheckTx> CheckTxKeyAsync(string txHash, string txKey, string address, CancellationToken ct = default);

    /// <summary>
    /// Get a transaction signature to prove it.
    /// </summary>
    /// <param name="txHash">specifies the transaction to prove.</param>
    /// <param name="address">is the destination public address of the transaction.</param>
    /// <param name="message">is a message to include with the signature to further authenticate the proof, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction signature.</returns>
    public abstract Task<string> GetTxProofAsync(string txHash, string address, string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Prove a transaction by checking its signature.
    /// </summary>
    /// <param name="txHash">specifies the transaction to prove.</param>
    /// <param name="address">is the destination public address of the transaction.</param>
    /// <param name="message">is a message included with the signature, or <c>null</c> for none.</param>
    /// <param name="signature">is the transaction signature to confirm.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the check.</returns>
    public abstract Task<MoneroCheckTx> CheckTxProofAsync(string txHash, string address, string? message, string signature, CancellationToken ct = default);

    /// <summary>
    /// Generate a signature to prove a spend. Unlike proving a transaction, it does not require the destination public address.
    /// </summary>
    /// <param name="txHash">specifies the transaction to prove.</param>
    /// <param name="message">is a message to include with the signature to further authenticate the proof, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the transaction signature.</returns>
    public abstract Task<string> GetSpendProofAsync(string txHash, string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Prove a spend using a signature. Unlike proving a transaction, it does not require the destination public address.
    /// </summary>
    /// <param name="txHash">specifies the transaction to prove.</param>
    /// <param name="message">is a message included with the signature, or <c>null</c> for none.</param>
    /// <param name="signature">is the transaction signature to confirm.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the signature is good, <c>false</c> otherwise.</returns>
    public abstract Task<bool> CheckSpendProofAsync(string txHash, string? message, string signature, CancellationToken ct = default);

    /// <summary>
    /// Generate a signature to prove the entire balance of the wallet.
    /// </summary>
    /// <param name="message">is a message included with the signature to further authenticate the proof, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the reserve proof signature.</returns>
    public abstract Task<string> GetReserveProofWalletAsync(string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Generate a signature to prove an available amount in an account.
    /// </summary>
    /// <param name="accountIdx">specifies the account to prove ownership of the amount.</param>
    /// <param name="amount">is the minimum amount to prove as available in the account.</param>
    /// <param name="message">is a message to include with the signature to further authenticate the proof, or <c>null</c> for none.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the reserve proof signature.</returns>
    public abstract Task<string> GetReserveProofAccountAsync(uint accountIdx, ulong amount, string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Proof of a reserve using a signature.
    /// </summary>
    /// <param name="address">is the public wallet address.</param>
    /// <param name="message">is a message included with the signature, or <c>null</c> for none.</param>
    /// <param name="signature">is the reserve proof signature to check.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of the check.</returns>
    public abstract Task<MoneroCheckReserve> CheckReserveProofAsync(string address, string? message, string signature, CancellationToken ct = default);

    // multisig

    /// <summary>
    /// Indicates if the wallet is a multisig wallet.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if the wallet is a multisig wallet, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsMultisigAsync(CancellationToken ct = default);

    /// <summary>
    /// Get multisig info about this wallet.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the multisig info of the wallet.</returns>
    public abstract Task<MoneroMultisigInfo> GetMultisigInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if importing multisig data is needed for returning a correct balance.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns><c>true</c> if importing multisig data is needed, <c>false</c> otherwise.</returns>
    public abstract Task<bool> IsMultisigImportNeededAsync(CancellationToken ct = default);

    /// <summary>
    /// Get multisig info as hex to share with participants to begin creating a multisig wallet.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>this wallet's multisig hex to share with the other participants.</returns>
    public abstract Task<string> PrepareMultisigAsync(CancellationToken ct = default);

    /// <summary>
    /// Make this wallet multisig by importing multisig hex from participants.
    /// </summary>
    /// <param name="multisigHexes">are multisig hex from each participant.</param>
    /// <param name="threshold">is the number of signatures needed to sign transfers.</param>
    /// <param name="password">is the wallet password.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>this wallet's multisig hex to share with the other participants if another step is needed.</returns>
    public abstract Task<string> MakeMultisigAsync(IReadOnlyList<string> multisigHexes, int threshold, string password, CancellationToken ct = default);

    /// <summary>
    /// Exchange multisig hex with participants in a multisig wallet.
    /// </summary>
    /// <param name="multisigHexes">are multisig hex from each participant.</param>
    /// <param name="password">is the wallet's password.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result with the multisig wallet's address once it is initialized, or the hex to share with the participants otherwise.</returns>
    public abstract Task<MoneroMultisigInitResult> ExchangeMultisigKeysAsync(IReadOnlyList<string> multisigHexes, string password, CancellationToken ct = default);

    /// <summary>
    /// Export this wallet's multisig info as hex for other participants.
    /// </summary>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>this wallet's multisig info as hex for other participants.</returns>
    public abstract Task<string> ExportMultisigHexAsync(CancellationToken ct = default);

    /// <summary>
    /// Import multisig info as hex from other participants.
    /// </summary>
    /// <param name="multisigHexes">are multisig hex from each participant.</param>
    /// <param name="refreshAfterImport">refreshes the wallet after the import if <c>true</c>.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the number of outputs signed with the given multisig hex.</returns>
    public abstract Task<int> ImportMultisigHexAsync(IReadOnlyList<string> multisigHexes, bool refreshAfterImport = true, CancellationToken ct = default);

    /// <summary>
    /// Sign multisig transactions from a multisig wallet.
    /// </summary>
    /// <param name="multisigTxHex">represents unsigned multisig transactions as hex.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the result of signing the multisig transactions.</returns>
    public abstract Task<MoneroMultisigSignResult> SignMultisigTxHexAsync(string multisigTxHex, CancellationToken ct = default);

    /// <summary>
    /// Submit signed multisig transactions from a multisig wallet.
    /// </summary>
    /// <param name="signedMultisigTxHex">represents signed multisig transactions as hex.</param>
    /// <param name="ct">a token to cancel the wait for the result.</param>
    /// <returns>the hashes of the submitted transactions.</returns>
    public abstract Task<List<string>> SubmitMultisigTxHexAsync(string signedMultisigTxHex, CancellationToken ct = default);
}
