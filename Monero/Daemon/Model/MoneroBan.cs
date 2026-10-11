using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Monero banhammer.
/// </summary>
public class MoneroBan
{
    /// <summary>
    /// e.g. 192.168.1.100
    /// </summary>
    [JsonPropertyName("host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Host { get; set; }

    /// <summary>
    /// Integer formatted IP.
    /// </summary>
    [JsonPropertyName("ip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Ip { get; set; }
    
    /// <summary>
    /// Indicates if host is banned.
    /// </summary>
    [JsonPropertyName("isBanned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBanned { get; set; }
    
    /// <summary>
    /// Seconds left until the ban is released.
    /// </summary>
    [JsonPropertyName("seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Seconds { get; set; }
}
