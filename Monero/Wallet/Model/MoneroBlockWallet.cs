using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Models a block that holds transactions of a wallet. The wallet doesn't have the hash of the block, only its height and its time, and
/// the block without a height holds the transactions that no block has yet, which are the ones in the pool or not relayed.
/// </summary>
public class MoneroBlockWallet : MoneroBlock
{
    /// <summary>
    /// Initialize a new block of a wallet.
    /// </summary>
    public MoneroBlockWallet()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this block as a deep copy of the given block. The transactions are copied, and they are in the new block.
    /// </summary>
    /// <param name="block">the block to copy.</param>
    public MoneroBlockWallet(MoneroBlockWallet block) : base(block)
    {
        // nothing more to copy
    }

    /// <summary>
    /// Copy this block.
    /// </summary>
    /// <returns>a deep copy of the block.</returns>
    public override MoneroBlockWallet Copy()
    {
        return new MoneroBlockWallet(this);
    }

    /// <summary>
    /// Transactions of the wallet in the block, which are read as <see cref="MoneroTxWallet"/>. Assigning the list sets the block of each
    /// transaction.
    /// </summary>
    [JsonPropertyName("txs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(MoneroTxListConverter<MoneroTxWallet>))]
    public override List<MoneroTx>? Txs
    {
        get => base.Txs;
        set => base.Txs = value;
    }

    /// <summary>
    /// Get the transactions of the block as transactions of a wallet.
    /// </summary>
    /// <returns>the transactions that are transactions of a wallet, in order. It is a new list.</returns>
    public List<MoneroTxWallet> GetTxsWallet()
    {
        return Txs?.OfType<MoneroTxWallet>().ToList() ?? [];
    }
}
