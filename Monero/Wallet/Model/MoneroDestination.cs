using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models a destination of an outgoing transfer: an address and an amount.
/// </summary>
public class MoneroDestination
{
    /// <summary>
    /// Initialize a new destination.
    /// </summary>
    public MoneroDestination()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new destination.
    /// </summary>
    /// <param name="address">the address to send to.</param>
    /// <param name="amount">the amount to send, in atomic units, or <c>null</c> for none.</param>
    public MoneroDestination(string address, ulong? amount = null)
    {
        Address = address;
        Amount = amount;
    }

    /// <summary>
    /// Construct this destination as a copy of the given destination.
    /// </summary>
    /// <param name="destination">the destination to copy.</param>
    public MoneroDestination(MoneroDestination destination)
    {
        Address = destination.Address;
        Amount = destination.Amount;
    }

    /// <summary>
    /// Copy this destination.
    /// </summary>
    /// <returns>a copy of the destination.</returns>
    public virtual MoneroDestination Copy()
    {
        return new MoneroDestination(this);
    }

    /// <summary>
    /// Address to send to.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Amount to send, in atomic units.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Amount { get; set; }
}
