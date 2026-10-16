namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Provides context or configuration for test methods to test a type.
/// </summary>
public class ModelTestContext
{
    /// <summary>Initialize a new test context.</summary>
    public ModelTestContext()
    {
    }

    /// <summary>
    /// Initialize a new test context as a copy of the given one.
    /// </summary>
    /// <param name="ctx">test context to copy.</param>
    public ModelTestContext(ModelTestContext ctx)
    {
        HasJson = ctx.HasJson;
        IsPruned = ctx.IsPruned;
        IsFull = ctx.IsFull;
        IsConfirmed = ctx.IsConfirmed;
        IsMinerTx = ctx.IsMinerTx;
        FromGetTxPool = ctx.FromGetTxPool;
        FromBinaryBlock = ctx.FromBinaryBlock;
        HasOutputIndices = ctx.HasOutputIndices;
        DoNotTestCopy = ctx.DoNotTestCopy;
        HasTxs = ctx.HasTxs;
        HasHex = ctx.HasHex;
        HeaderIsFull = ctx.HeaderIsFull;
        TxContext = ctx.TxContext;
    }

    /// <summary>Expect presence of json field.</summary>
    public bool? HasJson { get; set; }

    /// <summary>Expect pruning.</summary>
    public bool? IsPruned { get; set; }

    /// <summary>Expect complete model.</summary>
    public bool? IsFull { get; set; }

    /// <summary>Expect confirmations.</summary>
    public bool? IsConfirmed { get; set; }

    /// <summary>Expect miner tx.</summary>
    public bool? IsMinerTx { get; set; }

    /// <summary>Expect from tx pool.</summary>
    public bool? FromGetTxPool { get; set; }

    /// <summary>Expect from binary block.</summary>
    public bool? FromBinaryBlock { get; set; }

    /// <summary>Expect output indices.</summary>
    public bool? HasOutputIndices { get; set; }

    /// <summary>Disable copy tests.</summary>
    public bool? DoNotTestCopy { get; set; }

    /// <summary>Expect txs.</summary>
    public bool? HasTxs { get; set; }

    /// <summary>Expect hex field.</summary>
    public bool? HasHex { get; set; }

    /// <summary>Expect full header.</summary>
    public bool? HeaderIsFull { get; set; }

    /// <summary>Tx context.</summary>
    public ModelTestContext? TxContext { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"pruned={IsPruned}, confirmed={IsConfirmed}, minerTx={IsMinerTx}, fromGetTxPool={FromGetTxPool}, fromBinaryBlock={FromBinaryBlock}, hasOutputIndices={HasOutputIndices}, hasTxs={HasTxs}, hasHex={HasHex}, headerIsFull={HeaderIsFull}";
}

/// <summary>
/// Binary block test context.
/// </summary>
public class BinaryBlockContext : ModelTestContext
{
    /// <summary>Initialize a new binary block test context.</summary>
    public BinaryBlockContext()
    {
        HasHex = false;
        HeaderIsFull = false;
        HasTxs = true;
        TxContext = new ModelTestContext
        {
            IsPruned = false,
            IsConfirmed = true,
            FromGetTxPool = false,
            HasOutputIndices = false,
            FromBinaryBlock = true,
        };
    }
}
