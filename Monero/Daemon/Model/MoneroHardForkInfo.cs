using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models information regarding hard fork voting and readiness.
/// </summary>
public class MoneroHardForkInfo : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Block height at which hard fork would be enabled if voted in.
    /// </summary>
    [JsonPropertyName("earliestHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? EarliestHeight { get; set; }
    
    /// <summary>
    /// Indicates if hard fork is enforced.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsEnabled { get; set; }
    
    /// <summary>
    /// Current hard fork state.
    /// <c>0</c> (There is likely a hard fork),
    /// <c>1</c> (An update is needed to fork properly),
    /// <c>2</c> (Everything looks good).
    /// </summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? State { get; set; }
    
    /// <summary>
    /// Minimum percent of votes to trigger hard fork. Default is <c>80</c>.
    /// </summary>
    [JsonPropertyName("threshold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Threshold { get; set; }
    
    /// <summary>
    /// The major block version for the fork.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Version { get; set; }
    
    /// <summary>
    /// Number of votes towards hard fork.
    /// </summary>
    [JsonPropertyName("numVotes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumVotes { get; set; }
    
    /// <summary>
    /// Number of blocks over which current votes are cast. Default is 10080 blocks.
    /// </summary>
    [JsonPropertyName("window")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Window { get; set; }
    
    /// <summary>
    /// Hard fork voting status.
    /// </summary>
    [JsonPropertyName("voting")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Voting { get; set; }
}