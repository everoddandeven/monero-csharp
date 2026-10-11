using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Result of pruning the blockchain.
/// </summary>
public class MoneroPruneResult
{
    /// <summary>
    /// Indicates if daemon blockchain is pruned.
    /// </summary>
    [JsonPropertyName("isPruned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsPruned { get; set; }
    
    /// <summary>
    /// Block height at which pruning began.
    /// </summary>
    [JsonPropertyName("pruningSeed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PruningSeed { get; set; }

}