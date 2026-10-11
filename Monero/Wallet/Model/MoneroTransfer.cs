using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Base class of the transfers of funds to or from a wallet: the incoming transfers, the outgoing transfer and the queries of them. A
/// transfer is in its transaction, which is in its block.
/// </summary>
public abstract class MoneroTransfer
{
    /// <summary>
    /// Initialize a new transfer.
    /// </summary>
    protected MoneroTransfer()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this transfer as a copy of the given transfer. The transaction is not copied, and the copy has none.
    /// </summary>
    /// <param name="transfer">the transfer to copy.</param>
    protected MoneroTransfer(MoneroTransfer transfer)
    {
        Amount = transfer.Amount;
        AccountIndex = transfer.AccountIndex;
    }

    /// <summary>
    /// Copy this transfer.
    /// </summary>
    /// <returns>a deep copy of the transfer, with no transaction.</returns>
    public abstract MoneroTransfer Copy();

    /// <summary>
    /// Transaction that the transfer is in. It is set when the transaction gets the transfer, and it is not in the JSON.
    /// </summary>
    [JsonIgnore]
    public MoneroTxWallet? Tx { get; set; }

    /// <summary>
    /// Amount of the transfer, in atomic units.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Amount { get; set; }

    /// <summary>
    /// Index of the account that sends or receives the transfer.
    /// </summary>
    [JsonPropertyName("accountIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? AccountIndex { get; set; }

    /// <summary>
    /// Tell whether the transfer is incoming.
    /// </summary>
    /// <returns><c>true</c> for an incoming transfer, <c>false</c> for an outgoing one, and for a query what it asks for, or <c>null</c> for
    /// both.</returns>
    public abstract bool? GetIsIncoming();

    /// <summary>
    /// Tell whether the transfer is outgoing.
    /// </summary>
    /// <returns>the opposite of <see cref="GetIsIncoming"/>, or <c>null</c> if that is.</returns>
    public bool? GetIsOutgoing()
    {
        var incoming = GetIsIncoming();
        return incoming == null ? null : !incoming;
    }
}
