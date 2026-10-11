using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.Daemon.Model;

/// <summary>
/// Models an alternative chain seen by the node.
/// </summary>
public class MoneroAltChain
{
    /// <summary>
    /// Alternative block hashes seen in the alternative chain.
    /// </summary>
    [JsonPropertyName("blockHashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BlockHashes { get; set; }

    /// <summary>
    /// Least significant 64 bits of the cumulative difficulty of the alternative chain, which has 128 bits.
    /// </summary>
    [JsonPropertyName("difficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the cumulative difficulty of the alternative chain.
    /// </summary>
    [JsonPropertyName("difficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyHigh { get; set; }

    /// <summary>
    /// The cumulative difficulty of the alternative chain, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? Difficulty
    {
        get => MoneroUInt128.Combine(DifficultyLow, DifficultyHigh);
        set => (DifficultyLow, DifficultyHigh) = MoneroUInt128.Split(value);
    }

    /// <summary>
    /// Number of blocks in the alternative chain, from its fork in the main chain.
    /// </summary>
    [JsonPropertyName("length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Length { get; set; }

    /// <summary>
    /// Block height reached by alternative chain.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Parent block hash in the main chain.
    /// </summary>
    [JsonPropertyName("mainChainParentBlockHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MainChainParentBlockHash { get; set; }
}