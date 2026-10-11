using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models daemon mining status.
/// </summary>
public class MoneroMiningStatus
{
    /// <summary>
    /// Indicates if daemon mining is enabled.
    /// </summary>
    [JsonPropertyName("isActive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Indicates if the mining is running in background.
    /// </summary>
    [JsonPropertyName("isBackground")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBackground { get; set; }
    
    /// <summary>
    /// Wallet address daemon is mining to.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }
    
    /// <summary>
    /// Mining power in hashes per seconds.
    /// </summary>
    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Speed { get; set; }
    
    /// <summary>
    /// Number of running mining threads.
    /// </summary>
    [JsonPropertyName("numThreads")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumThreads { get; set; }
}
