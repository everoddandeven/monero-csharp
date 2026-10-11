using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of signing multisig transactions.
/// </summary>
public class MoneroMultisigSignResult
{
    /// <summary>
    /// The multisig transaction hex, signed.
    /// </summary>
    [JsonPropertyName("signedMultisigTxHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignedMultisigTxHex { get; set; }

    /// <summary>
    /// Hashes of the signed transactions.
    /// </summary>
    [JsonPropertyName("txHashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? TxHashes { get; set; }
}
