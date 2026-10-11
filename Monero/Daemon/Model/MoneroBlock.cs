using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a Monero block in the blockchain.
/// </summary>
public class MoneroBlock : MoneroBlockHeader
{
    private MoneroTx? _minerTx;
    private List<MoneroTx>? _txs;

    /// <summary>
    /// Initialize a new block.
    /// </summary>
    public MoneroBlock()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this block as a deep copy of the given block. The transactions are copied, and they are in the new block.
    /// </summary>
    /// <param name="block">the block to copy.</param>
    public MoneroBlock(MoneroBlock block) : base(block)
    {
        Hex = block.Hex;
        MinerTx = block.MinerTx?.Copy();

        if (block.Txs != null)
        {
            Txs = [.. block.Txs.Select(tx => tx?.Copy()!)];
        }

        if (block.TxHashes != null)
        {
            TxHashes = [.. block.TxHashes];
        }
    }

    /// <summary>
    /// Copy this block.
    /// </summary>
    /// <returns>a deep copy of the block.</returns>
    public override MoneroBlock Copy()
    {
        return new MoneroBlock(this);
    }

    /// <summary>
    /// Block in hexadecimal format.
    /// </summary>
    [JsonPropertyName("hex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hex { get; set; }

    /// <summary>
    /// The miner transaction associated to this block. Assigning it sets the block of the transaction.
    /// </summary>
    [JsonPropertyName("minerTx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroTx? MinerTx
    {
        get => _minerTx;
        set
        {
            _minerTx = value;
            if (value != null) value.Block = this;
        }
    }

    /// <summary>
    /// Transactions included in current block. Assigning the list sets the block of each transaction.
    /// </summary>
    [JsonPropertyName("txs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroTx>? Txs
    {
        get => _txs;
        set
        {
            _txs = value;
            if (value == null) return;
            foreach (var tx in value)
            {
                if (tx != null) tx.Block = this;
            }
        }
    }

    /// <summary>
    /// Hashes of the transactions of the block, when the transactions themselves are not fetched.
    /// </summary>
    [JsonPropertyName("txHashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? TxHashes { get; set; }
}
