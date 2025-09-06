
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
        
        public MoneroAccount(uint? index = null, string? primaryAddress = null, ulong? balance = null, ulong? unlockedBalance = null, List<MoneroSubaddress>? subaddresses = null)
        {
            _index = index;
            _primaryAddress = primaryAddress;
            _balance = balance;
            _unlockedBalance = unlockedBalance;
            _subaddresses = subaddresses;
        }

        public MoneroAccount(MoneroAccount other)
        {
            _index = other._index;
            _primaryAddress = other._primaryAddress;
            _balance = other._balance;
            _unlockedBalance = other._unlockedBalance;

            if (other._subaddresses != null)
            {
                _subaddresses = [];
                foreach (var subaddr in other._subaddresses)
                {
                    _subaddresses.Add(subaddr.Clone());
                }
            }
        }

        public bool Equals(MoneroAccount? other)
        {
            if (other == null) return false;
            if (this == other) return false;

            if (_subaddresses == null)
            {
                if (other._subaddresses != null) return false;
            }
            else
            {
                if (other._subaddresses == null) return false;

                int i = 0;

                foreach (MoneroSubaddress subaddr in _subaddresses)
                {
                    var otherSubaddr = other._subaddresses[i];
                    if (!subaddr.Equals(otherSubaddr)) return false;
                    i++;
                }
            }
            
            return _index == other._index && 
                   _primaryAddress == other._primaryAddress && 
                   _balance == other._balance && 
                   _unlockedBalance == other._unlockedBalance;
        }

        public MoneroAccount Clone()
        {
            return new MoneroAccount(this);
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
