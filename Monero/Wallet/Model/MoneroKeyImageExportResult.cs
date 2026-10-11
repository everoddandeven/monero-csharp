using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of exporting the key images of a wallet.
/// </summary>
public class MoneroKeyImageExportResult
{
    /// <summary>
    /// Index of the first key image among the outputs of the wallet.
    /// </summary>
    [JsonPropertyName("offset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Offset { get; set; }

    /// <summary>
    /// The exported key images, with their signatures.
    /// </summary>
    [JsonPropertyName("keyImages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroKeyImage>? KeyImages { get; set; }
}
