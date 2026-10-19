using System.Reflection;
using Monero.Common;
using Monero.Daemon.Model;
using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.UnitTests;

/// <summary>
/// Unit tests of <see cref="MoneroWallet"/>. They run without monero-c, a server or the network.
/// </summary>
[TestFixture]
public class MoneroWalletUnitTests
{
    // the methods of the wallet, in snake_case
    private static readonly string[] Methods =
    [
        "add_address_book_entry",
        "add_listener",
        "change_password",
        "check_reserve_proof",
        "check_spend_proof",
        "check_tx_key",
        "check_tx_proof",
        "close",
        "create_account",
        "create_subaddress",
        "create_tx",
        "create_txs",
        "decode_integrated_address",
        "delete_address_book_entry",
        "describe_multisig_tx_set",
        "describe_tx_set",
        "describe_unsigned_tx_set",
        "edit_address_book_entry",
        "exchange_multisig_keys",
        "export_key_images",
        "export_multisig_hex",
        "export_outputs",
        "freeze_output",
        "get_account",
        "get_account_tags",
        "get_accounts",
        "get_address",
        "get_address_book_entries",
        "get_address_index",
        "get_attribute",
        "get_balance",
        "get_daemon_connection",
        "get_daemon_height",
        "get_daemon_max_peer_height",
        "get_default_fee_priority",
        "get_height",
        "get_height_by_date",
        "get_integrated_address",
        "get_listeners",
        "get_multisig_info",
        "get_network_type",
        "get_outputs",
        "get_path",
        "get_payment_uri",
        "get_primary_address",
        "get_private_spend_key",
        "get_private_view_key",
        "get_public_spend_key",
        "get_public_view_key",
        "get_reserve_proof_account",
        "get_reserve_proof_wallet",
        "get_restore_height",
        "get_seed",
        "get_seed_language",
        "get_spend_proof",
        "get_subaddress",
        "get_subaddresses",
        "get_transfers",
        "get_tx",
        "get_tx_key",
        "get_tx_note",
        "get_tx_notes",
        "get_tx_proof",
        "get_txs",
        "get_unlocked_balance",
        "get_version",
        "import_key_images",
        "import_multisig_hex",
        "import_outputs",
        "is_closed",
        "is_connected_to_daemon",
        "is_daemon_synced",
        "is_daemon_trusted",
        "is_multisig",
        "is_multisig_import_needed",
        "is_output_frozen",
        "is_synced",
        "is_view_only",
        "make_multisig",
        "move_to",
        "parse_payment_uri",
        "prepare_multisig",
        "relay_tx",
        "relay_txs",
        "remove_listener",
        "remove_listeners",
        "rescan_blockchain",
        "rescan_spent",
        "save",
        "scan_txs",
        "set_account_tag_label",
        "set_attribute",
        "set_daemon_connection",
        "set_restore_height",
        "set_subaddress_label",
        "set_tx_note",
        "set_tx_notes",
        "sign_message",
        "sign_multisig_tx_hex",
        "sign_txs",
        "start_mining",
        "start_syncing",
        "stop_mining",
        "stop_syncing",
        "submit_multisig_tx_hex",
        "submit_txs",
        "sweep_dust",
        "sweep_output",
        "sweep_unlocked",
        "sync",
        "tag_accounts",
        "thaw_output",
        "untag_accounts",
        "verify_message",
        "wait_for_next_block"
    ];

    // in the wallet of Python, and not here: monero-c has no function for them
    private static readonly string[] Unsupported = ["get_new_key_images_from_last_import", "set_account_label"];

    // not async: they work on the list of listeners
    private static readonly string[] SyncMethods = ["add_listener", "remove_listener", "get_listeners", "remove_listeners"];

    // implemented with another method of the wallet
    private static readonly string[] DefaultMethods =
        ["describe_multisig_tx_set", "describe_unsigned_tx_set", "get_subaddress", "get_tx", "get_txs", "get_transfers", "set_daemon_connection"];

    private static string CSharpName(string snakeName)
    {
        var name = string.Concat(snakeName.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return SyncMethods.Contains(snakeName) ? name : name + "Async";
    }

    private static MethodInfo[] PublicMethods() => typeof(MoneroWallet)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Test]
    public void IsAbstract()
    {
        Assert.That(typeof(MoneroWallet).IsAbstract, Is.True);
    }

    [TestCaseSource(nameof(Methods))]
    public void HasTheMethod(string name)
    {
        var methods = PublicMethods().Where(method => method.Name == CSharpName(name)).ToArray();

        Assert.That(methods, Is.Not.Empty, $"{name} is not in MoneroWallet");
        if (DefaultMethods.Contains(name))
        {
            Assert.That(methods.Any(method => method is { IsVirtual: true, IsAbstract: false }), Is.True, $"{name} has a default");
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

    [TestCaseSource(nameof(Unsupported))]
    public void HasNotTheMethodThatMoneroCDoesNotSupport(string name)
    {
        Assert.That(PublicMethods().Select(method => method.Name), Does.Not.Contain(CSharpName(name)));
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
    public async Task GetSubaddressAsksForTheOneIndexAndReturnsTheSubaddress()
    {
        var wallet = new RecordingWallet();
        wallet.Subaddresses.Add(new MoneroSubaddress(1, 2) { Address = "addr" });

        var subaddress = await wallet.GetSubaddressAsync(1, 2);

        Assert.That(subaddress.Address, Is.EqualTo("addr"));
        Assert.That(wallet.RequestedAccount, Is.EqualTo(1u));
        Assert.That(wallet.RequestedIndices, Is.EqualTo(new[] { 2u }));
    }

    [Test]
    public void GetSubaddressThrowsWhenTheWalletReturnsNone()
    {
        var wallet = new RecordingWallet();

        var error = Assert.ThrowsAsync<MoneroError>(() => wallet.GetSubaddressAsync(1, 2));

        Assert.That(error!.Message, Does.Contain("1,2"));
    }

    [Test]
    public async Task GetTxsByHashesAsksWithAQueryOfTheHashes()
    {
        var wallet = new RecordingWallet();

        await wallet.GetTxsAsync(["aa", "bb"]);

        Assert.That(wallet.RequestedTxQuery!.Hashes, Is.EqualTo(new[] { "aa", "bb" }));
    }

    [Test]
    public async Task GetTxsByHashesReturnsTheTxsInTheOrderOfTheHashes()
    {
        var wallet = new RecordingWallet();
        wallet.Txs.Add(new MoneroTxWallet { Hash = "bb" });
        wallet.Txs.Add(new MoneroTxWallet { Hash = "aa" });
        wallet.Txs.Add(new MoneroTxWallet { Hash = "cc" });

        var txs = await wallet.GetTxsAsync(["cc", "aa", "bb"]);

        Assert.That(txs.Select(tx => tx.Hash), Is.EqualTo(new[] { "cc", "aa", "bb" }));
    }

    [Test]
    public async Task GetTxReturnsTheFirstTx()
    {
        var wallet = new RecordingWallet();
        wallet.Txs.Add(new MoneroTxWallet { Hash = "aa" });

        var tx = await wallet.GetTxAsync("aa");

        Assert.That(tx!.Hash, Is.EqualTo("aa"));
        Assert.That(wallet.RequestedTxQuery!.Hashes, Is.EqualTo(new[] { "aa" }));
    }

    [Test]
    public async Task GetTxIsNullWhenTheWalletFindsNothing()
    {
        var wallet = new RecordingWallet();

        Assert.That(await wallet.GetTxAsync("aa"), Is.Null);
    }

    [Test]
    public async Task GetTransfersOfAnAccountAsksForTheAccount()
    {
        var wallet = new RecordingWallet();

        await wallet.GetTransfersAsync(3u);

        Assert.That(wallet.RequestedTransferQuery!.AccountIndex, Is.EqualTo(3u));
        Assert.That(wallet.RequestedTransferQuery.SubaddressIndex, Is.Null);
    }

    [Test]
    public async Task GetTransfersOfASubaddressAsksForTheSubaddress()
    {
        var wallet = new RecordingWallet();

        await wallet.GetTransfersAsync(3u, 4u);

        Assert.That(wallet.RequestedTransferQuery!.AccountIndex, Is.EqualTo(3u));
        Assert.That(wallet.RequestedTransferQuery.SubaddressIndex, Is.EqualTo(4u));
    }

    [Test]
    public async Task DescribeUnsignedTxSetSendsASetWithTheUnsignedHex()
    {
        var wallet = new RecordingWallet();

        await wallet.DescribeUnsignedTxSetAsync("0102");

        Assert.That(wallet.DescribedTxSet!.UnsignedTxHex, Is.EqualTo("0102"));
        Assert.That(wallet.DescribedTxSet.MultisigTxHex, Is.Null);
    }

    [Test]
    public async Task DescribeMultisigTxSetSendsASetWithTheMultisigHex()
    {
        var wallet = new RecordingWallet();

        await wallet.DescribeMultisigTxSetAsync("0304");

        Assert.That(wallet.DescribedTxSet!.MultisigTxHex, Is.EqualTo("0304"));
        Assert.That(wallet.DescribedTxSet.UnsignedTxHex, Is.Null);
    }

    [Test]
    public async Task SetDaemonConnectionOfAUriBuildsTheConnection()
    {
        var wallet = new RecordingWallet();

        await wallet.SetDaemonConnectionAsync("http://node:18081", "user", "pass", "socks5://proxy:9050", true);

        Assert.That(wallet.DaemonConnection!.Uri, Is.EqualTo("http://node:18081"));
        Assert.That(wallet.DaemonConnection.Username, Is.EqualTo("user"));
        Assert.That(wallet.DaemonConnection.Password, Is.EqualTo("pass"));
        Assert.That(wallet.DaemonConnection.ProxyUri, Is.EqualTo("socks5://proxy:9050"));
        Assert.That(wallet.DaemonTrusted, Is.True);
    }

    [Test]
    public void SetDaemonConnectionOfAUriNeedsTheUri()
    {
        var wallet = new RecordingWallet();

        Assert.Throws<ArgumentNullException>(() => wallet.SetDaemonConnectionAsync((string)null!));
    }

    [Test]
    public async Task SetDaemonConnectionOfAnEmptyUriPassesItOn()
    {
        var wallet = new RecordingWallet();

        await wallet.SetDaemonConnectionAsync("");

        Assert.That(wallet.DaemonConnection!.Uri, Is.Empty);
    }

    [Test]
    public async Task TheDefaultsPassTheCancellationTokenOn()
    {
        var wallet = new RecordingWallet();
        using var source = new CancellationTokenSource();

        await wallet.GetSubaddressAsync(0, 0, source.Token).ContinueWith(_ => 0);
        await wallet.GetTxsAsync(["aa"], source.Token);
        await wallet.GetTransfersAsync(0u, 1u, source.Token);
        await wallet.DescribeUnsignedTxSetAsync("00", source.Token);
        await wallet.SetDaemonConnectionAsync("http://node:18081", ct: source.Token);

        Assert.That(wallet.Tokens, Has.Count.EqualTo(5));
        Assert.That(wallet.Tokens, Has.All.EqualTo(source.Token));
    }

    [Test]
    public async Task ASubclassCanOverrideADefault()
    {
        var wallet = new OverridingWallet();

        var tx = await wallet.GetTxAsync("aa");

        Assert.That(tx!.Hash, Is.EqualTo("overridden"));
    }

    /// <summary>
    /// A wallet that implements every abstract method of <see cref="MoneroWallet"/> by throwing.
    /// </summary>
    private abstract class StubWallet : MoneroWallet
    {
        public override void AddListener(MoneroWalletListener listener) => throw new NotSupportedException();
        public override void RemoveListener(MoneroWalletListener listener) => throw new NotSupportedException();
        public override List<MoneroWalletListener> GetListeners() => throw new NotSupportedException();
        public override void RemoveListeners() => throw new NotSupportedException();
        public override Task<MoneroVersion> GetVersionAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPathAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroNetworkType> GetNetworkTypeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsViewOnlyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsClosedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetSeedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetSeedLanguageAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPrivateViewKeyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPrivateSpendKeyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPublicViewKeyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPublicSpendKeyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPrimaryAddressAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetAddressAsync(uint accountIdx, uint subaddressIdx, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroSubaddress> GetAddressIndexAsync(string address, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroIntegratedAddress> GetIntegratedAddressAsync(string? standardAddress = null, string? paymentId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroIntegratedAddress> DecodeIntegratedAddressAsync(string integratedAddress, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string?> GetAttributeAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetAttributeAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxPriority> GetDefaultFeePriorityAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SaveAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task MoveToAsync(string path, string password, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task CloseAsync(bool save = false, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroRpcConnection?> GetDaemonConnectionAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetDaemonConnectionAsync(MoneroRpcConnection? connection, bool? isTrusted = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsConnectedToDaemonAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsDaemonSyncedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsDaemonTrustedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsSyncedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetHeightAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetDaemonHeightAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetDaemonMaxPeerHeightAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetHeightByDateAsync(int year, int month, int day, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetRestoreHeightAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetRestoreHeightAsync(ulong restoreHeight, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroSyncResult> SyncAsync(ulong? startHeight = null, MoneroWalletListener? listener = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task StartSyncingAsync(ulong syncPeriodInMs = 10000, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task StopSyncingAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> WaitForNextBlockAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task ScanTxsAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task RescanSpentAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task RescanBlockchainAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task StartMiningAsync(ulong? numThreads = null, bool? backgroundMining = null, bool? ignoreBattery = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task StopMiningAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> GetUnlockedBalanceAsync(uint? accountIdx = null, uint? subaddressIdx = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroAccount>> GetAccountsAsync(bool includeSubaddresses = false, string? tag = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroAccount> GetAccountAsync(uint accountIdx, bool includeSubaddresses = false, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroAccount> CreateAccountAsync(string? label = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroAccountTag>> GetAccountTagsAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task TagAccountsAsync(string tag, IReadOnlyList<uint> accountIndices, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task UntagAccountsAsync(IReadOnlyList<uint> accountIndices, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetAccountTagLabelAsync(string tag, string label, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroSubaddress>> GetSubaddressesAsync(uint accountIdx, IReadOnlyList<uint>? subaddressIndices = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroSubaddress> CreateSubaddressAsync(uint accountIdx, string? label = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetSubaddressLabelAsync(uint accountIdx, uint subaddressIdx, string? label = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroTxWallet>> GetTxsAsync(MoneroTxQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroTransfer>> GetTransfersAsync(MoneroTransferQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroOutputWallet>> GetOutputsAsync(MoneroOutputQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> ExportOutputsAsync(bool all = false, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<int> ImportOutputsAsync(string outputsHex, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroKeyImageExportResult> ExportKeyImagesAsync(bool all = false, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroKeyImageImportResult> ImportKeyImagesAsync(IReadOnlyList<MoneroKeyImage> keyImages, ulong offset = 0, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task FreezeOutputAsync(string keyImage, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task ThawOutputAsync(string keyImage, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsOutputFrozenAsync(string keyImage, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxWallet> CreateTxAsync(MoneroTxConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroTxWallet>> CreateTxsAsync(MoneroTxConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxWallet> SweepOutputAsync(MoneroTxConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroTxWallet>> SweepUnlockedAsync(MoneroTxConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroTxWallet>> SweepDustAsync(bool relay = false, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> RelayTxAsync(string txMetadata, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> RelayTxAsync(MoneroTxWallet tx, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<string>> RelayTxsAsync(IReadOnlyList<string> txMetadatas, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<string>> RelayTxsAsync(IReadOnlyList<MoneroTxWallet> txs, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxSet> DescribeTxSetAsync(MoneroTxSet txSet, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxSet> SignTxsAsync(string unsignedTxHex, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<string>> SubmitTxsAsync(string signedTxHex, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetTxNoteAsync(string txHash, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<string>> GetTxNotesAsync(IReadOnlyList<string> txHashes, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetTxNoteAsync(string txHash, string note, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task SetTxNotesAsync(IReadOnlyList<string> txHashes, IReadOnlyList<string> notes, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<MoneroAddressBookEntry>> GetAddressBookEntriesAsync(IReadOnlyList<ulong>? indices = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<ulong> AddAddressBookEntryAsync(string address, string? description = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task EditAddressBookEntryAsync(ulong index, string? address, string? description, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task DeleteAddressBookEntryAsync(ulong index, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetPaymentUriAsync(MoneroTxConfig config, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroTxConfig> ParsePaymentUriAsync(string uri, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> SignMessageAsync(string message, MoneroMessageSignatureType signatureType = MoneroMessageSignatureType.SignWithSpendKey, uint accountIdx = 0, uint subaddressIdx = 0, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroMessageSignatureResult> VerifyMessageAsync(string message, string address, string signature, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetTxKeyAsync(string txHash, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroCheckTx> CheckTxKeyAsync(string txHash, string txKey, string address, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetTxProofAsync(string txHash, string address, string? message = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroCheckTx> CheckTxProofAsync(string txHash, string address, string? message, string signature, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetSpendProofAsync(string txHash, string? message = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> CheckSpendProofAsync(string txHash, string? message, string signature, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetReserveProofWalletAsync(string? message = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> GetReserveProofAccountAsync(uint accountIdx, ulong amount, string? message = null, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroCheckReserve> CheckReserveProofAsync(string address, string? message, string signature, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsMultisigAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroMultisigInfo> GetMultisigInfoAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<bool> IsMultisigImportNeededAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> PrepareMultisigAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> MakeMultisigAsync(IReadOnlyList<string> multisigHexes, int threshold, string password, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroMultisigInitResult> ExchangeMultisigKeysAsync(IReadOnlyList<string> multisigHexes, string password, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<string> ExportMultisigHexAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<int> ImportMultisigHexAsync(IReadOnlyList<string> multisigHexes, bool refreshAfterImport = true, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<MoneroMultisigSignResult> SignMultisigTxHexAsync(string multisigTxHex, CancellationToken ct = default) => throw new NotSupportedException();
        public override Task<List<string>> SubmitMultisigTxHexAsync(string signedMultisigTxHex, CancellationToken ct = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// A wallet that answers the methods that the defaults use, and records how it was asked.
    /// </summary>
    private class RecordingWallet : StubWallet
    {
        public List<MoneroSubaddress> Subaddresses { get; } = [];
        public List<MoneroTxWallet> Txs { get; } = [];
        public uint? RequestedAccount { get; private set; }
        public IReadOnlyList<uint>? RequestedIndices { get; private set; }
        public MoneroTxQuery? RequestedTxQuery { get; private set; }
        public MoneroTransferQuery? RequestedTransferQuery { get; private set; }
        public MoneroTxSet? DescribedTxSet { get; private set; }
        public MoneroRpcConnection? DaemonConnection { get; private set; }
        public bool? DaemonTrusted { get; private set; }
        public List<CancellationToken> Tokens { get; } = [];

        public override Task<List<MoneroSubaddress>> GetSubaddressesAsync(uint accountIdx, IReadOnlyList<uint>? subaddressIndices = null, CancellationToken ct = default)
        {
            RequestedAccount = accountIdx;
            RequestedIndices = subaddressIndices;
            Tokens.Add(ct);
            return Task.FromResult(Subaddresses);
        }

        public override Task<List<MoneroTxWallet>> GetTxsAsync(MoneroTxQuery? query = null, CancellationToken ct = default)
        {
            RequestedTxQuery = query;
            Tokens.Add(ct);
            return Task.FromResult(Txs);
        }

        public override Task<List<MoneroTransfer>> GetTransfersAsync(MoneroTransferQuery? query = null, CancellationToken ct = default)
        {
            RequestedTransferQuery = query;
            Tokens.Add(ct);
            return Task.FromResult(new List<MoneroTransfer>());
        }

        public override Task<MoneroTxSet> DescribeTxSetAsync(MoneroTxSet txSet, CancellationToken ct = default)
        {
            DescribedTxSet = txSet;
            Tokens.Add(ct);
            return Task.FromResult(txSet);
        }

        public override Task SetDaemonConnectionAsync(MoneroRpcConnection? connection, bool? isTrusted = null, CancellationToken ct = default)
        {
            DaemonConnection = connection;
            DaemonTrusted = isTrusted;
            Tokens.Add(ct);
            return Task.CompletedTask;
        }
    }

    private class OverridingWallet : StubWallet
    {
        public override Task<MoneroTxWallet?> GetTxAsync(string txHash, CancellationToken ct = default)
        {
            return Task.FromResult<MoneroTxWallet?>(new MoneroTxWallet { Hash = "overridden" });
        }
    }
}
