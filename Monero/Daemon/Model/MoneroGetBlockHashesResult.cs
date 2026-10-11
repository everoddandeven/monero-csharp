using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of getting block hashes.
/// </summary>
public class MoneroGetBlockHashesResult
{
    /// <summary>
    /// Height of the first hash.
    /// </summary>
    [JsonPropertyName("startHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? StartHeight { get; set; }

    /// <summary>
    /// Height of the chain of the daemon when it answered.
    /// </summary>
    [JsonPropertyName("currentHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CurrentHeight { get; set; }

    /// <summary>
    /// The hashes of the blocks, from the start height.
    /// </summary>
    [JsonPropertyName("hashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Hashes { get; set; }
}
