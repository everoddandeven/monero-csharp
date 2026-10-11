using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Configures a query for the transfers of a wallet: all of them are returned, except the ones that don't meet the criteria that are set.
/// <para>
/// The JSON is the one that monero-c reads. The query of the transaction of the transfers is in <see cref="TxQuery"/>, as <c>txQuery</c>,
/// and it must not be a transaction query that has this query as its <see cref="MoneroTxQuery.TransferQuery"/>, since each would write the
/// other.
/// </para>
/// </summary>
public class MoneroTransferQuery : MoneroTransfer
{
    /// <summary>
    /// Initialize a new transfer query, which matches every transfer.
    /// </summary>
    public MoneroTransferQuery()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this query as a deep copy of the given query. The query of the transaction is copied too.
    /// </summary>
    /// <param name="query">the query to copy.</param>
    public MoneroTransferQuery(MoneroTransferQuery query) : base(query)
    {
        IsIncoming = query.IsIncoming;
        Address = query.Address;
        SubaddressIndex = query.SubaddressIndex;
        HasDestinations = query.HasDestinations;

        if (query.Addresses != null)
        {
            Addresses = [.. query.Addresses];
        }

        if (query.SubaddressIndices != null)
        {
            SubaddressIndices = [.. query.SubaddressIndices];
        }

        if (query.Destinations != null)
        {
            Destinations = [.. query.Destinations.Select(destination => destination?.Copy()!)];
        }

        TxQuery = query.TxQuery?.Copy();
    }

    /// <summary>
    /// Copy this query.
    /// </summary>
    /// <returns>a deep copy of the query, with no transaction.</returns>
    public override MoneroTransferQuery Copy()
    {
        return new MoneroTransferQuery(this);
    }

    /// <inheritdoc />
    public override bool? GetIsIncoming()
    {
        return IsIncoming;
    }

    /// <summary>
    /// Whether to match the incoming transfers, <c>false</c> to match the outgoing one, or <c>null</c> to match both.
    /// </summary>
    [JsonPropertyName("isIncoming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsIncoming { get; set; }

    /// <summary>
    /// Address that receives the incoming transfers to match.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Addresses that the outgoing transfers to match are sent from.
    /// </summary>
    [JsonPropertyName("addresses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Addresses { get; set; }

    /// <summary>
    /// Index of the subaddress that receives the incoming transfers to match.
    /// </summary>
    [JsonPropertyName("subaddressIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? SubaddressIndex { get; set; }

    /// <summary>
    /// Indices of the subaddresses that the outgoing transfers to match are sent from.
    /// </summary>
    [JsonPropertyName("subaddressIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<uint>? SubaddressIndices { get; set; }

    /// <summary>
    /// Destinations of the outgoing transfers to match.
    /// </summary>
    [JsonPropertyName("destinations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroDestination>? Destinations { get; set; }

    /// <summary>
    /// Whether to match the outgoing transfers that have destinations, or <c>false</c> to match the ones that have none.
    /// </summary>
    [JsonPropertyName("hasDestinations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasDestinations { get; set; }

    /// <summary>
    /// Query that the transactions of the transfers have to meet.
    /// </summary>
    [JsonPropertyName("txQuery")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroTxQuery? TxQuery { get; set; }
}
