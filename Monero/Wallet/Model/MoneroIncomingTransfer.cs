using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an incoming transfer of funds to a wallet.
/// </summary>
public class MoneroIncomingTransfer : MoneroTransfer
{
    /// <summary>
    /// Initialize a new incoming transfer.
    /// </summary>
    public MoneroIncomingTransfer()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this transfer as a copy of the given transfer. The transaction is not copied, and the copy has none.
    /// </summary>
    /// <param name="transfer">the transfer to copy.</param>
    public MoneroIncomingTransfer(MoneroIncomingTransfer transfer) : base(transfer)
    {
        SubaddressIndex = transfer.SubaddressIndex;
        Address = transfer.Address;
        NumSuggestedConfirmations = transfer.NumSuggestedConfirmations;
    }

    /// <summary>
    /// Copy this transfer.
    /// </summary>
    /// <returns>a copy of the transfer, with no transaction.</returns>
    public override MoneroIncomingTransfer Copy()
    {
        return new MoneroIncomingTransfer(this);
    }

    /// <inheritdoc />
    public override bool? GetIsIncoming()
    {
        return true;
    }

    /// <summary>
    /// Index of the subaddress that receives the transfer, in its account.
    /// </summary>
    [JsonPropertyName("subaddressIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? SubaddressIndex { get; set; }

    /// <summary>
    /// Address that receives the transfer.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Number of confirmations that the transfer needs before it is considered safe.
    /// </summary>
    [JsonPropertyName("numSuggestedConfirmations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumSuggestedConfirmations { get; set; }
}
