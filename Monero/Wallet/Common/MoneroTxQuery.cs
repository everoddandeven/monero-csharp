
using Monero.Common;

namespace Monero.Wallet.Common;

public class MoneroTxQuery : MoneroTxWallet
{
    private bool? isOutgoing;
    private bool? isIncoming;
    private List<string>? hashes;
    private bool? hasPaymentId;
    private List<string>? paymentIds;
    private ulong? height;
    private ulong? minHeight;
    private ulong? maxHeight;
    private bool? includeOutputs;
    private MoneroTransferQuery? transferQuery;
    private MoneroOutputQuery? inputQuery;
    private MoneroOutputQuery? outputQuery;

    public MoneroTxQuery()
    {

    }

    public MoneroTxQuery(MoneroTxQuery query) : base(query)
    {
        isOutgoing = query.isOutgoing;
        isIncoming = query.isIncoming;
        if (query.hashes != null) hashes = new List<string>(query.hashes);
        hasPaymentId = query.hasPaymentId;
        if (query.paymentIds != null) paymentIds = new List<string>(query.paymentIds);
        height = query.height;
        minHeight = query.minHeight;
        maxHeight = query.maxHeight;
        includeOutputs = query.includeOutputs;
        if (query.transferQuery != null) SetTransferQuery(new MoneroTransferQuery(query.transferQuery));
        if (query.inputQuery != null) SetInputQuery(new MoneroOutputQuery(query.inputQuery));
        if (query.outputQuery != null) SetOutputQuery(new MoneroOutputQuery(query.outputQuery));
    }

    public override MoneroTxQuery Clone()
    {
        return new MoneroTxQuery(this);
    }

    public override MoneroTxQuery SetIsConfirmed(bool? isConfirmed)
    {
        base.SetIsConfirmed(isConfirmed);
        return this;
    }

    public override MoneroTxQuery SetInTxPool(bool? inTxPool)
    {
        base.SetInTxPool(inTxPool);
        return this;
    }

    public override MoneroTxQuery SetIsFailed(bool? isFailed)
    {
        base.SetIsFailed(isFailed);
        return this;
    }

    public override MoneroTxQuery SetIsLocked(bool? isLocked)
    {
        base.SetIsLocked(isLocked);
        return this;
    }

    public override bool? IsOutgoing()
    {
        return isOutgoing;
    }

    public override MoneroTxQuery SetIsOutgoing(bool? isOutgoing)
    {
        this.isOutgoing = isOutgoing;
        return this;
    }

    public override bool? IsIncoming()
    {
        return isIncoming;
    }

    public override MoneroTxQuery SetIsIncoming(bool? isIncoming)
    {
        this.isIncoming = isIncoming;
        return this;
    }

    public override MoneroTxQuery SetHash(string? hash)
    {
        base.SetHash(hash);
        if (hash != null) SetHashes([hash]);
        else SetHashes(null);
        return this;
    }

    public List<string>? GetHashes()
    {
        return hashes;
    }

    public MoneroTxQuery SetHashes(List<string>? hashes)
    {
        this.hashes = hashes;
        return this;
    }

    public bool? HasPaymentId()
    {
        return hasPaymentId;
    }

    public MoneroTxQuery SetHasPaymentId(bool hasPaymentId)
    {
        this.hasPaymentId = hasPaymentId;
        return this;
    }

    public List<string>? GetPaymentIds()
    {
        return paymentIds;
    }

    public MoneroTxQuery SetPaymentIds(List<string>? paymentIds)
    {
        this.paymentIds = paymentIds;
        return this;
    }

    public override MoneroTxQuery SetPaymentId(string? paymentId)
    {
        if (paymentId != null) SetPaymentIds([paymentId]);
        else SetPaymentIds(null);
        return this;
    }

    public MoneroTxQuery SetHeight(ulong? height)
    {
        this.height = height;
        return this;
    }

    public override ulong? GetHeight()
    {
        return height;
    }

    public ulong? GetMinHeight()
    {
        return minHeight;
    }

    public MoneroTxQuery SetMinHeight(ulong? minHeight)
    {
        this.minHeight = minHeight;
        return this;
    }

    public ulong? GetMaxHeight()
    {
        return maxHeight;
    }

    public MoneroTxQuery SetMaxHeight(ulong? maxHeight)
    {
        this.maxHeight = maxHeight;
        return this;
    }

    public override MoneroTxQuery SetUnlockTime(ulong? unlockTime)
    {
        base.SetUnlockTime(unlockTime == null ? null : unlockTime);
        return this;
    }

    public bool? GetIncludeOutputs()
    {
        return includeOutputs;
    }

    public MoneroTxQuery SetIncludeOutputs(bool includeOutputs)
    {
        this.includeOutputs = includeOutputs;
        return this;
    }

    public MoneroTransferQuery? GetTransferQuery()
    {
        return transferQuery;
    }

    public MoneroTxQuery SetTransferQuery(MoneroTransferQuery? transferQuery)
    {
        this.transferQuery = transferQuery;
        if (transferQuery != null) transferQuery.SetTxQuery(this, false);
        return this;
    }

    public MoneroOutputQuery? GetInputQuery()
    {
        return inputQuery;
    }

    public MoneroTxQuery SetInputQuery(MoneroOutputQuery? inputQuery)
    {
        this.inputQuery = inputQuery;
        if (inputQuery != null) inputQuery.SetTxQuery(this);
        return this;
    }

    public MoneroOutputQuery? GetOutputQuery()
    {
        return outputQuery;
    }

    public MoneroTxQuery SetOutputQuery(MoneroOutputQuery? outputQuery)
    {
        this.outputQuery = outputQuery;
        if (outputQuery != null) outputQuery.SetTxQuery(this, false);
        return this;
    }

    public bool MeetsCriteria(MoneroTxWallet tx, bool queryChildren = true)
    {
        if (tx == null) throw new MoneroError("No tx given to MoneroTxQuery.MeetsCriteria()");

        // filter on tx
        if (GetHash() != null && !GetHash()!.Equals(tx.GetHash())) return false;
        if (GetPaymentId() != null && !GetPaymentId()!.Equals(tx.GetPaymentId())) return false;
        if (IsConfirmed() != null && IsConfirmed() != tx.IsConfirmed()) return false;
        if (InTxPool() != null && InTxPool() != tx.InTxPool()) return false;
        if (GetRelay() != null && GetRelay() != tx.GetRelay()) return false;
        if (IsRelayed() != null && IsRelayed() != tx.IsRelayed()) return false;
        if (IsFailed() != null && IsFailed() != tx.IsFailed()) return false;
        if (IsMinerTx() != null && IsMinerTx() != tx.IsMinerTx()) return false;
        if (IsLocked() != null && IsLocked() != tx.IsLocked()) return false;

        // filter on having a payment id
        if (HasPaymentId() != null)
        {
            if (HasPaymentId() == true && tx.GetPaymentId() == null) return false;
            if (HasPaymentId() != true && tx.GetPaymentId() != null) return false;
        }

        // filter on incoming
        if (IsIncoming() != null && IsIncoming() != (tx.IsIncoming() == true)) return false;

        // filter on outgoing
        if (IsOutgoing() != null && IsOutgoing() != (tx.IsOutgoing() == true)) return false;

        // filter on remaining fields
        ulong? txHeight = tx.GetBlock() == null ? null : tx.GetBlock()!.GetHeight();
        if (GetHashes() != null && !GetHashes()!.Contains(tx.GetHash()!)) return false;
        if (GetPaymentIds() != null && !GetPaymentIds()!.Contains(tx.GetPaymentId()!)) return false;
        if (GetHeight() != null && !GetHeight().Equals(txHeight)) return false;
        if (GetMinHeight() != null && txHeight != null && txHeight < GetMinHeight()) return false; // do not filter unconfirmed
        if (GetMaxHeight() != null && (txHeight == null || txHeight > GetMaxHeight())) return false;

        // done if not querying transfers or outputs
        if (!queryChildren) return true;

        // at least one transfer must meet transfer query if defined
        if (GetTransferQuery() != null)
        {
            bool matchFound = false;
            if (tx.GetOutgoingTransfer() != null && GetTransferQuery()!.MeetsCriteria(tx.GetOutgoingTransfer(), false)) matchFound = true;
            else if (tx.GetIncomingTransfers() != null)
            {
                foreach (MoneroIncomingTransfer incomingTransfer in tx.GetIncomingTransfers()!)
                {
                    if (GetTransferQuery()!.MeetsCriteria(incomingTransfer, false))
                    {
                        matchFound = true;
                        break;
                    }
                }
            }
            if (!matchFound) return false;
        }

        // at least one input must meet input query if defined
        if (GetInputQuery() != null)
        {
            if (tx.GetInputs() == null || tx.GetInputs()!.Count == 0) return false;
            bool matchFound = false;
            foreach (MoneroOutputWallet input in tx.GetInputsWallet())
            {
                if (GetInputQuery()!.MeetsCriteria(input, false))
                {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound) return false;
        }

        // at least one output must meet output query if defined
        if (GetOutputQuery() != null)
        {
            if (tx.GetOutputs() == null || tx.GetOutputs()!.Count == 0) return false;
            bool matchFound = false;
            foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
            {
                if (GetOutputQuery()!.MeetsCriteria(output, false))
                {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound) return false;
        }

        return true;  // transaction meets query criteria
    }

}
