using Monero.Common;

namespace Monero.Wallet.Common
{
    public class MoneroOutputWallet : MoneroOutput
    {
        private uint? _accountIndex;
        private uint? _subaddressIndex;
        private bool? _isSpent;
        private bool? _isFrozen;

        public MoneroOutputWallet()
        {
        }

        public MoneroOutputWallet(MoneroOutputWallet output): base(output)
        {
            this._accountIndex = output._accountIndex;
            this._subaddressIndex = output._subaddressIndex;
            this._isSpent = output._isSpent;
            this._isFrozen = output._isFrozen;
        }

        public override MoneroOutputWallet Clone()
        {
            return new MoneroOutputWallet(this);
        }

        public override MoneroTxWallet? GetTx()
        {
            return (MoneroTxWallet?)base.GetTx();
        }

        public override MoneroOutputWallet SetKeyImage(MoneroKeyImage? keyImage)
        {
            base.SetKeyImage(keyImage);
            return this;
        }

        public override MoneroOutputWallet SetTx(MoneroTx? tx)
        {
            //if (tx != null && !(tx instanceof MoneroTxWallet)) throw new MoneroError("Wallet output's transaction must be of type MoneroTxWallet");
            base.SetTx(tx);
            return this;
        }

        public uint? GetAccountIndex()
        {
            return _accountIndex;
        }

        public virtual MoneroOutputWallet SetAccountIndex(uint? accountIndex)
        {
            this._accountIndex = accountIndex;
            return this;
        }

        public uint? GetSubaddressIndex()
        {
            return _subaddressIndex;
        }

        public virtual MoneroOutputWallet SetSubaddressIndex(uint? subaddressIndex)
        {
            this._subaddressIndex = subaddressIndex;
            return this;
        }

        public override MoneroOutputWallet SetAmount(ulong? amount)
        {
            base.SetAmount(amount);
            return this;
        }

        public bool? IsSpent()
        {
            return _isSpent;
        }

        public virtual MoneroOutputWallet SetIsSpent(bool? isSpent)
        {
            this._isSpent = isSpent;
            return this;
        }

        public bool? IsFrozen()
        {
            return _isFrozen;
        }

        public virtual MoneroOutputWallet SetIsFrozen(bool? isFrozen)
        {
            this._isFrozen = isFrozen;
            return this;
        }


        public bool? IsLocked()
        {
            if (GetTx() == null) return null;
            return GetTx()!.IsLocked();
        }
    }
}
