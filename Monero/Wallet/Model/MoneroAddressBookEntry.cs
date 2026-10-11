using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an entry of the address book of a wallet.
/// </summary>
public class MoneroAddressBookEntry
{
    /// <summary>
    /// Initialize a new address book entry.
    /// </summary>
    public MoneroAddressBookEntry()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new address book entry.
    /// </summary>
    /// <param name="address">the address of the entry.</param>
    /// <param name="description">the description of the entry.</param>
    public MoneroAddressBookEntry(string address, string? description = null)
    {
        Address = address;
        Description = description;
    }

    /// <summary>
    /// Index of the entry in the address book.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Index { get; set; }

    /// <summary>
    /// Address of the entry.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Description of the entry.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Payment id of the entry, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("paymentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentId { get; set; }
}
