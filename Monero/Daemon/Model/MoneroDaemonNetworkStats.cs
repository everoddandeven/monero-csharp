using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the network statistics of the daemon since it started.
/// </summary>
public class MoneroDaemonNetworkStats : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Timestamp at which the daemon started, in seconds.
    /// </summary>
    [JsonPropertyName("startTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? StartTime { get; set; }

    /// <summary>
    /// Number of packets that the daemon received.
    /// </summary>
    [JsonPropertyName("totalPacketsIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TotalPacketsIn { get; set; }

    /// <summary>
    /// Number of bytes that the daemon received.
    /// </summary>
    [JsonPropertyName("totalBytesIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TotalBytesIn { get; set; }

    /// <summary>
    /// Number of packets that the daemon sent.
    /// </summary>
    [JsonPropertyName("totalPacketsOut")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TotalPacketsOut { get; set; }

    /// <summary>
    /// Number of bytes that the daemon sent.
    /// </summary>
    [JsonPropertyName("totalBytesOut")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TotalBytesOut { get; set; }
}
