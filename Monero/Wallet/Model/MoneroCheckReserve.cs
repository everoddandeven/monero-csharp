using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of checking a proof of the reserve of a wallet.
/// </summary>
public class MoneroCheckReserve : MoneroCheck
{
    /// <summary>
    /// Total amount that the proof covers, in atomic units.
    /// </summary>
    [JsonPropertyName("totalAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TotalAmount { get; set; }

    /// <summary>
    /// Amount of the proof that is spent by transactions that are not confirmed, in atomic units.
    /// </summary>
    [JsonPropertyName("unconfirmedSpentAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnconfirmedSpentAmount { get; set; }
}
