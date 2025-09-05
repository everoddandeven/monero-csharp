
namespace Monero.Wallet.Common
{
    public class MoneroAddressBookEntry
    {
        private uint? _index;
        private string? _address;
        private string? _paymentId;
        private string? _description;

        public MoneroAddressBookEntry(uint? index = null, string? address = null, string? description = null, string? paymentId = null)
        {
            _index = index;
            _address = address;
            _paymentId = paymentId;
            _description = description;
        }


        public uint? GetIndex()
        {
            return _index;
        }

        public MoneroAddressBookEntry SetIndex(uint? index)
        {
            _index = index;
            return this;
        }

        public string? GetAddress()
        {
            return _address;
        }

        public MoneroAddressBookEntry SetAddress(string? address)
        {
            _address = address;
            return this;
        }

        public string? GetPaymentId()
        {
            return _paymentId;
        }

        public MoneroAddressBookEntry SetPaymentId(string? paymentId)
        {
            _paymentId = paymentId;
            return this;
        }

        public string? GetDescription()
        {
            return _description;
        }

        public MoneroAddressBookEntry SetDescription(string? description)
        {
            _description = description;
            return this;
        }

    }
}
