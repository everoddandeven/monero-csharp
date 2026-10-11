using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of importing key images in a wallet.
/// </summary>
public class MoneroKeyImageImportResult
{
    /// <summary>
    /// Height of the wallet after the import.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }

    /// <summary>
    /// Amount of the imported outputs that are spent, in atomic units.
    /// </summary>
    [JsonPropertyName("spentAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? SpentAmount { get; set; }

    /// <summary>
    /// Amount of the imported outputs that are not spent, in atomic units.
    /// </summary>
    [JsonPropertyName("unspentAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnspentAmount { get; set; }
}
