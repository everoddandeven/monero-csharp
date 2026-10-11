using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of a step of the creation of a multisig wallet: the address of the wallet when the participants are done, and the
/// multisig hex to share with them for the next step.
/// </summary>
public class MoneroMultisigInitResult
{
    /// <summary>
    /// Address of the multisig wallet.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Multisig hex to share with the other participants.
    /// </summary>
    [JsonPropertyName("multisigHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MultisigHex { get; set; }
}
