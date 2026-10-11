using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of generating blocks, which a daemon in regtest mode does.
/// </summary>
public class MoneroGenerateBlocksResult
{
    /// <summary>
    /// Height of the chain after the blocks that were generated.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }

    /// <summary>
    /// Hashes of the blocks that were generated.
    /// </summary>
    [JsonPropertyName("blockHashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BlockHashes { get; set; }
}
