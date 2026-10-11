using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models information about a multisig wallet. The threshold and the number of participants of a wallet that is not multisig are not
/// meaningful: monero-cpp doesn't set them.
/// </summary>
public class MoneroMultisigInfo
{
    /// <summary>
    /// Whether the wallet is multisig.
    /// </summary>
    [JsonPropertyName("isMultisig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsMultisig { get; set; }

    /// <summary>
    /// Whether the multisig wallet is ready, which is when all of its participants joined.
    /// </summary>
    [JsonPropertyName("isReady")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsReady { get; set; }

    /// <summary>
    /// Number of participants that have to sign a transaction.
    /// </summary>
    [JsonPropertyName("threshold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Threshold { get; set; }

    /// <summary>
    /// Number of participants of the wallet.
    /// </summary>
    [JsonPropertyName("numParticipants")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? NumParticipants { get; set; }
}
