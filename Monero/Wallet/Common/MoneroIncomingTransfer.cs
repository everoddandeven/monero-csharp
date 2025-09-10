
using System.Text;
using Monero.Common;

namespace Monero.Wallet.Common;

public class MoneroIncomingTransfer : MoneroTransfer
{
    private uint? subaddressIndex;
    private string? address;
    private ulong numSuggestedConfirmations;

    public MoneroIncomingTransfer()
    {
        // nothing to initialize
    }

    public bool Equals(MoneroIncomingTransfer? other)
    {
        if (other == null) return false;
        if (other == this) return true;
        if (!base.Equals(other)) return false;

        return subaddressIndex == other.subaddressIndex &&
                address == other.address &&
                numSuggestedConfirmations == other.numSuggestedConfirmations;
    }

    public MoneroIncomingTransfer(MoneroIncomingTransfer transfer) : base(transfer)
    {
        subaddressIndex = transfer.subaddressIndex;
        address = transfer.address;
        numSuggestedConfirmations = transfer.numSuggestedConfirmations;
    }

    public override MoneroIncomingTransfer Clone()
    {
        return new MoneroIncomingTransfer(this);
    }

    public override MoneroIncomingTransfer SetTx(MoneroTxWallet? tx)
    {
        base.SetTx(tx);
        return this;
    }

    public override bool? IsIncoming()
    {
        return true;
    }

    public uint? GetSubaddressIndex()
    {
        return subaddressIndex;
    }

    public MoneroIncomingTransfer SetSubaddressIndex(uint? subaddressIndex)
    {
        this.subaddressIndex = subaddressIndex;
        return this;
    }

    public string? GetAddress()
    {
        return address;
    }

    public MoneroIncomingTransfer SetAddress(string? address)
    {
        this.address = address;
        return this;
    }

    public ulong GetNumSuggestedConfirmations()
    {
        return numSuggestedConfirmations;
    }

    public MoneroIncomingTransfer SetNumSuggestedConfirmations(ulong numSuggestedConfirmations)
    {
        this.numSuggestedConfirmations = numSuggestedConfirmations;
        return this;
    }

    public override string ToString(int indent)
    {
        var sb = new StringBuilder();
        sb.Append(base.ToString(indent) + "\n");
        sb.Append(GenUtils.KvLine("Subaddress index", GetSubaddressIndex(), indent));
        sb.Append(GenUtils.KvLine("Address", GetAddress(), indent));
        sb.Append(GenUtils.KvLine("Num suggested confirmations", GetNumSuggestedConfirmations(), indent));
        string str = sb.ToString();
        return str.Substring(0, str.Length - 1);
    }
}
