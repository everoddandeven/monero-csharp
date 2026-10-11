using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the blocks that hold the results of a call that returns transactions: <c>{"blocks":[...]}</c>. A transaction is in its block,
/// which has its height and its timestamp. The transactions that no block holds yet, in the pool, are in one block that has no height.
/// </summary>
public class MoneroBlocksResult
{
    /// <summary>
    /// The blocks with the transactions of the result.
    /// </summary>
    [JsonPropertyName("blocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroBlock>? Blocks { get; set; }

    /// <summary>
    /// Get the transactions of the result, in the order of the blocks. Each transaction knows its block.
    /// The order is not the one of a query by hashes: sort them by the hashes that were asked for.
    /// </summary>
    /// <returns>the transactions of all the blocks.</returns>
    public List<MoneroTx> GetTxs()
    {
        var txs = new List<MoneroTx>();
        if (Blocks == null) return txs;

        foreach (var block in Blocks)
        {
            if (block?.Txs == null) continue;
            txs.AddRange(block.Txs.Where(tx => tx != null));
        }

        return txs;
    }
}
