
namespace Monero.Wallet.Common
{
    public class MoneroAccount
    {
        private uint? _index;
        private string? _primaryAddress;
        private ulong? _balance;
        private ulong? _unlockedBalance;
        private string? _tag;
        private List<MoneroSubaddress>? _subaddresses;

        public MoneroAccount(uint? index = null, string? primaryAddress = null) {
            _index = index;
            _primaryAddress = primaryAddress;
        }

        public MoneroAccount(uint index, string primaryAddress, ulong balance, ulong unlockedBalance, List<MoneroSubaddress>? subaddresses = null)
        {
            _index = index;
            _primaryAddress = primaryAddress;
            _balance = balance;
            _unlockedBalance = unlockedBalance;
            _subaddresses = subaddresses;
        }

        public uint? GetIndex()
        {
            return _index;
        }

        public MoneroAccount SetIndex(uint? index)
        {
            _index = index;
            return this;
        }

        public string? GetPrimaryAddress()
        {
            return _primaryAddress;
        }

        public MoneroAccount SetPrimaryAddress(string? primaryAddress)
        {
            _primaryAddress = primaryAddress;
            return this;
        }

        public ulong? GetBalance()
        {
            return _balance;
        }

        public MoneroAccount SetBalance(ulong? balance)
        {
            _balance = balance;
            return this;
        }

        public ulong? GetUnlockedBalance()
        {
            return _unlockedBalance;
        }

        public MoneroAccount SetUnlockedBalance(ulong? unlockedBalance)
        {
            _unlockedBalance = unlockedBalance;
            return this;
        }

        public string? GetTag()
        {
            return _tag;
        }

        public MoneroAccount SetTag(string? tag)
        {
            _tag = tag;
            return this;
        }

        public List<MoneroSubaddress>? GetSubaddresses()
        {
            return _subaddresses;
        }

        public MoneroAccount SetSubaddresses(List<MoneroSubaddress>? subaddresses)
        {
            _subaddresses = subaddresses;
            if (subaddresses != null)
            {
                foreach (MoneroSubaddress subaddress in subaddresses)
                {
                    subaddress.SetAccountIndex(_index);
                }
            }

            return this;
        }
    }
}
