using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Monero output distribution entry.
/// </summary>
public class MoneroOutputDistributionEntry
{
    /// <summary>
    /// Outputs amount (atomic units).
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Amount { get; set; }

    /// <summary>
    /// The total number of outputs of amount in the chain before,
    /// not including, the block at <c>StartHeight</c>.
    /// </summary>
    [JsonPropertyName("base")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Base { get; set; }
    
    /// <summary>
    /// Outputs distribution.
    /// </summary>
    [JsonPropertyName("distribution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? Distribution { get; set; }
    
    /// <summary>
    /// Block start height. Note that this is not necessarily equal to the requested height,
    /// especially for amount equal to <c>0</c>.
    /// </summary>
    [JsonPropertyName("startHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? StartHeight { get; set; }
}