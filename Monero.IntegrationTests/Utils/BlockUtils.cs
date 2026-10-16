using Monero.Daemon;
using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Block test utilities.
/// </summary>
public static class BlockUtils
{
    /// <summary>
    /// Test a block header.
    /// </summary>
    /// <param name="header">header to test.</param>
    /// <param name="isFull">check full header.</param>
    public static void TestBlockHeader(MoneroBlockHeader header, bool? isFull)
    {
        // test base fields
        Assert.That(header.Height, Is.Not.Null);
        Assert.That(header.MajorVersion, Is.GreaterThan(0U));
        Assert.That(header.MinorVersion, Is.Not.Null);
        Assert.That(header.Timestamp, Is.Not.Null);

        if (header.Height == 0) Assert.That(header.Timestamp, Is.EqualTo(0UL));
        else Assert.That(header.Timestamp, Is.GreaterThan(0UL));

        Assert.That(header.PrevHash, Is.Not.Null);
        Assert.That(header.Nonce, Is.Not.Null);
        // TODO (monero-project): why is header nonce 0?
        if (header.Nonce == 0) TestContext.Progress.WriteLine($"header nonce is 0 at height {header.Height}");

        // never seen defined
        Assert.That(header.PowHash, Is.Null);

        // test full header details
        TestFullHeader(header, isFull);
    }

    /// <summary>
    /// Test full header details.
    /// </summary>
    /// <param name="header">header to test full details.</param>
    /// <param name="isFull">indicates if the full details of the header should be defined.</param>
    public static void TestFullHeader(MoneroBlockHeader header, bool? isFull)
    {
        // num txs always defined
        Assert.That(header.NumTxs, Is.Not.Null);
        Assert.That(header.Hash, Is.Not.Null.And.Length.EqualTo(64));

        if (isFull == true)
        {
            Assert.That(header.Size, Is.GreaterThan(0UL));
            Assert.That(header.Depth, Is.Not.Null);
            Assert.That(header.DifficultyLow, Is.GreaterThan(0UL));
            Assert.That(header.DifficultyHigh, Is.Not.Null);
            Assert.That(header.CumulativeDifficultyLow, Is.GreaterThan(0UL));
            Assert.That(header.CumulativeDifficultyHigh, Is.Not.Null);
            Assert.That(header.MinerTxHash, Is.Not.Null.And.Length.EqualTo(64));
            Assert.That(header.OrphanStatus, Is.Not.Null);
            Assert.That(header.Reward, Is.Not.Null);
            Assert.That(header.Weight, Is.GreaterThan(0UL));
        }
        else
        {
            Assert.That(header.Size, Is.Null);
            Assert.That(header.Depth, Is.Null);
            Assert.That(header.DifficultyLow, Is.Null);
            Assert.That(header.DifficultyHigh, Is.Null);
            Assert.That(header.CumulativeDifficultyLow, Is.Null);
            Assert.That(header.CumulativeDifficultyHigh, Is.Null);
            Assert.That(header.MinerTxHash, Is.Null);
            Assert.That(header.OrphanStatus, Is.Null);
            Assert.That(header.Reward, Is.Null);
            Assert.That(header.Weight, Is.Null);
        }
    }

    /// <summary>
    /// Test a block.
    /// </summary>
    /// <param name="block">block to test.</param>
    /// <param name="ctx">test context.</param>
    public static void TestBlock(MoneroBlock block, ModelTestContext ctx)
    {
        // test required fields
        Assert.That(block.MinerTx, Is.Not.Null, "Expected block miner tx");
        TxUtils.TestMinerTx(block.MinerTx);
        TestBlockHeader(block, ctx.HeaderIsFull);

        if (ctx.HasHex == true) Assert.That(block.Hex, Is.Not.Null.And.Length.GreaterThan(1));
        else Assert.That(block.Hex, Is.Null);

        var txs = block.Txs ?? [];
        var txHashes = block.TxHashes ?? [];

        if (ctx.HasTxs == true)
        {
            Assert.That(ctx.TxContext, Is.Not.Null);
            foreach (var tx in txs)
            {
                Assert.That(tx.Block, Is.SameAs(block));
                TxUtils.TestTx(tx, ctx.TxContext!);
            }

            // test duplicates
            if (block.NumTxs != null)
            {
                Assert.That(txs, Has.Count.EqualTo((int)block.NumTxs.Value));
                Assert.That(txHashes, Has.Count.EqualTo((int)block.NumTxs.Value));
                Assert.That(txHashes, Is.Unique, "Duplicate tx hashes found in block");
            }
            else
            {
                Assert.That(txs, Is.Empty);
                Assert.That(txHashes, Is.Empty);
            }
        }
        else
        {
            Assert.That(ctx.TxContext, Is.Null);
            Assert.That(txs, Is.Empty, "No txs expected");
        }
    }

    /// <summary>
    /// Test get blocks by range.
    /// </summary>
    /// <param name="daemon">daemon to test.</param>
    /// <param name="startHeight">range start height.</param>
    /// <param name="endHeight">range end height.</param>
    /// <param name="chainHeight">blockchain height.</param>
    /// <param name="chunked">get blocks range chunked.</param>
    /// <param name="blockCtx">binary block test context.</param>
    public static async Task TestGetBlocksRange(MoneroDaemon daemon, ulong? startHeight, ulong? endHeight, ulong chainHeight, bool chunked, BinaryBlockContext blockCtx)
    {
        // fetch blocks by range
        var realStartHeight = startHeight ?? 0;
        var realEndHeight = endHeight ?? chainHeight - 1;
        var blocks = chunked
            ? await daemon.GetBlocksByRangeChunkedAsync(startHeight, endHeight)
            : await daemon.GetBlocksByRangeAsync(startHeight, endHeight);

        var expectedNumBlocks = realEndHeight - realStartHeight + 1;
        Assert.That((ulong)blocks.Count, Is.EqualTo(expectedNumBlocks), $"Expected {expectedNumBlocks} block(s), got {blocks.Count}");

        // test each block
        for (var i = 0; i < blocks.Count; i++)
        {
            Assert.That(blocks[i].Height, Is.EqualTo(realStartHeight + (ulong)i));
            TestBlock(blocks[i], blockCtx);
        }
    }

    /// <summary>
    /// Test a mining block template.
    /// </summary>
    /// <param name="template">mining block template to test.</param>
    public static void TestBlockTemplate(MoneroBlockTemplate template)
    {
        Assert.That(template.BlockTemplateBlob, Is.Not.Null);
        Assert.That(template.BlockHashingBlob, Is.Not.Null);
        Assert.That(template.DifficultyLow, Is.Not.Null);
        Assert.That(template.DifficultyHigh, Is.Not.Null);
        Assert.That(template.ExpectedReward, Is.Not.Null);
        Assert.That(template.Height, Is.Not.Null);
        Assert.That(template.PrevHash, Is.Not.Null);
        Assert.That(template.ReservedOffset, Is.Not.Null);
        Assert.That(template.SeedHeight, Is.Not.Null);
        Assert.That(template.SeedHash, Is.Not.Null.And.Not.Empty);
        // next seed hash can be null or initialized
    }

    /// <summary>
    /// Test daemon alternative chain info.
    /// </summary>
    /// <param name="altChain">alternative chain info to test.</param>
    public static void TestAltChain(MoneroAltChain altChain)
    {
        Assert.That(altChain.BlockHashes, Is.Not.Null.And.Not.Empty);
        GenUtils.TestUnsignedBigInteger(altChain.DifficultyLow, true);
        GenUtils.TestUnsignedBigInteger(altChain.DifficultyHigh);
        Assert.That(altChain.Height, Is.GreaterThan(0UL));
        Assert.That(altChain.Length, Is.GreaterThan(0UL));
        Assert.That(altChain.MainChainParentBlockHash, Is.Not.Null.And.Length.EqualTo(64));
    }
}
