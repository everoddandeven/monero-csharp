using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Defines a Monero version.
/// </summary>
public class MoneroVersion
{
    /// <summary>
    /// Daemon version number.
    /// </summary>
    [JsonPropertyName("number")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Number { get; set; }
    
    /// <summary>
    /// Indicates if the daemon version corresponds to an official tagged release.
    /// </summary>
    [JsonPropertyName("isRelease")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRelease { get; set; }
}