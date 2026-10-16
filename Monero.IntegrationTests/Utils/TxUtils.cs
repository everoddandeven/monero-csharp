using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Transaction test utilities.
/// </summary>
public static class TxUtils
{
    /// <summary>
    /// Test a transaction.
    /// </summary>
    /// <param name="tx">transaction to test.</param>
    /// <param name="ctx">test context.</param>
    public static void TestTx(MoneroTx? tx, ModelTestContext ctx)
    {
        Assert.That(tx, Is.Not.Null, "No tx provided");
        new TxTester(tx!, ctx).Run();
    }

    /// <summary>
    /// Test a miner transaction.
    /// </summary>
    /// <param name="minerTx">miner transaction to test.</param>
    public static void TestMinerTx(MoneroTx? minerTx)
    {
        Assert.That(minerTx, Is.Not.Null);
        Assert.That(minerTx!.IsMinerTx, Is.Not.Null);
        Assert.That(minerTx.Version, Is.Not.Null);
        Assert.That(minerTx.Extra, Is.Not.Null.And.Not.Empty);
        Assert.That(minerTx.UnlockTime, Is.Not.Null);
        Assert.That(minerTx.IsConfirmed, Is.True);
    }
}

/// <summary>
/// Test a <see cref="MoneroTx"/>.
/// </summary>
public class TxTester
{
    private readonly MoneroTx _tx;
    private readonly ModelTestContext _ctx;

    /// <summary>
    /// Initialize a new transaction tester.
    /// </summary>
    /// <param name="tx">transaction to test.</param>
    /// <param name="ctx">test context.</param>
    public TxTester(MoneroTx tx, ModelTestContext ctx)
    {
        Assert.That(ctx.IsPruned, Is.Not.Null);
        Assert.That(ctx.IsConfirmed, Is.Not.Null);
        Assert.That(ctx.FromGetTxPool, Is.Not.Null);
        _tx = tx;
        _ctx = ctx;
    }

    // standard across all txs
    private void TestStandard()
    {
        Assert.That(_tx.Hash, Is.Not.Null.And.Length.EqualTo(64));
        if (_tx.IsRelayed == null) Assert.That(_tx.InTxPool, Is.True);
        Assert.That(_tx.IsConfirmed, Is.Not.Null);
        Assert.That(_tx.InTxPool, Is.Not.Null);
        Assert.That(_tx.IsMinerTx, Is.Not.Null);
        Assert.That(_tx.IsDoubleSpendSeen, Is.Not.Null);
        Assert.That(_tx.Version, Is.Not.Null);
        Assert.That(_tx.UnlockTime, Is.Not.Null);
        Assert.That(_tx.Extra, Is.Not.Null.And.Not.Empty);
        GenUtils.TestUnsignedBigInteger(_tx.Fee, true);

        // test presence of output indices
        if (_tx.IsMinerTx == true)
        {
            Assert.That(_tx.OutputIndices ?? [], Is.Empty);
            Assert.That(_tx.Fee, Is.EqualTo(0UL));
            Assert.That(_tx.Inputs ?? [], Is.Empty);
            Assert.That(_tx.Signatures ?? [], Is.Empty);
        }

        if (_tx.InTxPool == true || _ctx.FromGetTxPool == true || _ctx.HasOutputIndices == false)
        {
            Assert.That(_tx.OutputIndices ?? [], Is.Empty);
        }
        else
        {
            Assert.That(_tx.OutputIndices ?? [], Is.Not.Empty);
        }
    }

    private void TestConfirmed()
    {
        if (_ctx.IsConfirmed == true) Assert.That(_tx.IsConfirmed, Is.True);
        else if (_ctx.IsConfirmed == false) Assert.That(_tx.IsConfirmed, Is.False);

        if (_tx.IsConfirmed == true)
        {
            var block = _tx.Block;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Txs, Does.Contain(_tx));
            Assert.That(block.Height, Is.GreaterThan(0UL));
            Assert.That(block.Timestamp, Is.GreaterThan(0UL));
            Assert.That(_tx.Relay, Is.True);
            Assert.That(_tx.IsRelayed, Is.True);
            Assert.That(_tx.IsFailed, Is.False);
            Assert.That(_tx.InTxPool, Is.False);
            Assert.That(_tx.IsDoubleSpendSeen, Is.False);

            if (_ctx.FromBinaryBlock == true) Assert.That(_tx.NumConfirmations, Is.Null);
            else Assert.That(_tx.NumConfirmations, Is.GreaterThan(0UL));
        }
        else
        {
            Assert.That(_tx.Block, Is.Null, $"Expected block tx to be null: {_tx.Block}");
            Assert.That(_tx.NumConfirmations, Is.EqualTo(0UL));
        }
    }

    private void TestInTxPool()
    {
        if (_tx.InTxPool == true)
        {
            Assert.That(_tx.IsConfirmed, Is.False);
            Assert.That(_tx.IsDoubleSpendSeen, Is.False);
            Assert.That(_tx.LastFailedHeight, Is.Null);
            Assert.That(_tx.LastFailedHash, Is.Null);
            Assert.That(_tx.ReceivedTimestamp, Is.GreaterThan(0UL));

            if (_ctx.FromGetTxPool == true)
            {
                Assert.That(_tx.Size, Is.GreaterThan(0UL));
                Assert.That(_tx.Weight, Is.GreaterThan(0UL));
                Assert.That(_tx.IsKeptByBlock, Is.Not.Null);
                Assert.That(_tx.MaxUsedBlockHeight, Is.Not.Null);
                Assert.That(_tx.MaxUsedBlockHash, Is.Not.Null);
            }
        }
        else
        {
            Assert.That(_tx.LastRelayedTimestamp, Is.Null);
        }
    }

    private void TestFailed()
    {
        if (_tx.IsFailed == true)
        {
            Assert.That(_tx.ReceivedTimestamp, Is.GreaterThan(0UL));
        }
        else
        {
            if (_tx.IsRelayed == null)
            {
                Assert.That(_tx.Relay, Is.Null);
            }
            else if (_tx.IsRelayed == true)
            {
                Assert.That(_tx.IsDoubleSpendSeen, Is.False);
            }
            else
            {
                Assert.That(_tx.IsRelayed, Is.False);
                if (_ctx.FromGetTxPool == true)
                {
                    Assert.That(_tx.Relay, Is.False);
                    Assert.That(_tx.IsDoubleSpendSeen, Is.Not.Null);
                }
            }
        }

        Assert.That(_tx.LastFailedHeight, Is.Null);
        Assert.That(_tx.LastFailedHash, Is.Null);

        // received time only for tx pool or failed txs
        if (_tx.ReceivedTimestamp != null) Assert.That(_tx.InTxPool == true || _tx.IsFailed == true, Is.True);
    }

    private void TestInputsAndOutputs()
    {
        if (_tx.IsMinerTx != true) Assert.That(_tx.Inputs ?? [], Is.Not.Empty);

        foreach (var input in _tx.Inputs ?? [])
        {
            Assert.That(input.Tx, Is.SameAs(_tx));
            OutputUtils.TestInput(input, _ctx);
        }

        Assert.That(_tx.Outputs ?? [], Is.Not.Empty);
        foreach (var output in _tx.Outputs ?? [])
        {
            Assert.That(output.Tx, Is.SameAs(_tx));
            OutputUtils.TestOutput(output, _ctx);
        }
    }

    // non-pruned transaction
    private void TestFullTx()
    {
        Assert.That(_tx.Version, Is.Not.Null);
        Assert.That(_tx.UnlockTime, Is.Not.Null);
        Assert.That(_tx.Extra, Is.Not.Null.And.Not.Empty);

        if (_ctx.FromBinaryBlock == true)
        {
            Assert.That(_tx.FullHex, Is.Null);
            Assert.That(_tx.RctSigPrunable, Is.Null);
        }
        else
        {
            Assert.That(_tx.FullHex, Is.Not.Null.And.Not.Empty);
        }

        Assert.That(_tx.IsDoubleSpendSeen, Is.False);
        if (_tx.IsConfirmed == true)
        {
            Assert.That(_tx.LastRelayedTimestamp, Is.Null);
            Assert.That(_tx.ReceivedTimestamp, Is.Null);
        }
        else
        {
            if (_tx.IsRelayed == true) Assert.That(_tx.LastRelayedTimestamp, Is.GreaterThan(0UL));
            else Assert.That(_tx.LastRelayedTimestamp, Is.Null);

            Assert.That(_tx.ReceivedTimestamp, Is.GreaterThan(0UL));
        }
    }

    private void TestPruning()
    {
        // tx might be pruned regardless of configuration
        var isPruned = _tx.PrunedHex != null;
        if (_ctx.IsPruned == true) Assert.That(isPruned, Is.True);

        if (_ctx.FromGetTxPool == true || _ctx.FromBinaryBlock == true) Assert.That(_tx.PrunableHash, Is.Null);
        else Assert.That(_tx.PrunableHash, Is.Not.Null);

        if (isPruned)
        {
            Assert.That(_tx.RctSigPrunable, Is.Null);
            Assert.That(_tx.Size, Is.Null);
            Assert.That(_tx.LastRelayedTimestamp, Is.Null);
            Assert.That(_tx.ReceivedTimestamp, Is.Null);
            Assert.That(_tx.PrunedHex, Is.Not.Null);
        }
        else
        {
            TestFullTx();
        }
    }

    /// <summary>
    /// Run the test.
    /// </summary>
    public void Run()
    {
        TestStandard();
        TestConfirmed();
        TestInTxPool();
        TestFailed();
        TestInputsAndOutputs();
        TestPruning();
    }
}
