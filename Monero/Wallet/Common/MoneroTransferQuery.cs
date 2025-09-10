using Monero.Common;

namespace Monero.Wallet.Common;

public class MoneroTransferQuery : MoneroTransfer
{
    private MoneroTxQuery? txQuery;
    private bool? isIncoming;
    private string? address;
    private List<string>? addresses;
    private uint? subaddressIndex;
    private List<uint>? subaddressIndices;
    private List<MoneroDestination>? destinations;
    private bool? hasDestinations;

    public MoneroTransferQuery()
    {

    }

    public MoneroTransferQuery(MoneroTransferQuery query) : base(query)
    {
        isIncoming = query.isIncoming;
        address = query.address;
        if (query.addresses != null) addresses = new List<string>(query.addresses);
        subaddressIndex = query.subaddressIndex;
        if (query.subaddressIndices != null) subaddressIndices = new List<uint>(query.subaddressIndices);
        if (query.destinations != null)
        {
            destinations = new List<MoneroDestination>();
            foreach (MoneroDestination destination in query.GetDestinations() ?? []) destinations.Add(destination.Clone());
        }
        hasDestinations = query.hasDestinations;
        txQuery = query.txQuery; // reference original by default, MoneroTxQuery's deep copy will Set this to itself
        Validate();
    }

    private void Validate()
    {
        //if (_subaddressIndex != null && _subaddressIndex < 0) throw new MoneroError("Subaddress index must be >= 0");
        //if (_subaddressIndices != null) foreach (uint subaddressIdx in _subaddressIndices) if (subaddressIdx < 0) throw new MoneroError("Subaddress indices must be >= 0");
    }
    public override MoneroTransferQuery Clone()
    {
        return new MoneroTransferQuery(this);
    }

    public override MoneroTransferQuery SetAmount(ulong? amount)
    {
        base.SetAmount(amount);
        return this;
    }

    public MoneroTxQuery? GetTxQuery()
    {
        return txQuery;
    }

    public MoneroTransferQuery SetTxQuery(MoneroTxQuery? query, bool setTransferQuery = true)
    {
        txQuery = query;
        if (setTransferQuery && txQuery != null) txQuery.SetTransferQuery(this);
        return this;
    }

    public override bool? IsIncoming()
    {
        return isIncoming;
    }

    public MoneroTransferQuery SetIsIncoming(bool? isIncoming)
    {
        this.isIncoming = isIncoming;
        return this;
    }

    public override bool? IsOutgoing()
    {
        return isIncoming == null ? null : !isIncoming;
    }

    public MoneroTransferQuery SetIsOutgoing(bool? isOutgoing)
    {
        isIncoming = isOutgoing == null ? null : !isOutgoing;
        return this;
    }

    public string? GetAddress()
    {
        return address;
    }

    public MoneroTransferQuery SetAddress(string? address)
    {
        this.address = address;
        return this;
    }

    public List<string>? GetAddresses()
    {
        return addresses;
    }

    public MoneroTransferQuery SetAddresses(List<string>? addresses)
    {
        this.addresses = addresses;
        return this;
    }

    public override MoneroTransferQuery SetAccountIndex(uint? subaddressIdx)
    {
        base.SetAccountIndex(subaddressIdx);
        return this;
    }

    public uint? GetSubaddressIndex()
    {
        return subaddressIndex;
    }

    public MoneroTransferQuery SetSubaddressIndex(uint? subaddressIndex)
    {
        this.subaddressIndex = subaddressIndex;
        Validate();
        return this;
    }

    public List<uint>? GetSubaddressIndices()
    {
        return subaddressIndices;
    }

    public MoneroTransferQuery SetSubaddressIndices(List<uint>? subaddressIndices)
    {
        this.subaddressIndices = subaddressIndices;
        Validate();
        return this;
    }

    public List<MoneroDestination>? GetDestinations()
    {
        return destinations;
    }

    public MoneroTransferQuery SetDestinations(List<MoneroDestination>? destinations)
    {
        this.destinations = destinations;
        return this;
    }

    public bool? HasDestinations()
    {
        return hasDestinations;
    }

    public MoneroTransferQuery SetHasDestinations(bool? hasDestinations)
    {
        this.hasDestinations = hasDestinations;
        return this;
    }

    public virtual bool MeetsCriteria(MoneroTransfer? transfer, bool queryParent = true)
    {
        if (transfer == null) throw new MoneroError("transfer is null");

        // filter on common fields
        if (IsIncoming() != null && IsIncoming() != transfer.IsIncoming()) return false;
        if (IsOutgoing() != null && IsOutgoing() != transfer.IsOutgoing()) return false;
        if (GetAmount() != null && ((ulong)GetAmount()!).CompareTo(transfer.GetAmount()) != 0) return false;
        if (GetAccountIndex() != null && !GetAccountIndex().Equals(transfer.GetAccountIndex())) return false;

        // filter on incoming fields
        if (transfer is MoneroIncomingTransfer)
        {
            if (HasDestinations() == true) return false;
            MoneroIncomingTransfer inTransfer = (MoneroIncomingTransfer)transfer;
            if (GetAddress() != null && !GetAddress()!.Equals(inTransfer.GetAddress())) return false;
            if (GetAddresses() != null && !GetAddresses()!.Contains(inTransfer.GetAddress()!)) return false;
            if (GetSubaddressIndex() != null && !GetSubaddressIndex().Equals(inTransfer.GetSubaddressIndex())) return false;
            if (GetSubaddressIndices() != null && !GetSubaddressIndices()!.Contains((uint)inTransfer.GetSubaddressIndex()!)) return false;
        }

        // filter on outgoing fields
        else if (transfer is MoneroOutgoingTransfer)
        {
            MoneroOutgoingTransfer outTransfer = (MoneroOutgoingTransfer)transfer;

            // filter on addresses
            if (GetAddress() != null && (outTransfer.GetAddresses() == null || !outTransfer.GetAddresses()!.Contains(GetAddress()!))) return false;   // TODO: will filter all transfers if they don't contain addresses
            if (GetAddresses() != null)
            {
                HashSet<string> intersections = new HashSet<string>(GetAddresses()!);
                intersections.IntersectWith(outTransfer.GetAddresses()!);
                if (intersections.Count == 0) return false;  // must have overlapping addresses
            }

            // filter on subaddress indices
            if (GetSubaddressIndex() != null && (outTransfer.GetSubaddressIndices() == null || !outTransfer.GetSubaddressIndices()!.Contains((uint)GetSubaddressIndex()!))) return false;
            if (GetSubaddressIndices() != null)
            {
                HashSet<uint> intersections = new HashSet<uint>(GetSubaddressIndices()!);
                intersections.IntersectWith(outTransfer.GetSubaddressIndices()!);
                if (intersections.Count == 0) return false;  // must have overlapping subaddress indices
            }

            // filter on having destinations
            if (HasDestinations() != null)
            {
                if (HasDestinations() == true && outTransfer.GetDestinations() == null) return false;
                if (!HasDestinations() == true && outTransfer.GetDestinations() != null) return false;
            }

            // filter on destinations TODO: start with test for this
            //    if (GetDestinations() != null && GetDestinations() != transfer.GetDestinations()) return false;
        }

        // otherwise invalid type
        else throw new Exception("Transfer must be MoneroIncomingTransfer or MoneroOutgoingTransfer");

        // filter with tx filter
        if (queryParent && GetTxQuery() != null && !GetTxQuery()!.MeetsCriteria(transfer.GetTx()!, false)) return false;
        return true;
    }
}
