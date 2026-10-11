using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an outgoing transfer of funds from a wallet.
/// </summary>
public class MoneroOutgoingTransfer : MoneroTransfer
{
    /// <summary>
    /// Initialize a new outgoing transfer.
    /// </summary>
    public MoneroOutgoingTransfer()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this transfer as a deep copy of the given transfer. The transaction is not copied, and the copy has none.
    /// </summary>
    /// <param name="transfer">the transfer to copy.</param>
    public MoneroOutgoingTransfer(MoneroOutgoingTransfer transfer) : base(transfer)
    {
        if (transfer.SubaddressIndices != null)
        {
            SubaddressIndices = [.. transfer.SubaddressIndices];
        }

        if (transfer.Addresses != null)
        {
            Addresses = [.. transfer.Addresses];
        }

        if (transfer.Destinations != null)
        {
            Destinations = [.. transfer.Destinations.Select(destination => destination?.Copy()!)];
        }
    }

    /// <summary>
    /// Copy this transfer.
    /// </summary>
    /// <returns>a deep copy of the transfer, with no transaction.</returns>
    public override MoneroOutgoingTransfer Copy()
    {
        return new MoneroOutgoingTransfer(this);
    }

    /// <inheritdoc />
    public override bool? GetIsIncoming()
    {
        return false;
    }

    /// <summary>
    /// Indices of the subaddresses of the account that the funds are sent from.
    /// </summary>
    [JsonPropertyName("subaddressIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<uint>? SubaddressIndices { get; set; }

    /// <summary>
    /// Addresses that the funds are sent from.
    /// </summary>
    [JsonPropertyName("addresses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Addresses { get; set; }

    /// <summary>
    /// Destinations that the funds are sent to. A wallet that only has the keys doesn't know them.
    /// </summary>
    [JsonPropertyName("destinations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroDestination>? Destinations { get; set; }
}
