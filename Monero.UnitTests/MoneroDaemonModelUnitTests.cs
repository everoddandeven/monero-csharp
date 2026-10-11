using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Monero.Common;
using Monero.Daemon.Model;
using static Monero.UnitTests.ModelTestSupport;

namespace Monero.UnitTests;

/// <summary>
/// Serialization and deserialization of the daemon data model.
/// <para>
/// The JSON in <c>Fixtures/Daemon</c> was written by monero-c (and so by monero-cpp) from a regtest node, one file for every result that the
/// daemon returns. The models have to read it, and to write it back unchanged. What a regtest node can not produce (peers, bans, alternative
/// chains, a pool with transactions in the histogram, 128-bit values over 64 bits...) is written by hand in the tests, in the same shape.
/// </para>
/// <para>
/// Every test runs without monero-c, a node or the network.
/// </para>
/// </summary>
[TestFixture]
public class MoneroDaemonModelUnitTests
{
    private const string TopHash = "debce41d53f56ad4db3348a73cf52870f7ce18e64b6e961f164282516a803209";
    private const string PrevHash = "d52697790f85fc3bd5dcdb1334a8a9ea06d07c0d89dee26bc8bace65a01491f2";
    private const string SeedHash = "418015bb9ae982a1975da7d79277c2705727a56894ba0fb246adaabb1f4632e3";
    private const string TxHash = "2937db7948bd94f536866c9c02405b147b4dd2bf9d838ddfb8f33963c4926d4b";
    private const string MinerTxHash = "0f5c276f3df330857d5aac540338dff4908f854b24e88951cde2b0a17ce9b4af";
    private const string PrunableHash = "f7a26b7989caf255d7b686c0e0b42897bba4dd056c4cdd6054d7af3ecb9f4db7";

    // ------------------------------------------------------------------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// The fixtures and the type of the model that every one of them holds.
    /// </summary>
    private static readonly (string Name, Type Type)[] FixtureTypes =
    [
        ("alt_chains_empty", typeof(List<MoneroAltChain>)),
        ("block", typeof(MoneroBlock)),
        ("block_headers", typeof(List<MoneroBlockHeader>)),
        ("block_template", typeof(MoneroBlockTemplate)),
        ("block_with_tx", typeof(MoneroBlock)),
        ("blocks", typeof(List<MoneroBlock>)),
        ("blocks_by_hash_result", typeof(MoneroGetBlocksByHashResult)),
        ("daemon_info", typeof(MoneroDaemonInfo)),
        ("fee_estimate", typeof(MoneroFeeEstimate)),
        ("generate_blocks_result", typeof(MoneroGenerateBlocksResult)),
        ("hard_fork_info", typeof(MoneroHardForkInfo)),
        ("last_block_header", typeof(MoneroBlockHeader)),
        ("miner_data", typeof(MoneroMinerData)),
        ("miner_tx_sum", typeof(MoneroMinerTxSum)),
        ("mining_status", typeof(MoneroMiningStatus)),
        ("network_stats", typeof(MoneroDaemonNetworkStats)),
        ("output_distribution", typeof(List<MoneroOutputDistributionEntry>)),
        ("output_histogram", typeof(List<MoneroOutputHistogramEntry>)),
        ("peers_empty", typeof(List<MoneroPeer>)),
        ("prune_result", typeof(MoneroPruneResult)),
        ("submit_tx_result", typeof(MoneroSubmitTxResult)),
        ("sync_info", typeof(MoneroDaemonSyncInfo)),
        ("tx_in_block", typeof(MoneroBlocksResult)),
        ("tx_in_pool", typeof(MoneroBlocksResult)),
        ("tx_pool", typeof(MoneroBlocksResult)),
        ("tx_pool_stats", typeof(MoneroTxPoolStats)),
        ("tx_pruned_in_block", typeof(MoneroBlocksResult)),
        ("version", typeof(MoneroVersion)),
    ];

    /// <summary>
    /// The classes of the model that are read from and written to JSON.
    /// </summary>
    private static readonly Type[] ModelTypes = typeof(MoneroTx).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                       && type.Namespace == "Monero.Daemon.Model"
                       && type.GetConstructor(Type.EmptyTypes) != null
                       && type != typeof(MoneroDaemonListener)
                       && !typeof(JsonSerializerContext).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

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
        return ModelTestSupport.FixturePath("Daemon", name);
    }

    private static string Fixture(string name)
    {
        return ModelTestSupport.Fixture("Daemon", name);
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
        return ModelTestSupport.ReadWith(engine, json, type, MoneroDaemonJsonContext.Default);
    }

    private static string WriteWith(Engine engine, object model, Type type)
    {
        return ModelTestSupport.WriteWith(engine, model, type, MoneroDaemonJsonContext.Default);
    }


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

    public static IEnumerable<string> FixtureNames => FixtureTypes.Select(fixture => fixture.Name);

    [Test]
    public void DaemonInfo_ReadsTheFixture()
    {
        var info = ReadFixture<MoneroDaemonInfo>("daemon_info");

        Assert.Multiple(() =>
        {
            Assert.That(info.TopBlockHash, Is.EqualTo(TopHash));
            Assert.That(info.Credits, Is.EqualTo(0));
            Assert.That(info.Version, Is.EqualTo("0.18.5.1-release"));
            Assert.That(info.NumAltBlocks, Is.EqualTo(0));
            Assert.That(info.BlockSizeLimit, Is.EqualTo(600000));
            Assert.That(info.BlockSizeMedian, Is.EqualTo(300000));
            Assert.That(info.BlockWeightLimit, Is.EqualTo(600000));
            Assert.That(info.BlockWeightMedian, Is.EqualTo(300000));
            Assert.That(info.BootstrapDaemonAddress, Is.Null);
            Assert.That(info.DifficultyLow, Is.EqualTo(1));
            Assert.That(info.DifficultyHigh, Is.EqualTo(0));
            Assert.That(info.Difficulty, Is.EqualTo((UInt128)1));
            Assert.That(info.CumulativeDifficultyLow, Is.EqualTo(102));
            Assert.That(info.CumulativeDifficultyHigh, Is.EqualTo(0));
            Assert.That(info.CumulativeDifficulty, Is.EqualTo((UInt128)102));
            Assert.That(info.FreeSpace, Is.EqualTo(86356631552UL));
            Assert.That(info.NumOfflinePeers, Is.EqualTo(0));
            Assert.That(info.NumOnlinePeers, Is.EqualTo(0));
            Assert.That(info.Height, Is.EqualTo(102));
            Assert.That(info.HeightWithoutBootstrap, Is.EqualTo(102));
            Assert.That(info.NetworkType, Is.EqualTo(MoneroNetworkType.MainNet));
            Assert.That(info.IsOffline, Is.True);
            Assert.That(info.NumIncomingConnections, Is.EqualTo(0));
            Assert.That(info.NumOutgoingConnections, Is.EqualTo(0));
            Assert.That(info.NumRpcConnections, Is.EqualTo(2));
            Assert.That(info.StartTimestamp, Is.EqualTo(1791736227));
            Assert.That(info.AdjustedTimestamp, Is.EqualTo(1791736362));
            Assert.That(info.Target, Is.EqualTo(120));
            Assert.That(info.TargetHeight, Is.EqualTo(0));
            Assert.That(info.NumTxs, Is.EqualTo(1));
            Assert.That(info.NumTxsPool, Is.EqualTo(0));
            Assert.That(info.WasBootstrapEverUsed, Is.False);
            Assert.That(info.DatabaseSize, Is.EqualTo(376832));
            Assert.That(info.UpdateAvailable, Is.False);
            Assert.That(info.IsBusySyncing, Is.False);
            Assert.That(info.IsSynchronized, Is.True);
            Assert.That(info.IsRestricted, Is.False);
            Assert.That(info.IsRegtest, Is.True);
        });
    }

    [Test]
    public void DaemonSyncInfo_ReadsTheFixture()
    {
        var info = ReadFixture<MoneroDaemonSyncInfo>("sync_info");

        Assert.Multiple(() =>
        {
            Assert.That(info.Credits, Is.EqualTo(0));
            Assert.That(info.Height, Is.EqualTo(102));
            Assert.That(info.TargetHeight, Is.EqualTo(0));
            Assert.That(info.NextNeededPruningSeed, Is.EqualTo(1));
            Assert.That(info.Peers, Is.Null);
            Assert.That(info.Spans, Is.Null);
            Assert.That(info.Overview, Is.Null);
        });
    }

    [Test]
    public void HardForkInfo_ReadsTheFixture()
    {
        var info = ReadFixture<MoneroHardForkInfo>("hard_fork_info");

        Assert.Multiple(() =>
        {
            Assert.That(info.Credits, Is.EqualTo(0));
            Assert.That(info.EarliestHeight, Is.EqualTo(1));
            Assert.That(info.IsEnabled, Is.True);
            Assert.That(info.State, Is.EqualTo(0));
            Assert.That(info.Threshold, Is.EqualTo(0));
            Assert.That(info.Version, Is.EqualTo(16));
            Assert.That(info.NumVotes, Is.EqualTo(101));
            Assert.That(info.Window, Is.EqualTo(102));
            Assert.That(info.Voting, Is.EqualTo(16));
        });
    }

    [Test]
    public void FeeEstimate_ReadsTheFixture()
    {
        var estimate = ReadFixture<MoneroFeeEstimate>("fee_estimate");

        Assert.Multiple(() =>
        {
            Assert.That(estimate.Fee, Is.EqualTo(1200000));
            Assert.That(estimate.QuantizationMask, Is.EqualTo(10000));
            Assert.That(estimate.Fees, Is.EqualTo(new ulong[] { 1200000, 4700000, 19000000, 240000000 }));
        });
    }

    [Test]
    public void MiningStatus_ReadsTheFixture()
    {
        var status = ReadFixture<MoneroMiningStatus>("mining_status");

        Assert.Multiple(() =>
        {
            Assert.That(status.IsActive, Is.False);
            Assert.That(status.Speed, Is.EqualTo(0));
            Assert.That(status.NumThreads, Is.EqualTo(0));
            Assert.That(status.IsBackground, Is.Null);
            Assert.That(status.Address, Is.Null);
        });
    }

    [Test]
    public void MinerTxSum_ReadsTheFixture()
    {
        var sum = ReadFixture<MoneroMinerTxSum>("miner_tx_sum");

        Assert.Multiple(() =>
        {
            Assert.That(sum.EmissionSumLow, Is.EqualTo(351840365463352UL));
            Assert.That(sum.EmissionSumHigh, Is.EqualTo(0));
            Assert.That(sum.EmissionSum, Is.EqualTo((UInt128)351840365463352UL));
            Assert.That(sum.FeeSumLow, Is.EqualTo(0));
            Assert.That(sum.FeeSumHigh, Is.EqualTo(0));
            Assert.That(sum.FeeSum, Is.EqualTo((UInt128)0));
        });
    }

    [Test]
    public void PruneResult_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroPruneResult>("prune_result");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsPruned, Is.False);
            Assert.That(result.PruningSeed, Is.EqualTo(0));
        });
    }

    [Test]
    public void SubmitTxResult_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroSubmitTxResult>("submit_tx_result");

        Assert.Multiple(() =>
        {
            Assert.That(result.Credits, Is.EqualTo(0));
            Assert.That(result.IsGood, Is.True);
            Assert.That(result.IsRelayed, Is.True);
            Assert.That(result.IsDoubleSpend, Is.False);
            Assert.That(result.IsFeeTooLow, Is.False);
            Assert.That(result.IsMixinTooLow, Is.False);
            Assert.That(result.HasInvalidInput, Is.False);
            Assert.That(result.HasInvalidOutput, Is.False);
            Assert.That(result.HasTooFewOutputs, Is.False);
            Assert.That(result.IsOverspend, Is.False);
            Assert.That(result.IsTooBig, Is.False);
            Assert.That(result.SanityCheckFailed, Is.False);
            Assert.That(result.IsTxExtraTooBig, Is.False);
            Assert.That(result.IsNonZeroUnlockTime, Is.False);
            Assert.That(result.Reason, Is.Null);
        });
    }

    [Test]
    public void GenerateBlocksResult_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroGenerateBlocksResult>("generate_blocks_result");

        Assert.Multiple(() =>
        {
            Assert.That(result.Height, Is.EqualTo(101));
            Assert.That(result.BlockHashes, Is.EqualTo(new[] { TopHash }));
        });
    }

    [Test]
    public void Version_ReadsTheFixture()
    {
        var version = ReadFixture<MoneroVersion>("version");

        Assert.Multiple(() =>
        {
            Assert.That(version.Number, Is.EqualTo(196624));
            Assert.That(version.IsRelease, Is.True);
            // the number is the major version in the high 16 bits and the minor in the low ones
            Assert.That(version.Number >> 16, Is.EqualTo(3));
            Assert.That(version.Number & 0xFFFF, Is.EqualTo(16));
        });
    }

    [Test]
    public void DaemonNetworkStats_ReadsTheFixture()
    {
        var stats = ReadFixture<MoneroDaemonNetworkStats>("network_stats");

        Assert.Multiple(() =>
        {
            Assert.That(stats.StartTime, Is.EqualTo(1791736227));
            Assert.That(stats.TotalPacketsIn, Is.EqualTo(0));
            Assert.That(stats.TotalBytesIn, Is.EqualTo(0));
            Assert.That(stats.TotalPacketsOut, Is.EqualTo(0));
            Assert.That(stats.TotalBytesOut, Is.EqualTo(0));
            Assert.That(stats.Credits, Is.Null);
        });
    }

    [Test]
    public void TxPoolStats_ReadsTheFixture()
    {
        var stats = ReadFixture<MoneroTxPoolStats>("tx_pool_stats");

        Assert.Multiple(() =>
        {
            Assert.That(stats.NumTxs, Is.EqualTo(1));
            Assert.That(stats.NumNotRelayed, Is.EqualTo(0));
            Assert.That(stats.NumFailing, Is.EqualTo(0));
            Assert.That(stats.NumDoubleSpends, Is.EqualTo(0));
            Assert.That(stats.Num10M, Is.EqualTo(0));
            Assert.That(stats.FeeTotal, Is.EqualTo(2599200000));
            Assert.That(stats.BytesMax, Is.EqualTo(2166));
            Assert.That(stats.BytesMed, Is.EqualTo(2166));
            Assert.That(stats.BytesMin, Is.EqualTo(2166));
            Assert.That(stats.BytesTotal, Is.EqualTo(2166));
            Assert.That(stats.OldestTimestamp, Is.EqualTo(1791736242));
            Assert.That(stats.Histo, Is.Not.Null.And.Empty);
            Assert.That(stats.Histo98Pc, Is.Null);
        });
    }

    [Test]
    public void OutputHistogram_ReadsTheFixture()
    {
        var entries = ReadFixture<List<MoneroOutputHistogramEntry>>("output_histogram");

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(entries[0].Amount, Is.EqualTo(0));
            Assert.That(entries[0].NumInstances, Is.EqualTo(103));
            Assert.That(entries[0].UnlockedInstances, Is.EqualTo(0));
            Assert.That(entries[0].RecentInstances, Is.EqualTo(0));
            Assert.That(entries[1].Amount, Is.EqualTo(17592186044415UL));
            Assert.That(entries[1].NumInstances, Is.EqualTo(1));
            Assert.That(entries[1].UnlockedInstances, Is.EqualTo(0));
            Assert.That(entries[1].RecentInstances, Is.EqualTo(0));
        });
    }

    [Test]
    public void OutputDistribution_ReadsTheFixture()
    {
        var entries = ReadFixture<List<MoneroOutputDistributionEntry>>("output_distribution");

        Assert.That(entries, Has.Count.EqualTo(1));
        var entry = entries[0];
        Assert.Multiple(() =>
        {
            Assert.That(entry.Amount, Is.EqualTo(0));
            Assert.That(entry.Base, Is.EqualTo(0));
            Assert.That(entry.StartHeight, Is.EqualTo(0));
            Assert.That(entry.Distribution, Has.Count.EqualTo(102));
            Assert.That(entry.Distribution!.Take(3), Is.EqualTo(new ulong[] { 0, 1, 1 }));
            Assert.That(entry.Distribution!.TakeLast(2), Is.EqualTo(new ulong[] { 1, 3 }));
        });
    }

    [Test]
    public void EmptyLists_AreReadAsEmptyLists()
    {
        Assert.That(ReadFixture<List<MoneroAltChain>>("alt_chains_empty"), Is.Not.Null.And.Empty);
        Assert.That(ReadFixture<List<MoneroPeer>>("peers_empty"), Is.Not.Null.And.Empty);
    }

    [Test]
    public void BlockHeader_ReadsTheFixture()
    {
        var header = ReadFixture<MoneroBlockHeader>("last_block_header");

        Assert.Multiple(() =>
        {
            Assert.That(header.Height, Is.EqualTo(101));
            Assert.That(header.Timestamp, Is.EqualTo(1791736242));
            Assert.That(header.Size, Is.EqualTo(2252));
            Assert.That(header.Weight, Is.EqualTo(2252));
            Assert.That(header.LongTermWeight, Is.EqualTo(176470));
            Assert.That(header.Depth, Is.EqualTo(0));
            Assert.That(header.DifficultyLow, Is.EqualTo(1));
            Assert.That(header.DifficultyHigh, Is.EqualTo(0));
            Assert.That(header.Difficulty, Is.EqualTo((UInt128)1));
            Assert.That(header.CumulativeDifficultyLow, Is.EqualTo(102));
            Assert.That(header.CumulativeDifficultyHigh, Is.EqualTo(0));
            Assert.That(header.CumulativeDifficulty, Is.EqualTo((UInt128)102));
            Assert.That(header.MajorVersion, Is.EqualTo(16));
            Assert.That(header.MinorVersion, Is.EqualTo(16));
            Assert.That(header.Nonce, Is.EqualTo(0));
            Assert.That(header.MinerTxHash, Is.EqualTo(MinerTxHash));
            Assert.That(header.NumTxs, Is.EqualTo(1));
            Assert.That(header.Reward, Is.EqualTo(35180227487959UL));
            Assert.That(header.Hash, Is.EqualTo(TopHash));
            Assert.That(header.PrevHash, Is.EqualTo(PrevHash));
            Assert.That(header.OrphanStatus, Is.False);
            Assert.That(header.PowHash, Is.Null);
        });
    }

    [Test]
    public void BlockHeaders_ReadTheFixture()
    {
        var headers = ReadFixture<List<MoneroBlockHeader>>("block_headers");

        Assert.That(headers.Select(header => header.Height), Is.EqualTo(new ulong?[] { 3, 4, 5, 6 }));
        Assert.That(headers.Select(header => header.Depth), Is.EqualTo(new ulong?[] { 98, 97, 96, 95 }));
        Assert.That(headers[2].Hash, Is.EqualTo("3cbd831176e400ad42332927bf72a6f130600b791742b6545c8b7671aa73c615"));
        Assert.That(headers, Is.All.Matches<MoneroBlockHeader>(header => header.PrevHash != null && header.Hash != null));
    }

    [Test]
    public void BlockTemplate_ReadsTheFixture()
    {
        var template = ReadFixture<MoneroBlockTemplate>("block_template");

        Assert.Multiple(() =>
        {
            Assert.That(template.BlockTemplateBlob, Does.StartWith("1010b2f3aed606d52697790f85fc3bd5dcdb1334a8a9ea06d07c0d89dee26bc8bace65a01491f2"));
            Assert.That(template.BlockHashingBlob, Does.StartWith("1010b2f3aed606d52697790f85fc3bd5dcdb1334a8a9ea06d07c0d89dee26bc8bace65a01491f2"));
            Assert.That(template.PrevHash, Is.EqualTo(PrevHash));
            Assert.That(template.SeedHash, Is.EqualTo(SeedHash));
            // an empty string is a value, it is not the same as a field that is not set
            Assert.That(template.NextSeedHash, Is.EqualTo(""));
            Assert.That(template.DifficultyLow, Is.EqualTo(1));
            Assert.That(template.DifficultyHigh, Is.EqualTo(0));
            Assert.That(template.Difficulty, Is.EqualTo((UInt128)1));
            Assert.That(template.ExpectedReward, Is.EqualTo(35180227487959UL));
            Assert.That(template.Height, Is.EqualTo(101));
            Assert.That(template.ReservedOffset, Is.EqualTo(0));
            Assert.That(template.SeedHeight, Is.EqualTo(0));
        });
    }

    [Test]
    public void MinerData_ReadsTheFixture()
    {
        var data = ReadFixture<MoneroMinerData>("miner_data");

        Assert.Multiple(() =>
        {
            Assert.That(data.MajorVersion, Is.EqualTo(16));
            Assert.That(data.Height, Is.EqualTo(101));
            Assert.That(data.PrevHash, Is.EqualTo(PrevHash));
            Assert.That(data.SeedHash, Is.EqualTo(SeedHash));
            // the difficulty of this one is a hexadecimal string, it is not split in two halves
            Assert.That(data.Difficulty, Is.EqualTo("0x1"));
            Assert.That(data.MedianWeight, Is.EqualTo(300000));
            Assert.That(data.AlreadyGeneratedCoins, Is.EqualTo(3535693871620981UL));
            Assert.That(data.TxPoolBacklog, Has.Count.EqualTo(1));
        });

        var entry = data.TxPoolBacklog![0];
        Assert.Multiple(() =>
        {
            Assert.That(entry.Hash, Is.EqualTo(TxHash));
            Assert.That(entry.Fee, Is.EqualTo(2599200000));
            Assert.That(entry.Weight, Is.EqualTo(2166));
            Assert.That(entry.NumConfirmations, Is.EqualTo(0));
            Assert.That(entry.IsMinerTx, Is.False);
            Assert.That(entry.Relay, Is.True);
            Assert.That(entry.IsRelayed, Is.True);
            Assert.That(entry.IsConfirmed, Is.False);
            Assert.That(entry.InTxPool, Is.True);
            Assert.That(entry.IsFailed, Is.False);
        });
    }

    [Test]
    public void Block_ReadsTheFixture()
    {
        var block = ReadFixture<MoneroBlock>("block");

        Assert.Multiple(() =>
        {
            Assert.That(block.Height, Is.EqualTo(5));
            Assert.That(block.Timestamp, Is.EqualTo(1791736235));
            Assert.That(block.Size, Is.EqualTo(85));
            Assert.That(block.Weight, Is.EqualTo(85));
            Assert.That(block.LongTermWeight, Is.EqualTo(176470));
            Assert.That(block.Depth, Is.EqualTo(96));
            Assert.That(block.Difficulty, Is.EqualTo((UInt128)1));
            Assert.That(block.CumulativeDifficulty, Is.EqualTo((UInt128)6));
            Assert.That(block.MajorVersion, Is.EqualTo(16));
            Assert.That(block.MinorVersion, Is.EqualTo(16));
            Assert.That(block.Nonce, Is.EqualTo(0));
            Assert.That(block.MinerTxHash, Is.EqualTo("3beb7fe6c6e94dc3f42597ac2dcf83a625e14032e959dda621f0a4ddc7065ea9"));
            Assert.That(block.NumTxs, Is.EqualTo(0));
            Assert.That(block.Reward, Is.EqualTo(35184070099967UL));
            Assert.That(block.Hash, Is.EqualTo("3cbd831176e400ad42332927bf72a6f130600b791742b6545c8b7671aa73c615"));
            Assert.That(block.PrevHash, Is.EqualTo("eb7821e9b270ead1a915a5297fd29b70dccfd9e8eb20fdef05cc396a583ece08"));
            Assert.That(block.OrphanStatus, Is.False);
            Assert.That(block.Hex, Has.Length.EqualTo(258));
            Assert.That(block.Txs, Is.Null);
            Assert.That(block.TxHashes, Is.Null);
        });

        var minerTx = block.MinerTx!;
        Assert.That(minerTx, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(minerTx.Block, Is.SameAs(block));
            Assert.That(minerTx.GetHeight(), Is.EqualTo(5));
            Assert.That(minerTx.Version, Is.EqualTo(2));
            Assert.That(minerTx.NumConfirmations, Is.EqualTo(97));
            Assert.That(minerTx.UnlockTime, Is.EqualTo(65));
            Assert.That(minerTx.IsMinerTx, Is.True);
            Assert.That(minerTx.Relay, Is.True);
            Assert.That(minerTx.IsRelayed, Is.True);
            Assert.That(minerTx.IsConfirmed, Is.True);
            Assert.That(minerTx.IsFailed, Is.False);
            Assert.That(minerTx.Inputs, Is.Null);
            Assert.That(minerTx.Outputs, Has.Count.EqualTo(1));
            Assert.That(minerTx.Outputs![0].Tx, Is.SameAs(minerTx));
            Assert.That(minerTx.Outputs![0].Amount, Is.EqualTo(35184070099967UL));
            Assert.That(minerTx.Outputs![0].StealthPublicKey, Is.EqualTo("ee22a65918fbfa145c754c3ec7b9e1b987da54f12cf629be23b1f4f34979a8f9"));
            Assert.That(minerTx.Extra, Has.Count.EqualTo(36));
            Assert.That(minerTx.Extra!.Take(4), Is.EqualTo(new byte[] { 1, 129, 175, 11 }));
        });
    }

    [Test]
    public void Block_WithTxHashes_ReadsTheFixture()
    {
        var block = ReadFixture<MoneroBlock>("block_with_tx");

        Assert.Multiple(() =>
        {
            Assert.That(block.Height, Is.EqualTo(101));
            Assert.That(block.Hash, Is.EqualTo(TopHash));
            Assert.That(block.NumTxs, Is.EqualTo(1));
            Assert.That(block.TxHashes, Is.EqualTo(new[] { TxHash }));
            Assert.That(block.Txs, Is.Null);
            Assert.That(block.Hex, Does.StartWith("1010b2f3aed606"));
            Assert.That(block.MinerTx, Is.Not.Null);
            Assert.That(block.MinerTx!.Block, Is.SameAs(block));
            Assert.That(block.MinerTx!.Extra, Has.Count.EqualTo(36));
            Assert.That(block.MinerTx!.UnlockTime, Is.EqualTo(161));
        });
    }

    [Test]
    public void Blocks_ReadTheFixture()
    {
        var blocks = ReadFixture<List<MoneroBlock>>("blocks");

        Assert.That(blocks.Select(block => block.Height), Is.EqualTo(new ulong?[] { 99, 100, 101 }));
        Assert.Multiple(() =>
        {
            Assert.That(blocks[0].Txs, Is.Null);
            Assert.That(blocks[1].Txs, Is.Null);
            Assert.That(blocks[2].Txs, Has.Count.EqualTo(1));
            Assert.That(blocks[2].TxHashes, Is.EqualTo(new[] { TxHash }));
            Assert.That(blocks[2].Txs![0].Hash, Is.EqualTo(TxHash));
            Assert.That(blocks[2].Txs![0].Block, Is.SameAs(blocks[2]));
            Assert.That(blocks[2].Txs![0].GetHeight(), Is.EqualTo(101));
            Assert.That(blocks, Is.All.Matches<MoneroBlock>(block => block.MinerTx != null && ReferenceEquals(block.MinerTx.Block, block)));
        });
    }

    [Test]
    public void BlocksByHashResult_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroGetBlocksByHashResult>("blocks_by_hash_result");

        Assert.That(result.CurrentHeight, Is.EqualTo(102));
        Assert.That(result.Blocks, Has.Count.EqualTo(10));
        Assert.That(result.Blocks!.Select(block => block.Height), Is.EqualTo(Enumerable.Range(1, 10).Select(height => (ulong?)height)));
        Assert.That(result.Blocks, Is.All.Matches<MoneroBlock>(block => block.MinerTx != null && ReferenceEquals(block.MinerTx.Block, block)));
    }

    [Test]
    public void TxInBlock_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroBlocksResult>("tx_in_block");

        Assert.That(result.Blocks, Has.Count.EqualTo(1));
        var block = result.Blocks![0];
        // the block of a transaction is the height and the time, and nothing else
        Assert.Multiple(() =>
        {
            Assert.That(block.Height, Is.EqualTo(101));
            Assert.That(block.Timestamp, Is.EqualTo(1791736242));
            Assert.That(block.Hash, Is.Null);
            Assert.That(block.MinerTx, Is.Null);
            Assert.That(block.Txs, Has.Count.EqualTo(1));
        });

        var tx = block.Txs![0];
        Assert.Multiple(() =>
        {
            Assert.That(tx.Block, Is.SameAs(block));
            Assert.That(tx.GetHeight(), Is.EqualTo(101));
            Assert.That(tx.Version, Is.EqualTo(2));
            Assert.That(tx.Fee, Is.EqualTo(2599200000));
            Assert.That(tx.NumConfirmations, Is.EqualTo(1));
            Assert.That(tx.UnlockTime, Is.EqualTo(0));
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.FullHex, Has.Length.EqualTo(4332));
            Assert.That(tx.PrunedHex, Is.Null);
            Assert.That(tx.PrunableHex, Is.Null);
            Assert.That(tx.PrunableHash, Is.EqualTo(PrunableHash));
            Assert.That(tx.IsMinerTx, Is.False);
            Assert.That(tx.Relay, Is.True);
            Assert.That(tx.IsRelayed, Is.True);
            Assert.That(tx.IsConfirmed, Is.True);
            Assert.That(tx.InTxPool, Is.False);
            Assert.That(tx.IsDoubleSpendSeen, Is.False);
            Assert.That(tx.IsFailed, Is.False);
            Assert.That(tx.ReceivedTimestamp, Is.Null);
            Assert.That(tx.OutputIndices, Is.EqualTo(new ulong[] { 101, 102 }));
            Assert.That(tx.Extra, Has.Count.EqualTo(44));
            Assert.That(tx.Extra!.Take(4), Is.EqualTo(new byte[] { 1, 158, 133, 96 }));
            Assert.That(tx.Inputs, Has.Count.EqualTo(2));
            Assert.That(tx.Outputs, Has.Count.EqualTo(2));
        });

        var input = tx.Inputs![0];
        Assert.Multiple(() =>
        {
            Assert.That(input.Tx, Is.SameAs(tx));
            Assert.That(input.Amount, Is.EqualTo(0));
            Assert.That(input.RingOutputIndices, Is.EqualTo(new ulong[] { 0, 10, 1, 2, 3, 2, 5, 4, 1, 1, 1, 1, 1, 2, 1, 2 }));
            Assert.That(input.KeyImage!.Hex, Is.EqualTo("ac416ea1b15a977da8c60cf74b312670461d08eb8c37ab1b5811fa4fe6dc57ac"));
            Assert.That(input.KeyImage!.Signature, Is.Null);
            Assert.That(input.Index, Is.Null);
            Assert.That(input.StealthPublicKey, Is.Null);
            Assert.That(tx.Inputs![1].KeyImage!.Hex, Is.EqualTo("aa2aab09bf9ab29c37cad262cb8b70334d52a2e8d9343ae9dbb01c93033c3e54"));
        });

        var output = tx.Outputs![0];
        Assert.Multiple(() =>
        {
            Assert.That(output.Tx, Is.SameAs(tx));
            Assert.That(output.Amount, Is.EqualTo(0));
            Assert.That(output.Index, Is.EqualTo(101));
            Assert.That(output.StealthPublicKey, Is.EqualTo("ebcac95a27a64ff82133b2f2fe973981a3b732d8d87fc87e32434b18ff37919d"));
            Assert.That(output.Mask, Is.EqualTo("4eb97883730fd746d9f1d336478e82880e23267308118b88e90a3920210bf620"));
            Assert.That(output.KeyImage, Is.Null);
            Assert.That(output.RingOutputIndices, Is.Null);
            Assert.That(tx.Outputs![1].Index, Is.EqualTo(102));
            Assert.That(tx.Outputs![1].Mask, Is.EqualTo("0c96d4c8bd6454752ce9e1c80dc39401dc7306090bd2912d4c05abe580aafe62"));
        });
    }

    [Test]
    public void TxInPool_ReadsTheFixture_WithABlockThatHasNoHeight()
    {
        var result = ReadFixture<MoneroBlocksResult>("tx_in_pool");

        Assert.That(result.Blocks, Has.Count.EqualTo(1));
        var block = result.Blocks![0];
        Assert.Multiple(() =>
        {
            Assert.That(block.Height, Is.Null);
            Assert.That(block.Timestamp, Is.Null);
            Assert.That(block.Txs, Has.Count.EqualTo(1));
        });

        var tx = block.Txs![0];
        Assert.Multiple(() =>
        {
            Assert.That(tx.Block, Is.SameAs(block));
            Assert.That(tx.GetHeight(), Is.Null);
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.InTxPool, Is.True);
            Assert.That(tx.IsConfirmed, Is.False);
            Assert.That(tx.NumConfirmations, Is.EqualTo(0));
            Assert.That(tx.ReceivedTimestamp, Is.EqualTo(1791736242));
            Assert.That(tx.IsRelayed, Is.True);
            Assert.That(tx.Relay, Is.Null);
            Assert.That(tx.OutputIndices, Is.Null);
            Assert.That(tx.Inputs, Has.Count.EqualTo(2));
            Assert.That(tx.Outputs, Has.Count.EqualTo(2));
            Assert.That(tx.Outputs![0].Index, Is.Null);
        });
    }

    [Test]
    public void TxPool_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroBlocksResult>("tx_pool");

        Assert.That(result.Blocks, Has.Count.EqualTo(1));
        Assert.That(result.Blocks![0].Height, Is.Null);
        Assert.That(result.GetTxs().Select(tx => tx.Hash), Is.EqualTo(new[] { TxHash }));
    }

    [Test]
    public void PrunedTx_ReadsTheFixture()
    {
        var result = ReadFixture<MoneroBlocksResult>("tx_pruned_in_block");

        var tx = result.GetTxs().Single();
        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.PrunedHex, Has.Length.EqualTo(614));
            Assert.That(tx.FullHex, Is.Null);
            Assert.That(tx.PrunableHex, Is.Null);
            Assert.That(tx.PrunableHash, Is.EqualTo(PrunableHash));
            Assert.That(tx.GetHeight(), Is.EqualTo(101));
        });
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // what a regtest node can not produce
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Peer_IsReadAndWritten()
    {
        const string json = """
                            {
                              "id": "1a2b3c4d5e6f7a8b",
                              "address": "203.0.113.7:18080",
                              "host": "203.0.113.7",
                              "port": 18080,
                              "isOnline": true,
                              "lastSeenTimestamp": 1791736000,
                              "pruningSeed": 386,
                              "rpcPort": 18081,
                              "rpcCreditsPerHash": 250,
                              "hash": "9d0d2d0c5b2f2f8d9a4e1b7c6e3f0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b",
                              "avgDownload": 1500,
                              "avgUpload": 800,
                              "currentDownload": 10,
                              "currentUpload": 20,
                              "height": 3200000,
                              "isIncoming": false,
                              "liveTime": 600,
                              "isLocalIp": false,
                              "isLocalHost": false,
                              "numReceives": 42,
                              "numSends": 43,
                              "receiveIdleTime": 5,
                              "sendIdleTime": 6,
                              "state": "normal",
                              "numSupportFlags": 1,
                              "addressType": 3
                            }
                            """;

        var peer = Read<MoneroPeer>(json);

        Assert.Multiple(() =>
        {
            Assert.That(peer.Id, Is.EqualTo("1a2b3c4d5e6f7a8b"));
            Assert.That(peer.Address, Is.EqualTo("203.0.113.7:18080"));
            Assert.That(peer.Host, Is.EqualTo("203.0.113.7"));
            Assert.That(peer.Port, Is.EqualTo(18080));
            Assert.That(peer.IsOnline, Is.True);
            Assert.That(peer.LastSeenTimestamp, Is.EqualTo(1791736000));
            Assert.That(peer.PruningSeed, Is.EqualTo(386));
            Assert.That(peer.RpcPort, Is.EqualTo(18081));
            Assert.That(peer.RpcCreditsPerHash, Is.EqualTo(250));
            Assert.That(peer.Hash, Does.StartWith("9d0d2d0c"));
            Assert.That(peer.AvgDownload, Is.EqualTo(1500));
            Assert.That(peer.AvgUpload, Is.EqualTo(800));
            Assert.That(peer.CurrentDownload, Is.EqualTo(10));
            Assert.That(peer.CurrentUpload, Is.EqualTo(20));
            Assert.That(peer.Height, Is.EqualTo(3200000));
            Assert.That(peer.IsIncoming, Is.False);
            Assert.That(peer.LiveTime, Is.EqualTo(600));
            Assert.That(peer.IsLocalIp, Is.False);
            Assert.That(peer.IsLocalHost, Is.False);
            Assert.That(peer.NumReceives, Is.EqualTo(42));
            Assert.That(peer.NumSends, Is.EqualTo(43));
            Assert.That(peer.ReceiveIdleTime, Is.EqualTo(5));
            Assert.That(peer.SendIdleTime, Is.EqualTo(6));
            Assert.That(peer.State, Is.EqualTo("normal"));
            Assert.That(peer.NumSupportFlags, Is.EqualTo(1));
            Assert.That(peer.ConnectionType, Is.EqualTo(MoneroConnectionType.Tor));
        });

        AssertSameJson(json, Write(peer));
    }

    [TestCase(0, MoneroConnectionType.Invalid)]
    [TestCase(1, MoneroConnectionType.Ipv4)]
    [TestCase(2, MoneroConnectionType.Ipv6)]
    [TestCase(3, MoneroConnectionType.Tor)]
    [TestCase(4, MoneroConnectionType.I2P)]
    public void Peer_ConnectionType_IsTheNumberOfAddressType(int number, MoneroConnectionType type)
    {
        var peer = Read<MoneroPeer>($$"""{"addressType":{{number}}}""");

        Assert.That(peer.ConnectionType, Is.EqualTo(type));
        Assert.That(Write(peer), Is.EqualTo($$"""{"addressType":{{number}}}"""));
    }

    [Test]
    public void Peer_WithoutAddressType_HasNoConnectionType()
    {
        Assert.That(Read<MoneroPeer>("""{"host":"203.0.113.7"}""").ConnectionType, Is.Null);
    }

    [Test]
    public void SyncInfo_WithPeersAndSpans_IsReadAndWritten()
    {
        const string json = """
                            {
                              "credits": 0,
                              "topBlockHash": "debce41d53f56ad4db3348a73cf52870f7ce18e64b6e961f164282516a803209",
                              "height": 3200000,
                              "targetHeight": 3200100,
                              "nextNeededPruningSeed": 386,
                              "overview": "overview of the sync",
                              "peers": [
                                { "id": "1a2b3c4d5e6f7a8b", "host": "203.0.113.7", "port": 18080, "addressType": 1 },
                                { "id": "2b3c4d5e6f7a8b9c", "host": "2001:db8::1", "port": 18080, "addressType": 2 }
                              ],
                              "spans": [
                                {
                                  "connectionId": "c0ffee",
                                  "numBlocks": 20,
                                  "remoteAddress": "203.0.113.7:18080",
                                  "rate": 1000,
                                  "speed": 900,
                                  "size": 123456,
                                  "startHeight": 3200001
                                }
                              ]
                            }
                            """;

        var info = Read<MoneroDaemonSyncInfo>(json);

        Assert.Multiple(() =>
        {
            Assert.That(info.TopBlockHash, Is.EqualTo(TopHash));
            Assert.That(info.Height, Is.EqualTo(3200000));
            Assert.That(info.TargetHeight, Is.EqualTo(3200100));
            Assert.That(info.NextNeededPruningSeed, Is.EqualTo(386));
            Assert.That(info.Overview, Is.EqualTo("overview of the sync"));
            Assert.That(info.Peers!.Select(peer => peer.ConnectionType), Is.EqualTo(new MoneroConnectionType?[] { MoneroConnectionType.Ipv4, MoneroConnectionType.Ipv6 }));
            Assert.That(info.Peers![1].Host, Is.EqualTo("2001:db8::1"));
            Assert.That(info.Spans, Has.Count.EqualTo(1));
            Assert.That(info.Spans![0].ConnectionId, Is.EqualTo("c0ffee"));
            Assert.That(info.Spans![0].NumBlocks, Is.EqualTo(20));
            Assert.That(info.Spans![0].RemoteAddress, Is.EqualTo("203.0.113.7:18080"));
            Assert.That(info.Spans![0].Rate, Is.EqualTo(1000));
            Assert.That(info.Spans![0].Speed, Is.EqualTo(900));
            Assert.That(info.Spans![0].Size, Is.EqualTo(123456));
            Assert.That(info.Spans![0].StartHeight, Is.EqualTo(3200001));
        });

        AssertSameJson(json, Write(info));
    }

    [Test]
    public void Ban_IsReadAndWritten()
    {
        const string json = """{"host":"203.0.113.7","ip":3405803783,"isBanned":true,"seconds":3600}""";

        var ban = Read<MoneroBan>(json);

        Assert.Multiple(() =>
        {
            Assert.That(ban.Host, Is.EqualTo("203.0.113.7"));
            Assert.That(ban.Ip, Is.EqualTo(3405803783));
            Assert.That(ban.IsBanned, Is.True);
            Assert.That(ban.Seconds, Is.EqualTo(3600));
        });

        AssertSameJson(json, Write(ban));
    }

    [Test]
    public void Ban_IpIsAnUnsigned32BitNumber()
    {
        var ban = Read<MoneroBan>($$"""{"ip":{{uint.MaxValue}}}""");

        Assert.That(ban.Ip, Is.EqualTo(uint.MaxValue));
        Assert.That(Write(ban), Is.EqualTo($$"""{"ip":{{uint.MaxValue}}}"""));
        Assert.Throws<JsonException>(() => Read<MoneroBan>($$"""{"ip":{{uint.MaxValue + 1UL}}}"""));
    }

    [Test]
    public void Bans_AreReadAndWrittenAsAList()
    {
        const string json = """[{"host":"203.0.113.7","isBanned":true,"seconds":10},{"ip":16909060,"seconds":0}]""";

        var bans = Read<List<MoneroBan>>(json);

        Assert.That(bans, Has.Count.EqualTo(2));
        Assert.That(bans[1].Ip, Is.EqualTo(16909060));
        Assert.That(bans[1].Seconds, Is.EqualTo(0));
        AssertSameJson(json, Write(bans));
    }

    [Test]
    public void AltChain_IsReadAndWritten()
    {
        const string json = """
                            {
                              "blockHashes": [
                                "9d0d2d0c5b2f2f8d9a4e1b7c6e3f0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b",
                                "eb7821e9b270ead1a915a5297fd29b70dccfd9e8eb20fdef05cc396a583ece08"
                              ],
                              "difficultyLow": 18446744073709551615,
                              "difficultyHigh": 2,
                              "length": 2,
                              "height": 3200000,
                              "mainChainParentBlockHash": "d52697790f85fc3bd5dcdb1334a8a9ea06d07c0d89dee26bc8bace65a01491f2"
                            }
                            """;

        var chain = Read<MoneroAltChain>(json);

        Assert.Multiple(() =>
        {
            Assert.That(chain.BlockHashes, Has.Count.EqualTo(2));
            Assert.That(chain.DifficultyLow, Is.EqualTo(ulong.MaxValue));
            Assert.That(chain.DifficultyHigh, Is.EqualTo(2));
            Assert.That(chain.Difficulty, Is.EqualTo(new UInt128(2, ulong.MaxValue)));
            Assert.That(chain.Length, Is.EqualTo(2));
            Assert.That(chain.Height, Is.EqualTo(3200000));
            Assert.That(chain.MainChainParentBlockHash, Is.EqualTo(PrevHash));
        });

        AssertSameJson(json, Write(chain));
    }

    [Test]
    public void UpdateCheckResult_IsReadAndWritten()
    {
        const string json = """
                            {
                              "isUpdateAvailable": true,
                              "version": "0.18.5.0",
                              "hash": "9d0d2d0c5b2f2f8d9a4e1b7c6e3f0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b",
                              "autoUri": "https://downloads.getmonero.org/cli/linux64",
                              "userUri": "https://www.getmonero.org/downloads/"
                            }
                            """;

        var result = Read<MoneroDaemonUpdateCheckResult>(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsUpdateAvailable, Is.True);
            Assert.That(result.Version, Is.EqualTo("0.18.5.0"));
            Assert.That(result.Hash, Does.StartWith("9d0d2d0c"));
            Assert.That(result.AutoUri, Is.EqualTo("https://downloads.getmonero.org/cli/linux64"));
            Assert.That(result.UserUri, Is.EqualTo("https://www.getmonero.org/downloads/"));
        });

        AssertSameJson(json, Write(result));
    }

    [Test]
    public void UpdateDownloadResult_IsAnUpdateCheckResult_WithThePathOfTheDownload()
    {
        const string json = """{"isUpdateAvailable":true,"version":"0.18.5.0","downloadPath":"/tmp/monero-linux-x64-v0.18.5.0.tar.bz2"}""";

        var result = Read<MoneroDaemonUpdateDownloadResult>(json);

        Assert.That(result, Is.InstanceOf<MoneroDaemonUpdateCheckResult>());
        Assert.That(result.IsUpdateAvailable, Is.True);
        Assert.That(result.Version, Is.EqualTo("0.18.5.0"));
        Assert.That(result.DownloadPath, Is.EqualTo("/tmp/monero-linux-x64-v0.18.5.0.tar.bz2"));
        AssertSameJson(json, Write(result));
    }

    [Test]
    public void AddAuxiliaryPowResult_IsReadAndWritten()
    {
        const string json = """
                            {
                              "credits": 0,
                              "topBlockHash": "debce41d53f56ad4db3348a73cf52870f7ce18e64b6e961f164282516a803209",
                              "blockTemplateBlob": "1010b2f3aed606",
                              "blockHashingBlob": "1010b2f3aed607",
                              "merkleRoot": "9d0d2d0c5b2f2f8d9a4e1b7c6e3f0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b",
                              "merkleTreeDepth": 2,
                              "auxPow": [
                                { "id": "aa", "hash": "bb" },
                                { "id": "cc", "hash": "dd" }
                              ]
                            }
                            """;

        var result = Read<MoneroAddAuxiliaryPowResult>(json);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<MoneroRpcPaymentInfo>());
            Assert.That(result.Credits, Is.EqualTo(0));
            Assert.That(result.TopBlockHash, Is.EqualTo(TopHash));
            Assert.That(result.BlockTemplateBlob, Is.EqualTo("1010b2f3aed606"));
            Assert.That(result.BlockHashingBlob, Is.EqualTo("1010b2f3aed607"));
            Assert.That(result.MerkleRoot, Does.StartWith("9d0d2d0c"));
            Assert.That(result.MerkleTreeDepth, Is.EqualTo(2));
            Assert.That(result.AuxPow!.Select(aux => (aux.Id, aux.Hash)), Is.EqualTo(new[] { ("aa", "bb"), ("cc", "dd") }));
        });

        AssertSameJson(json, Write(result));
    }

    [Test]
    public void TxBacklogEntry_IsAnEmptyObject()
    {
        var entry = Read<MoneroTxBacklogEntry>("{}");
        Assert.That(entry, Is.Not.Null);
        Assert.That(Write(entry), Is.EqualTo("{}"));

        var entries = Read<List<MoneroTxBacklogEntry>>("[{},{},{}]");
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(Write(entries), Is.EqualTo("[{},{},{}]"));

        // a field that it does not have is ignored
        Assert.That(Write(Read<MoneroTxBacklogEntry>("""{"fee":1}""")), Is.EqualTo("{}"));
    }

    [Test]
    public void KeyImage_IsReadAndWritten()
    {
        const string json = """{"hex":"ac416ea1b15a977da8c60cf74b312670461d08eb8c37ab1b5811fa4fe6dc57ac","signature":"0102"}""";

        var keyImage = Read<MoneroKeyImage>(json);

        Assert.That(keyImage.Hex, Does.StartWith("ac416ea1"));
        Assert.That(keyImage.Signature, Is.EqualTo("0102"));
        AssertSameJson(json, Write(keyImage));
    }

    [Test]
    public void KeyImageSpentStatuses_AreNumbers()
    {
        var statuses = Read<List<MoneroKeyImageSpentStatus>>("[0,1,2,1]");

        Assert.That(statuses, Is.EqualTo(new[]
        {
            MoneroKeyImageSpentStatus.NotSpent, MoneroKeyImageSpentStatus.Confirmed, MoneroKeyImageSpentStatus.TxPool, MoneroKeyImageSpentStatus.Confirmed
        }));
        Assert.That(Write(statuses), Is.EqualTo("[0,1,2,1]"));
        Assert.That(JsonSerializer.Serialize(statuses, MoneroDaemonJsonContext.Default.ListMoneroKeyImageSpentStatus), Is.EqualTo("[0,1,2,1]"));
    }

    [Test]
    public void TxPoolStats_WithTransactions_HasTheHistogram()
    {
        const string json = """
                            {
                              "numTxs": 4,
                              "numNotRelayed": 1,
                              "numFailing": 2,
                              "numDoubleSpends": 3,
                              "num10m": 4,
                              "feeTotal": 18446744073709551615,
                              "bytesMax": 9000,
                              "bytesMed": 3000,
                              "bytesMin": 1500,
                              "bytesTotal": 15000,
                              "histo": { "0": 1, "300": 2, "18446744073709551615": 3 },
                              "histo98pc": 4000,
                              "oldestTimestamp": 1791736242
                            }
                            """;

        var stats = Read<MoneroTxPoolStats>(json);

        Assert.Multiple(() =>
        {
            Assert.That(stats.NumTxs, Is.EqualTo(4));
            Assert.That(stats.NumNotRelayed, Is.EqualTo(1));
            Assert.That(stats.NumFailing, Is.EqualTo(2));
            Assert.That(stats.NumDoubleSpends, Is.EqualTo(3));
            Assert.That(stats.Num10M, Is.EqualTo(4));
            Assert.That(stats.FeeTotal, Is.EqualTo(ulong.MaxValue));
            Assert.That(stats.BytesMax, Is.EqualTo(9000));
            Assert.That(stats.BytesMed, Is.EqualTo(3000));
            Assert.That(stats.BytesMin, Is.EqualTo(1500));
            Assert.That(stats.BytesTotal, Is.EqualTo(15000));
            Assert.That(stats.Histo, Is.EqualTo(new Dictionary<ulong, ulong> { [0] = 1, [300] = 2, [ulong.MaxValue] = 3 }));
            Assert.That(stats.Histo98Pc, Is.EqualTo(4000));
            Assert.That(stats.OldestTimestamp, Is.EqualTo(1791736242));
        });

        AssertSameJson(json, Write(stats));
    }

    [Test]
    public void DaemonInfo_WithEveryOptionalField_IsReadAndWritten()
    {
        const string json = """
                            {
                              "version": "0.18.5.1-release",
                              "bootstrapDaemonAddress": "node.example.org:18081",
                              "networkType": 2,
                              "isOffline": false,
                              "isRestricted": true,
                              "isRegtest": false,
                              "wasBootstrapEverUsed": true,
                              "difficultyLow": 0,
                              "difficultyHigh": 1,
                              "cumulativeDifficultyLow": 18446744073709551615,
                              "cumulativeDifficultyHigh": 18446744073709551615
                            }
                            """;

        var info = Read<MoneroDaemonInfo>(json);

        Assert.Multiple(() =>
        {
            Assert.That(info.BootstrapDaemonAddress, Is.EqualTo("node.example.org:18081"));
            Assert.That(info.NetworkType, Is.EqualTo(MoneroNetworkType.StageNet));
            Assert.That(info.IsRestricted, Is.True);
            Assert.That(info.IsRegtest, Is.False);
            Assert.That(info.WasBootstrapEverUsed, Is.True);
            Assert.That(info.Difficulty, Is.EqualTo(UInt128.One << 64));
            Assert.That(info.CumulativeDifficulty, Is.EqualTo(UInt128.MaxValue));
        });

        AssertSameJson(json, Write(info));
    }

    [TestCase(0, MoneroNetworkType.MainNet)]
    [TestCase(1, MoneroNetworkType.TestNet)]
    [TestCase(2, MoneroNetworkType.StageNet)]
    public void DaemonInfo_NetworkType_IsTheNumber(int number, MoneroNetworkType type)
    {
        var info = Read<MoneroDaemonInfo>($$"""{"networkType":{{number}}}""");

        Assert.That(info.NetworkType, Is.EqualTo(type));
        Assert.That(Write(info), Is.EqualTo($$"""{"networkType":{{number}}}"""));
    }

    [Test]
    public void MiningStatus_WhenMining_IsReadAndWritten()
    {
        const string json = """{"isActive":true,"isBackground":true,"address":"44AFFq5kSiGBoZ","speed":1500,"numThreads":4}""";

        var status = Read<MoneroMiningStatus>(json);

        Assert.That(status.IsActive, Is.True);
        Assert.That(status.IsBackground, Is.True);
        Assert.That(status.Address, Is.EqualTo("44AFFq5kSiGBoZ"));
        Assert.That(status.Speed, Is.EqualTo(1500));
        Assert.That(status.NumThreads, Is.EqualTo(4));
        AssertSameJson(json, Write(status));
    }

    [Test]
    public void SubmitTxResult_WhenRejected_HasTheReason()
    {
        const string json = """{"isGood":false,"isRelayed":false,"isDoubleSpend":true,"isFeeTooLow":true,"isMixinTooLow":true,"isOverspend":true,"isTooBig":true,"reason":"double spend"}""";

        var result = Read<MoneroSubmitTxResult>(json);

        Assert.That(result.IsGood, Is.False);
        Assert.That(result.IsDoubleSpend, Is.True);
        Assert.That(result.Reason, Is.EqualTo("double spend"));
        AssertSameJson(json, Write(result));
    }

    [Test]
    public void BlockHeader_WithTheProofOfWorkHash_IsReadAndWritten()
    {
        var header = Read<MoneroBlockHeader>("""{"height":1,"powHash":"00ff"}""");

        Assert.That(header.PowHash, Is.EqualTo("00ff"));
        Assert.That(Write(header), Is.EqualTo("""{"height":1,"powHash":"00ff"}"""));
    }

    [Test]
    public void Tx_WithEveryField_IsReadAndWritten()
    {
        const string json = """
                            {
                              "hash": "2937db7948bd94f536866c9c02405b147b4dd2bf9d838ddfb8f33963c4926d4b",
                              "version": 2,
                              "isMinerTx": false,
                              "paymentId": "0000000000000000",
                              "fee": 2599200000,
                              "ringSize": 16,
                              "relay": true,
                              "isRelayed": true,
                              "isConfirmed": true,
                              "inTxPool": false,
                              "isLocked": true,
                              "numConfirmations": 7,
                              "unlockTime": 161,
                              "lastRelayedTimestamp": 1791736243,
                              "receivedTimestamp": 1791736242,
                              "isDoubleSpendSeen": false,
                              "key": "0a0b0c",
                              "fullHex": "02000102",
                              "prunedHex": "0200",
                              "prunableHex": "0102",
                              "prunableHash": "f7a26b7989caf255d7b686c0e0b42897bba4dd056c4cdd6054d7af3ecb9f4db7",
                              "size": 2166,
                              "weight": 2166,
                              "inputs": [
                                {
                                  "amount": 0,
                                  "ringOutputIndices": [1, 2, 3],
                                  "keyImage": { "hex": "ac416e" }
                                }
                              ],
                              "outputs": [
                                { "amount": 0, "index": 101, "stealthPublicKey": "ebcac9", "mask": "4eb978" }
                              ],
                              "outputIndices": [101],
                              "metadata": "6d657461",
                              "commonTxSets": "cafe",
                              "extra": [1, 255, 0, 128],
                              "rctSignatures": "rct",
                              "rctSigPrunable": "rctprunable",
                              "isKeptByBlock": false,
                              "isFailed": true,
                              "lastFailedHeight": 99,
                              "lastFailedHash": "aabb",
                              "maxUsedBlockHeight": 100,
                              "maxUsedBlockHash": "ccdd",
                              "signatures": ["s1", "s2"]
                            }
                            """;

        var tx = Read<MoneroTx>(json);

        Assert.Multiple(() =>
        {
            Assert.That(tx.Hash, Is.EqualTo(TxHash));
            Assert.That(tx.Version, Is.EqualTo(2));
            Assert.That(tx.IsMinerTx, Is.False);
            Assert.That(tx.PaymentId, Is.EqualTo(MoneroTx.DefaultPaymentId));
            Assert.That(tx.Fee, Is.EqualTo(2599200000));
            Assert.That(tx.RingSize, Is.EqualTo(16));
            Assert.That(tx.Relay, Is.True);
            Assert.That(tx.IsRelayed, Is.True);
            Assert.That(tx.IsConfirmed, Is.True);
            Assert.That(tx.InTxPool, Is.False);
            Assert.That(tx.IsLocked, Is.True);
            Assert.That(tx.NumConfirmations, Is.EqualTo(7));
            Assert.That(tx.UnlockTime, Is.EqualTo(161));
            Assert.That(tx.LastRelayedTimestamp, Is.EqualTo(1791736243));
            Assert.That(tx.ReceivedTimestamp, Is.EqualTo(1791736242));
            Assert.That(tx.IsDoubleSpendSeen, Is.False);
            Assert.That(tx.Key, Is.EqualTo("0a0b0c"));
            Assert.That(tx.FullHex, Is.EqualTo("02000102"));
            Assert.That(tx.PrunedHex, Is.EqualTo("0200"));
            Assert.That(tx.PrunableHex, Is.EqualTo("0102"));
            Assert.That(tx.PrunableHash, Is.EqualTo(PrunableHash));
            Assert.That(tx.Size, Is.EqualTo(2166));
            Assert.That(tx.Weight, Is.EqualTo(2166));
            Assert.That(tx.Inputs, Has.Count.EqualTo(1));
            Assert.That(tx.Inputs![0].RingOutputIndices, Is.EqualTo(new ulong[] { 1, 2, 3 }));
            Assert.That(tx.Inputs![0].KeyImage!.Hex, Is.EqualTo("ac416e"));
            Assert.That(tx.Outputs, Has.Count.EqualTo(1));
            Assert.That(tx.Outputs![0].Index, Is.EqualTo(101));
            Assert.That(tx.Outputs![0].Mask, Is.EqualTo("4eb978"));
            Assert.That(tx.OutputIndices, Is.EqualTo(new ulong[] { 101 }));
            Assert.That(tx.Metadata, Is.EqualTo("6d657461"));
            Assert.That(tx.CommonTxSets, Is.EqualTo("cafe"));
            Assert.That(tx.Extra, Is.EqualTo(new byte[] { 1, 255, 0, 128 }));
            Assert.That(tx.RctSignatures, Is.EqualTo("rct"));
            Assert.That(tx.RctSigPrunable, Is.EqualTo("rctprunable"));
            Assert.That(tx.IsKeptByBlock, Is.False);
            Assert.That(tx.IsFailed, Is.True);
            Assert.That(tx.LastFailedHeight, Is.EqualTo(99));
            Assert.That(tx.LastFailedHash, Is.EqualTo("aabb"));
            Assert.That(tx.MaxUsedBlockHeight, Is.EqualTo(100));
            Assert.That(tx.MaxUsedBlockHash, Is.EqualTo("ccdd"));
            Assert.That(tx.Signatures, Is.EqualTo(new[] { "s1", "s2" }));
            Assert.That(tx.Block, Is.Null);
        });

        AssertSameJson(json, Write(tx));
    }

    [Test]
    public void Output_WithEveryField_IsReadAndWritten()
    {
        const string json = """{"keyImage":{"hex":"ac","signature":"bd"},"amount":18446744073709551615,"index":7,"ringOutputIndices":[1,2],"stealthPublicKey":"ee","mask":"ff"}""";

        var output = Read<MoneroOutput>(json);

        Assert.Multiple(() =>
        {
            Assert.That(output.KeyImage!.Hex, Is.EqualTo("ac"));
            Assert.That(output.KeyImage!.Signature, Is.EqualTo("bd"));
            Assert.That(output.Amount, Is.EqualTo(ulong.MaxValue));
            Assert.That(output.Index, Is.EqualTo(7));
            Assert.That(output.RingOutputIndices, Is.EqualTo(new ulong[] { 1, 2 }));
            Assert.That(output.StealthPublicKey, Is.EqualTo("ee"));
            Assert.That(output.Mask, Is.EqualTo("ff"));
            Assert.That(output.Tx, Is.Null);
        });

        AssertSameJson(json, Write(output));
    }

    [Test]
    public void BlocksResult_GetTxs_ListsTheTxsInTheOrderOfTheBlocks()
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

        var result = Read<MoneroBlocksResult>(json);
        var txs = result.GetTxs();

        Assert.That(txs.Select(tx => tx.Hash), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(txs.Select(tx => tx.GetHeight()), Is.EqualTo(new ulong?[] { 10, 10, null }));
    }

    [Test]
    public void BlocksResult_GetTxs_IsEmpty_WithoutBlocks()
    {
        Assert.That(Read<MoneroBlocksResult>("{}").GetTxs(), Is.Empty);
        Assert.That(Read<MoneroBlocksResult>("""{"blocks":[]}""").GetTxs(), Is.Empty);
        Assert.That(new MoneroBlocksResult().GetTxs(), Is.Empty);
    }

    [Test]
    public void BlocksResult_GetTxs_ReturnsANewListEveryTime()
    {
        var result = ReadFixture<MoneroBlocksResult>("tx_in_block");

        var txs = result.GetTxs();
        txs.Clear();

        Assert.That(result.GetTxs(), Has.Count.EqualTo(1));
        Assert.That(result.Blocks![0].Txs, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetBlockHashesResult_IsReadAndWritten()
    {
        const string json = """{"startHeight":10,"currentHeight":103,"hashes":["aa","bb","cc"]}""";

        var result = Read<MoneroGetBlockHashesResult>(json);

        Assert.That(result.StartHeight, Is.EqualTo(10));
        Assert.That(result.CurrentHeight, Is.EqualTo(103));
        Assert.That(result.Hashes, Is.EqualTo(new[] { "aa", "bb", "cc" }));
        AssertSameJson(json, Write(result));
    }

    [Test]
    public void ConnectionSpan_IsReadAndWritten()
    {
        const string json = """{"connectionId":"c0ffee","numBlocks":20,"remoteAddress":"203.0.113.7:18080","rate":1,"speed":2,"size":3,"startHeight":4}""";

        var span = Read<MoneroConnectionSpan>(json);

        Assert.That(span.ConnectionId, Is.EqualTo("c0ffee"));
        Assert.That(span.NumBlocks, Is.EqualTo(20));
        Assert.That(span.StartHeight, Is.EqualTo(4));
        AssertSameJson(json, Write(span));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the rules that every model follows
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void ModelTypes_AreAllFound()
    {
        // a guard for the guards: if the search finds nothing, the tests over the models would pass without testing anything
        Assert.That(ModelTypes.Length, Is.GreaterThanOrEqualTo(30));
        Assert.That(ModelTypes, Does.Contain(typeof(MoneroTx)).And.Contain(typeof(MoneroBlock)).And.Contain(typeof(MoneroTxBacklogEntry)));
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
        Assert.That(JsonSerializer.Serialize(model, type, MoneroDaemonJsonContext.Default), Is.EqualTo("{}"));
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
        var model = Activator.CreateInstance(type)!;
        var json = WriteWith(engine, new Sampler().Populate(model), type);
        var withUnknown = json == "{}" ? "{" + unknown + "}" : json[..^1] + "," + unknown + "}";

        var read = ReadWith(engine, withUnknown, type);

        Assert.That(Diff(JsonNode.Parse(json), JsonNode.Parse(WriteWith(engine, read, type))), Is.Null);
    }

    [TestCaseSource(nameof(ModelEngineCases))]
    public void Model_WithEveryPropertySet_IsReadAndWritten(Type type, Engine engine)
    {
        var model = new Sampler().Populate(Activator.CreateInstance(type)!);
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
        Assert.That(MoneroDaemonJsonContext.Default.GetTypeInfo(type), Is.Not.Null, $"{type.Name} is not in MoneroDaemonJsonContext");
    }

    [Test]
    public void GeneratedCode_ReadsWithTheTypeInformationOfTheContext()
    {
        var info = JsonSerializer.Deserialize(Fixture("daemon_info"), MoneroDaemonJsonContext.Default.MoneroDaemonInfo)!;

        Assert.That(info.Height, Is.EqualTo(102));
        Assert.That(JsonSerializer.Serialize(info, MoneroDaemonJsonContext.Default.MoneroDaemonInfo), Is.EqualTo(Write(info)));

        var blocks = JsonSerializer.Deserialize(Fixture("blocks"), MoneroDaemonJsonContext.Default.ListMoneroBlock)!;
        Assert.That(blocks, Has.Count.EqualTo(3));
        Assert.That(blocks[2].Txs![0].Block, Is.SameAs(blocks[2]));
    }

    [Test]
    public void Models_AreReadAndWrittenWithoutCycles_AlthoughTheyPointToTheirParents()
    {
        var block = ReadFixture<MoneroBlock>("block_with_tx");
        block.Txs = [new MoneroTx { Hash = "a", Inputs = [new MoneroOutput { Amount = 1 }], Outputs = [new MoneroOutput { Amount = 2 }] }];

        Assert.That(block.Txs[0].Block, Is.SameAs(block));
        Assert.That(block.Txs[0].Inputs![0].Tx, Is.SameAs(block.Txs[0]));

        string json = "";
        Assert.DoesNotThrow(() => json = Write(block));
        Assert.That(json, Does.Not.Contain("\"block\"").And.Not.Contain("\"tx\""));
        Assert.That(JsonNode.Parse(json)!["txs"]![0]!["inputs"]![0]!.AsObject().Select(property => property.Key), Is.EqualTo(new[] { "amount" }));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // nulls, numbers, enumerations, text
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void FieldsThatAreNotSet_AreLeftOut_ButZeroAndFalseAreNot()
    {
        var info = new MoneroDaemonInfo { Height = 0, IsOffline = false, Version = "" };

        Assert.That(Write(info), Is.EqualTo("""{"version":"","height":0,"isOffline":false}"""));
    }

    [Test]
    public void SmallModel_IsWrittenInCompactJson_InTheOrderOfTheProperties()
    {
        Assert.That(Write(new MoneroVersion { Number = 1, IsRelease = true }), Is.EqualTo("""{"number":1,"isRelease":true}"""));
        Assert.That(Write(new MoneroPruneResult { IsPruned = true, PruningSeed = 3 }), Is.EqualTo("""{"isPruned":true,"pruningSeed":3}"""));
    }

    [Test]
    public void Numbers_UseTheWholeRangeOfTheirType()
    {
        var tx = Read<MoneroTx>($$"""{"fee":{{ulong.MaxValue}},"version":{{uint.MaxValue}},"unlockTime":0}""");

        Assert.That(tx.Fee, Is.EqualTo(ulong.MaxValue));
        Assert.That(tx.Version, Is.EqualTo(uint.MaxValue));
        Assert.That(tx.UnlockTime, Is.EqualTo(0));
        Assert.That(Write(tx), Is.EqualTo($$"""{"version":{{uint.MaxValue}},"fee":{{ulong.MaxValue}},"unlockTime":0}"""));

        var info = Read<MoneroDaemonInfo>($$"""{"numTxs":{{int.MaxValue}},"numTxsPool":{{int.MinValue}}}""");
        Assert.That(info.NumTxs, Is.EqualTo(int.MaxValue));
        Assert.That(info.NumTxsPool, Is.EqualTo(int.MinValue));
    }

    [TestCase("{")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("[{}]")]
    [TestCase("\"text\"")]
    [TestCase("42")]
    [TestCase("true")]
    [TestCase("{\"height\":\"102\"}")]
    [TestCase("{\"height\":-1}")]
    [TestCase("{\"height\":1.5}")]
    [TestCase("{\"height\":18446744073709551616}")]
    [TestCase("{\"height\":{}}")]
    [TestCase("{\"height\":[]}")]
    [TestCase("{\"isOffline\":1}")]
    [TestCase("{\"isOffline\":\"true\"}")]
    [TestCase("{\"numTxs\":2147483648}")]
    [TestCase("{\"numTxs\":1.5}")]
    [TestCase("{\"networkType\":\"mainnet\"}")]
    [TestCase("{\"networkType\":1.5}")]
    [TestCase("{\"version\":1}")]
    [TestCase("{\"height\":1,}")]
    [TestCase("{\"height\":1} {\"height\":2}")]
    public void DaemonInfo_WithJsonThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroDaemonInfo>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroDaemonJsonContext.Default.MoneroDaemonInfo));
        Assert.Throws<JsonException>(() => MoneroJson.Deserialize<MoneroDaemonInfo>(Encoding.UTF8.GetBytes(json)));
    }

    [TestCase("""{"blocks":{}}""")]
    [TestCase("""{"blocks":[1]}""")]
    [TestCase("""{"blocks":[{"txs":{}}]}""")]
    [TestCase("""{"blocks":[{"txs":[1]}]}""")]
    [TestCase("""{"blocks":[{"txs":[{"extra":[256]}]}]}""")]
    [TestCase("""{"blocks":[{"txs":[{"extra":[-1]}]}]}""")]
    [TestCase("""{"blocks":[{"txs":[{"extra":"AQID"}]}]}""")]
    [TestCase("""{"blocks":[{"txs":[{"inputs":[{"keyImage":"ac"}]}]}]}""")]
    [TestCase("""{"blocks":[{"txs":[{"outputIndices":[-1]}]}]}""")]
    public void BlocksResult_WithJsonThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroBlocksResult>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroDaemonJsonContext.Default.MoneroBlocksResult));
    }

    [TestCase("""{"histo":{"abc":1}}""")]
    [TestCase("""{"histo":{"-1":1}}""")]
    [TestCase("""{"histo":{"1":-1}}""")]
    [TestCase("""{"histo":[1,2]}""")]
    public void TxPoolStats_WithAHistogramThatIsNotValid_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => Read<MoneroTxPoolStats>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MoneroDaemonJsonContext.Default.MoneroTxPoolStats));
    }

    [Test]
    public void Text_WithEscapesAndUnicode_SurvivesTheRoundTrip()
    {
        const string text = "line\nsecond \"quoted\" back\\slash \t tab ünï ☃ \U0001F600 </script> & <>";
        var info = new MoneroDaemonSyncInfo { Overview = text };

        var json = Write(info);

        Assert.That(Read<MoneroDaemonSyncInfo>(json).Overview, Is.EqualTo(text));
        Assert.That(JsonSerializer.Deserialize(json, MoneroDaemonJsonContext.Default.MoneroDaemonSyncInfo)!.Overview, Is.EqualTo(text));
        Assert.That(JsonNode.Parse(json)!["overview"]!.GetValue<string>(), Is.EqualTo(text));
    }

    [Test]
    public void Text_IsReadFromUtf8Bytes()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"overview":"ünï ☃ 😀"}""");

        Assert.That(MoneroJson.Deserialize<MoneroDaemonSyncInfo>(bytes).Overview, Is.EqualTo("ünï ☃ \U0001F600"));
    }

    [Test]
    public void Extra_IsAnArrayOfBytes_NotBase64()
    {
        var tx = Read<MoneroTx>("""{"extra":[1,251,213,0,255]}""");

        Assert.That(tx.Extra, Is.EqualTo(new byte[] { 1, 251, 213, 0, 255 }));
        Assert.That(Write(tx), Is.EqualTo("""{"extra":[1,251,213,0,255]}"""));
        Assert.That(Write(new MoneroTx { Extra = [] }), Is.EqualTo("""{"extra":[]}"""));
    }

    [Test]
    public void EmptyLists_AreNotTheSameAsListsThatAreNotSet()
    {
        var tx = Read<MoneroTx>("""{"inputs":[],"outputs":[],"extra":[],"signatures":[],"outputIndices":[]}""");

        Assert.That(tx.Inputs, Is.Not.Null.And.Empty);
        Assert.That(tx.Outputs, Is.Not.Null.And.Empty);
        Assert.That(tx.Extra, Is.Not.Null.And.Empty);
        Assert.That(tx.Signatures, Is.Not.Null.And.Empty);
        Assert.That(tx.OutputIndices, Is.Not.Null.And.Empty);
        Assert.That(Write(tx), Is.EqualTo("""{"inputs":[],"outputs":[],"outputIndices":[],"extra":[],"signatures":[]}"""));
        Assert.That(Write(Read<MoneroTx>("{}")), Is.EqualTo("{}"));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // 128-bit numbers
    // ------------------------------------------------------------------------------------------------------------------------------

    public static IEnumerable<TestCaseData> UInt128Values()
    {
        yield return new TestCaseData(UInt128.Zero, 0UL, 0UL);
        yield return new TestCaseData(UInt128.One, 1UL, 0UL);
        yield return new TestCaseData((UInt128)ulong.MaxValue, ulong.MaxValue, 0UL);
        yield return new TestCaseData(UInt128.One << 64, 0UL, 1UL);
        yield return new TestCaseData((UInt128.One << 64) + 1, 1UL, 1UL);
        yield return new TestCaseData(new UInt128(0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL), 0xFEDCBA9876543210UL, 0x0123456789ABCDEFUL);
        yield return new TestCaseData(UInt128.MaxValue, ulong.MaxValue, ulong.MaxValue);
    }

    [TestCaseSource(nameof(UInt128Values))]
    public void UInt128_IsSplitInTwoHalves_AndJoinedAgain(UInt128 value, ulong low, ulong high)
    {
        Assert.That(MoneroUInt128.Split(value), Is.EqualTo(((ulong?)low, (ulong?)high)));
        Assert.That(MoneroUInt128.Combine(low, high), Is.EqualTo(value));
    }

    [Test]
    public void UInt128_WithAHalfThatIsMissing_CountsThatHalfAsZero()
    {
        Assert.That(MoneroUInt128.Combine(5, null), Is.EqualTo((UInt128)5));
        Assert.That(MoneroUInt128.Combine(null, 5), Is.EqualTo(new UInt128(5, 0)));
        Assert.That(MoneroUInt128.Combine(null, null), Is.Null);
        Assert.That(MoneroUInt128.Split(null), Is.EqualTo(((ulong?)null, (ulong?)null)));
    }

    [TestCaseSource(nameof(UInt128Values))]
    public void Difficulty_IsWrittenAsTwoHalves_AndReadFromThem(UInt128 value, ulong low, ulong high)
    {
        var info = new MoneroDaemonInfo { Difficulty = value, CumulativeDifficulty = value };

        var json = Write(info);

        Assert.That(info.DifficultyLow, Is.EqualTo(low));
        Assert.That(info.DifficultyHigh, Is.EqualTo(high));
        Assert.That(info.CumulativeDifficultyLow, Is.EqualTo(low));
        Assert.That(info.CumulativeDifficultyHigh, Is.EqualTo(high));
        Assert.That(json, Does.Contain($"\"difficultyLow\":{low},\"difficultyHigh\":{high}"));
        Assert.That(JsonNode.Parse(json)!.AsObject().ContainsKey("difficulty"), Is.False);
        Assert.That(JsonNode.Parse(json)!.AsObject().ContainsKey("cumulativeDifficulty"), Is.False);

        var read = Read<MoneroDaemonInfo>(json);
        Assert.That(read.Difficulty, Is.EqualTo(value));
        Assert.That(read.CumulativeDifficulty, Is.EqualTo(value));
    }

    [Test]
    public void Difficulty_SetToNull_ClearsBothHalves()
    {
        var header = new MoneroBlockHeader { DifficultyLow = 5, DifficultyHigh = 6 };

        header.Difficulty = null;

        Assert.That(header.DifficultyLow, Is.Null);
        Assert.That(header.DifficultyHigh, Is.Null);
        Assert.That(Write(header), Is.EqualTo("{}"));
    }

    [Test]
    public void Difficulty_WithOnlyOneHalfInTheJson_IsStillAValue()
    {
        Assert.That(Read<MoneroBlockHeader>("""{"difficultyLow":7}""").Difficulty, Is.EqualTo((UInt128)7));
        Assert.That(Read<MoneroBlockHeader>("""{"difficultyHigh":7}""").Difficulty, Is.EqualTo(new UInt128(7, 0)));
        Assert.That(Read<MoneroBlockHeader>("{}").Difficulty, Is.Null);
    }

    [Test]
    public void Difficulty_IsInherited_ByTheBlock()
    {
        var block = Read<MoneroBlock>("""{"difficultyLow":1,"difficultyHigh":1,"cumulativeDifficultyLow":2,"cumulativeDifficultyHigh":3}""");

        Assert.That(block.Difficulty, Is.EqualTo((UInt128.One << 64) + 1));
        Assert.That(block.CumulativeDifficulty, Is.EqualTo(new UInt128(3, 2)));
    }

    [Test]
    public void EveryNumberOfTwoHalves_IsCompletedByTheModel()
    {
        // A ulong? ending in Low is the least significant half of a 128-bit number: the model needs the High half and the number itself.
        // This finds the ones that are added later too.
        var pairs = new List<(Type Type, PropertyInfo Low, PropertyInfo High, PropertyInfo Number)>();

        foreach (var type in ModelTypes)
        {
            foreach (var low in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(property => property.Name.EndsWith("Low", StringComparison.Ordinal) && property.PropertyType == typeof(ulong?)))
            {
                var name = low.Name[..^3];
                var high = type.GetProperty(name + "High");
                var number = type.GetProperty(name);

                Assert.That(high, Is.Not.Null, $"{type.Name}.{low.Name} has no {name}High");
                Assert.That(number, Is.Not.Null, $"{type.Name}.{low.Name} has no {name}");
                Assert.That(low.PropertyType, Is.EqualTo(typeof(ulong?)));
                Assert.That(high!.PropertyType, Is.EqualTo(typeof(ulong?)));
                Assert.That(number!.PropertyType, Is.EqualTo(typeof(UInt128?)));
                Assert.That(number.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition, Is.EqualTo(JsonIgnoreCondition.Always), $"{type.Name}.{name} is in the JSON");
                pairs.Add((type, low, high, number));
            }
        }

        // MoneroDaemonInfo (2), MoneroBlockHeader (2), MoneroBlockTemplate, MoneroMinerTxSum (2), MoneroAltChain
        Assert.That(pairs, Has.Count.GreaterThanOrEqualTo(8));

        foreach (var (type, low, high, number) in pairs)
        {
            foreach (var value in new[] { UInt128.Zero, UInt128.One << 64, UInt128.MaxValue, new UInt128(0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL) })
            {
                var model = Activator.CreateInstance(type)!;
                number.SetValue(model, (UInt128?)value);

                Assert.That(MoneroUInt128.Combine((ulong?)low.GetValue(model), (ulong?)high.GetValue(model)), Is.EqualTo(value), $"{type.Name}.{number.Name}");

                var json = JsonSerializer.Serialize(model, type, MoneroJson.Options);
                var read = JsonSerializer.Deserialize(json, type, MoneroJson.Options)!;
                Assert.That(number.GetValue(read), Is.EqualTo(value), $"{type.Name}.{number.Name} after the JSON");
            }
        }
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // the parents of a tx, an output, a block
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Block_SetsItselfAsTheBlockOfItsTxs_AndOfItsMinerTx()
    {
        var minerTx = new MoneroTx { Hash = "miner" };
        var txs = new List<MoneroTx> { new() { Hash = "a" }, new() { Hash = "b" } };

        var block = new MoneroBlock { Height = 5, MinerTx = minerTx, Txs = txs };

        Assert.That(minerTx.Block, Is.SameAs(block));
        Assert.That(txs, Is.All.Matches<MoneroTx>(tx => ReferenceEquals(tx.Block, block)));
        Assert.That(txs.Select(tx => tx.GetHeight()), Is.All.EqualTo(5));

        // the height is the one of the block now, not a copy
        block.Height = 6;
        Assert.That(txs[0].GetHeight(), Is.EqualTo(6));
    }

    [Test]
    public void Tx_SetsItselfAsTheTxOfItsInputsAndOutputs()
    {
        var inputs = new List<MoneroOutput> { new() { Amount = 1 }, new() { Amount = 2 } };
        var outputs = new List<MoneroOutput> { new() { Amount = 3 } };

        var tx = new MoneroTx { Inputs = inputs, Outputs = outputs };

        Assert.That(inputs.Concat(outputs), Is.All.Matches<MoneroOutput>(output => ReferenceEquals(output.Tx, tx)));
    }

    [Test]
    public void Tx_WhenItsListsAreReplacedOrCleared_IsStillSafe()
    {
        var tx = new MoneroTx { Inputs = [new MoneroOutput()], Outputs = [new MoneroOutput()] };
        var replaced = new List<MoneroOutput> { new() };

        tx.Inputs = replaced;
        Assert.That(replaced[0].Tx, Is.SameAs(tx));

        Assert.DoesNotThrow(() =>
        {
            tx.Inputs = null;
            tx.Outputs = null;
        });
        Assert.That(tx.Inputs, Is.Null);
        Assert.That(tx.Outputs, Is.Null);

        var block = new MoneroBlock();
        Assert.DoesNotThrow(() =>
        {
            block.Txs = null;
            block.MinerTx = null;
        });
        Assert.That(block.Txs, Is.Null);
        Assert.That(block.MinerTx, Is.Null);
    }

    [Test]
    public void Tx_GetHeight_IsNull_WithoutABlock()
    {
        Assert.That(new MoneroTx().GetHeight(), Is.Null);
        Assert.That(new MoneroTx { Block = new MoneroBlock() }.GetHeight(), Is.Null);
        Assert.That(new MoneroTx { Block = new MoneroBlock { Height = 0 } }.GetHeight(), Is.EqualTo(0));
    }

    [Test]
    public void Parents_AreSetByTheDeserializers_WhateverTheOrderOfTheFields()
    {
        // the txs come before the height of the block, and the outputs before the fee of the tx
        const string json = """{"txs":[{"outputs":[{"amount":1}],"inputs":[{"amount":2}],"fee":3}],"minerTx":{"outputs":[{"amount":4}]},"height":9}""";

        foreach (var engine in Enum.GetValues<Engine>())
        {
            var block = (MoneroBlock)ReadWith(engine, json, typeof(MoneroBlock));
            var tx = block.Txs![0];

            Assert.That(tx.Block, Is.SameAs(block), engine.ToString());
            Assert.That(tx.GetHeight(), Is.EqualTo(9), engine.ToString());
            Assert.That(tx.Outputs![0].Tx, Is.SameAs(tx), engine.ToString());
            Assert.That(tx.Inputs![0].Tx, Is.SameAs(tx), engine.ToString());
            Assert.That(block.MinerTx!.Block, Is.SameAs(block), engine.ToString());
            Assert.That(block.MinerTx!.Outputs![0].Tx, Is.SameAs(block.MinerTx), engine.ToString());
        }
    }

    [Test]
    public void Block_WithManyTxs_IsReadAndWritten()
    {
        var tx = JsonNode.Parse(Fixture("tx_in_block"))!["blocks"]![0]!["txs"]![0]!;
        var txs = new JsonArray();
        for (var i = 0; i < 2000; i++) txs.Add(tx.DeepClone());
        var json = new JsonObject { ["height"] = 1, ["txs"] = txs }.ToJsonString();

        var block = Read<MoneroBlock>(json);

        Assert.That(block.Txs, Has.Count.EqualTo(2000));
        Assert.That(block.Txs, Is.All.Matches<MoneroTx>(t => ReferenceEquals(t.Block, block)
                                                           && t.Inputs!.All(input => ReferenceEquals(input.Tx, t))
                                                           && t.Outputs!.All(output => ReferenceEquals(output.Tx, t))));
        AssertSameJson(json, Write(block));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // copies
    // ------------------------------------------------------------------------------------------------------------------------------

    private static readonly Type[] CopyTypes = [typeof(MoneroTx), typeof(MoneroBlock), typeof(MoneroBlockHeader), typeof(MoneroOutput), typeof(MoneroKeyImage)];

    public static IEnumerable<TestCaseData> CopyCases()
    {
        return CopyTypes.Select(type => new TestCaseData(type).SetArgDisplayNames(type.Name));
    }

    [TestCaseSource(nameof(CopyCases))]
    public void Copy_HasEveryPropertyOfTheOriginal(Type type)
    {
        // every property has a different value, so a property that is copied from another one is found
        var model = new Sampler().Populate(Activator.CreateInstance(type)!);

        var copy = type.GetMethod("Copy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.EmptyTypes)!.Invoke(model, null)!;

        Assert.That(copy, Is.Not.SameAs(model));
        Assert.That(copy.GetType(), Is.EqualTo(type));
        AssertSameJson(JsonSerializer.Serialize(model, type, MoneroJson.Options), JsonSerializer.Serialize(copy, type, MoneroJson.Options));
    }

    [TestCaseSource(nameof(CopyCases))]
    public void Copy_OfAModelWithNothingSet_HasNothingSet(Type type)
    {
        var copy = type.GetMethod("Copy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.EmptyTypes)!.Invoke(Activator.CreateInstance(type), null)!;

        Assert.That(JsonSerializer.Serialize(copy, type, MoneroJson.Options), Is.EqualTo("{}"));
    }

    [Test]
    public void Tx_Copy_KeepsThePrunedHexAndThePrunableHexApart()
    {
        var copy = new MoneroTx { PrunedHex = "aa", PrunableHex = "bb", FullHex = "cc" }.Copy();

        Assert.That(copy.PrunedHex, Is.EqualTo("aa"));
        Assert.That(copy.PrunableHex, Is.EqualTo("bb"));
        Assert.That(copy.FullHex, Is.EqualTo("cc"));
    }

    [Test]
    public void Tx_Copy_IsDeep()
    {
        var tx = ReadFixture<MoneroBlocksResult>("tx_in_block").GetTxs().Single();
        var before = Write(tx);

        var copy = tx.Copy();
        copy.Inputs![0].Amount = 999;
        copy.Inputs![0].RingOutputIndices!.Add(999);
        copy.Inputs![0].KeyImage!.Hex = "changed";
        copy.Outputs![0].Mask = "changed";
        copy.Outputs!.RemoveAt(1);
        copy.OutputIndices!.Clear();
        copy.Extra![0] = 77;
        copy.Hash = "changed";

        Assert.That(Write(tx), Is.EqualTo(before));
    }

    [Test]
    public void Tx_Copy_IsTheParentOfItsOwnOutputs_AndHasNoBlock()
    {
        var tx = ReadFixture<MoneroBlocksResult>("tx_in_block").GetTxs().Single();

        var copy = tx.Copy();

        Assert.That(copy.Block, Is.Null);
        Assert.That(copy.Inputs, Is.All.Matches<MoneroOutput>(input => ReferenceEquals(input.Tx, copy)));
        Assert.That(copy.Outputs, Is.All.Matches<MoneroOutput>(output => ReferenceEquals(output.Tx, copy)));
        Assert.That(tx.Inputs, Is.All.Matches<MoneroOutput>(input => ReferenceEquals(input.Tx, tx)));
        Assert.That(tx.Block, Is.Not.Null);
    }

    [Test]
    public void Block_Copy_IsDeep_AndIsTheParentOfItsOwnTxs()
    {
        var block = ReadFixture<List<MoneroBlock>>("blocks")[2];
        var before = Write(block);

        var copy = block.Copy();
        copy.Txs![0].Fee = 1;
        copy.Txs!.Add(new MoneroTx());
        copy.MinerTx!.Hash = "changed";
        copy.TxHashes!.Clear();
        copy.Height = 1;

        Assert.That(Write(block), Is.EqualTo(before));
        Assert.That(block.Txs![0].Block, Is.SameAs(block));
        Assert.That(block.MinerTx!.Block, Is.SameAs(block));
        Assert.That(copy.Txs![0].Block, Is.SameAs(copy));
        Assert.That(copy.MinerTx!.Block, Is.SameAs(copy));
    }

    [Test]
    public void Copy_IsOfTheSameClass_WhateverTheTypeItIsCalledOn()
    {
        MoneroBlockHeader header = new MoneroBlock { Height = 3, Hex = "ab" };

        var copy = header.Copy();

        Assert.That(copy, Is.InstanceOf<MoneroBlock>());
        Assert.That(((MoneroBlock)copy).Hex, Is.EqualTo("ab"));
        Assert.That(copy.Height, Is.EqualTo(3));
    }

    [Test]
    public void KeyImage_Copy_IsIndependent()
    {
        var keyImage = new MoneroKeyImage { Hex = "aa", Signature = "bb" };

        var copy = keyImage.Copy();
        copy.Hex = "cc";

        Assert.That(keyImage.Hex, Is.EqualTo("aa"));
        Assert.That(copy.Signature, Is.EqualTo("bb"));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // MoneroJson
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void MoneroJson_ReadsAndWritesBytesAndStringsTheSame()
    {
        var json = Fixture("block_with_tx");

        var fromString = MoneroJson.Deserialize<MoneroBlock>(json);
        var fromBytes = MoneroJson.Deserialize<MoneroBlock>(Encoding.UTF8.GetBytes(json));

        Assert.That(Write(fromBytes), Is.EqualTo(Write(fromString)));
        Assert.That(Encoding.UTF8.GetString(MoneroJson.SerializeToUtf8Bytes(fromString)), Is.EqualTo(Write(fromString)));
        Assert.That(MoneroJson.SerializeToUtf8Bytes(fromBytes), Is.EqualTo(MoneroJson.SerializeToUtf8Bytes(fromString)));
    }

    [Test]
    public void MoneroJson_ReadsASpanThatItDoesNotKeep()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"overview":"before"}""");

        var info = MoneroJson.Deserialize<MoneroDaemonSyncInfo>(bytes.AsSpan());
        Array.Clear(bytes);

        Assert.That(info.Overview, Is.EqualTo("before"));
    }

    [Test]
    public void MoneroJson_ReadsALiteralOfUtf8Bytes()
    {
        Assert.That(MoneroJson.Deserialize<MoneroVersion>("""{"number":5}"""u8).Number, Is.EqualTo(5));
    }

    [Test]
    public void MoneroJson_Options_OmitTheNulls()
    {
        Assert.That(MoneroJson.Options.DefaultIgnoreCondition, Is.EqualTo(JsonIgnoreCondition.WhenWritingNull));
    }

    [Test]
    public void MoneroJson_Deserialize_OfNull_IsAnErrorThatNamesTheType()
    {
        var fromString = Assert.Throws<JsonException>(() => MoneroJson.Deserialize<MoneroDaemonInfo>("null"));
        var fromBytes = Assert.Throws<JsonException>(() => MoneroJson.Deserialize<MoneroDaemonInfo>("null"u8));

        Assert.That(fromString!.Message, Does.Contain(nameof(MoneroDaemonInfo)));
        Assert.That(fromBytes!.Message, Does.Contain(nameof(MoneroDaemonInfo)));
    }

    [Test]
    public void MoneroJson_CanBeUsedFromManyThreads()
    {
        var json = Fixture("block_with_tx");
        var expected = Write(MoneroJson.Deserialize<MoneroBlock>(json));
        var results = new string[200];

        Parallel.For(0, results.Length, i => results[i] = Write(MoneroJson.Deserialize<MoneroBlock>(json)));

        Assert.That(results, Is.All.EqualTo(expected));
    }

    // ------------------------------------------------------------------------------------------------------------------------------
    // enumerations
    // ------------------------------------------------------------------------------------------------------------------------------

    [Test]
    public void Enumerations_HaveTheNumbersOfMoneroCpp()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)MoneroNetworkType.MainNet, Is.EqualTo(0));
            Assert.That((int)MoneroNetworkType.TestNet, Is.EqualTo(1));
            Assert.That((int)MoneroNetworkType.StageNet, Is.EqualTo(2));
            Assert.That((int)MoneroNetworkType.Regtest, Is.EqualTo(3));
            Assert.That(Enum.GetValues<MoneroNetworkType>(), Has.Length.EqualTo(4));

            Assert.That((int)MoneroConnectionType.Invalid, Is.EqualTo(0));
            Assert.That((int)MoneroConnectionType.Ipv4, Is.EqualTo(1));
            Assert.That((int)MoneroConnectionType.Ipv6, Is.EqualTo(2));
            Assert.That((int)MoneroConnectionType.Tor, Is.EqualTo(3));
            Assert.That((int)MoneroConnectionType.I2P, Is.EqualTo(4));
            Assert.That(Enum.GetValues<MoneroConnectionType>(), Has.Length.EqualTo(5));

            Assert.That((int)MoneroKeyImageSpentStatus.NotSpent, Is.EqualTo(0));
            Assert.That((int)MoneroKeyImageSpentStatus.Confirmed, Is.EqualTo(1));
            Assert.That((int)MoneroKeyImageSpentStatus.TxPool, Is.EqualTo(2));
            Assert.That(Enum.GetValues<MoneroKeyImageSpentStatus>(), Has.Length.EqualTo(3));
        });
    }

    [TestCase(0, ExpectedResult = MoneroNetworkType.MainNet)]
    [TestCase(1, ExpectedResult = MoneroNetworkType.TestNet)]
    [TestCase(2, ExpectedResult = MoneroNetworkType.StageNet)]
    [TestCase(3, ExpectedResult = MoneroNetworkType.Regtest)]
    public MoneroNetworkType NetworkTypeParser_ParsesTheNumber(int number)
    {
        return MoneroNetworkTypeParser.Parse(number);
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(int.MaxValue)]
    [TestCase(int.MinValue)]
    public void NetworkTypeParser_RejectsANumberThatIsNotANetwork(int number)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => MoneroNetworkTypeParser.Parse(number));

        Assert.That(exception!.ActualValue, Is.EqualTo(number));
    }

    [TestCase("mainnet", ExpectedResult = MoneroNetworkType.MainNet)]
    [TestCase("MainNet", ExpectedResult = MoneroNetworkType.MainNet)]
    [TestCase("MAINNET", ExpectedResult = MoneroNetworkType.MainNet)]
    [TestCase("testnet", ExpectedResult = MoneroNetworkType.TestNet)]
    [TestCase("  TestNet\n", ExpectedResult = MoneroNetworkType.TestNet)]
    [TestCase("stagenet", ExpectedResult = MoneroNetworkType.StageNet)]
    [TestCase("StageNet", ExpectedResult = MoneroNetworkType.StageNet)]
    [TestCase("regtest", ExpectedResult = MoneroNetworkType.Regtest)]
    [TestCase("RegTest", ExpectedResult = MoneroNetworkType.Regtest)]
    public MoneroNetworkType NetworkTypeParser_ParsesTheName(string name)
    {
        return MoneroNetworkTypeParser.Parse(name);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("fakechain")]
    [TestCase("main net")]
    [TestCase("0")]
    public void NetworkTypeParser_RejectsANameThatIsNotANetwork(string name)
    {
        Assert.Throws<ArgumentException>(() => MoneroNetworkTypeParser.Parse(name));
    }

    [Test]
    public void TxDefaultPaymentId_IsEightZeroBytes()
    {
        Assert.That(MoneroTx.DefaultPaymentId, Is.EqualTo("0000000000000000"));
    }
}
