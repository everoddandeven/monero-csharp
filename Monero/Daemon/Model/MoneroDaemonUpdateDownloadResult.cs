using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of downloading an update.
/// </summary>
public class MoneroDaemonUpdateDownloadResult : MoneroDaemonUpdateCheckResult
{
    /// <summary>
    /// Path used to download update.
    /// </summary>
    [JsonPropertyName("downloadPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DownloadPath { get; set; }
}