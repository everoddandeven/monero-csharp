using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of checking a transaction with its key, or with a proof of it.
/// </summary>
public class MoneroCheckTx : MoneroCheck
{
    /// <summary>
    /// Whether the transaction is in the pool of the daemon.
    /// </summary>
    [JsonPropertyName("inTxPool")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InTxPool { get; set; }

    /// <summary>
    /// Number of confirmations of the transaction.
    /// </summary>
    [JsonPropertyName("numConfirmations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumConfirmations { get; set; }

    /// <summary>
    /// Amount that the address received in the transaction, in atomic units.
    /// </summary>
    [JsonPropertyName("receivedAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ReceivedAmount { get; set; }
}
