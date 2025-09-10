
using System.Text;
using Monero.Common;

namespace Monero.Wallet.Common;

public abstract class MoneroTransfer
{
    private MoneroTxWallet? _tx;
    private ulong? _amount;
    private uint? _accountIndex;

    public MoneroTransfer() { }

    public MoneroTransfer(MoneroTransfer transfer)
    {
        _tx = transfer._tx;
        _amount = transfer._amount;
        _accountIndex = transfer._accountIndex;
    }

    public abstract MoneroTransfer Clone();

    public MoneroTxWallet? GetTx()
    {
        return _tx;
    }

    public virtual MoneroTransfer SetTx(MoneroTxWallet? tx)
    {
        _tx = tx;
        return this;
    }

    public abstract bool? IsIncoming();

    public virtual bool? IsOutgoing()
    {
        return !IsIncoming();
    }

    public ulong? GetAmount()
    {
        return _amount;
    }

    public virtual MoneroTransfer SetAmount(ulong? amount)
    {
        _amount = amount;
        return this;
    }

    public uint? GetAccountIndex()
    {
        return _accountIndex;
    }

    public virtual MoneroTransfer SetAccountIndex(uint? accountIndex)
    {
        _accountIndex = accountIndex;
        return this;
    }

    public MoneroTransfer Merge(MoneroTransfer transfer)
    {
        if (this == transfer) return this;

        // merge txs if they're different which comes back to merging transfers
        if (this.GetTx() != transfer.GetTx())
        {
            this.GetTx()!.Merge(transfer.GetTx()!);
            return this;
        }

        // otherwise merge transfer fields
        this.SetAccountIndex(GenUtils.Reconcile(this.GetAccountIndex(), transfer.GetAccountIndex()));

        // TODO monero-project: failed tx in pool (after testUpdateLockedDifferentAccounts()) causes non-originating saved wallets to return duplicate incoming transfers but one has amount of 0
        if (this.GetAmount() != null && transfer.GetAmount() != null && !this.GetAmount().Equals(transfer.GetAmount()) && (0 == this.GetAmount()) || 0 == transfer.GetAmount())
        {
            MoneroUtils.Log(0, "WARNING: monero-project returning transfers with 0 amount/numSuggestedConfirmations");
        }
        else
        {
            this.SetAmount(GenUtils.Reconcile(this.GetAmount(), transfer.GetAmount()));
        }

        return this;
    }

    public bool Equals(MoneroTransfer? other)
    {
        if (other == null) return false;
        if (this == other) return true;
        if (_accountIndex == null)
        {
            if (other._accountIndex != null) return false;
        }
        else if (!_accountIndex.Equals(other._accountIndex)) return false;
        if (_amount == null)
        {
            if (other._amount != null) return false;
        }
        else if (!_amount.Equals(other._amount)) return false;
        return true;
    }

    public override string ToString()
    {
        return ToString(0);
    }

    public virtual string ToString(int indent)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(GenUtils.KvLine("Is incoming", this.IsIncoming(), indent));
        sb.Append(GenUtils.KvLine("Amount", this.GetAmount() != null ? this.GetAmount().ToString() : null, indent));
        sb.Append(GenUtils.KvLine("Account index", this.GetAccountIndex(), indent));
        string str = sb.ToString();
        return str.Length == 0 ? str : str.Substring(0, str.Length - 1);	  // strip last newline
    }
}
