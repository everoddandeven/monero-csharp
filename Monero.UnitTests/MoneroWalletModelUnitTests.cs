using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Monero.Common;
using Monero.Daemon.Model;
using Monero.Wallet.Model;
using static Monero.UnitTests.ModelTestSupport;

namespace Monero.UnitTests;

/// <summary>
/// Serialization and deserialization of the wallet data model.
/// <para>
/// The JSON in <c>Fixtures/Wallet</c> was written by monero-c (and so by monero-cpp) from wallets on a regtest node: one file for every
/// result that a wallet returns, taken while the wallets mined, received, made, relayed, signed and swept transactions. The models have to
/// read it, and to write it back unchanged. What the wallets of that run didn't make (the configs and the queries that a caller sends, the
/// accounts with tags, a decoded address) is written by hand, in the shape that monero-c reads or writes.
/// </para>
/// <para>
/// Every test runs without monero-c, a node or the network.
/// </para>
/// </summary>
[TestFixture]
public class MoneroWalletModelUnitTests
{
    // ------------------------------------------------------------------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The fixtures and the type of the model that every one of them holds.
    /// </summary>
    private static readonly (string Name, Type Type)[] FixtureTypes =
    [
        ("account_b_with_subaddresses", typeof(MoneroAccount)),
        ("account_created", typeof(MoneroAccount)),
        ("account_with_subaddresses", typeof(MoneroAccount)),
        ("account_with_subaddresses_after", typeof(MoneroAccount)),
        ("account_without_subaddresses", typeof(MoneroAccount)),
        ("accounts_after", typeof(List<MoneroAccount>)),
        ("accounts_with_subaddresses", typeof(List<MoneroAccount>)),
        ("accounts_without_subaddresses", typeof(List<MoneroAccount>)),
        ("address_book_entries", typeof(List<MoneroAddressBookEntry>)),
        ("check_reserve", typeof(MoneroCheckReserve)),
        ("check_reserve_bad", typeof(MoneroCheckReserve)),
        ("check_tx", typeof(MoneroCheckTx)),
        ("check_tx_proof_bad", typeof(MoneroCheckTx)),
        ("check_tx_proof_good", typeof(MoneroCheckTx)),
        ("integrated_address", typeof(MoneroIntegratedAddress)),
        ("integrated_address_decoded", typeof(MoneroIntegratedAddress)),
        ("key_images_export", typeof(MoneroKeyImageExportResult)),
        ("key_images_import_result", typeof(MoneroKeyImageImportResult)),
        ("listener_output_received_first", typeof(MoneroWalletBlocksResult)),
        ("listener_output_received_last", typeof(MoneroWalletBlocksResult)),
        ("listener_output_spent_first", typeof(MoneroWalletBlocksResult)),
        ("listener_output_spent_last", typeof(MoneroWalletBlocksResult)),
        ("message_signature_spend", typeof(MoneroMessageSignatureResult)),
        ("message_signature_view", typeof(MoneroMessageSignatureResult)),
        ("message_signature_wrong_message", typeof(MoneroMessageSignatureResult)),
        ("multisig_exchange_result_1", typeof(MoneroMultisigInitResult)),
        ("multisig_exchange_result_2", typeof(MoneroMultisigInitResult)),
        ("multisig_info_after_make", typeof(MoneroMultisigInfo)),
        ("multisig_info_none", typeof(MoneroMultisigInfo)),
        ("multisig_info_ready", typeof(MoneroMultisigInfo)),
        ("outputs_frozen", typeof(MoneroWalletBlocksResult)),
        ("outputs_miner", typeof(MoneroWalletBlocksResult)),
        ("outputs_of_b", typeof(MoneroWalletBlocksResult)),
        ("outputs_of_tx", typeof(MoneroWalletBlocksResult)),
        ("outputs_spent", typeof(MoneroWalletBlocksResult)),
        ("rpc_connection", typeof(MoneroRpcConnection)),
        ("seed_languages", typeof(List<string>)),
        ("subaddress_created", typeof(MoneroSubaddress)),
        ("subaddress_of_address", typeof(MoneroSubaddress)),
        ("subaddresses", typeof(List<MoneroSubaddress>)),
        ("submitted_hashes", typeof(List<string>)),
        ("sync_result", typeof(MoneroSyncResult)),
        ("sync_result_b", typeof(MoneroSyncResult)),
        ("sync_result_c", typeof(MoneroSyncResult)),
        ("transfers_incoming_only", typeof(MoneroWalletBlocksResult)),
        ("transfers_miner", typeof(MoneroWalletBlocksResult)),
        ("transfers_outgoing", typeof(MoneroWalletBlocksResult)),
        ("transfers_pool", typeof(MoneroWalletBlocksResult)),
        ("transfers_to_subaddress", typeof(MoneroWalletBlocksResult)),
        ("tx_config_from_uri", typeof(MoneroTxConfig)),
        ("tx_notes", typeof(List<string>)),
        ("tx_outgoing_confirmed", typeof(MoneroWalletBlocksResult)),
        ("tx_sets_created", typeof(MoneroTxSetsResult)),
        ("tx_sets_created_many", typeof(MoneroTxSetsResult)),
        ("tx_sets_created_relayed", typeof(MoneroTxSetsResult)),
        ("tx_sets_described", typeof(MoneroTxSetsResult)),
        ("tx_sets_signed", typeof(MoneroTxSetsResult)),
        ("tx_sets_swept_dust", typeof(MoneroTxSetsResult)),
        ("tx_sets_swept_output", typeof(MoneroTxSetsResult)),
        ("tx_sets_swept_unlocked", typeof(MoneroTxSetsResult)),
        ("tx_sets_unsigned", typeof(MoneroTxSetsResult)),
        ("tx_to_subaddress", typeof(MoneroWalletBlocksResult)),
        ("txs_empty", typeof(MoneroWalletBlocksResult)),
        ("txs_incoming_only", typeof(MoneroWalletBlocksResult)),
        ("txs_miner", typeof(MoneroWalletBlocksResult)),
        ("txs_pool", typeof(MoneroWalletBlocksResult)),
        ("txs_to_subaddress", typeof(MoneroWalletBlocksResult)),
        ("wallet_version", typeof(MoneroVersion)),
    ];

    /// <summary>
    /// The classes of the wallet model that are read from and written to JSON.
    /// </summary>
    private static readonly Type[] ModelTypes = typeof(MoneroTxWallet).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                       && (type.Namespace == "Monero.Wallet.Model" || type == typeof(MoneroRpcConnection))
                       && type.GetConstructor(Type.EmptyTypes) != null
                       && type != typeof(MoneroWalletListener)
                       && !typeof(JsonSerializerContext).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    public static IEnumerable<string> FixtureNames => FixtureTypes.Select(fixture => fixture.Name);

    public static IEnumerable<TestCaseData> FixtureCases()
    {
        foreach (var engine in Enum.GetValues<Engine>())
        {
            foreach (var (name, type) in FixtureTypes)
            {
                yield return new TestCaseData(name, type, engine).SetName($"{name} ({engine})");
            }
        }
    }

    public static IEnumerable<TestCaseData> ModelCases()
    {
        return ModelTypes.Select(type => new TestCaseData(type).SetArgDisplayNames(type.Name));
    }

    public static IEnumerable<TestCaseData> ModelEngineCases()
    {
        foreach (var engine in Enum.GetValues<Engine>())
        {
            foreach (var type in ModelTypes)
            {
                yield return new TestCaseData(type, engine).SetName($"{type.Name} ({engine})");
            }
        }
    }

    private static string FixturePath(string name)
    {
        return ModelTestSupport.FixturePath("Wallet", name);
    }

    private static string Fixture(string name)
    {
        return ModelTestSupport.Fixture("Wallet", name);
    }

    private static T Read<T>(string json)
    {
        return MoneroJson.Deserialize<T>(json);
    }

    private static T ReadFixture<T>(string name)
    {
        return MoneroJson.Deserialize<T>(Fixture(name));
    }

    private static string Write<T>(T model)
    {
        return MoneroJson.Serialize(model);
    }

    private static object ReadWith(Engine engine, string json, Type type)
    {
        return ModelTestSupport.ReadWith(engine, json, type, MoneroWalletJsonContext.Default);
    }

    private static string WriteWith(Engine engine, object model, Type type)
    {
        return ModelTestSupport.WriteWith(engine, model, type, MoneroWalletJsonContext.Default);
    }

    /// <summary>
    /// A sampler that knows which classes the lists of the transaction of a wallet and of the block of a wallet hold.
    /// </summary>
    private static Sampler NewSampler()
    {
        return new Sampler(property =>
        {
            if (typeof(MoneroTxWallet).IsAssignableFrom(property.DeclaringType) && (property.Name == "Inputs" || property.Name == "Outputs")) return typeof(MoneroOutputWallet);
            if (typeof(MoneroBlockWallet).IsAssignableFrom(property.DeclaringType) && property.Name == "Txs") return typeof(MoneroTxWallet);
            return null;
        });
    }

    private const string TxHash = "c8b34eec8048721fe18b5d7416edebe76b367c4df636e0186ca82c995d860355";
    private const string AddressA = "42jhNF2LEEzjo9Qaw8s4BPNVp6MgDsaeqNHHixtsFwUh3QrMm2iJ1BM2aenMsNUatXPvoXaniuzUXGBi5o4B66ja3c7Ltm7";
    private const string AddressB = "46qnfaSR9ww5HgZKXtUZJViiqVjiK8WijHWoZTGKbf6VTeTaUiP5fGG1UY4rUvBp2ZjJrYdWJsDQUDZUyceHk7uPHhoPX8z";
    private const string AddressASub1 = "8BnSaX515n5BUg27ABzMVXTfPVagwXNZ13bz9CQTWJwfhtFXA9YxPXEeTY9NgufW3SSyJBnMEkNvc8xR2QkhJR8D44PasFi";
    private const string KeyImageOfBlock1 = "81a89cd97718ced48fd838ef2fba631aa072fbe0ab181a1ca0bc4c4af325bb75";

    // ------------------------------------------------------------------------------------------------------------------------------
    // fixtures: what monero-c writes
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Fixtures_AreAllTested()
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(FixturePath("x"))!, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.That(files, Is.EqualTo(FixtureTypes.Select(fixture => fixture.Name).OrderBy(name => name, StringComparer.Ordinal)));
    }

    [Test]
    public void Fixtures_AreJson_WithoutByteOrderMark()
    {
        foreach (var (name, _) in FixtureTypes)
        {
            var bytes = File.ReadAllBytes(FixturePath(name));
            Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), $"{name} has a byte order mark");
            Assert.DoesNotThrow(() => JsonNode.Parse(bytes), $"{name} is not JSON");
        }
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_IsWrittenBackUnchanged(string name, Type type, Engine engine)
    {
        var json = Fixture(name);

        var model = ReadWith(engine, json, type);
        var written = WriteWith(engine, model, type);

        Assert.That(Diff(JsonNode.Parse(json), JsonNode.Parse(written)), Is.Null);
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_IsWrittenBackUnchanged_AfterReadingTheWrittenJson(string name, Type type, Engine engine)
    {
        var first = WriteWith(engine, ReadWith(engine, Fixture(name), type), type);
        var second = WriteWith(engine, ReadWith(engine, first, type), type);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Fixture_IsReadTheSameWithReflectionAndWithTheGeneratedCode([ValueSource(nameof(FixtureNames))] string name)
    {
        var type = FixtureTypes.Single(fixture => fixture.Name == name).Type;
        var json = Fixture(name);

        var reflection = WriteWith(Engine.Reflection, ReadWith(Engine.SourceGenerated, json, type), type);
        var generated = WriteWith(Engine.SourceGenerated, ReadWith(Engine.Reflection, json, type), type);

        Assert.That(Diff(JsonNode.Parse(reflection), JsonNode.Parse(generated)), Is.Null);
    }

    [Test]
    public void SyncResult_ReadsTheFixtures()
    {
        var first = ReadFixture<MoneroSyncResult>("sync_result");

        Assert.Multiple(() =>
        {
            Assert.That(first.NumBlocksFetched, Is.EqualTo(100));
            Assert.That(first.ReceivedMoney, Is.True);
            Assert.That(ReadFixture<MoneroSyncResult>("sync_result_b").NumBlocksFetched, Is.EqualTo(101));
            Assert.That(ReadFixture<MoneroSyncResult>("sync_result_c").NumBlocksFetched, Is.EqualTo(114));
        });
    }

    [Test]
    public void RpcConnection_ReadsTheFixture()
    {
        var connection = ReadFixture<MoneroRpcConnection>("rpc_connection");

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.EqualTo("http://127.0.0.1:18081"));
            Assert.That(connection.Priority, Is.EqualTo(0));
            Assert.That(connection.SslVerify, Is.False);
            Assert.That(connection.Username, Is.Null);
            Assert.That(connection.Password, Is.Null);
            Assert.That(connection.ProxyUri, Is.Null);
            Assert.That(connection.TimeoutMs, Is.Null);
            Assert.That(connection.IsOnline, Is.Null);
        });
    }

    [Test]
    public void WalletVersion_ReadsTheFixture()
    {
        var version = ReadFixture<MoneroVersion>("wallet_version");

        Assert.That(version.Number, Is.EqualTo(65568));
        Assert.That(version.IsRelease, Is.False);
    }

    [Test]
    public void Account_WithSubaddresses_ReadsTheFixture()
    {
        var account = ReadFixture<MoneroAccount>("account_with_subaddresses");

        Assert.Multiple(() =>
        {
            Assert.That(account.Index, Is.EqualTo(0));
            Assert.That(account.PrimaryAddress, Is.EqualTo(AddressA));
            Assert.That(account.Balance, Is.EqualTo(3518101685576566));
            Assert.That(account.UnlockedBalance, Is.EqualTo(1442502852058827));
            Assert.That(account.Tag, Is.Null);
            Assert.That(account.Subaddresses, Has.Count.EqualTo(1));
        });

        var subaddress = account.Subaddresses![0];
        Assert.Multiple(() =>
        {
            Assert.That(subaddress.AccountIndex, Is.EqualTo(0));
            Assert.That(subaddress.Index, Is.EqualTo(0));
            Assert.That(subaddress.Address, Is.EqualTo(AddressA));
            Assert.That(subaddress.Label, Is.EqualTo("Primary account"));
            Assert.That(subaddress.Balance, Is.EqualTo(3518101685576566));
            Assert.That(subaddress.UnlockedBalance, Is.EqualTo(1442502852058827));
            Assert.That(subaddress.NumUnspentOutputs, Is.EqualTo(100));
            Assert.That(subaddress.NumBlocksToUnlock, Is.EqualTo(59));
            Assert.That(subaddress.IsUsed, Is.True);
        });
    }

    [Test]
    public void Account_WithoutSubaddresses_HasNoListOfThem()
    {
        var account = ReadFixture<MoneroAccount>("account_without_subaddresses");

        Assert.That(account.PrimaryAddress, Is.EqualTo(AddressA));
        Assert.That(account.Subaddresses, Is.Null);
    }

    [Test]
    public void Account_Created_HasNothingYet()
    {
        var account = ReadFixture<MoneroAccount>("account_created");

        Assert.Multiple(() =>
        {
            Assert.That(account.Index, Is.EqualTo(1));
            Assert.That(account.Balance, Is.EqualTo(0));
            Assert.That(account.UnlockedBalance, Is.EqualTo(0));
            Assert.That(account.PrimaryAddress, Does.StartWith("857siCx3"));
            Assert.That(account.Subaddresses, Is.Null);
        });
    }

    [Test]
    public void Accounts_ReadTheFixture()
    {
        var accounts = ReadFixture<List<MoneroAccount>>("accounts_with_subaddresses");

        Assert.That(accounts.Select(account => account.Index), Is.EqualTo(new uint?[] { 0, 1 }));
        Assert.That(accounts[0].Subaddresses!.Select(subaddress => subaddress.Label), Is.EqualTo(new[] { "Primary account", "deposits" }));
        Assert.That(accounts[0].Subaddresses!.Select(subaddress => subaddress.IsUsed), Is.EqualTo(new bool?[] { true, false }));
        Assert.That(accounts[1].Subaddresses!.Single().Label, Is.EqualTo("savings"));
        Assert.That(accounts[1].Subaddresses!.Single().AccountIndex, Is.EqualTo(1));
        Assert.That(ReadFixture<List<MoneroAccount>>("accounts_without_subaddresses"), Is.All.Matches<MoneroAccount>(account => account.Subaddresses == null));
    }

    [Test]
    public void Subaddresses_ReadTheFixtures()
    {
        var created = ReadFixture<MoneroSubaddress>("subaddress_created");
        var subaddresses = ReadFixture<List<MoneroSubaddress>>("subaddresses");
        var ofAddress = ReadFixture<MoneroSubaddress>("subaddress_of_address");

        Assert.Multiple(() =>
        {
            Assert.That(created.AccountIndex, Is.EqualTo(0));
            Assert.That(created.Index, Is.EqualTo(1));
            Assert.That(created.Address, Is.EqualTo(AddressASub1));
            Assert.That(created.Label, Is.EqualTo("deposits"));
            Assert.That(created.IsUsed, Is.False);
            Assert.That(created.NumBlocksToUnlock, Is.EqualTo(0));
            Assert.That(subaddresses.Select(subaddress => subaddress.Index), Is.EqualTo(new uint?[] { 0, 1 }));
            // the answer to a query by address is the indices, and nothing else
            Assert.That(ofAddress.AccountIndex, Is.EqualTo(0));
            Assert.That(ofAddress.Index, Is.EqualTo(1));
            Assert.That(ofAddress.Address, Is.Null);
            Assert.That(ofAddress.Label, Is.Null);
        });
    }

    [Test]
    public void AccountOfAnotherWallet_ReadsTheFixture()
    {
        var account = ReadFixture<MoneroAccount>("account_b_with_subaddresses");

        Assert.That(account.Balance, Is.EqualTo(3000000000));
        Assert.That(account.UnlockedBalance, Is.EqualTo(0));
        Assert.That(account.Subaddresses!.Select(subaddress => subaddress.Balance), Is.EqualTo(new ulong?[] { 1700000000, 1300000000 }));
        Assert.That(account.Subaddresses!.Select(subaddress => subaddress.NumBlocksToUnlock), Is.EqualTo(new ulong?[] { 8, 9 }));
        Assert.That(account.Subaddresses!.Select(subaddress => subaddress.Label), Is.EqualTo(new[] { "Primary account", "from a" }));
    }

    [Test]
    public void IntegratedAddress_ReadsTheFixtures()
    {
        foreach (var name in new[] { "integrated_address", "integrated_address_decoded" })
        {
            var address = ReadFixture<MoneroIntegratedAddress>(name);

            Assert.That(address.StandardAddress, Is.EqualTo(AddressA), name);
            Assert.That(address.PaymentId, Is.EqualTo("0123456789abcdef"), name);
            Assert.That(address.IntegratedAddress, Does.StartWith("4CSNP3qp"), name);
        }
    }

    [Test]
    public void MessageSignatureResults_ReadTheFixtures()
    {
        var spend = ReadFixture<MoneroMessageSignatureResult>("message_signature_spend");
        var view = ReadFixture<MoneroMessageSignatureResult>("message_signature_view");
        var wrong = ReadFixture<MoneroMessageSignatureResult>("message_signature_wrong_message");

        Assert.Multiple(() =>
        {
            Assert.That(spend.IsGood, Is.True);
            Assert.That(spend.IsOld, Is.False);
            Assert.That(spend.Version, Is.EqualTo(2));
            Assert.That(spend.SignatureType, Is.EqualTo(MoneroMessageSignatureType.SignWithSpendKey));
            Assert.That(view.IsGood, Is.True);
            Assert.That(view.SignatureType, Is.EqualTo(MoneroMessageSignatureType.SignWithViewKey));
            Assert.That(wrong.IsGood, Is.False);
            Assert.That(wrong.Version, Is.EqualTo(0));
            Assert.That(wrong.SignatureType, Is.EqualTo(MoneroMessageSignatureType.SignWithViewKey));
        });
    }

    [Test]
    public void AddressBookEntries_ReadTheFixture()
    {
        var entries = ReadFixture<List<MoneroAddressBookEntry>>("address_book_entries");

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(entries[0].Index, Is.EqualTo(0));
            Assert.That(entries[0].Address, Is.EqualTo(AddressB));
            Assert.That(entries[0].Description, Is.EqualTo("friend"));
            Assert.That(entries[0].PaymentId, Is.Null);
            Assert.That(entries[1].Index, Is.EqualTo(1));
            Assert.That(entries[1].Address, Is.EqualTo(AddressASub1));
            Assert.That(entries[1].Description, Is.EqualTo("my deposits"));
        });
    }

    [Test]
    public void Txs_OfMinerTxs_ReadTheFixture()
    {
        var result = ReadFixture<MoneroWalletBlocksResult>("txs_miner");

        Assert.That(result.Blocks, Has.Count.EqualTo(2));
        Assert.That(result.Blocks!.Select(block => block.Height), Is.EqualTo(new ulong?[] { 1, 2 }));
        Assert.That(result.Blocks![0].Timestamp, Is.EqualTo(1791741214));
        Assert.That(result.Blocks![0].Hash, Is.Null);

        var tx = result.Blocks![0].Txs!.Single();
        Assert.That(tx, Is.InstanceOf<MoneroTxWallet>());
        var wallet = (MoneroTxWallet)tx;
        Assert.Multiple(() =>
        {
            Assert.That(wallet.Hash, Is.EqualTo("a4d361eaf5925c0bcd7784766da5e9e373b937b380b5bb679227e5ba93b19b8b"));
            Assert.That(wallet.Fee, Is.EqualTo(0));
            Assert.That(wallet.NumConfirmations, Is.EqualTo(100));
            Assert.That(wallet.UnlockTime, Is.EqualTo(61));
            Assert.That(wallet.IsMinerTx, Is.True);
            Assert.That(wallet.Relay, Is.True);
            Assert.That(wallet.IsRelayed, Is.True);
            Assert.That(wallet.IsConfirmed, Is.True);
            Assert.That(wallet.InTxPool, Is.False);
            Assert.That(wallet.IsLocked, Is.False);
            Assert.That(wallet.IsDoubleSpendSeen, Is.False);
            Assert.That(wallet.IsFailed, Is.False);
            Assert.That(wallet.IsIncoming, Is.True);
            Assert.That(wallet.IsOutgoing, Is.False);
            Assert.That(wallet.OutgoingTransfer, Is.Null);
            Assert.That(wallet.Note, Is.Null);
            Assert.That(wallet.Block, Is.SameAs(result.Blocks![0]));
            Assert.That(wallet.GetHeight(), Is.EqualTo(1));
            Assert.That(wallet.TxSet, Is.Null);
            Assert.That(wallet.IncomingTransfers, Has.Count.EqualTo(1));
        });

        var transfer = wallet.IncomingTransfers![0];
        Assert.Multiple(() =>
        {
            Assert.That(transfer.Tx, Is.SameAs(wallet));
            Assert.That(transfer.Amount, Is.EqualTo(35184338534400));
            Assert.That(transfer.AccountIndex, Is.EqualTo(0));
            Assert.That(transfer.SubaddressIndex, Is.EqualTo(0));
            Assert.That(transfer.NumSuggestedConfirmations, Is.EqualTo(2));
            Assert.That(transfer.Address, Is.EqualTo(AddressA));
            Assert.That(transfer.GetIsIncoming(), Is.True);
            Assert.That(transfer.GetIsOutgoing(), Is.False);
            Assert.That(wallet.GetIncomingAmount(), Is.EqualTo(35184338534400));
            Assert.That(wallet.GetOutgoingAmount(), Is.EqualTo(0));
        });

        // the transfers of the call that gets the transfers are the same JSON, and the same models
        Assert.That(Write(ReadFixture<MoneroWalletBlocksResult>("transfers_miner")), Is.EqualTo(Write(result)));
        Assert.That(result.GetTxs().Select(t => t.Hash), Is.EqualTo(new[] { "a4d361eaf5925c0bcd7784766da5e9e373b937b380b5bb679227e5ba93b19b8b", "6ab24be3baa4bfdcfb5e498b25da349a6cd4743e2c676e845042393f9570ec45" }));
        Assert.That(result.GetTransfers().Select(t => t.Amount), Is.EqualTo(new ulong?[] { 35184338534400, 35184271425600 }));
    }

    [Test]
    public void Tx_InThePool_IsInTheBlockWithoutAHeight()
    {
        var result = ReadFixture<MoneroWalletBlocksResult>("txs_pool");

        var block = result.Blocks!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(block.Height, Is.Null);
            Assert.That(block.Timestamp, Is.Null);
            Assert.That(block.Txs, Has.Count.EqualTo(1));
        });

        var tx = block.GetTxsWallet().Single();
        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.GetHeight(), Is.Null);
            Assert.That(tx.Fee, Is.EqualTo(2599200000));
            Assert.That(tx.NumConfirmations, Is.EqualTo(0));
            Assert.That(tx.IsConfirmed, Is.False);
            Assert.That(tx.InTxPool, Is.True);
            Assert.That(tx.IsLocked, Is.True);
            Assert.That(tx.IsIncoming, Is.False);
            Assert.That(tx.IsOutgoing, Is.True);
            Assert.That(tx.IncomingTransfers, Is.Null);
            Assert.That(tx.OutgoingTransfer, Is.Not.Null);
        });

        var transfer = tx.OutgoingTransfer!;
        Assert.Multiple(() =>
        {
            Assert.That(transfer.Tx, Is.SameAs(tx));
            Assert.That(transfer.Amount, Is.EqualTo(1000000000));
            Assert.That(transfer.AccountIndex, Is.EqualTo(0));
            Assert.That(transfer.SubaddressIndices, Is.EqualTo(new uint[] { 0 }));
            Assert.That(transfer.Addresses, Is.EqualTo(new[] { AddressA }));
            Assert.That(transfer.Destinations, Has.Count.EqualTo(1));
            Assert.That(transfer.Destinations![0].Address, Is.EqualTo(AddressB));
            Assert.That(transfer.Destinations![0].Amount, Is.EqualTo(1000000000));
            Assert.That(transfer.GetIsIncoming(), Is.False);
            Assert.That(transfer.GetIsOutgoing(), Is.True);
            Assert.That(tx.GetOutgoingAmount(), Is.EqualTo(1000000000));
            Assert.That(tx.GetIncomingAmount(), Is.EqualTo(0));
            Assert.That(tx.GetTransfers(), Is.EqualTo(new MoneroTransfer[] { transfer }));
        });
    }

    [Test]
    public void Tx_WhenMined_IsInABlockWithAHeight()
    {
        var tx = ReadFixture<MoneroWalletBlocksResult>("tx_outgoing_confirmed").GetTxs().Single();

        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.GetHeight(), Is.EqualTo(101));
            Assert.That(tx.Block!.Timestamp, Is.EqualTo(1791741219));
            Assert.That(tx.IsConfirmed, Is.True);
            Assert.That(tx.InTxPool, Is.False);
            Assert.That(tx.NumConfirmations, Is.EqualTo(1));
            Assert.That(tx.OutgoingTransfer!.Destinations!.Single().Address, Is.EqualTo(AddressB));
        });
    }

    [Test]
    public void Tx_OfAnotherWallet_HasOnlyAnIncomingTransfer()
    {
        var tx = ReadFixture<MoneroWalletBlocksResult>("txs_incoming_only").GetTxs().Single();

        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.IsIncoming, Is.True);
            Assert.That(tx.IsOutgoing, Is.False);
            Assert.That(tx.OutgoingTransfer, Is.Null);
            Assert.That(tx.IncomingTransfers!.Single().Address, Is.EqualTo(AddressB));
            Assert.That(tx.IncomingTransfers!.Single().Amount, Is.EqualTo(1000000000));
            Assert.That(tx.IncomingTransfers!.Single().NumSuggestedConfirmations, Is.EqualTo(1));
        });
    }

    [Test]
    public void Tx_ToASubaddress_HasTheDestinationsThatItPays()
    {
        var tx = ReadFixture<MoneroWalletBlocksResult>("tx_to_subaddress").GetTxs().Single();

        var transfer = tx.OutgoingTransfer!;
        Assert.Multiple(() =>
        {
            Assert.That(tx.Fee, Is.EqualTo(3423600000));
            Assert.That(transfer.Amount, Is.EqualTo(50700000000));
            Assert.That(transfer.Destinations!.Select(destination => destination.Address), Is.EqualTo(new[] { AddressASub1, AddressB }));
            Assert.That(transfer.Destinations!.Select(destination => destination.Amount), Is.EqualTo(new ulong?[] { 50000000000, 700000000 }));
        });

        var incoming = ReadFixture<MoneroWalletBlocksResult>("transfers_to_subaddress").GetTransfers().Single();
        Assert.Multiple(() =>
        {
            Assert.That(incoming, Is.InstanceOf<MoneroIncomingTransfer>());
            Assert.That(((MoneroIncomingTransfer)incoming).SubaddressIndex, Is.EqualTo(1));
            Assert.That(((MoneroIncomingTransfer)incoming).Address, Does.StartWith("82U3oVXs"));
            Assert.That(incoming.Amount, Is.EqualTo(1300000000));
        });
    }

    [Test]
    public void EmptyResults_ReadAsEmptyLists()
    {
        Assert.That(ReadFixture<MoneroWalletBlocksResult>("txs_empty").Blocks, Is.Not.Null.And.Empty);
        Assert.That(ReadFixture<MoneroWalletBlocksResult>("txs_empty").GetTxs(), Is.Empty);
        Assert.That(ReadFixture<MoneroTxSetsResult>("tx_sets_swept_dust").TxSets, Is.Not.Null.And.Empty);
        Assert.That(ReadFixture<MoneroTxSetsResult>("tx_sets_swept_dust").GetTxs(), Is.Empty);
    }

    [Test]
    public void Outputs_ReadTheFixture()
    {
        var result = ReadFixture<MoneroWalletBlocksResult>("outputs_miner");

        Assert.That(result.Blocks!.Select(block => block.Height), Is.EqualTo(new ulong?[] { 1, 2 }));
        var tx = result.Blocks![0].GetTxsWallet().Single();
        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo("a4d361eaf5925c0bcd7784766da5e9e373b937b380b5bb679227e5ba93b19b8b"));
            Assert.That(tx.IsLocked, Is.False);
            Assert.That(tx.Fee, Is.Null);
            Assert.That(tx.IncomingTransfers, Is.Null);
            Assert.That(tx.Outputs, Has.Count.EqualTo(1));
            Assert.That(tx.Outputs![0], Is.InstanceOf<MoneroOutputWallet>());
        });

        var output = tx.GetOutputsWallet().Single();
        Assert.Multiple(() =>
        {
            Assert.That(output.Tx, Is.SameAs(tx));
            Assert.That(output.GetTxWallet(), Is.SameAs(tx));
            Assert.That(output.Amount, Is.EqualTo(35184338534400));
            Assert.That(output.Index, Is.EqualTo(0));
            Assert.That(output.StealthPublicKey, Is.EqualTo("11ee4ab187e78d9f685e95ed5b465d32f1be87a58c8721b730552fe750237ae8"));
            Assert.That(output.KeyImage!.Hex, Is.EqualTo(KeyImageOfBlock1));
            Assert.That(output.AccountIndex, Is.EqualTo(0));
            Assert.That(output.SubaddressIndex, Is.EqualTo(0));
            Assert.That(output.IsSpent, Is.False);
            Assert.That(output.IsFrozen, Is.False);
        });

        Assert.That(result.GetOutputs().Select(o => o.Index), Is.EqualTo(new ulong?[] { 0, 1 }));
        Assert.That(result.GetInputs(), Is.Empty);
    }

    [Test]
    public void Outputs_FrozenAndSpent_ReadTheFixtures()
    {
        var frozen = ReadFixture<MoneroWalletBlocksResult>("outputs_frozen").GetOutputs().Single();
        var spent = ReadFixture<MoneroWalletBlocksResult>("outputs_spent");

        Assert.That(frozen.IsFrozen, Is.True);
        Assert.That(frozen.IsSpent, Is.False);
        Assert.That(spent.Blocks!.Select(block => block.Height), Is.EqualTo(new ulong?[] { 1, 2, 3, 41, 42, 43 }));
        Assert.That(spent.GetOutputs(), Has.Count.EqualTo(6));
        Assert.That(spent.GetOutputs(), Is.All.Matches<MoneroOutputWallet>(output => output.IsSpent == true && output.IsFrozen == false));
        Assert.That(spent.GetOutputs().Select(output => output.Index), Is.EqualTo(new ulong?[] { 0, 1, 2, 40, 41, 42 }));
        Assert.That(ReadFixture<MoneroWalletBlocksResult>("outputs_of_tx").GetOutputs().Single().Amount, Is.EqualTo(70362393616637));
        Assert.That(ReadFixture<MoneroWalletBlocksResult>("outputs_of_b").GetOutputs(), Has.Count.EqualTo(1));
    }

    [Test]
    public void ListenerNotifications_ReadTheFixtures()
    {
        var received = ReadFixture<MoneroWalletBlocksResult>("listener_output_received_first");
        var receivedLast = ReadFixture<MoneroWalletBlocksResult>("listener_output_received_last");
        var spent = ReadFixture<MoneroWalletBlocksResult>("listener_output_spent_first");
        var spentLast = ReadFixture<MoneroWalletBlocksResult>("listener_output_spent_last");

        // a received output is in the outputs of its tx, which has what the listener of monero-java gets
        var output = received.GetOutputs().Single();
        var tx = output.GetTxWallet()!;
        Assert.Multiple(() =>
        {
            Assert.That(output.Amount, Is.EqualTo(35184338534400));
            Assert.That(output.AccountIndex, Is.EqualTo(0));
            Assert.That(output.SubaddressIndex, Is.EqualTo(0));
            Assert.That(output.KeyImage, Is.Null);
            Assert.That(tx.Version, Is.EqualTo(2));
            Assert.That(tx.UnlockTime, Is.EqualTo(61));
            Assert.That(tx.Hash, Is.EqualTo("a4d361eaf5925c0bcd7784766da5e9e373b937b380b5bb679227e5ba93b19b8b"));
            Assert.That(tx.IsLocked, Is.True);
            Assert.That(tx.GetHeight(), Is.EqualTo(1));
            Assert.That(received.GetInputs(), Is.Empty);
            Assert.That(receivedLast.GetOutputs().Single().IsSpent, Is.True);
            Assert.That(receivedLast.GetOutputs().Single().KeyImage!.Hex, Does.StartWith("d29e47f7"));
            Assert.That(receivedLast.GetOutputs().Single().GetTxWallet()!.GetHeight(), Is.EqualTo(54));
        });

        // a spent output is in the inputs of its tx
        var input = spent.GetInputs().Single();
        Assert.Multiple(() =>
        {
            Assert.That(input.Amount, Is.EqualTo(35184338534400));
            Assert.That(input.AccountIndex, Is.EqualTo(0));
            Assert.That(input.SubaddressIndex, Is.EqualTo(0));
            Assert.That(input.GetTxWallet()!.Hash, Is.EqualTo(TxHash));
            Assert.That(input.GetTxWallet()!.GetHeight(), Is.EqualTo(101));
            Assert.That(input.GetTxWallet()!.UnlockTime, Is.EqualTo(0));
            Assert.That(spent.GetOutputs(), Is.Empty);
            Assert.That(spentLast.GetInputs().Single().Amount, Is.EqualTo(3899200000));
            Assert.That(spentLast.GetInputs().Single().GetTxWallet()!.GetHeight(), Is.EqualTo(103));
        });
    }

    [Test]
    public void TxSet_OfACreatedTx_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroTxSetsResult>("tx_sets_created");

        var set = result.TxSets!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(set.UnsignedTxHex, Is.Null);
            Assert.That(set.SignedTxHex, Is.Null);
            Assert.That(set.MultisigTxHex, Is.Null);
            Assert.That(set.Txs, Has.Count.EqualTo(1));
        });

        var tx = set.Txs![0];
        Assert.Multiple(() =>
        {
            Assert.That(tx.TxSet, Is.SameAs(set));
            Assert.That(tx.Block, Is.Null);
            Assert.That(tx.GetHeight(), Is.Null);
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.Key, Is.EqualTo("dd87b002daee91626b8ff9caa55ad22d84e7556ed387819130c5443331261e0f"));
            Assert.That(tx.Fee, Is.EqualTo(2599200000));
            Assert.That(tx.RingSize, Is.EqualTo(16));
            Assert.That(tx.Weight, Is.EqualTo(2166));
            Assert.That(tx.FullHex, Has.Length.EqualTo(4332));
            Assert.That(tx.Metadata, Does.StartWith("011673657269616c697a6174696f6e3a3a61726368697665"));
            Assert.That(tx.Relay, Is.False);
            Assert.That(tx.IsRelayed, Is.False);
            Assert.That(tx.IsConfirmed, Is.False);
            Assert.That(tx.InTxPool, Is.False);
            Assert.That(tx.IsLocked, Is.True);
            Assert.That(tx.IsFailed, Is.False);
            Assert.That(tx.IsOutgoing, Is.True);
            Assert.That(tx.Inputs, Has.Count.EqualTo(2));
            Assert.That(tx.GetInputsWallet().Select(input => input.KeyImage!.Hex), Is.EqualTo(new[]
            {
                KeyImageOfBlock1, "14ded12241530c0e1725a5e91836db290209eed78b988b1ffc1e979109fac1df"
            }));
            Assert.That(tx.GetInputsWallet(), Is.All.Matches<MoneroOutputWallet>(input => ReferenceEquals(input.Tx, tx)));
            Assert.That(tx.OutgoingTransfer!.Tx, Is.SameAs(tx));
            Assert.That(tx.OutgoingTransfer!.Amount, Is.EqualTo(1000000000));
            Assert.That(tx.OutgoingTransfer!.Destinations!.Single().Address, Is.EqualTo(AddressB));
            Assert.That(tx.OutgoingTransfer!.SubaddressIndices, Is.Null);
            Assert.That(result.GetTxs(), Is.EqualTo(new[] { tx }));
        });
    }

    [Test]
    public void TxSets_OfManyTxsAndOfSweeps_ReadTheFixtures()
    {
        var relayed = ReadFixture<MoneroTxSetsResult>("tx_sets_created_relayed").GetTxs().Single();
        var many = ReadFixture<MoneroTxSetsResult>("tx_sets_created_many").GetTxs().Single();
        var unlocked = ReadFixture<MoneroTxSetsResult>("tx_sets_swept_unlocked").GetTxs().Single();
        var output = ReadFixture<MoneroTxSetsResult>("tx_sets_swept_output").GetTxs().Single();

        Assert.Multiple(() =>
        {
            // a tx that is relayed is in the pool at once
            Assert.That(relayed.Relay, Is.True);
            Assert.That(relayed.IsRelayed, Is.True);
            Assert.That(relayed.InTxPool, Is.True);
            Assert.That(relayed.LastRelayedTimestamp, Is.EqualTo(1791741220));
            Assert.That(relayed.IsDoubleSpendSeen, Is.False);
            Assert.That(relayed.Key, Has.Length.EqualTo(256));
            Assert.That(relayed.OutgoingTransfer!.Destinations!.Select(destination => destination.Amount), Is.EqualTo(new ulong?[] { 50000000000, 700000000 }));
            Assert.That(many.Fee, Is.EqualTo(13409100000));
            Assert.That(many.OutgoingTransfer!.Amount, Is.EqualTo(700000000));
            Assert.That(many.OutgoingTransfer!.Destinations, Has.Count.EqualTo(2));
            // a sweep that has the subaddress that it sweeps, and a sweep of one output that has no subaddress
            Assert.That(unlocked.Inputs, Has.Count.EqualTo(1));
            Assert.That(unlocked.OutgoingTransfer!.SubaddressIndices, Is.EqualTo(new uint[] { 1 }));
            Assert.That(unlocked.OutgoingTransfer!.Amount, Is.EqualTo(48191600000));
            Assert.That(output.Inputs, Has.Count.EqualTo(1));
            Assert.That(output.OutgoingTransfer!.SubaddressIndices, Is.Null);
            Assert.That(output.OutgoingTransfer!.Amount, Is.EqualTo(35180584031167));
        });
    }

    [Test]
    public void TxSet_OfAViewOnlyWallet_HasTheUnsignedHex()
    {
        var set = ReadFixture<MoneroTxSetsResult>("tx_sets_unsigned").TxSets!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(set.UnsignedTxHex, Does.StartWith("4d6f6e65726f20756e7369676e6564"));
            Assert.That(set.SignedTxHex, Is.Null);
            Assert.That(set.Txs!.Single().Hash, Is.EqualTo("9f1aac892cab44ec42d4530fcd91aeaf6050f60065fd9abc08cf3217a8937dc3"));
            // the view-only wallet doesn't know the key images, so the inputs have the placeholder
            Assert.That(set.Txs!.Single().GetInputsWallet().Select(input => input.KeyImage!.Hex), Is.All.EqualTo("0100000000000000000000000000000000000000000000000000000000000000"));
            Assert.That(set.Txs!.Single().OutgoingTransfer!.Amount, Is.EqualTo(200000000));
        });
    }

    [Test]
    public void TxSet_Signed_HasTheSignedHexAndNoDetails()
    {
        var set = ReadFixture<MoneroTxSetsResult>("tx_sets_signed").TxSets!.Single();

        var tx = set.Txs!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(set.SignedTxHex, Does.StartWith("4d6f6e65726f207369676e6564"));
            Assert.That(set.UnsignedTxHex, Is.Null);
            Assert.That(tx.Hash, Is.EqualTo("c315e3005ff8b39e20735ac2f1a72e6249a20406aadccf986694fa1ea9a28b9b"));
            Assert.That(tx.Key, Is.EqualTo("12b5cddea20b50215d40be02a557bcb66351b5a081a18ba85842b8c21cf40605"));
            Assert.That(tx.FullHex, Has.Length.EqualTo(4332));
            Assert.That(tx.IsOutgoing, Is.True);
            Assert.That(tx.Fee, Is.Null);
            Assert.That(tx.OutgoingTransfer, Is.Null);
            Assert.That(ReadFixture<List<string>>("submitted_hashes"), Is.EqualTo(new[] { tx.Hash }));
        });
    }

    [Test]
    public void TxSet_Described_HasTheDetailsOfTheTx()
    {
        var tx = ReadFixture<MoneroTxSetsResult>("tx_sets_described").GetTxs().Single();

        Assert.Multiple(() =>
        {
            Assert.That(tx.Fee, Is.EqualTo(2599200000));
            Assert.That(tx.InputSum, Is.EqualTo(70364919152761));
            Assert.That(tx.OutputSum, Is.EqualTo(70362319952761));
            Assert.That(tx.ChangeAmount, Is.EqualTo(70362119952761));
            Assert.That(tx.NumDummyOutputs, Is.EqualTo(0));
            Assert.That(tx.ChangeAddress, Is.EqualTo(AddressA));
            Assert.That(tx.ExtraHex, Does.StartWith("019ac500"));
            Assert.That(tx.GetInputsWallet().Select(input => input.Index), Is.EqualTo(new ulong?[] { 3, 53 }));
            Assert.That(tx.GetInputsWallet().Select(input => input.Amount), Is.EqualTo(new ulong?[] { 35184137208383, 35180781944378 }));
            Assert.That(tx.GetInputsWallet()[0].StealthPublicKey, Does.StartWith("f17f71e0"));
            Assert.That(tx.GetInputsWallet()[0].KeyImage, Is.Null);
            Assert.That(tx.OutgoingTransfer!.Amount, Is.Null);
            Assert.That(tx.OutgoingTransfer!.Destinations!.Single().Amount, Is.EqualTo(200000000));
        });
    }

    [Test]
    public void KeyImages_ReadTheFixtures()
    {
        var export = ReadFixture<MoneroKeyImageExportResult>("key_images_export");
        var import = ReadFixture<MoneroKeyImageImportResult>("key_images_import_result");

        Assert.Multiple(() =>
        {
            Assert.That(export.Offset, Is.EqualTo(0));
            Assert.That(export.KeyImages, Has.Count.EqualTo(117));
            Assert.That(export.KeyImages![0].Hex, Is.EqualTo(KeyImageOfBlock1));
            Assert.That(export.KeyImages![0].Signature, Does.StartWith("db6ec78cfdd106a6d0ac"));
            Assert.That(export.KeyImages![0].Signature, Has.Length.EqualTo(128));
            Assert.That(export.KeyImages![116].Hex, Is.EqualTo("56f720a563768b5fa213f86730305c85d047cba5992484463cb8604d3841c804"));
            Assert.That(import.Height, Is.EqualTo(113));
            Assert.That(import.SpentAmount, Is.EqualTo(211097575812726));
            Assert.That(import.UnspentAmount, Is.EqualTo(3975402619868544));
        });
    }

    [Test]
    public void Checks_ReadTheFixtures()
    {
        var tx = ReadFixture<MoneroCheckTx>("check_tx");
        var proof = ReadFixture<MoneroCheckTx>("check_tx_proof_good");
        var bad = ReadFixture<MoneroCheckTx>("check_tx_proof_bad");
        var reserve = ReadFixture<MoneroCheckReserve>("check_reserve");
        var badReserve = ReadFixture<MoneroCheckReserve>("check_reserve_bad");

        Assert.Multiple(() =>
        {
            Assert.That(tx.IsGood, Is.True);
            Assert.That(tx.InTxPool, Is.False);
            Assert.That(tx.NumConfirmations, Is.EqualTo(1));
            Assert.That(tx.ReceivedAmount, Is.EqualTo(1000000000));
            Assert.That(Write(proof), Is.EqualTo(Write(tx)));
            // a proof that is not good has nothing else
            Assert.That(bad.IsGood, Is.False);
            Assert.That(bad.InTxPool, Is.Null);
            Assert.That(bad.NumConfirmations, Is.Null);
            Assert.That(bad.ReceivedAmount, Is.Null);
            Assert.That(reserve.IsGood, Is.True);
            Assert.That(reserve.TotalAmount, Is.EqualTo(3553278313864525));
            Assert.That(reserve.UnconfirmedSpentAmount, Is.EqualTo(0));
            Assert.That(badReserve.IsGood, Is.False);
            Assert.That(badReserve.TotalAmount, Is.Null);
        });
    }

    [Test]
    public void Multisig_ReadsTheFixtures()
    {
        var none = ReadFixture<MoneroMultisigInfo>("multisig_info_none");
        var afterMake = ReadFixture<MoneroMultisigInfo>("multisig_info_after_make");
        var ready = ReadFixture<MoneroMultisigInfo>("multisig_info_ready");
        var first = ReadFixture<MoneroMultisigInitResult>("multisig_exchange_result_1");
        var second = ReadFixture<MoneroMultisigInitResult>("multisig_exchange_result_2");

        Assert.Multiple(() =>
        {
            // the threshold and the number of participants of a wallet that is not multisig are whatever monero-cpp left in them
            Assert.That(none.IsMultisig, Is.False);
            Assert.That(none.Threshold, Is.Not.Null);
            Assert.That(none.NumParticipants, Is.Not.Null);
            Assert.That(afterMake.IsMultisig, Is.True);
            Assert.That(afterMake.IsReady, Is.False);
            Assert.That(afterMake.Threshold, Is.EqualTo(2));
            Assert.That(afterMake.NumParticipants, Is.EqualTo(2));
            Assert.That(ready.IsReady, Is.True);
            // the two participants end with the same address of the wallet, and each with the hex that it shares
            Assert.That(first.Address, Does.StartWith("47W7b8rD"));
            Assert.That(second.Address, Is.EqualTo(first.Address));
            Assert.That(first.MultisigHex, Does.StartWith("MultisigxV2R"));
            Assert.That(second.MultisigHex, Is.Not.EqualTo(first.MultisigHex));
        });
    }

    [Test]
    public void TxConfig_OfAPaymentUri_ReadsTheFixture()
    {
        var config = ReadFixture<MoneroTxConfig>("tx_config_from_uri");

        Assert.Multiple(() =>
        {
            Assert.That(config.Note, Is.EqualTo("for lunch"));
            Assert.That(config.RecipientName, Is.EqualTo("Bob"));
            Assert.That(config.Destinations, Has.Count.EqualTo(1));
            Assert.That(config.Address, Is.EqualTo(AddressB));
            Assert.That(config.Amount, Is.EqualTo(1500000000));
            Assert.That(config.Relay, Is.Null);
            Assert.That(config.Priority, Is.Null);
        });
    }

    [Test]
    public void Lists_OfText_ReadTheFixtures()
    {
        Assert.That(ReadFixture<List<string>>("tx_notes"), Is.EqualTo(new[] { "", "to my deposits" }));
        Assert.That(ReadFixture<List<string>>("seed_languages"), Has.Count.EqualTo(12));
        Assert.That(ReadFixture<List<string>>("seed_languages"), Does.Contain("English").And.Contain("Chinese (simplified)"));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // what a caller sends: the configs and the queries, in the JSON that monero-c reads
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void WalletConfig_WithEveryField_IsReadAndWritten()
    {
        const string json = """
                            {
                              "path": "/wallets/main",
                              "password": "secret",
                              "networkType": 3,
                              "server": {
                                "uri": "http://127.0.0.1:18081",
                                "username": "user",
                                "password": "pass",
                                "proxyUri": "socks5://127.0.0.1:9050",
                                "zmqUri": "tcp://127.0.0.1:18083",
                                "priority": 1,
                                "timeoutMs": 30000,
                                "sslVerify": false
                              },
                              "isTrustedDaemon": true,
                              "seed": "vortex degrees outbreak teeming gimmick school rounded tonic observant injury leech ought problems ahead upcoming ledge textbook cigar atrium trash dunes eavesdrop dullness evolved vortex",
                              "seedOffset": "offset",
                              "primaryAddress": "48W9YHwPzRz9aPTeXCA6kmSpW6HsvmWx578jj3of2gT3JwZzwTf33amESBoNDkL6SVK34Q2HTKqgYbGyE1hBws3wCrcBDR2",
                              "privateViewKey": "e8c2288181bad9ec410d7322efd65f663c6da57bd1d1198636278a039743a600",
                              "privateSpendKey": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                              "restoreHeight": 3200000,
                              "language": "Spanish",
                              "saveCurrent": true,
                              "accountLookahead": 50,
                              "subaddressLookahead": 200,
                              "isMultisig": false,
                              "regtest": false
                            }
                            """;

        var config = Read<MoneroWalletConfig>(json);

        Assert.Multiple(() =>
        {
            Assert.That(config.Path, Is.EqualTo("/wallets/main"));
            Assert.That(config.Password, Is.EqualTo("secret"));
            Assert.That(config.NetworkType, Is.EqualTo(MoneroNetworkType.Regtest));
            Assert.That(config.Server!.Uri, Is.EqualTo("http://127.0.0.1:18081"));
            Assert.That(config.Server!.Username, Is.EqualTo("user"));
            Assert.That(config.Server!.Password, Is.EqualTo("pass"));
            Assert.That(config.Server!.ProxyUri, Is.EqualTo("socks5://127.0.0.1:9050"));
            Assert.That(config.Server!.ZmqUri, Is.EqualTo("tcp://127.0.0.1:18083"));
            Assert.That(config.Server!.Priority, Is.EqualTo(1));
            Assert.That(config.Server!.TimeoutMs, Is.EqualTo(30000));
            Assert.That(config.Server!.SslVerify, Is.False);
            Assert.That(config.IsTrustedDaemon, Is.True);
            Assert.That(config.Seed, Does.StartWith("vortex degrees"));
            Assert.That(config.SeedOffset, Is.EqualTo("offset"));
            Assert.That(config.PrimaryAddress, Does.StartWith("48W9YHwP"));
            Assert.That(config.PrivateViewKey, Does.StartWith("e8c22881"));
            Assert.That(config.PrivateSpendKey, Does.StartWith("01234567"));
            Assert.That(config.RestoreHeight, Is.EqualTo(3200000));
            Assert.That(config.Language, Is.EqualTo("Spanish"));
            Assert.That(config.SaveCurrent, Is.True);
            Assert.That(config.AccountLookahead, Is.EqualTo(50));
            Assert.That(config.SubaddressLookahead, Is.EqualTo(200));
            Assert.That(config.IsMultisig, Is.False);
            Assert.That(config.Regtest, Is.False);
        });

        AssertSameJson(json, Write(config));
    }

    [Test]
    public void WalletConfig_OfARegtestWallet_IsWhatTheIntegrationTestsOfMoneroCSend()
    {
        var config = new MoneroWalletConfig
        {
            NetworkType = MoneroNetworkType.Regtest,
            Server = new MoneroRpcConnection("http://127.0.0.1:18081"),
            IsTrustedDaemon = true
        };

        AssertSameJson("""{"networkType":3,"server":{"uri":"http://127.0.0.1:18081"},"isTrustedDaemon":true}""", Write(config));
    }

    [Test]
    public void TxConfig_WithEveryField_IsReadAndWritten()
    {
        const string json = """
                            {
                              "destinations": [
                                { "address": "46qnfaSR9ww5HgZKXtUZJViiqVjiK8WijHWoZTGKbf6VTeTaUiP5fGG1UY4rUvBp2ZjJrYdWJsDQUDZUyceHk7uPHhoPX8z", "amount": 300000000 },
                                { "address": "8BnSaX515n5BUg27ABzMVXTfPVagwXNZ13bz9CQTWJwfhtFXA9YxPXEeTY9NgufW3SSyJBnMEkNvc8xR2QkhJR8D44PasFi", "amount": 400000000 }
                              ],
                              "subtractFeeFrom": [0, 1],
                              "paymentId": "0123456789abcdef",
                              "priority": 3,
                              "ringSize": 16,
                              "fee": 2599200000,
                              "accountIndex": 1,
                              "subaddressIndices": [0, 2, 5],
                              "canSplit": true,
                              "relay": false,
                              "note": "for lunch",
                              "recipientName": "Bob",
                              "belowAmount": 1000000000,
                              "sweepEachSubaddress": true,
                              "keyImage": "81a89cd97718ced48fd838ef2fba631aa072fbe0ab181a1ca0bc4c4af325bb75"
                            }
                            """;

        var config = Read<MoneroTxConfig>(json);

        Assert.Multiple(() =>
        {
            Assert.That(config.Destinations!.Select(destination => destination.Amount), Is.EqualTo(new ulong?[] { 300000000, 400000000 }));
            Assert.That(config.SubtractFeeFrom, Is.EqualTo(new uint[] { 0, 1 }));
            Assert.That(config.PaymentId, Is.EqualTo("0123456789abcdef"));
            Assert.That(config.Priority, Is.EqualTo(MoneroTxPriority.Elevated));
            Assert.That(config.RingSize, Is.EqualTo(16));
            Assert.That(config.Fee, Is.EqualTo(2599200000));
            Assert.That(config.AccountIndex, Is.EqualTo(1));
            Assert.That(config.SubaddressIndices, Is.EqualTo(new uint[] { 0, 2, 5 }));
            Assert.That(config.CanSplit, Is.True);
            Assert.That(config.Relay, Is.False);
            Assert.That(config.Note, Is.EqualTo("for lunch"));
            Assert.That(config.RecipientName, Is.EqualTo("Bob"));
            Assert.That(config.BelowAmount, Is.EqualTo(1000000000));
            Assert.That(config.SweepEachSubaddress, Is.True);
            Assert.That(config.KeyImage, Does.StartWith("81a89cd9"));
            // with two destinations, there is no address and no amount of the config
            Assert.That(config.Address, Is.Null);
            Assert.That(config.Amount, Is.Null);
        });

        AssertSameJson(json, Write(config));
    }

    [TestCase(0, MoneroTxPriority.Default)]
    [TestCase(1, MoneroTxPriority.Unimportant)]
    [TestCase(2, MoneroTxPriority.Normal)]
    [TestCase(3, MoneroTxPriority.Elevated)]
    public void TxConfig_Priority_IsTheNumber(int number, MoneroTxPriority priority)
    {
        var config = Read<MoneroTxConfig>($$"""{"priority":{{number}}}""");

        Assert.That(config.Priority, Is.EqualTo(priority));
        Assert.That(Write(config), Is.EqualTo($$"""{"priority":{{number}}}"""));
    }

    [Test]
    public void TxConfig_WithOneDestination_HasItsAddressAndAmount()
    {
        var config = new MoneroTxConfig(AddressB, 1000000000) { AccountIndex = 0, Relay = false };

        Assert.That(config.Address, Is.EqualTo(AddressB));
        Assert.That(config.Amount, Is.EqualTo(1000000000));

        // they are not in the JSON, the destination is, which is what monero-c reads
        AssertSameJson($$"""{"accountIndex":0,"destinations":[{"address":"{{AddressB}}","amount":1000000000}],"relay":false}""", Write(config));
    }

    [Test]
    public void TxConfig_SettingTheAddressAndTheAmount_MakesTheDestination()
    {
        var config = new MoneroTxConfig();

        Assert.That(config.Address, Is.Null);
        config.Address = AddressB;
        config.Amount = 5;

        Assert.That(config.Destinations, Has.Count.EqualTo(1));
        Assert.That(Write(config), Is.EqualTo($$"""{"destinations":[{"address":"{{AddressB}}","amount":5}]}"""));

        config.Address = AddressA;
        Assert.That(config.Destinations, Has.Count.EqualTo(1));
        Assert.That(config.Destinations![0].Address, Is.EqualTo(AddressA));
        Assert.That(config.Destinations![0].Amount, Is.EqualTo(5));
    }

    [Test]
    public void TxConfig_WithManyDestinations_RefusesAnAddressAndAnAmount()
    {
        var config = new MoneroTxConfig().AddDestination(AddressA, 1).AddDestination(AddressB, 2);

        Assert.That(config.Destinations, Has.Count.EqualTo(2));
        Assert.Throws<InvalidOperationException>(() => config.Address = AddressA);
        Assert.Throws<InvalidOperationException>(() => config.Amount = 3);
        Assert.That(config.Destinations!.Select(destination => destination.Amount), Is.EqualTo(new ulong?[] { 1, 2 }));
    }

    [Test]
    public void TxQuery_WithEveryField_IsReadAndWritten()
    {
        const string json = """
                            {
                              "hashes": ["aa", "bb"],
                              "hasPaymentId": true,
                              "paymentIds": ["0123456789abcdef"],
                              "height": 100,
                              "minHeight": 50,
                              "maxHeight": 150,
                              "includeOutputs": true,
                              "isConfirmed": true,
                              "isLocked": false,
                              "inTxPool": false,
                              "isIncoming": true,
                              "isOutgoing": false,
                              "isMinerTx": false,
                              "isFailed": false,
                              "isRelayed": true,
                              "transferQuery": { "isIncoming": true, "subaddressIndex": 1, "address": "x", "amount": 5, "accountIndex": 0 },
                              "inputQuery": { "isSpent": true, "minAmount": 1, "maxAmount": 9, "subaddressIndices": [0, 1] },
                              "outputQuery": { "isFrozen": false, "accountIndex": 2, "subaddressIndex": 3, "amount": 7 }
                            }
                            """;

        var query = Read<MoneroTxQuery>(json);

        Assert.Multiple(() =>
        {
            Assert.That(query.Hashes, Is.EqualTo(new[] { "aa", "bb" }));
            Assert.That(query.HasPaymentId, Is.True);
            Assert.That(query.PaymentIds, Is.EqualTo(new[] { "0123456789abcdef" }));
            Assert.That(query.Height, Is.EqualTo(100));
            Assert.That(query.MinHeight, Is.EqualTo(50));
            Assert.That(query.MaxHeight, Is.EqualTo(150));
            Assert.That(query.IncludeOutputs, Is.True);
            Assert.That(query.IsConfirmed, Is.True);
            Assert.That(query.IsLocked, Is.False);
            Assert.That(query.InTxPool, Is.False);
            Assert.That(query.IsIncoming, Is.True);
            Assert.That(query.IsOutgoing, Is.False);
            Assert.That(query.IsMinerTx, Is.False);
            Assert.That(query.IsFailed, Is.False);
            Assert.That(query.IsRelayed, Is.True);
            Assert.That(query.TransferQuery!.IsIncoming, Is.True);
            Assert.That(query.TransferQuery!.GetIsIncoming(), Is.True);
            Assert.That(query.TransferQuery!.GetIsOutgoing(), Is.False);
            Assert.That(query.TransferQuery!.SubaddressIndex, Is.EqualTo(1));
            Assert.That(query.TransferQuery!.Address, Is.EqualTo("x"));
            Assert.That(query.TransferQuery!.Amount, Is.EqualTo(5));
            Assert.That(query.TransferQuery!.AccountIndex, Is.EqualTo(0));
            Assert.That(query.InputQuery!.IsSpent, Is.True);
            Assert.That(query.InputQuery!.MinAmount, Is.EqualTo(1));
            Assert.That(query.InputQuery!.MaxAmount, Is.EqualTo(9));
            Assert.That(query.InputQuery!.SubaddressIndices, Is.EqualTo(new uint[] { 0, 1 }));
            Assert.That(query.OutputQuery!.IsFrozen, Is.False);
            Assert.That(query.OutputQuery!.AccountIndex, Is.EqualTo(2));
            Assert.That(query.OutputQuery!.SubaddressIndex, Is.EqualTo(3));
            Assert.That(query.OutputQuery!.Amount, Is.EqualTo(7));
        });

        AssertSameJson(json, Write(query));
    }

    [TestCase("""{"minHeight":1000000}""")]
    [TestCase("""{"inTxPool":true}""")]
    [TestCase("""{"hashes":["c8b34eec8048721fe18b5d7416edebe76b367c4df636e0186ca82c995d860355"]}""")]
    [TestCase("""{"isConfirmed":true,"minHeight":50,"maxHeight":60}""")]
    [TestCase("""{"minHeight":1,"maxHeight":2}""")]
    public void TxQuery_ThatMoneroCTestsSend_IsWrittenBackUnchanged(string json)
    {
        AssertSameJson(json, Write(Read<MoneroTxQuery>(json)));
    }

    [TestCase("""{"isIncoming":true,"txQuery":{"isConfirmed":true,"minHeight":50,"maxHeight":60}}""")]
    [TestCase("""{"isIncoming":false,"txQuery":{"inTxPool":true}}""")]
    [TestCase("""{"isIncoming":true,"txQuery":{"minHeight":1,"maxHeight":2}}""")]
    [TestCase("""{"subaddressIndex":1,"isIncoming":true}""")]
    [TestCase("""{"isIncoming":false}""")]
    public void TransferQuery_ThatMoneroCTestsSend_IsWrittenBackUnchanged(string json)
    {
        var query = Read<MoneroTransferQuery>(json);

        AssertSameJson(json, Write(query));
        Assert.That(query.TxQuery == null, Is.EqualTo(!json.Contains("txQuery")));
    }

    [TestCase("""{"txQuery":{"isLocked":false}}""")]
    [TestCase("""{"txQuery":{"isLocked":true}}""")]
    [TestCase("""{"isSpent":false,"txQuery":{"isLocked":false}}""")]
    [TestCase("""{"isSpent":false,"isFrozen":false,"txQuery":{"isLocked":false,"minHeight":30,"maxHeight":40}}""")]
    [TestCase("""{"isFrozen":true}""")]
    [TestCase("""{"isSpent":true}""")]
    [TestCase("""{"txQuery":{"hashes":["c8b34eec8048721fe18b5d7416edebe76b367c4df636e0186ca82c995d860355"]}}""")]
    public void OutputQuery_ThatMoneroCTestsSend_IsWrittenBackUnchanged(string json)
    {
        var query = Read<MoneroOutputQuery>(json);

        AssertSameJson(json, Write(query));
    }

    [Test]
    public void TransferQuery_HoldsTheQueryOfItsTx()
    {
        var query = Read<MoneroTransferQuery>("""{"isIncoming":true,"txQuery":{"isConfirmed":true,"minHeight":50,"maxHeight":60}}""");

        Assert.Multiple(() =>
        {
            Assert.That(query.IsIncoming, Is.True);
            Assert.That(query.TxQuery!.IsConfirmed, Is.True);
            Assert.That(query.TxQuery!.MinHeight, Is.EqualTo(50));
            Assert.That(query.TxQuery!.MaxHeight, Is.EqualTo(60));
            Assert.That(query.TxQuery!.TransferQuery, Is.Null);
        });
    }

    [Test]
    public void TransferQuery_ThatAsksForNeither_HasNoDirection()
    {
        var query = new MoneroTransferQuery();

        Assert.That(query.GetIsIncoming(), Is.Null);
        Assert.That(query.GetIsOutgoing(), Is.Null);
        Assert.That(Write(query), Is.EqualTo("{}"));
    }

    [Test]
    public void TransferQuery_WithDestinations_IsWritten()
    {
        var query = new MoneroTransferQuery { HasDestinations = true, Destinations = [new MoneroDestination(AddressB, 5)], Addresses = [AddressA], SubaddressIndices = [0, 1] };

        AssertSameJson($$"""{"hasDestinations":true,"destinations":[{"address":"{{AddressB}}","amount":5}],"addresses":["{{AddressA}}"],"subaddressIndices":[0,1]}""", Write(query));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the results that the wallets of the run didn't make
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Subaddress_WithEveryField_IsReadAndWritten()
    {
        const string json = """{"accountIndex":2,"index":7,"address":"a","label":"l","balance":18446744073709551615,"unlockedBalance":5,"numUnspentOutputs":3,"isUsed":true,"numBlocksToUnlock":9}""";

        var subaddress = Read<MoneroSubaddress>(json);

        Assert.That(subaddress.AccountIndex, Is.EqualTo(2));
        Assert.That(subaddress.Index, Is.EqualTo(7));
        Assert.That(subaddress.Balance, Is.EqualTo(ulong.MaxValue));
        Assert.That(subaddress.NumBlocksToUnlock, Is.EqualTo(9));
        AssertSameJson(json, Write(subaddress));
    }

    [Test]
    public void Account_WithATag_IsReadAndWritten()
    {
        const string json = """{"index":3,"primaryAddress":"a","balance":10,"unlockedBalance":5,"tag":"savings","subaddresses":[{"accountIndex":3,"index":0}]}""";

        var account = Read<MoneroAccount>(json);

        Assert.That(account.Tag, Is.EqualTo("savings"));
        Assert.That(account.Subaddresses!.Single().AccountIndex, Is.EqualTo(3));
        AssertSameJson(json, Write(account));
    }

    [Test]
    public void AccountTags_AreReadAndWritten()
    {
        const string json = """[{"tag":"savings","label":"my savings","accountIndices":[0,2]},{"tag":"work","label":"work"}]""";

        var tags = Read<List<MoneroAccountTag>>(json);

        Assert.That(tags, Has.Count.EqualTo(2));
        Assert.That(tags[0].Tag, Is.EqualTo("savings"));
        Assert.That(tags[0].Label, Is.EqualTo("my savings"));
        Assert.That(tags[0].AccountIndices, Is.EqualTo(new uint[] { 0, 2 }));
        Assert.That(tags[1].AccountIndices, Is.Null);
        AssertSameJson(json, Write(tags));
        Assert.That(JsonSerializer.Serialize(tags, MoneroWalletJsonContext.Default.ListMoneroAccountTag), Is.EqualTo(Write(tags)));
    }

    [TestCase(0, MoneroAddressType.PrimaryAddress)]
    [TestCase(1, MoneroAddressType.IntegratedAddress)]
    [TestCase(2, MoneroAddressType.Subaddress)]
    public void DecodedAddress_IsReadAndWritten_WithTheNumbers(int number, MoneroAddressType type)
    {
        var json = $$"""{"address":"{{AddressA}}","addressType":{{number}},"networkType":3}""";

        var decoded = Read<MoneroDecodedAddress>(json);

        Assert.That(decoded.Address, Is.EqualTo(AddressA));
        Assert.That(decoded.AddressType, Is.EqualTo(type));
        Assert.That(decoded.NetworkType, Is.EqualTo(MoneroNetworkType.Regtest));
        AssertSameJson(json, Write(decoded));
        Assert.That(Write(new MoneroDecodedAddress(AddressA, type, MoneroNetworkType.StageNet)), Does.Contain("\"networkType\":2"));
    }

    [Test]
    public void MultisigSignResult_IsReadAndWritten()
    {
        const string json = """{"signedMultisigTxHex":"4d756c7469736967","txHashes":["aa","bb"]}""";

        var result = Read<MoneroMultisigSignResult>(json);

        Assert.That(result.SignedMultisigTxHex, Is.EqualTo("4d756c7469736967"));
        Assert.That(result.TxHashes, Is.EqualTo(new[] { "aa", "bb" }));
        AssertSameJson(json, Write(result));
    }

    [Test]
    public void TxSet_WithEveryHex_IsReadAndWritten()
    {
        const string json = """{"txs":[{"hash":"aa"}],"signedTxHex":"01","unsignedTxHex":"02","multisigTxHex":"03"}""";

        var set = Read<MoneroTxSet>(json);

        Assert.That(set.SignedTxHex, Is.EqualTo("01"));
        Assert.That(set.UnsignedTxHex, Is.EqualTo("02"));
        Assert.That(set.MultisigTxHex, Is.EqualTo("03"));
        Assert.That(set.Txs!.Single().TxSet, Is.SameAs(set));
        AssertSameJson(json, Write(set));
    }

    [Test]
    public void Tx_OfAWallet_WithEveryFieldOfItsOwn_IsReadAndWritten()
    {
        const string json = """
                            {
                              "hash": "aa",
                              "isIncoming": true,
                              "isOutgoing": true,
                              "incomingTransfers": [
                                { "amount": 1, "accountIndex": 0, "subaddressIndex": 2, "address": "x", "numSuggestedConfirmations": 10 },
                                { "amount": 2, "accountIndex": 0, "subaddressIndex": 3 }
                              ],
                              "outgoingTransfer": { "amount": 3, "accountIndex": 0, "subaddressIndices": [0, 1], "addresses": ["y", "z"], "destinations": [{ "address": "w", "amount": 3 }] },
                              "note": "a note",
                              "inputSum": 20,
                              "outputSum": 18,
                              "changeAddress": "c",
                              "changeAmount": 15,
                              "numDummyOutputs": 1,
                              "extraHex": "0102",
                              "inputs": [{ "amount": 20, "accountIndex": 0, "subaddressIndex": 0, "isSpent": true, "keyImage": { "hex": "ab" } }],
                              "outputs": [{ "amount": 15, "index": 4, "accountIndex": 0, "subaddressIndex": 0, "isSpent": false, "isFrozen": true }]
                            }
                            """;

        var tx = Read<MoneroTxWallet>(json);

        Assert.Multiple(() =>
        {
            Assert.That(tx.IncomingTransfers, Has.Count.EqualTo(2));
            Assert.That(tx.IncomingTransfers!.Select(transfer => transfer.SubaddressIndex), Is.EqualTo(new uint?[] { 2, 3 }));
            Assert.That(tx.IncomingTransfers!.Select(transfer => transfer.Tx), Is.All.SameAs(tx));
            Assert.That(tx.OutgoingTransfer!.Tx, Is.SameAs(tx));
            Assert.That(tx.OutgoingTransfer!.SubaddressIndices, Is.EqualTo(new uint[] { 0, 1 }));
            Assert.That(tx.OutgoingTransfer!.Addresses, Is.EqualTo(new[] { "y", "z" }));
            Assert.That(tx.Note, Is.EqualTo("a note"));
            Assert.That(tx.InputSum, Is.EqualTo(20));
            Assert.That(tx.OutputSum, Is.EqualTo(18));
            Assert.That(tx.ChangeAddress, Is.EqualTo("c"));
            Assert.That(tx.ChangeAmount, Is.EqualTo(15));
            Assert.That(tx.NumDummyOutputs, Is.EqualTo(1));
            Assert.That(tx.ExtraHex, Is.EqualTo("0102"));
            Assert.That(tx.GetInputsWallet().Single().IsSpent, Is.True);
            Assert.That(tx.GetInputsWallet().Single().KeyImage!.Hex, Is.EqualTo("ab"));
            Assert.That(tx.GetOutputsWallet().Single().IsFrozen, Is.True);
            Assert.That(tx.GetOutputsWallet().Single().Index, Is.EqualTo(4));
            Assert.That(tx.GetIncomingAmount(), Is.EqualTo(3));
            Assert.That(tx.GetOutgoingAmount(), Is.EqualTo(3));
            // the outgoing transfer comes first, as in monero-java
            Assert.That(tx.GetTransfers().Select(transfer => transfer.Amount), Is.EqualTo(new ulong?[] { 3, 1, 2 }));
            Assert.That(tx.GetTransfers().Select(transfer => transfer.GetIsIncoming()), Is.EqualTo(new bool?[] { false, true, true }));
        });

        AssertSameJson(json, Write(tx));
    }

    [Test]
    public void Subaddress_Account_AndTheOthers_HaveAConstructorForTheirIndices()
    {
        var subaddress = new MoneroSubaddress(2, 5);
        var account = new MoneroAccount(1, AddressA);
        var entry = new MoneroAddressBookEntry(AddressB, "friend");
        var tag = new MoneroAccountTag("t", "label", [0]);
        var connection = new MoneroRpcConnection("http://localhost:18081", "u", "p");

        Assert.That(Write(subaddress), Is.EqualTo("""{"accountIndex":2,"index":5}"""));
        Assert.That(Write(account), Is.EqualTo($$"""{"index":1,"primaryAddress":"{{AddressA}}"}"""));
        Assert.That(Write(entry), Is.EqualTo($$"""{"address":"{{AddressB}}","description":"friend"}"""));
        Assert.That(Write(tag), Is.EqualTo("""{"tag":"t","label":"label","accountIndices":[0]}"""));
        Assert.That(Write(connection), Is.EqualTo("""{"uri":"http://localhost:18081","username":"u","password":"p"}"""));
        Assert.That(Write(new MoneroSyncResult(5, true)), Is.EqualTo("""{"numBlocksFetched":5,"receivedMoney":true}"""));
        Assert.That(Write(new MoneroDestination(AddressA, 7)), Is.EqualTo($$"""{"address":"{{AddressA}}","amount":7}"""));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the wallet reads its outputs and its txs as the classes of a wallet
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void BlocksOfAWallet_HaveTheTxsOfAWallet_AndTheBlocksOfTheDaemonDoNot()
    {
        const string json = """{"height":5,"txs":[{"hash":"aa","isIncoming":true,"incomingTransfers":[{"amount":1}],"inputs":[{"accountIndex":2}]}]}""";

        var wallet = Read<MoneroBlockWallet>(json);
        var daemon = Read<MoneroBlock>(json);

        Assert.That(wallet.Txs!.Single(), Is.InstanceOf<MoneroTxWallet>());
        Assert.That(((MoneroTxWallet)wallet.Txs!.Single()).IncomingTransfers, Has.Count.EqualTo(1));
        Assert.That(((MoneroTxWallet)wallet.Txs!.Single()).GetInputsWallet().Single().AccountIndex, Is.EqualTo(2));
        Assert.That(daemon.Txs!.Single(), Is.Not.InstanceOf<MoneroTxWallet>());
        Assert.That(daemon.Txs!.Single().Inputs!.Single(), Is.Not.InstanceOf<MoneroOutputWallet>());
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void TxOfAWallet_ReadsItsInputsAndOutputs_AsWalletOutputs(Engine engine)
    {
        var tx = (MoneroTxWallet)ReadWith(engine, """{"inputs":[{"accountIndex":1},{"isSpent":true}],"outputs":[{"subaddressIndex":4}]}""", typeof(MoneroTxWallet));

        Assert.Multiple(() =>
        {
            Assert.That(tx.Inputs, Has.Count.EqualTo(2));
            Assert.That(tx.Inputs, Is.All.InstanceOf<MoneroOutputWallet>());
            Assert.That(tx.Outputs!.Single(), Is.InstanceOf<MoneroOutputWallet>());
            Assert.That(((MoneroOutputWallet)tx.Inputs![0]).AccountIndex, Is.EqualTo(1));
            Assert.That(((MoneroOutputWallet)tx.Inputs![1]).IsSpent, Is.True);
            Assert.That(tx.Inputs, Is.All.Matches<MoneroOutput>(input => ReferenceEquals(input.Tx, tx)));
        });
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void TxOfAWallet_WritesItsOutputs_AsTheClassesThatTheyAre(Engine engine)
    {
        var tx = new MoneroTxWallet
        {
            Inputs = [new MoneroOutputWallet { AccountIndex = 1 }, new MoneroOutput { Amount = 5 }]
        };

        AssertSameJson("""{"inputs":[{"accountIndex":1},{"amount":5}]}""", WriteWith(engine, tx, typeof(MoneroTxWallet)));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Lists_OfAWallet_KeepAnElementThatIsNull(Engine engine)
    {
        var tx = (MoneroTxWallet)ReadWith(engine, """{"inputs":[null,{"accountIndex":1}]}""", typeof(MoneroTxWallet));
        var block = (MoneroBlockWallet)ReadWith(engine, """{"txs":[null,{"hash":"aa"}]}""", typeof(MoneroBlockWallet));

        Assert.That(tx.Inputs, Has.Count.EqualTo(2));
        Assert.That(tx.Inputs![0], Is.Null);
        Assert.That(block.Txs, Has.Count.EqualTo(2));
        Assert.That(block.Txs![0], Is.Null);
        Assert.That(block.GetTxsWallet(), Has.Count.EqualTo(1));
        AssertSameJson("""{"inputs":[null,{"accountIndex":1}]}""", WriteWith(engine, tx, typeof(MoneroTxWallet)));
        AssertSameJson("""{"txs":[null,{"hash":"aa"}]}""", WriteWith(engine, block, typeof(MoneroBlockWallet)));
    }

    [TestCase("""{"inputs":{}}""")]
    [TestCase("""{"inputs":"x"}""")]
    [TestCase("""{"inputs":[1]}""")]
    [TestCase("""{"outputs":[[]]}""")]
    [TestCase("""{"incomingTransfers":{}}""")]
    [TestCase("""{"incomingTransfers":[{"subaddressIndex":-1}]}""")]
    [TestCase("""{"incomingTransfers":[{"accountIndex":4294967296}]}""")]
    [TestCase("""{"outgoingTransfer":[]}""")]
    [TestCase("""{"outgoingTransfer":{"destinations":{}}}""")]
    [TestCase("""{"outgoingTransfer":{"subaddressIndices":[1.5]}}""")]
    public void TxOfAWallet_WithJsonThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroTxWallet>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroWalletJsonContext.Default.MoneroTxWallet));
    }

    [TestCase("""{"txs":{}}""")]
    [TestCase("""{"txs":"x"}""")]
    [TestCase("""{"txs":[1]}""")]
    [TestCase("""{"txs":[{"inputs":{}}]}""")]
    public void BlockOfAWallet_WithJsonThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroBlockWallet>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroWalletJsonContext.Default.MoneroBlockWallet));
    }

    [TestCase("""{"blocks":{}}""")]
    [TestCase("""{"blocks":[{"txs":[{"incomingTransfers":[{"amount":"1"}]}]}]}""")]
    [TestCase("""{"blocks":[{"height":-1}]}""")]
    [TestCase("")]
    [TestCase("[]")]
    [TestCase("null")]
    public void WalletBlocksResult_WithJsonThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroWalletBlocksResult>(json));
    }

    [Test]
    public void MoneroWalletBlocksResult_GetTxs_ListsTheTxsInTheOrderOfTheBlocks()
    {
        const string json = """
                            {
                              "blocks": [
                                { "height": 10, "txs": [ { "hash": "a" }, { "hash": "b" } ] },
                                { "height": 11 },
                                { "height": 12, "txs": [] },
                                { "txs": [ { "hash": "c" } ] },
                                null
                              ]
                            }
                            """;

        var result = Read<MoneroWalletBlocksResult>(json);
        var txs = result.GetTxs();

        Assert.That(txs.Select(tx => tx.Hash), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(txs.Select(tx => tx.GetHeight()), Is.EqualTo(new ulong?[] { 10, 10, null }));
        Assert.That(new MoneroWalletBlocksResult().GetTxs(), Is.Empty);
        Assert.That(new MoneroWalletBlocksResult().GetTransfers(), Is.Empty);
        Assert.That(new MoneroWalletBlocksResult().GetOutputs(), Is.Empty);
        Assert.That(new MoneroWalletBlocksResult().GetInputs(), Is.Empty);
        Assert.That(new MoneroTxSetsResult().GetTxs(), Is.Empty);
    }

    [Test]
    public void MoneroTxSetsResult_GetTxs_ListsTheTxsInTheOrderOfTheSets()
    {
        var result = Read<MoneroTxSetsResult>("""{"txSets":[{"txs":[{"hash":"a"},{"hash":"b"}]},{"unsignedTxHex":"00"},{"txs":[{"hash":"c"}]}]}""");

        Assert.That(result.GetTxs().Select(tx => tx.Hash), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(result.GetTxs().Select(tx => tx.TxSet), Is.EqualTo(new[] { result.TxSets![0], result.TxSets![0], result.TxSets![2] }));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the parents of the transfers, the outputs, the txs
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Tx_SetsItselfAsTheTxOfItsTransfers()
    {
        var incoming = new List<MoneroIncomingTransfer> { new() { Amount = 1 }, new() { Amount = 2 } };
        var outgoing = new MoneroOutgoingTransfer { Amount = 3 };

        var tx = new MoneroTxWallet { IncomingTransfers = incoming, OutgoingTransfer = outgoing };

        Assert.That(incoming, Is.All.Matches<MoneroIncomingTransfer>(transfer => ReferenceEquals(transfer.Tx, tx)));
        Assert.That(outgoing.Tx, Is.SameAs(tx));
    }

    [Test]
    public void Set_SetsItselfAsTheSetOfItsTxs()
    {
        var txs = new List<MoneroTxWallet> { new() { Hash = "a" }, new() { Hash = "b" } };

        var set = new MoneroTxSet { Txs = txs };

        Assert.That(txs, Is.All.Matches<MoneroTxWallet>(tx => ReferenceEquals(tx.TxSet, set)));
    }

    [Test]
    public void Block_OfAWallet_SetsItselfAsTheBlockOfItsTxs()
    {
        var txs = new List<MoneroTx> { new MoneroTxWallet { Hash = "a" }, new MoneroTxWallet { Hash = "b" } };

        var block = new MoneroBlockWallet { Height = 5, Txs = txs };

        Assert.That(txs, Is.All.Matches<MoneroTx>(tx => ReferenceEquals(tx.Block, block)));
        Assert.That(txs.Select(tx => tx.GetHeight()), Is.All.EqualTo(5));
        Assert.That(block.GetTxsWallet(), Has.Count.EqualTo(2));
    }

    [Test]
    public void Parents_AreSetByTheDeserializers_WhateverTheOrderOfTheFields()
    {
        // the transfers come before the hash of the tx, and the tx comes before the height of its block
        const string json = """{"txs":[{"incomingTransfers":[{"amount":1}],"outgoingTransfer":{"amount":2},"outputs":[{"amount":3}],"hash":"aa"}],"height":9}""";

        foreach (var engine in Enum.GetValues<Engine>())
        {
            var block = (MoneroBlockWallet)ReadWith(engine, json, typeof(MoneroBlockWallet));
            var tx = block.GetTxsWallet().Single();

            Assert.That(tx.Block, Is.SameAs(block), engine.ToString());
            Assert.That(tx.GetHeight(), Is.EqualTo(9), engine.ToString());
            Assert.That(tx.IncomingTransfers![0].Tx, Is.SameAs(tx), engine.ToString());
            Assert.That(tx.OutgoingTransfer!.Tx, Is.SameAs(tx), engine.ToString());
            Assert.That(tx.GetOutputsWallet()[0].Tx, Is.SameAs(tx), engine.ToString());
        }
    }

    [Test]
    public void Parents_AreNotInTheJson_SoAGraphIsWrittenWithoutCycles()
    {
        var set = new MoneroTxSet
        {
            Txs =
            [
                new MoneroTxWallet
                {
                    Hash = "a",
                    IncomingTransfers = [new MoneroIncomingTransfer { Amount = 1 }],
                    OutgoingTransfer = new MoneroOutgoingTransfer { Amount = 2 },
                    Inputs = [new MoneroOutputWallet { Amount = 3 }]
                }
            ]
        };
        var block = new MoneroBlockWallet { Height = 1, Txs = [set.Txs![0]] };

        var json = "";
        Assert.DoesNotThrow(() => json = Write(set));

        Assert.That(json, Does.Not.Contain("txSet").And.Not.Contain("\"block\"").And.Not.Contain("\"tx\""));
        Assert.That(block.Txs![0].Block, Is.SameAs(block));
        Assert.That(set.Txs![0].TxSet, Is.SameAs(set));
        AssertSameJson("""{"txs":[{"hash":"a","incomingTransfers":[{"amount":1}],"outgoingTransfer":{"amount":2},"inputs":[{"amount":3}]}]}""", json);
    }

    [Test]
    public void Tx_WhenItsListsAreReplacedOrCleared_IsStillSafe()
    {
        var tx = new MoneroTxWallet { IncomingTransfers = [new MoneroIncomingTransfer()], OutgoingTransfer = new MoneroOutgoingTransfer(), Inputs = [new MoneroOutputWallet()] };
        var replaced = new List<MoneroIncomingTransfer> { new() };

        tx.IncomingTransfers = replaced;
        Assert.That(replaced[0].Tx, Is.SameAs(tx));

        Assert.DoesNotThrow(() =>
        {
            tx.IncomingTransfers = null;
            tx.OutgoingTransfer = null;
            tx.Inputs = null;
            new MoneroTxSet { Txs = null };
            new MoneroBlockWallet { Txs = null };
        });
        Assert.That(tx.GetTransfers(), Is.Empty);
        Assert.That(tx.GetInputsWallet(), Is.Empty);
        Assert.That(tx.GetIncomingAmount(), Is.EqualTo(0));
    }

    [Test]
    public void BlockWithManyTxs_IsReadAndWritten()
    {
        var tx = JsonNode.Parse(Fixture("txs_miner"))!["blocks"]![0]!["txs"]![0]!;
        var txs = new JsonArray();
        for (var i = 0; i < 2000; i++) txs.Add(tx.DeepClone());
        var json = new JsonObject { ["blocks"] = new JsonArray(new JsonObject { ["height"] = 1, ["txs"] = txs }) }.ToJsonString();

        var result = Read<MoneroWalletBlocksResult>(json);

        Assert.That(result.GetTxs(), Has.Count.EqualTo(2000));
        Assert.That(result.GetTxs(), Is.All.Matches<MoneroTxWallet>(t => ReferenceEquals(t.Block, result.Blocks![0]) && ReferenceEquals(t.IncomingTransfers![0].Tx, t)));
        AssertSameJson(json, Write(result));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the rules that every model follows
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void ModelTypes_AreAllFound()
    {
        // a guard for the guards: if the search finds nothing, the tests over the models would pass without testing anything
        Assert.That(ModelTypes.Length, Is.GreaterThanOrEqualTo(30));
        Assert.That(ModelTypes, Does.Contain(typeof(MoneroTxWallet)).And.Contain(typeof(MoneroTxQuery)).And.Contain(typeof(MoneroRpcConnection)).And.Contain(typeof(MoneroTxSet)));
        Assert.That(ModelTypes, Does.Not.Contain(typeof(MoneroTransfer)), "an abstract class has no JSON of its own");
    }

    [TestCaseSource(nameof(ModelCases))]
    public void Model_PropertiesHaveACamelCaseJsonName(Type type)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in JsonProperties(type))
        {
            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

            Assert.That(name, Is.Not.Null, $"{type.Name}.{property.Name} has no JsonPropertyName");
            Assert.That(name, Does.Match("^[a-z][A-Za-z0-9]*$"), $"{type.Name}.{property.Name} is not camel case");
            Assert.That(names.Add(name!), Is.True, $"{type.Name}.{property.Name} repeats the name {name}");
        }
    }

    [TestCaseSource(nameof(ModelCases))]
    public void Model_PropertiesCanBeUnset_AndAreReadAndWritten(Type type)
    {
        foreach (var property in JsonProperties(type))
        {
            var canBeNull = Nullable.GetUnderlyingType(property.PropertyType) != null || !property.PropertyType.IsValueType;

            Assert.That(canBeNull, Is.True, $"{type.Name}.{property.Name} can not be unset");
            Assert.That(property.CanRead && property.CanWrite && property.GetSetMethod() != null, Is.True, $"{type.Name}.{property.Name} is read only");
        }
    }

    [TestCaseSource(nameof(ModelCases))]
    public void Model_HasNoPublicFields(Type type)
    {
        Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
    }

    [TestCaseSource(nameof(ModelCases))]
    public void Model_WhenNothingIsSet_IsAnEmptyObject_WhateverTheOptions(Type type)
    {
        var model = Activator.CreateInstance(type)!;

        Assert.That(JsonSerializer.Serialize(model, type), Is.EqualTo("{}"), "default options of the serializer");
        Assert.That(JsonSerializer.Serialize(model, type, MoneroJson.Options), Is.EqualTo("{}"));
        Assert.That(JsonSerializer.Serialize(model, type, MoneroWalletJsonContext.Default), Is.EqualTo("{}"));
    }

    [TestCaseSource(nameof(ModelEngineCases))]
    public void Model_ReadFromAnEmptyObject_HasNothingSet(Type type, Engine engine)
    {
        var model = ReadWith(engine, "{}", type);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0))
        {
            Assert.That(property.GetValue(model), Is.Null, $"{type.Name}.{property.Name}");
        }
    }

    [TestCaseSource(nameof(ModelEngineCases))]
    public void Model_ReadFromNullFields_HasNothingSet(Type type, Engine engine)
    {
        var json = "{" + string.Join(",", JsonProperties(type).Select(property => $"\"{property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name}\":null")) + "}";

        var model = ReadWith(engine, json, type);

        foreach (var property in JsonProperties(type))
        {
            Assert.That(property.GetValue(model), Is.Null, $"{type.Name}.{property.Name}");
        }

        Assert.That(WriteWith(engine, model, type), Is.EqualTo("{}"));
    }

    [TestCaseSource(nameof(ModelEngineCases))]
    public void Model_ReadFromAnObjectWithUnknownFields_IgnoresThem(Type type, Engine engine)
    {
        const string unknown = "\"unknownField\":{\"a\":[1,2,{\"b\":null}],\"c\":\"d\"},\"anotherUnknownField\":[true],\"yetAnotherUnknownField\":1.5";
        var model = NewSampler().Populate(Activator.CreateInstance(type)!);
        var json = WriteWith(engine, model, type);
        var withUnknown = json == "{}" ? "{" + unknown + "}" : json[..^1] + "," + unknown + "}";

        var read = ReadWith(engine, withUnknown, type);

        Assert.That(Diff(JsonNode.Parse(json), JsonNode.Parse(WriteWith(engine, read, type))), Is.Null);
    }

    [TestCaseSource(nameof(ModelEngineCases))]
    public void Model_WithEveryPropertySet_IsReadAndWritten(Type type, Engine engine)
    {
        var model = NewSampler().Populate(Activator.CreateInstance(type)!);
        var expectedNames = JsonProperties(type).Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name).ToArray();

        var json = WriteWith(engine, model, type);
        var node = JsonNode.Parse(json)!.AsObject();

        // every property is written, with its name, and nothing else is
        Assert.That(node.Select(property => property.Key), Is.EquivalentTo(expectedNames));

        // and it is read back to the same
        var read = ReadWith(engine, json, type);
        Assert.That(Diff(node, JsonNode.Parse(WriteWith(engine, read, type))), Is.Null);

        // value by value, not only as JSON
        foreach (var property in JsonProperties(type).Where(property => property.PropertyType.IsValueType || property.PropertyType == typeof(string)))
        {
            Assert.That(property.GetValue(read), Is.EqualTo(property.GetValue(model)), $"{type.Name}.{property.Name}");
        }
    }

    [TestCaseSource(nameof(ModelCases))]
    public void Model_IsKnownToTheGeneratedCode(Type type)
    {
        Assert.That(MoneroWalletJsonContext.Default.GetTypeInfo(type), Is.Not.Null, $"{type.Name} is not in MoneroWalletJsonContext");
    }

    [Test]
    public void GeneratedCode_ReadsWithTheTypeInformationOfTheContext()
    {
        var result = JsonSerializer.Deserialize(Fixture("txs_miner"), MoneroWalletJsonContext.Default.MoneroWalletBlocksResult)!;

        Assert.That(result.Blocks, Has.Count.EqualTo(2));
        Assert.That(JsonSerializer.Serialize(result, MoneroWalletJsonContext.Default.MoneroWalletBlocksResult), Is.EqualTo(Write(result)));

        var accounts = JsonSerializer.Deserialize(Fixture("accounts_with_subaddresses"), MoneroWalletJsonContext.Default.ListMoneroAccount)!;
        Assert.That(accounts, Has.Count.EqualTo(2));
        Assert.That(JsonSerializer.Deserialize(Fixture("tx_sets_created"), MoneroWalletJsonContext.Default.MoneroTxSetsResult)!.GetTxs().Single().TxSet, Is.Not.Null);
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // copies
    // ------------------------------------------------------------------------------------------------------------------------------

    private static readonly Type[] CopyTypes =
    [
        typeof(MoneroTxWallet), typeof(MoneroTxQuery), typeof(MoneroOutputWallet), typeof(MoneroOutputQuery), typeof(MoneroIncomingTransfer),
        typeof(MoneroOutgoingTransfer), typeof(MoneroTransferQuery), typeof(MoneroTxSet), typeof(MoneroBlockWallet), typeof(MoneroSubaddress),
        typeof(MoneroAccount), typeof(MoneroDestination), typeof(MoneroTxConfig), typeof(MoneroWalletConfig), typeof(MoneroRpcConnection)
    ];

    public static IEnumerable<TestCaseData> CopyCases()
    {
        return CopyTypes.Select(type => new TestCaseData(type).SetArgDisplayNames(type.Name));
    }

    private static object CopyOf(Type type, object model)
    {
        return type.GetMethod("Copy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.EmptyTypes)!.Invoke(model, null)!;
    }

    [TestCaseSource(nameof(CopyCases))]
    public void Copy_HasEveryPropertyOfTheOriginal(Type type)
    {
        // every property has a different value, so a property that is copied from another one is found
        var model = NewSampler().Populate(Activator.CreateInstance(type)!);

        var copy = CopyOf(type, model);

        Assert.That(copy, Is.Not.SameAs(model));
        Assert.That(copy.GetType(), Is.EqualTo(type));
        AssertSameJson(JsonSerializer.Serialize(model, type, MoneroJson.Options), JsonSerializer.Serialize(copy, type, MoneroJson.Options));
    }

    [TestCaseSource(nameof(CopyCases))]
    public void Copy_OfAModelWithNothingSet_HasNothingSet(Type type)
    {
        var copy = CopyOf(type, Activator.CreateInstance(type)!);

        Assert.That(JsonSerializer.Serialize(copy, type, MoneroJson.Options), Is.EqualTo("{}"));
    }

    [Test]
    public void Copy_OfATx_IsDeep_AndHasItsOwnTransfersAndOutputs()
    {
        var tx = ReadFixture<MoneroWalletBlocksResult>("tx_to_subaddress").GetTxs().Single();
        tx.IncomingTransfers = [new MoneroIncomingTransfer { Amount = 9, SubaddressIndex = 1 }];
        tx.Outputs = [new MoneroOutputWallet { Amount = 4, IsFrozen = true }];
        var before = Write(tx);

        var copy = tx.Copy();
        copy.OutgoingTransfer!.Destinations![0].Amount = 1;
        copy.OutgoingTransfer!.Addresses!.Clear();
        copy.IncomingTransfers![0].Amount = 1;
        copy.GetOutputsWallet()[0].IsFrozen = false;
        copy.Note = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(Write(tx), Is.EqualTo(before));
            Assert.That(copy.Block, Is.Null);
            Assert.That(copy.TxSet, Is.Null);
            Assert.That(tx.Block, Is.Not.Null);
            Assert.That(copy.OutgoingTransfer!.Tx, Is.SameAs(copy));
            Assert.That(copy.IncomingTransfers!.Single().Tx, Is.SameAs(copy));
            Assert.That(copy.GetOutputsWallet()[0], Is.InstanceOf<MoneroOutputWallet>());
            Assert.That(copy.GetOutputsWallet()[0].Tx, Is.SameAs(copy));
            Assert.That(tx.OutgoingTransfer!.Tx, Is.SameAs(tx));
        });
    }

    [Test]
    public void Copy_OfASet_HasItsOwnTxs()
    {
        var set = ReadFixture<MoneroTxSetsResult>("tx_sets_unsigned").TxSets!.Single();
        var before = Write(set);

        var copy = set.Copy();
        copy.Txs![0].Fee = 1;
        copy.UnsignedTxHex = "00";

        Assert.That(Write(set), Is.EqualTo(before));
        Assert.That(copy.Txs![0].TxSet, Is.SameAs(copy));
        Assert.That(set.Txs![0].TxSet, Is.SameAs(set));
    }

    [Test]
    public void Copy_OfABlock_HasItsOwnTxsOfAWallet()
    {
        var block = ReadFixture<MoneroWalletBlocksResult>("tx_to_subaddress").Blocks!.Single();
        var before = Write(block);

        var copy = block.Copy();
        copy.GetTxsWallet()[0].OutgoingTransfer!.Amount = 1;

        Assert.That(Write(block), Is.EqualTo(before));
        Assert.That(copy.GetTxsWallet(), Has.Count.EqualTo(1));
        Assert.That(copy.Txs![0].Block, Is.SameAs(copy));
    }

    [Test]
    public void Copy_IsOfTheSameClass_WhateverTheTypeItIsCalledOn()
    {
        MoneroTx tx = new MoneroTxWallet { Hash = "aa", Note = "n" };
        MoneroOutput output = new MoneroOutputWallet { AccountIndex = 1 };
        MoneroTransfer incoming = new MoneroIncomingTransfer { SubaddressIndex = 2 };
        MoneroTransfer query = new MoneroTransferQuery { IsIncoming = false };
        MoneroBlock block = new MoneroBlockWallet { Height = 3 };
        MoneroTxWallet txQuery = new MoneroTxQuery { MinHeight = 4 };
        MoneroOutputWallet outputQuery = new MoneroOutputQuery { MinAmount = 5 };

        Assert.Multiple(() =>
        {
            Assert.That(tx.Copy(), Is.InstanceOf<MoneroTxWallet>());
            Assert.That(((MoneroTxWallet)tx.Copy()).Note, Is.EqualTo("n"));
            Assert.That(((MoneroOutputWallet)output.Copy()).AccountIndex, Is.EqualTo(1));
            Assert.That(((MoneroIncomingTransfer)incoming.Copy()).SubaddressIndex, Is.EqualTo(2));
            Assert.That(((MoneroTransferQuery)query.Copy()).GetIsIncoming(), Is.False);
            Assert.That(block.Copy(), Is.InstanceOf<MoneroBlockWallet>());
            Assert.That(((MoneroTxQuery)txQuery.Copy()).MinHeight, Is.EqualTo(4));
            Assert.That(((MoneroOutputQuery)outputQuery.Copy()).MinAmount, Is.EqualTo(5));
        });
    }

    [Test]
    public void Copy_OfAQuery_CopiesTheQueriesInsideIt()
    {
        var query = Read<MoneroTxQuery>("""{"transferQuery":{"txQuery":{"minHeight":1}},"inputQuery":{"minAmount":2},"outputQuery":{"subaddressIndices":[3]}}""");

        var copy = query.Copy();
        copy.TransferQuery!.TxQuery!.MinHeight = 9;
        copy.InputQuery!.MinAmount = 9;
        copy.OutputQuery!.SubaddressIndices!.Add(9);

        Assert.That(query.TransferQuery!.TxQuery!.MinHeight, Is.EqualTo(1));
        Assert.That(query.InputQuery!.MinAmount, Is.EqualTo(2));
        Assert.That(query.OutputQuery!.SubaddressIndices, Is.EqualTo(new uint[] { 3 }));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // enumerations and text
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Enumerations_HaveTheNumbersOfMoneroCpp()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)MoneroTxPriority.Default, Is.EqualTo(0));
            Assert.That((int)MoneroTxPriority.Unimportant, Is.EqualTo(1));
            Assert.That((int)MoneroTxPriority.Normal, Is.EqualTo(2));
            Assert.That((int)MoneroTxPriority.Elevated, Is.EqualTo(3));
            Assert.That(Enum.GetValues<MoneroTxPriority>(), Has.Length.EqualTo(4));

            Assert.That((int)MoneroAddressType.PrimaryAddress, Is.EqualTo(0));
            Assert.That((int)MoneroAddressType.IntegratedAddress, Is.EqualTo(1));
            Assert.That((int)MoneroAddressType.Subaddress, Is.EqualTo(2));
            Assert.That(Enum.GetValues<MoneroAddressType>(), Has.Length.EqualTo(3));

            Assert.That((int)MoneroMessageSignatureType.SignWithSpendKey, Is.EqualTo(0));
            Assert.That((int)MoneroMessageSignatureType.SignWithViewKey, Is.EqualTo(1));
            Assert.That(Enum.GetValues<MoneroMessageSignatureType>(), Has.Length.EqualTo(2));
        });
    }

    [TestCase("spend", MoneroMessageSignatureType.SignWithSpendKey)]
    [TestCase("view", MoneroMessageSignatureType.SignWithViewKey)]
    public void MessageSignatureType_IsTheTextThatTheResultHas(string text, MoneroMessageSignatureType type)
    {
        var json = $$"""{"signatureType":"{{text}}"}""";

        Assert.That(Read<MoneroMessageSignatureResult>(json).SignatureType, Is.EqualTo(type));
        Assert.That(Write(new MoneroMessageSignatureResult { SignatureType = type }), Is.EqualTo(json));
        Assert.That(JsonSerializer.Deserialize(json, MoneroWalletJsonContext.Default.MoneroMessageSignatureResult)!.SignatureType, Is.EqualTo(type));
        Assert.That(JsonSerializer.Serialize(new MoneroMessageSignatureResult { SignatureType = type }, MoneroWalletJsonContext.Default.MoneroMessageSignatureResult), Is.EqualTo(json));
    }

    [TestCase("""{"signatureType":"both"}""")]
    [TestCase("""{"signatureType":"Spend"}""")]
    [TestCase("""{"signatureType":0}""")]
    [TestCase("""{"signatureType":true}""")]
    [TestCase("""{"signatureType":{}}""")]
    public void MessageSignatureType_ThatIsNotSpendOrView_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroMessageSignatureResult>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroWalletJsonContext.Default.MoneroMessageSignatureResult));
    }

    [Test]
    public void MessageSignatureType_ThatIsNotKnown_IsNotWritten()
    {
        Assert.Throws<JsonException>(() => Write(new MoneroMessageSignatureResult { SignatureType = (MoneroMessageSignatureType)5 }));
    }

    [Test]
    public void Numbers_UseTheWholeRangeOfTheirType()
    {
        var subaddress = Read<MoneroSubaddress>($$"""{"accountIndex":{{uint.MaxValue}},"balance":{{ulong.MaxValue}}}""");

        Assert.That(subaddress.AccountIndex, Is.EqualTo(uint.MaxValue));
        Assert.That(subaddress.Balance, Is.EqualTo(ulong.MaxValue));
        Assert.That(Write(subaddress), Is.EqualTo($$"""{"accountIndex":{{uint.MaxValue}},"balance":{{ulong.MaxValue}}}"""));
        Assert.Throws<JsonException>(() => Read<MoneroSubaddress>($$"""{"accountIndex":{{uint.MaxValue + 1UL}}}"""));
        Assert.Throws<JsonException>(() => Read<MoneroSubaddress>("""{"balance":-1}"""));
    }

    [Test]
    public void Text_WithEscapesAndUnicode_SurvivesTheRoundTrip()
    {
        const string text = "line\nsecond \"quoted\" back\\slash \t tab ünï ☃ \U0001F600 </script> & <>";

        var entry = Read<MoneroAddressBookEntry>(Write(new MoneroAddressBookEntry { Description = text }));
        var tx = Read<MoneroTxWallet>(Write(new MoneroTxWallet { Note = text }));

        Assert.That(entry.Description, Is.EqualTo(text));
        Assert.That(tx.Note, Is.EqualTo(text));
        Assert.That(JsonSerializer.Deserialize(Write(new MoneroTxWallet { Note = text }), MoneroWalletJsonContext.Default.MoneroTxWallet)!.Note, Is.EqualTo(text));
    }

    [Test]
    public void EmptyText_IsAValue_NotAFieldThatIsNotSet()
    {
        var notes = Read<List<string>>("""["","a"]""");

        Assert.That(notes, Is.EqualTo(new[] { "", "a" }));
        Assert.That(Write(new MoneroTxWallet { Note = "" }), Is.EqualTo("""{"note":""}"""));
        Assert.That(Write(new MoneroTxWallet { Note = null }), Is.EqualTo("{}"));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // MoneroJson, and the listener
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void MoneroJson_ReadsAndWritesBytesAndStringsTheSame()
    {
        var json = Fixture("tx_sets_created");

        var fromString = MoneroJson.Deserialize<MoneroTxSetsResult>(json);
        var fromBytes = MoneroJson.Deserialize<MoneroTxSetsResult>(Encoding.UTF8.GetBytes(json));

        Assert.That(Write(fromBytes), Is.EqualTo(Write(fromString)));
        Assert.That(Encoding.UTF8.GetString(MoneroJson.SerializeToUtf8Bytes(fromString)), Is.EqualTo(Write(fromString)));
    }

    [Test]
    public void MoneroJson_CanBeUsedFromManyThreads()
    {
        var json = Fixture("tx_sets_created_relayed");
        var expected = Write(MoneroJson.Deserialize<MoneroTxSetsResult>(json));
        var results = new string[200];

        Parallel.For(0, results.Length, i => results[i] = Write(MoneroJson.Deserialize<MoneroTxSetsResult>(json)));

        Assert.That(results, Is.All.EqualTo(expected));
    }

    private sealed class RecordingListener : MoneroWalletListener
    {
        public readonly List<string> Calls = [];

        public override void OnSyncProgress(ulong height, ulong startHeight, ulong endHeight, double percentDone, string message) => Calls.Add($"progress {height} {startHeight} {endHeight} {percentDone} {message}");

        public override void OnNewBlock(ulong height) => Calls.Add($"block {height}");

        public override void OnBalancesChanged(ulong newBalance, ulong newUnlockedBalance) => Calls.Add($"balances {newBalance} {newUnlockedBalance}");

        public override void OnOutputReceived(MoneroOutputWallet output) => Calls.Add($"received {output.Amount} {output.GetTxWallet()?.Hash}");

        public override void OnOutputSpent(MoneroOutputWallet output) => Calls.Add($"spent {output.Amount} {output.GetTxWallet()?.GetHeight()}");
    }

    [Test]
    public void Listener_ThatOverridesNothing_DoesNothing()
    {
        var listener = new MoneroWalletListener();
        var output = ReadFixture<MoneroWalletBlocksResult>("listener_output_received_first").GetOutputs().Single();

        Assert.DoesNotThrow(() =>
        {
            listener.OnSyncProgress(1, 0, 2, 0.5, "message");
            listener.OnNewBlock(1);
            listener.OnBalancesChanged(1, 1);
            listener.OnOutputReceived(output);
            listener.OnOutputSpent(output);
        });
    }

    [Test]
    public void Listener_GetsTheOutputsOfTheNotifications_InTheirTxAndBlock()
    {
        var listener = new RecordingListener();

        listener.OnOutputReceived(ReadFixture<MoneroWalletBlocksResult>("listener_output_received_first").GetOutputs().Single());
        listener.OnOutputSpent(ReadFixture<MoneroWalletBlocksResult>("listener_output_spent_first").GetInputs().Single());
        listener.OnNewBlock(7);
        listener.OnBalancesChanged(5, 3);
        listener.OnSyncProgress(2, 1, 10, 0.1, "Synchronizing");

        Assert.That(listener.Calls, Is.EqualTo(new[]
        {
            "received 35184338534400 a4d361eaf5925c0bcd7784766da5e9e373b937b380b5bb679227e5ba93b19b8b",
            "spent 35184338534400 101",
            "block 7",
            "balances 5 3",
            "progress 2 1 10 0.1 Synchronizing"
        }));
    }
}
