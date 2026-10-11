using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Base class of the results of checking a proof of a transaction or of a reserve.
/// </summary>
public class MoneroCheck
{
    /// <summary>
    /// Whether the proof is valid.
    /// </summary>
    [JsonPropertyName("isGood")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsGood { get; set; }
}
