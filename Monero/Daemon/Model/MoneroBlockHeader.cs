using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a Monero block header which contains information about the block.
/// </summary>
public class MoneroBlockHeader
{
    /// <summary>
    /// Initialize a new block header.
    /// </summary>
    public MoneroBlockHeader()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this block header as a copy of the given one.
    /// </summary>
    /// <param name="header">the block header to copy.</param>
    public MoneroBlockHeader(MoneroBlockHeader header)
    {
        Hash = header.Hash;
        Height = header.Height;
        Timestamp = header.Timestamp;
        Size = header.Size;
        Weight = header.Weight;
        LongTermWeight = header.LongTermWeight;
        Depth = header.Depth;
        DifficultyLow = header.DifficultyLow;
        DifficultyHigh = header.DifficultyHigh;
        CumulativeDifficultyLow = header.CumulativeDifficultyLow;
        CumulativeDifficultyHigh = header.CumulativeDifficultyHigh;
        MajorVersion = header.MajorVersion;
        MinorVersion = header.MinorVersion;
        Nonce = header.Nonce;
        MinerTxHash = header.MinerTxHash;
        NumTxs = header.NumTxs;
        OrphanStatus = header.OrphanStatus;
        PrevHash = header.PrevHash;
        Reward = header.Reward;
        PowHash = header.PowHash;
    }

    /// <summary>
    /// Copy this block header.
    /// </summary>
    /// <returns>a copy of the block header.</returns>
    public virtual MoneroBlockHeader Copy()
    {
        return new MoneroBlockHeader(this);
    }

    /// <summary>
    /// Block's hash.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; set; }
    
    /// <summary>
    /// Block's height.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Block's timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Timestamp { get; set; }
    
    /// <summary>
    /// Block size.
    /// </summary>
    /// <returns></returns>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Size { get; set; }
    
    /// <summary>
    /// Block weight.
    /// </summary>
    [JsonPropertyName("weight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Weight { get; set; }    
    
    /// <summary>
    /// Long term block weight.
    /// </summary>
    [JsonPropertyName("longTermWeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? LongTermWeight { get; set; }
    
    /// <summary>
    /// Block current chain depth.
    /// </summary>
    [JsonPropertyName("depth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Depth { get; set; }
    
    /// <summary>
    /// Least significant 64 bits of the block difficulty, which has 128 bits.
    /// </summary>
    [JsonPropertyName("difficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the block difficulty.
    /// </summary>
    [JsonPropertyName("difficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyHigh { get; set; }

    /// <summary>
    /// Block difficulty, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? Difficulty
    {
        get => MoneroUInt128.Combine(DifficultyLow, DifficultyHigh);
        set => (DifficultyLow, DifficultyHigh) = MoneroUInt128.Split(value);
    }

    /// <summary>
    /// Least significant 64 bits of the block's cumulative difficulty, which has 128 bits.
    /// </summary>
    [JsonPropertyName("cumulativeDifficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CumulativeDifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the block's cumulative difficulty.
    /// </summary>
    [JsonPropertyName("cumulativeDifficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CumulativeDifficultyHigh { get; set; }

    /// <summary>
    /// Block's cumulative difficulty, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? CumulativeDifficulty
    {
        get => MoneroUInt128.Combine(CumulativeDifficultyLow, CumulativeDifficultyHigh);
        set => (CumulativeDifficultyLow, CumulativeDifficultyHigh) = MoneroUInt128.Split(value);
    }

    /// <summary>
    /// Block's major version.
    /// </summary>
    [JsonPropertyName("majorVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? MajorVersion { get; set; }
    
    /// <summary>
    /// Block's minor version.
    /// </summary>
    [JsonPropertyName("minorVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? MinorVersion { get; set; }

    /// <summary>
    /// Block's nonce.
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Nonce { get; set; }

    /// <summary>
    /// Block's miner tx hash.
    /// </summary>
    [JsonPropertyName("minerTxHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MinerTxHash { get; set; }

    /// <summary>
    /// Block's txs count.
    /// </summary>
    [JsonPropertyName("numTxs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? NumTxs { get; set; }
    
    /// <summary>
    /// Indicates if block is orphan.
    /// </summary>
    [JsonPropertyName("orphanStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OrphanStatus { get; set; }

    /// <summary>
    /// Previous block hash.
    /// </summary>
    [JsonPropertyName("prevHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrevHash { get; set; }

    /// <summary>
    /// Block's reward (atomic units).
    /// </summary>
    [JsonPropertyName("reward")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Reward { get; set; }

    /// <summary>
    /// Block's PoW hash.
    /// </summary>
    [JsonPropertyName("powHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PowHash { get; set; }
    
}