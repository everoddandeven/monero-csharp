using Monero.Common;

namespace Monero.Wallet.Common
{
    public class MoneroCheckReserve : MoneroCheck
    {
        private ulong? _totalAmount;
        private ulong? _unconfirmedSpentAmount;

        public MoneroCheckReserve(bool isGood = false, ulong? totalAmount = null, ulong? unconfirmedSpentAmount = null)
            : base(isGood)
        {
            _totalAmount = totalAmount;
            _unconfirmedSpentAmount = unconfirmedSpentAmount;
        }

        public ulong? GetTotalAmount()
        {
            return _totalAmount;
        }

        public MoneroCheckReserve SetTotalAmount(ulong? totalAmount)
        {
            _totalAmount = totalAmount;
            return this;
        }

        public ulong? GetUnconfirmedSpentAmount()
        {
            return _unconfirmedSpentAmount;
        }

        public MoneroCheckReserve SetUnconfirmedSpentAmount(ulong? unconfirmedSpentAmount)
        {
            _unconfirmedSpentAmount = unconfirmedSpentAmount;
            return this;
        }
    }
}
