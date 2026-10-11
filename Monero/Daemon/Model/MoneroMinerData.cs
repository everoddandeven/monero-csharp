using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the data that a miner needs to build a block.
/// </summary>
public class MoneroMinerData : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Major version of the next block.
    /// </summary>
    [JsonPropertyName("majorVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? MajorVersion { get; set; }

    /// <summary>
    /// Height of the next block.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }

    /// <summary>
    /// Hash of the top block of the chain.
    /// </summary>
    [JsonPropertyName("prevHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrevHash { get; set; }

    /// <summary>
    /// Hash of the block whose hash seeds the proof of work of the next block.
    /// </summary>
    [JsonPropertyName("seedHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeedHash { get; set; }

    /// <summary>
    /// Difficulty of the next block, in hexadecimal format as the daemon writes it.
    /// </summary>
    [JsonPropertyName("difficulty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Difficulty { get; set; }

    /// <summary>
    /// Median weight of the last blocks.
    /// </summary>
    [JsonPropertyName("medianWeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? MedianWeight { get; set; }

    /// <summary>
    /// Coins that were emitted so far (atomic units).
    /// </summary>
    [JsonPropertyName("alreadyGeneratedCoins")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? AlreadyGeneratedCoins { get; set; }

    /// <summary>
    /// The transactions of the pool that a block can take, with their fees and weights.
    /// </summary>
    [JsonPropertyName("txPoolBacklog")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroTx>? TxPoolBacklog { get; set; }
}
