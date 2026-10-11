using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models daemon synchronization information.
/// </summary>
public class MoneroDaemonSyncInfo : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Current node block height.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Peers connected to the daemon.
    /// </summary>
    [JsonPropertyName("peers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroPeer>? Peers { get; set; }
    
    /// <summary>
    /// Connection spans which the node is currently pulling sync data from.
    /// Empty if node is fully synced.
    /// </summary>
    [JsonPropertyName("spans")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroConnectionSpan>? Spans { get; set; }

    /// <summary>
    /// Target height which the node is syncing from. <c>0</c> if node is fully synced.
    /// </summary>
    [JsonPropertyName("targetHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TargetHeight { get; set; }
    
    /// <summary>
    /// The next pruning seed needed for pruned sync.
    /// </summary>
    [JsonPropertyName("nextNeededPruningSeed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NextNeededPruningSeed { get; set; }
    
    /// <summary>
    /// Overview of current block queue where each character in the string represents a block set in the queue.
    /// <c>.</c> -> Requested but not received.
    /// <c>o</c> -> Set received.
    /// <c>m</c> -> Received set that matches the next blocks needed.
    /// </summary>
    [JsonPropertyName("overview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Overview { get; set; }
    
}