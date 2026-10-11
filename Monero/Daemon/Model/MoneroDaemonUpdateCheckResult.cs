using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of checking for a daemon update.
/// </summary>
public class MoneroDaemonUpdateCheckResult
{
    /// <summary>
    /// Indicates if a newer version of daemon is available to download.
    /// </summary>
    [JsonPropertyName("isUpdateAvailable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsUpdateAvailable { get; set; }
    
    /// <summary>
    /// Daemon version available to download.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }
    
    /// <summary>
    /// Hash used to verify download.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; set; }

    /// <summary>
    /// Uri used to download update.
    /// </summary>
    [JsonPropertyName("autoUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AutoUri { get; set; }

    /// <summary>
    /// User-defined uri to download update.
    /// </summary>
    [JsonPropertyName("userUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

    public string? UserUri { get; set; }
    
}