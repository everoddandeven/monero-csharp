using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of verifying the signature of a message.
/// </summary>
public class MoneroMessageSignatureResult
{
    /// <summary>
    /// Whether the signature is valid for the message and the address.
    /// </summary>
    [JsonPropertyName("isGood")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsGood { get; set; }

    /// <summary>
    /// Whether the signature is in the old format.
    /// </summary>
    [JsonPropertyName("isOld")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOld { get; set; }

    /// <summary>
    /// Version of the format of the signature.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Version { get; set; }

    /// <summary>
    /// The key that signed the message.
    /// </summary>
    [JsonPropertyName("signatureType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroMessageSignatureType? SignatureType { get; set; }
}
