
namespace Monero.Wallet.Common;

public class MoneroIncomingTransferComparer : Comparer<MoneroIncomingTransfer>
{
    private static readonly MoneroTxHeightComparer TxHeightComparator = new MoneroTxHeightComparer();

    public override int Compare(MoneroIncomingTransfer? t1, MoneroIncomingTransfer? t2)
    {
        // compare by height
        if (t1 == null && t2 == null) return 0;
        if (t1 == null) return -1;
        if (t2 == null) return 1;
        int heightComparison = TxHeightComparator.Compare(t1.GetTx(), t2.GetTx());
        if (heightComparison != 0) return heightComparison;

        // compare by account and subaddress index
        if (t1.GetAccountIndex() < t2.GetAccountIndex()) return -1;
        else if (t1.GetAccountIndex() == t2.GetAccountIndex())
        {
            // Gestione dei valori null per subaddressIndex
            var sub1 = t1.GetSubaddressIndex();
            var sub2 = t2.GetSubaddressIndex();
            if (sub1 == null && sub2 == null) return 0;
            if (sub1 == null) return -1;
            if (sub2 == null) return 1;
            return sub1.Value.CompareTo(sub2.Value);
        }
        return 1;
    }
}
