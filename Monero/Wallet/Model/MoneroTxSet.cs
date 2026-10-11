using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Groups the transactions that a wallet made with the hex that signs or relays them. The hex is in the set and not in the transactions: a
/// view-only wallet makes an unsigned set, the wallet that has the keys signs it, and a multisig wallet shares the multisig hex.
/// </summary>
public class MoneroTxSet
{
    private List<MoneroTxWallet>? _txs;

    /// <summary>
    /// Initialize a new transaction set.
    /// </summary>
    public MoneroTxSet()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this set as a deep copy of the given set. The transactions are copied, and they are in the new set.
    /// </summary>
    /// <param name="txSet">the set to copy.</param>
    public MoneroTxSet(MoneroTxSet txSet)
    {
        SignedTxHex = txSet.SignedTxHex;
        UnsignedTxHex = txSet.UnsignedTxHex;
        MultisigTxHex = txSet.MultisigTxHex;

        if (txSet.Txs != null)
        {
            Txs = [.. txSet.Txs.Select(tx => tx?.Copy()!)];
        }
    }

    /// <summary>
    /// Copy this set.
    /// </summary>
    /// <returns>a deep copy of the set.</returns>
    public virtual MoneroTxSet Copy()
    {
        return new MoneroTxSet(this);
    }

    /// <summary>
    /// Transactions of the set. Assigning the list sets the set of each transaction.
    /// </summary>
    [JsonPropertyName("txs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroTxWallet>? Txs
    {
        get => _txs;
        set
        {
            _txs = value;
            if (value == null) return;
            foreach (var tx in value)
            {
                if (tx != null) tx.TxSet = this;
            }
        }
    }

    /// <summary>
    /// Signed transactions in hexadecimal format, which a view-only wallet submits.
    /// </summary>
    [JsonPropertyName("signedTxHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignedTxHex { get; set; }

    /// <summary>
    /// Unsigned transactions in hexadecimal format, which the wallet that has the keys signs.
    /// </summary>
    [JsonPropertyName("unsignedTxHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnsignedTxHex { get; set; }

    /// <summary>
    /// Multisig transactions in hexadecimal format, which the participants of a multisig wallet sign.
    /// </summary>
    [JsonPropertyName("multisigTxHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MultisigTxHex { get; set; }
}
