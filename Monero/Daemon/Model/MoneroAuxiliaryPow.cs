using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models an auxiliary proof of work, which is merge mined with the blocks of Monero.
/// </summary>
public class MoneroAuxiliaryPow
{
    /// <summary>
    /// Id of the auxiliary chain, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Hash of the block of the auxiliary chain, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; set; }
}
