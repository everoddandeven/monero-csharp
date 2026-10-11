using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of the calls that make transactions: <c>{"txSets":[...]}</c>. The calls that sweep can make several sets, and the other
/// calls make one.
/// </summary>
public class MoneroTxSetsResult
{
    /// <summary>
    /// Sets of the transactions that the wallet made.
    /// </summary>
    [JsonPropertyName("txSets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroTxSet>? TxSets { get; set; }

    /// <summary>
    /// Get the transactions of the sets.
    /// </summary>
    /// <returns>the transactions, in the order of the sets. It is a new list.</returns>
    public List<MoneroTxWallet> GetTxs()
    {
        var txs = new List<MoneroTxWallet>();
        foreach (var txSet in TxSets ?? [])
        {
            if (txSet?.Txs != null) txs.AddRange(txSet.Txs.Where(tx => tx != null));
        }

        return txs;
    }
}
