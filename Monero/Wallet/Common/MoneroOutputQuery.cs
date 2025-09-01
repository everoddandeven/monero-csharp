
using Monero.Common;

namespace Monero.Wallet.Common
{
    public class MoneroOutputQuery : MoneroOutputWallet
    {
        protected MoneroTxQuery? txQuery;
        private List<uint> subaddressIndices = [];
        private ulong? minAmount;
        private ulong? maxAmount;

        public MoneroOutputQuery() { }

        public MoneroOutputQuery(MoneroOutputQuery query) : base(query)
        {
            if (query.GetMinAmount() != null) this.minAmount = query.GetMinAmount();
            if (query.GetMaxAmount() != null) this.maxAmount = query.GetMaxAmount();
            if (query.subaddressIndices != null) this.subaddressIndices = new List<uint>(query.subaddressIndices);
            this.txQuery = query.txQuery;  // reference original by default, MoneroTxQuery's deep copy will Set this to itself
        }

        public override MoneroOutputQuery SetKeyImage(MoneroKeyImage? keyImage)
        {
            base.SetKeyImage(keyImage);
            return this;
        }

        public override MoneroOutputQuery SetIsSpent(bool? isSpent)
        {
            base.SetIsSpent(isSpent);
            return this;
        }

        public override MoneroOutputQuery SetAccountIndex(uint? accountIndex)
        {
            base.SetAccountIndex(accountIndex);
            return this;
        }

        public override MoneroOutputQuery SetSubaddressIndex(uint? subaddressIndex)
        {
            base.SetSubaddressIndex(subaddressIndex);
            return this;
        }

        public override MoneroOutputQuery Clone()
        {
            return new MoneroOutputQuery(this);
        }

        public ulong? GetMinAmount()
        {
            return minAmount;
        }

        public MoneroOutputQuery SetMinAmount(ulong minAmount)
        {
            this.minAmount = minAmount;
            return this;
        }

        public ulong? GetMaxAmount()
        {
            return maxAmount;
        }

        public MoneroOutputQuery SetMaxAmount(ulong maxAmount)
        {
            this.maxAmount = maxAmount;
            return this;
        }

        public MoneroTxQuery? GetTxQuery()
        {
            return txQuery;
        }

        public MoneroOutputQuery SetTxQuery(MoneroTxQuery? txQuery)
        {
            this.txQuery = txQuery;
            if (txQuery != null) txQuery.SetOutputQuery(this);
            return this;
        }

        public List<uint> GetSubaddressIndices()
        {
            return subaddressIndices;
        }

        public MoneroOutputQuery? SetSubaddressIndices(List<uint> subaddressIndices)
        {
            this.subaddressIndices = subaddressIndices;
            return this;
        }

        public bool MeetsCriteria(MoneroOutputWallet output)
        {
            throw new NotImplementedException();
        }
    }
}
