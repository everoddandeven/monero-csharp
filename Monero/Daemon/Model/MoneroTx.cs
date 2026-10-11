using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// A transaction of the blockchain or of the pool of the daemon, with its inputs and outputs. It is in its <see cref="Block"/>, which is the
/// one that holds it in the chain, or the one without a height that holds the transactions of the pool.
/// </summary>
public class MoneroTx
{
    /// <summary>
    /// The payment id of a transaction that has none: eight bytes of zeros, as hexadecimal.
    /// </summary>
    public static readonly string DefaultPaymentId = "0000000000000000";

    /// <summary>
    /// Initialize a new Monero transaction.
    /// </summary>
    public MoneroTx()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this transaction as a deep copy of the given transaction. The copy is not in a block: the block that holds it sets it.
    /// </summary>
    /// <param name="tx">the transaction to copy.</param>
    public MoneroTx(MoneroTx tx)
    {
        Hash = tx.Hash;
        Version = tx.Version;
        IsMinerTx = tx.IsMinerTx;
        PaymentId = tx.PaymentId;
        Fee = tx.Fee;
        RingSize = tx.RingSize;
        Relay = tx.Relay;
        IsRelayed = tx.IsRelayed;
        IsConfirmed = tx.IsConfirmed;
        InTxPool = tx.InTxPool;
        IsLocked = tx.IsLocked;
        NumConfirmations = tx.NumConfirmations;
        UnlockTime = tx.UnlockTime;
        LastRelayedTimestamp = tx.LastRelayedTimestamp;
        ReceivedTimestamp = tx.ReceivedTimestamp;
        IsDoubleSpendSeen = tx.IsDoubleSpendSeen;
        Key = tx.Key;
        FullHex = tx.FullHex;
        PrunedHex = tx.PrunedHex;
        PrunableHex = tx.PrunableHex;
        PrunableHash = tx.PrunableHash;
        Size = tx.Size;
        Weight = tx.Weight;

        if (tx.Inputs != null)
        {
            Inputs = [.. tx.Inputs.Select(input => input?.Copy()!)];
        }

        if (tx.Outputs != null)
        {
            Outputs = [.. tx.Outputs.Select(output => output?.Copy()!)];
        }

        if (tx.OutputIndices != null)
        {
            OutputIndices = [.. tx.OutputIndices];
        }

        Metadata = tx.Metadata;
        CommonTxSets = tx.CommonTxSets;

        if (tx.Extra != null)
        {
            Extra = [.. tx.Extra];
        }

        RctSignatures = tx.RctSignatures;
        RctSigPrunable = tx.RctSigPrunable;
        IsKeptByBlock = tx.IsKeptByBlock;
        IsFailed = tx.IsFailed;
        LastFailedHeight = tx.LastFailedHeight;
        LastFailedHash = tx.LastFailedHash;
        MaxUsedBlockHeight = tx.MaxUsedBlockHeight;
        MaxUsedBlockHash = tx.MaxUsedBlockHash;

        if (tx.Signatures != null)
        {
            Signatures = [.. tx.Signatures];
        }
    }
    
    /// <summary>
    /// Transaction block. It is not part of the JSON, since the block holds its transactions.
    /// A block sets it when the transactions are assigned to it.
    /// </summary>
    [JsonIgnore]
    public MoneroBlock? Block { get; set; }


    /// <summary>
    /// Get transaction's height.
    /// </summary>
    /// <returns>transaction block height, if confirmed.</returns>
    public ulong? GetHeight()
    {
        return Block?.Height;
    }
    
    /// <summary>
    /// Transaction hash.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; set; }
    
    /// <summary>
    /// Transaction version.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Version { get; set; }
    
    /// <summary>
    /// Indicates if it is a miner tx.
    /// </summary>
    [JsonPropertyName("isMinerTx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsMinerTx { get; set; }
    
    /// <summary>
    /// Transaction payment id
    /// </summary>
    [JsonPropertyName("paymentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentId { get; set; }
    
    /// <summary>
    /// Transaction fee (atomic units).
    /// </summary>
    [JsonPropertyName("fee")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Fee { get; set; }
    
    /// <summary>
    /// Transaction ring size.
    /// </summary>
    [JsonPropertyName("ringSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? RingSize { get; set; }
    
    /// <summary>
    /// Indicates if transaction should be relayed.
    /// </summary>
    [JsonPropertyName("relay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Relay { get; set; }
    
    /// <summary>
    /// Indicates if transaction is relayed to the network.
    /// </summary>
    [JsonPropertyName("isRelayed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRelayed { get; set; }
    
    /// <summary>
    /// Indicates if transaction is confirmed
    /// </summary>
    [JsonPropertyName("isConfirmed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsConfirmed { get; set; }
    
    /// <summary>
    /// Indicates if transaction is in pool.
    /// </summary>
    [JsonPropertyName("inTxPool")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InTxPool { get; set; }
    
    /// <summary>
    /// Indicates if transaction is locked, so its outputs can't be spent yet.
    /// </summary>
    [JsonPropertyName("isLocked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLocked { get; set; }
    
    /// <summary>
    /// Number of blockchain confirmations.
    /// </summary>
    [JsonPropertyName("numConfirmations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumConfirmations { get; set; }
    
    /// <summary>
    /// Unlock time.
    /// </summary>
    [JsonPropertyName("unlockTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnlockTime { get; set; }
    
    /// <summary>
    /// Last time transaction was relayed.
    /// </summary>
    [JsonPropertyName("lastRelayedTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? LastRelayedTimestamp { get; set; }
    
    /// <summary>
    /// Receive timestamp of the transaction.
    /// </summary>
    [JsonPropertyName("receivedTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ReceivedTimestamp { get; set; }
    
    /// <summary>
    /// Indicates if a double spending has occurred.
    /// </summary>
    [JsonPropertyName("isDoubleSpendSeen")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsDoubleSpendSeen { get; set; }
    
    /// <summary>
    /// Transaction private key.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }
    
    /// <summary>
    /// Full transaction details in hexadecimal format.
    /// </summary>
    [JsonPropertyName("fullHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FullHex { get; set; }
    
    /// <summary>
    /// Pruned transaction details in hexadecimal format.
    /// </summary>
    [JsonPropertyName("prunedHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrunedHex { get; set; }
    
    /// <summary>
    /// Prunable transaction details in hexadecimal format.
    /// </summary>
    [JsonPropertyName("prunableHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrunableHex { get; set; }
    
    /// <summary>
    /// Prunable transaction hash.
    /// </summary>
    [JsonPropertyName("prunableHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrunableHash { get; set; }
    
    /// <summary>
    /// Transaction size.
    /// Backward compatibility for <c>Weight</c>.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Size { get; set; }
    
    /// <summary>
    /// Transaction weight.
    /// </summary>
    [JsonPropertyName("weight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Weight { get; set; }
    
    private List<MoneroOutput>? _inputs;
    private List<MoneroOutput>? _outputs;

    /// <summary>
    /// Inputs used in transaction. Assigning the list sets the transaction of each input. A transaction of a wallet overrides it, so that
    /// its inputs are read as wallet outputs.
    /// </summary>
    [JsonPropertyName("inputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual List<MoneroOutput>? Inputs
    {
        get => _inputs;
        set
        {
            _inputs = value;
            SetTx(value);
        }
    }
    
    /// <summary>
    /// Outputs created by transaction. Assigning the list sets the transaction of each output. A transaction of a wallet overrides it, so that
    /// its outputs are read as wallet outputs.
    /// </summary>
    [JsonPropertyName("outputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual List<MoneroOutput>? Outputs
    {
        get => _outputs;
        set
        {
            _outputs = value;
            SetTx(value);
        }
    }

    private void SetTx(List<MoneroOutput>? outputs)
    {
        if (outputs == null) return;
        foreach (var output in outputs)
        {
            if (output != null) output.Tx = this;
        }
    }
    
    /// <summary>
    /// Transaction output indices.
    /// </summary>
    [JsonPropertyName("outputIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? OutputIndices { get; set; }
    
    /// <summary>
    /// Transaction metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Common transaction sets.
    /// </summary>
    [JsonPropertyName("commonTxSets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CommonTxSets { get; set; }
    
    /// <summary>
    /// Extra info, the bytes of the extra field of the transaction.
    /// </summary>
    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<byte>? Extra { get; set; }
    
    /// <summary>
    /// RingCT signatures used in transaction.
    /// </summary>
    [JsonPropertyName("rctSignatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RctSignatures { get; set; }
    
    /// <summary>
    /// Prunable RingCT signature.
    /// </summary>
    [JsonPropertyName("rctSigPrunable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RctSigPrunable { get; set; }
    
    /// <summary>
    /// Indicates if transaction was included in a block at least once.
    /// </summary>
    [JsonPropertyName("isKeptByBlock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsKeptByBlock { get; set; }
    
    /// <summary>
    /// Indicates if transaction validation failed.
    /// </summary>
    [JsonPropertyName("isFailed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsFailed { get; set; }
    
    /// <summary>
    /// Block height at which transaction validation failed.
    /// </summary>
    [JsonPropertyName("lastFailedHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? LastFailedHeight { get; set; }
    
    /// <summary>
    /// Block hash at which transaction validation failed.
    /// </summary>
    [JsonPropertyName("lastFailedHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastFailedHash { get; set; }
    
    /// <summary>
    /// Height of most recent block with an output used in this transaction.
    /// </summary>
    [JsonPropertyName("maxUsedBlockHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? MaxUsedBlockHeight { get; set; }
    
    /// <summary>
    /// Hash of most recent block with an output used in this transaction.
    /// </summary>
    [JsonPropertyName("maxUsedBlockHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MaxUsedBlockHash { get; set; }
    
    /// <summary>
    /// Signatures of transaction signers.
    /// </summary>
    [JsonPropertyName("signatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Signatures { get; set; }

    /// <summary>
    /// Copy current transaction.
    /// </summary>
    /// <returns>a deep copy of the transaction, which is not in a block.</returns>
    public virtual MoneroTx Copy()
    {
        return new MoneroTx(this);
    }
}