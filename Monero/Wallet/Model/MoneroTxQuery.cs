using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Configures a query for the transactions of a wallet: all of them are returned, except the ones that don't meet the criteria that are set.
/// It has every field of a transaction of a wallet to match, and the ones of its own.
/// <para>
/// The JSON is the one that monero-c reads. It can hold the queries of the transfers and of the outputs of the transactions, but not a
/// transfer query or an output query that has this query as its <c>TxQuery</c>, since each would write the other.
/// </para>
/// </summary>
public class MoneroTxQuery : MoneroTxWallet
{
    /// <summary>
    /// Initialize a new transaction query, which matches every transaction.
    /// </summary>
    public MoneroTxQuery()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this query as a deep copy of the given query. The queries of the transfers and of the outputs are copied too.
    /// </summary>
    /// <param name="query">the query to copy.</param>
    public MoneroTxQuery(MoneroTxQuery query) : base(query)
    {
        HasPaymentId = query.HasPaymentId;
        Height = query.Height;
        MinHeight = query.MinHeight;
        MaxHeight = query.MaxHeight;
        IncludeOutputs = query.IncludeOutputs;

        if (query.Hashes != null)
        {
            Hashes = [.. query.Hashes];
        }

        if (query.PaymentIds != null)
        {
            PaymentIds = [.. query.PaymentIds];
        }

        TransferQuery = query.TransferQuery?.Copy();
        InputQuery = query.InputQuery?.Copy();
        OutputQuery = query.OutputQuery?.Copy();
    }

    /// <summary>
    /// Copy this query.
    /// </summary>
    /// <returns>a deep copy of the query.</returns>
    public override MoneroTxQuery Copy()
    {
        return new MoneroTxQuery(this);
    }

    /// <summary>
    /// Hashes of the transactions to match.
    /// </summary>
    [JsonPropertyName("hashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Hashes { get; set; }

    /// <summary>
    /// Whether to match the transactions that have a payment id, or <c>false</c> to match the ones that have none.
    /// </summary>
    [JsonPropertyName("hasPaymentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasPaymentId { get; set; }

    /// <summary>
    /// Payment ids of the transactions to match.
    /// </summary>
    [JsonPropertyName("paymentIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? PaymentIds { get; set; }

    /// <summary>
    /// Height of the block of the transactions to match.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }

    /// <summary>
    /// Minimum height of the block of the transactions to match.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? MinHeight { get; set; }

    /// <summary>
    /// Maximum height of the block of the transactions to match.
    /// </summary>
    [JsonPropertyName("maxHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? MaxHeight { get; set; }

    /// <summary>
    /// Whether the transactions that match have their outputs.
    /// </summary>
    [JsonPropertyName("includeOutputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeOutputs { get; set; }

    /// <summary>
    /// Query that the transfers of the transactions to match have to meet.
    /// </summary>
    [JsonPropertyName("transferQuery")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroTransferQuery? TransferQuery { get; set; }

    /// <summary>
    /// Query that the inputs of the transactions to match have to meet.
    /// </summary>
    [JsonPropertyName("inputQuery")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroOutputQuery? InputQuery { get; set; }

    /// <summary>
    /// Query that the outputs of the transactions to match have to meet.
    /// </summary>
    [JsonPropertyName("outputQuery")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroOutputQuery? OutputQuery { get; set; }
}
