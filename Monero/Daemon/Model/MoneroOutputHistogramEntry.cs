using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Entry in a Monero output histogram (see get_output_histogram of Daemon RPC documentation).
/// </summary>
public class MoneroOutputHistogramEntry
{
    /// <summary>
    /// Output amount (atomic-units).
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Amount { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("numInstances")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumInstances { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("unlockedInstances")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnlockedInstances { get; set; }
    
    /// <summary>
    /// Number of instances of the amount that were created recently.
    /// </summary>
    [JsonPropertyName("recentInstances")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? RecentInstances { get; set; }
}