using Monero.Common;

namespace Monero.Wallet.Common
{
    public class MoneroTransferQuery : MoneroTransfer
    {
        private MoneroTxQuery? _txQuery;
        private bool? _isIncoming;
        private string? _address;
        private List<string>? _addresses;
        private uint? _subaddressIndex;
        private List<uint>? _subaddressIndices;
        private List<MoneroDestination>? _destinations;
        private bool? _hasDestinations;

        public MoneroTransferQuery()
        {

        }

        public MoneroTransferQuery(MoneroTransferQuery query) : base(query)
        {
            this._isIncoming = query._isIncoming;
            this._address = query._address;
            if (query._addresses != null) this._addresses = new List<string>(query._addresses);
            this._subaddressIndex = query._subaddressIndex;
            if (query._subaddressIndices != null) this._subaddressIndices = new List<uint>(query._subaddressIndices);
            if (query._destinations != null)
            {
                this._destinations = new List<MoneroDestination>();
                foreach (MoneroDestination destination in query.GetDestinations() ?? []) this._destinations.Add(destination.Clone());
            }
            this._hasDestinations = query._hasDestinations;
            this._txQuery = query._txQuery; // reference original by default, MoneroTxQuery's deep copy will Set this to itself
            Validate();
        }

        private void Validate()
        {
            if (_subaddressIndex != null && _subaddressIndex < 0) throw new MoneroError("Subaddress index must be >= 0");
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
            return _txQuery;
        }

        public MoneroTransferQuery SetTxQuery(MoneroTxQuery? query, bool setTransferQuery = true)
        {
            _txQuery = query;
            if (setTransferQuery && _txQuery != null) _txQuery.SetTransferQuery(this);
            return this;
        }

        public override bool? IsIncoming()
        {
            return _isIncoming;
        }

        public MoneroTransferQuery SetIsIncoming(bool? isIncoming)
        {
            this._isIncoming = isIncoming;
            return this;
        }

        public override bool? IsOutgoing()
        {
            return _isIncoming == null ? null : !_isIncoming;
        }

        public MoneroTransferQuery SetIsOutgoing(bool? isOutgoing)
        {
            _isIncoming = isOutgoing == null ? null : !isOutgoing;
            return this;
        }

        public string? GetAddress()
        {
            return _address;
        }

        public MoneroTransferQuery SetAddress(string? address)
        {
            this._address = address;
            return this;
        }

        public List<string>? GetAddresses()
        {
            return _addresses;
        }

        public MoneroTransferQuery SetAddresses(List<string>? addresses)
        {
            this._addresses = addresses;
            return this;
        }

        public override MoneroTransferQuery SetAccountIndex(uint? subaddressIdx)
        {
            base.SetAccountIndex(subaddressIdx);
            return this;
        }

        public uint? GetSubaddressIndex()
        {
            return _subaddressIndex;
        }

        public MoneroTransferQuery SetSubaddressIndex(uint? subaddressIndex)
        {
            this._subaddressIndex = subaddressIndex;
            Validate();
            return this;
        }

        public List<uint>? GetSubaddressIndices()
        {
            return _subaddressIndices;
        }

        public MoneroTransferQuery SetSubaddressIndices(List<uint>? subaddressIndices)
        {
            this._subaddressIndices = subaddressIndices;
            Validate();
            return this;
        }

        public List<MoneroDestination>? GetDestinations()
        {
            return _destinations;
        }

        public MoneroTransferQuery SetDestinations(List<MoneroDestination>? destinations)
        {
            this._destinations = destinations;
            return this;
        }

        public bool? HasDestinations()
        {
            return _hasDestinations;
        }

        public MoneroTransferQuery SetHasDestinations(bool? hasDestinations)
        {
            this._hasDestinations = hasDestinations;
            return this;
        }

        public virtual bool MeetsCriteria(MoneroTransfer? transfer, bool queryParent = true)
        {
            if (transfer == null) throw new MoneroError("transfer is null");
            
            // filter on common fields
            if (IsIncoming() != null && IsIncoming() != transfer.IsIncoming()) return false;
            if (IsOutgoing() != null && IsOutgoing() != transfer.IsOutgoing()) return false;
            if (GetAmount() != null && ((ulong)GetAmount()).CompareTo(transfer.GetAmount()) != 0) return false;
            if (GetAccountIndex() != null && !GetAccountIndex().Equals(transfer.GetAccountIndex())) return false;
            
            // filter on incoming fields
            if (transfer is MoneroIncomingTransfer) {
              if (HasDestinations() == true) return false;
              MoneroIncomingTransfer inTransfer = (MoneroIncomingTransfer) transfer;
              if (GetAddress() != null && !GetAddress().Equals(inTransfer.GetAddress())) return false;
              if (GetAddresses() != null && !GetAddresses().Contains(inTransfer.GetAddress())) return false;
              if (GetSubaddressIndex() != null && !GetSubaddressIndex().Equals(inTransfer.GetSubaddressIndex())) return false;
              if (GetSubaddressIndices() != null && !GetSubaddressIndices().Contains((uint)inTransfer.GetSubaddressIndex())) return false;
            }

            // filter on outgoing fields
            else if (transfer is MoneroOutgoingTransfer) {
              MoneroOutgoingTransfer outTransfer = (MoneroOutgoingTransfer) transfer;
              
              // filter on addresses
              if (GetAddress() != null && (outTransfer.GetAddresses() == null || !outTransfer.GetAddresses().Contains(this.GetAddress()))) return false;   // TODO: will filter all transfers if they don't contain addresses
              if (GetAddresses() != null) {
                HashSet<string> intersections = new HashSet<string>(this.GetAddresses());
                intersections.IntersectWith(outTransfer.GetAddresses());
                if (intersections.Count == 0) return false;  // must have overlapping addresses
              }
              
              // filter on subaddress indices
              if (GetSubaddressIndex() != null && (outTransfer.GetSubaddressIndices() == null || !outTransfer.GetSubaddressIndices().Contains((uint)this.GetSubaddressIndex()))) return false;
              if (GetSubaddressIndices() != null) {
                HashSet<uint> intersections = new HashSet<uint>(this.GetSubaddressIndices());
                intersections.IntersectWith(outTransfer.GetSubaddressIndices());
                if (intersections.Count == 0) return false;  // must have overlapping subaddress indices
              }
              
              // filter on having destinations
              if (HasDestinations() != null) {
                if (HasDestinations() == true && outTransfer.GetDestinations() == null) return false;
                if (!HasDestinations() == true && outTransfer.GetDestinations() != null) return false;
              }
              
              // filter on destinations TODO: start with test for this
              //    if (GetDestinations() != null && GetDestinations() != transfer.GetDestinations()) return false;
            }
            
            // otherwise invalid type
            else throw new Exception("Transfer must be MoneroIncomingTransfer or MoneroOutgoingTransfer");
            
            // filter with tx filter
            if (queryParent && GetTxQuery() != null && !GetTxQuery().MeetsCriteria(transfer.GetTx(), false)) return false;
            return true;
        }
    }
}
