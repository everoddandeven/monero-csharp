using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of getting blocks by hash.
/// </summary>
public class MoneroGetBlocksByHashResult
{
    /// <summary>
    /// Height of the chain of the daemon when it answered.
    /// </summary>
    [JsonPropertyName("currentHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CurrentHeight { get; set; }

    /// <summary>
    /// The blocks that were found.
    /// </summary>
    [JsonPropertyName("blocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroBlock>? Blocks { get; set; }
}
