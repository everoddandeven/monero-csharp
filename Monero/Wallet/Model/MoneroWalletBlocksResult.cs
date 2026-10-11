using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of the calls that get the transactions, the transfers and the outputs of a wallet: <c>{"blocks":[...]}</c>. Each
/// transaction is in its block, and each transfer and each output is in its transaction. The block without a height holds the transactions
/// that are not in a block yet.
/// </summary>
public class MoneroWalletBlocksResult
{
    /// <summary>
    /// Blocks that hold the transactions, in the order of the chain, with the block without a height last.
    /// </summary>
    [JsonPropertyName("blocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroBlockWallet>? Blocks { get; set; }

    /// <summary>
    /// Get the transactions of the blocks.
    /// </summary>
    /// <returns>the transactions, in the order of the blocks. It is a new list.</returns>
    public List<MoneroTxWallet> GetTxs()
    {
        var txs = new List<MoneroTxWallet>();
        foreach (var block in Blocks ?? [])
        {
            if (block != null) txs.AddRange(block.GetTxsWallet());
        }

        return txs;
    }

    /// <summary>
    /// Get the transfers of the transactions of the blocks.
    /// </summary>
    /// <returns>the transfers, in the order of the transactions, the outgoing transfer of a transaction before its incoming ones. It is a new
    /// list.</returns>
    public List<MoneroTransfer> GetTransfers()
    {
        var transfers = new List<MoneroTransfer>();
        foreach (var tx in GetTxs())
        {
            transfers.AddRange(tx.GetTransfers());
        }

        return transfers;
    }

    /// <summary>
    /// Get the outputs of the transactions of the blocks, which are the ones that the wallet received.
    /// </summary>
    /// <returns>the outputs, in the order of the transactions. It is a new list.</returns>
    public List<MoneroOutputWallet> GetOutputs()
    {
        var outputs = new List<MoneroOutputWallet>();
        foreach (var tx in GetTxs())
        {
            outputs.AddRange(tx.GetOutputsWallet());
        }

        return outputs;
    }

    /// <summary>
    /// Get the inputs of the transactions of the blocks, which are the outputs that the wallet spent. It is where the notification of a spent
    /// output has it.
    /// </summary>
    /// <returns>the inputs, in the order of the transactions. It is a new list.</returns>
    public List<MoneroOutputWallet> GetInputs()
    {
        var inputs = new List<MoneroOutputWallet>();
        foreach (var tx in GetTxs())
        {
            inputs.AddRange(tx.GetInputsWallet());
        }

        return inputs;
    }
}
