using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.Daemon.Model;

/// <summary>
/// Monero block template to mine.
/// </summary>
public class MoneroBlockTemplate
{
    /// <summary>
    /// Blob on which to try to mine a new block.
    /// </summary>
    [JsonPropertyName("blockTemplateBlob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BlockTemplateBlob { get; set; }
    
    /// <summary>
    /// Blob on which to try to find a valid nonce.
    /// </summary>
    [JsonPropertyName("blockHashingBlob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BlockHashingBlob { get; set; }
    
    /// <summary>
    /// Least significant 64 bits of the network difficulty, which has 128 bits.
    /// </summary>
    [JsonPropertyName("difficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the network difficulty.
    /// </summary>
    [JsonPropertyName("difficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyHigh { get; set; }

    /// <summary>
    /// The network difficulty, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? Difficulty
    {
        get => MoneroUInt128.Combine(DifficultyLow, DifficultyHigh);
        set => (DifficultyLow, DifficultyHigh) = MoneroUInt128.Split(value);
    }
    
    /// <summary>
    /// Coinbase reward expected to be received if block is successfully mined.
    /// </summary>
    [JsonPropertyName("expectedReward")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ExpectedReward { get; set; }
    
    /// <summary>
    /// Height on which to mine.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Hash of the most recent block on which to mine the next block.
    /// </summary>
    [JsonPropertyName("prevHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrevHash { get; set; }
    
    /// <summary>
    /// Reserved offset.
    /// </summary>
    [JsonPropertyName("reservedOffset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ReservedOffset { get; set; }
    
    /// <summary>
    /// Height of block to use as seed for Random-X proof-of-work.
    /// </summary>
    [JsonPropertyName("seedHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? SeedHeight { get; set; }
    
    /// <summary>
    /// Hash of block to use as seed for Random-X proof-of-work.
    /// </summary>
    [JsonPropertyName("seedHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeedHash { get; set; }
    
    /// <summary>
    /// Hash of the next block to use as seed for Random-X proof-of-work.
    /// </summary>
    [JsonPropertyName("nextSeedHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextSeedHash { get; set; }

}