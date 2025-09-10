using Monero.Common;

namespace Monero.Wallet.Common;


public class MoneroTxSet
{
    private List<MoneroTxWallet>? txs;
    private string? multisigTxHex;
    private string? unsignedTxHex;
    private string? signedTxHex;

    public List<MoneroTxWallet>? GetTxs()
    {
        return txs;
    }

    public MoneroTxSet SetTxs(List<MoneroTxWallet>? txs)
    {
        this.txs = txs;
        return this;
    }

    public string? GetMultisigTxHex()
    {
        return multisigTxHex;
    }

    public MoneroTxSet SetMultisigTxHex(string? multisigTxHex)
    {
        this.multisigTxHex = multisigTxHex;
        return this;
    }

    public string? GetUnsignedTxHex()
    {
        return unsignedTxHex;
    }

    public MoneroTxSet SetUnsignedTxHex(string? unsignedTxHex)
    {
        this.unsignedTxHex = unsignedTxHex;
        return this;
    }

    public string? GetSignedTxHex()
    {
        return signedTxHex;
    }

    public MoneroTxSet SetSignedTxHex(string? signedTxHex)
    {
        this.signedTxHex = signedTxHex;
        return this;
    }

    public MoneroTxSet Merge(MoneroTxSet? txSet)
    {
        if (txSet == null) throw new MoneroError("Tx set is null");
        if (this == txSet) return this;

        // merge sets
        SetMultisigTxHex(GenUtils.Reconcile(GetMultisigTxHex(), txSet.GetMultisigTxHex()));
        SetUnsignedTxHex(GenUtils.Reconcile(GetUnsignedTxHex(), txSet.GetUnsignedTxHex()));
        SetSignedTxHex(GenUtils.Reconcile(GetSignedTxHex(), txSet.GetSignedTxHex()));

        // merge txs
        if (txSet.GetTxs() != null)
        {
            foreach (MoneroTxWallet tx in txSet.GetTxs()!)
            {
                tx.SetTxSet(this);
                MoneroUtils.MergeTx(txs!, tx);
            }
        }

        return this;
    }
}
