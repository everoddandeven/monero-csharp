using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an integrated address: a standard address with a payment id.
/// </summary>
public class MoneroIntegratedAddress
{
    /// <summary>
    /// Standard address that the integrated address is made of.
    /// </summary>
    [JsonPropertyName("standardAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StandardAddress { get; set; }

    /// <summary>
    /// Payment id that the integrated address is made of, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("paymentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentId { get; set; }

    /// <summary>
    /// The integrated address.
    /// </summary>
    [JsonPropertyName("integratedAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IntegratedAddress { get; set; }
}
