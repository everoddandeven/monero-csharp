using Monero.Common;

namespace Monero.Wallet.Common
{
    public class MoneroTxWallet : MoneroTx
    {
        private MoneroTxSet? _txSet;
        private bool? _isIncoming;
        private bool? _isOutgoing;
        private List<MoneroIncomingTransfer>? _incomingTransfers;
        private MoneroOutgoingTransfer? _outgoingTransfer;
        private string? _note;
        private bool? _isLocked;
        private ulong? _inputSum;
        private ulong? _outputSum;
        private string? _changeAddress;
        private ulong? _changeAmount;
        private uint? _numDummyOutputs;
        private string? _extraHex;  // TODO: refactor MoneroTx to only use extra as hex string

        public bool Equals(MoneroTxWallet other, bool checkInputs = true, bool checkOutputs = true)
        {
            if (!base.Equals(other, checkInputs, checkOutputs)) return false;
            
            return IsIncoming() == other.IsIncoming() &&
                   IsOutgoing() == other.IsOutgoing() &&
                   GetNote() == other.GetNote() &&
                   IsLocked() == other.IsLocked() &&
                   GetInputSum() == other.GetInputSum() &&
                   GetChangeAddress() == other.GetChangeAddress() &&
                   GetChangeAmount() == other.GetChangeAmount() &&
                   GetNumDummyOutputs() == other.GetNumDummyOutputs() &&
                   GetExtraHex() == other.GetExtraHex();
        }
        
        public MoneroTxWallet()
        {
            // nothing to initialize
        }

        public MoneroTxWallet(MoneroTxWallet tx) : base(tx)
        {
            _txSet = tx._txSet;
            _isIncoming = tx._isIncoming;
            _isOutgoing = tx._isOutgoing;
            if (tx._incomingTransfers != null)
            {
                _incomingTransfers = new List<MoneroIncomingTransfer>();
                foreach (MoneroIncomingTransfer transfer in tx._incomingTransfers)
                {
                    _incomingTransfers.Add(transfer.Clone().SetTx(this));
                }
            }
            if (tx._outgoingTransfer != null) _outgoingTransfer = tx._outgoingTransfer.Clone().SetTx(this);
            _note = tx._note;
            _isLocked = tx._isLocked;
            _inputSum = tx._inputSum;
            _outputSum = tx._outputSum;
            _changeAddress = tx._changeAddress;
            _changeAmount = tx._changeAmount;
            _numDummyOutputs = tx._numDummyOutputs;
            _extraHex = tx._extraHex;
        }

        public override MoneroTxWallet Clone()
        {
            return new MoneroTxWallet(this);
        }

        public override MoneroTxWallet Merge(MoneroTx tx)
        {
            if (tx != null && tx is not MoneroTxWallet) throw new MoneroError("Wallet transaction must be merged with type MoneroTxWallet");
            return Merge((MoneroTxWallet)tx!);
        }

        public MoneroTxWallet Merge(MoneroTxWallet tx)
        {
            if (!(tx is MoneroTxWallet)) throw new MoneroError("Wallet transaction must be merged with type MoneroTxWallet");
            if (this == tx) return this;

            // merge base classes
            base.Merge(tx);

            // merge tx set if they're different which comes back to merging txs
            if (_txSet != tx.GetTxSet())
            {
                if (_txSet == null)
                {
                    _txSet = new MoneroTxSet();
                    _txSet.SetTxs([this]);
                }
                if (tx.GetTxSet() == null)
                {
                    tx.SetTxSet(new MoneroTxSet());
                    tx.GetTxSet()!.SetTxs([tx]);
                }
                _txSet.Merge(tx.GetTxSet());
                return this;
            }

            // merge incoming transfers
            if (tx.GetIncomingTransfers() != null)
            {
                if (GetIncomingTransfers() == null) SetIncomingTransfers(new List<MoneroIncomingTransfer>());
                foreach (MoneroIncomingTransfer transfer in tx.GetIncomingTransfers()!)
                {
                    transfer.SetTx(this);
                    MergeIncomingTransfer(GetIncomingTransfers()!, transfer);
                }
            }

            // merge outgoing transfer
            if (tx.GetOutgoingTransfer() != null)
            {
                tx.GetOutgoingTransfer()!.SetTx(this);
                if (GetOutgoingTransfer() == null) SetOutgoingTransfer(tx.GetOutgoingTransfer());
                else GetOutgoingTransfer()!.Merge(tx.GetOutgoingTransfer()!);
            }

            // merge simple extensions
            SetIsIncoming(GenUtils.Reconcile(IsIncoming(), tx.IsIncoming(), null, true, null)); // outputs seen on confirmation
            SetIsOutgoing(GenUtils.Reconcile(IsOutgoing(), tx.IsOutgoing()));
            SetNote(GenUtils.Reconcile(GetNote(), tx.GetNote()));
            SetIsLocked(GenUtils.Reconcile(IsLocked(), tx.IsLocked(), null, false, null));  // tx can become unlocked
            SetInputSum(GenUtils.Reconcile(GetInputSum(), tx.GetInputSum()));
            SetOutputSum(GenUtils.Reconcile(GetOutputSum(), tx.GetOutputSum()));
            SetChangeAddress(GenUtils.Reconcile(GetChangeAddress(), tx.GetChangeAddress()));
            SetChangeAmount(GenUtils.Reconcile(GetChangeAmount(), tx.GetChangeAmount()));
            SetNumDummyOutputs(GenUtils.Reconcile(GetNumDummyOutputs(), tx.GetNumDummyOutputs()));
            SetExtraHex(GenUtils.Reconcile(GetExtraHex(), tx.GetExtraHex()));

            return this;  // for chaining
        }

        // private helper to merge transfers
        private static void MergeIncomingTransfer(List<MoneroIncomingTransfer> transfers, MoneroIncomingTransfer transfer)
        {
            foreach (MoneroIncomingTransfer aTransfer in transfers)
            {
                if (aTransfer.GetAccountIndex() == transfer.GetAccountIndex() && aTransfer.GetSubaddressIndex() == transfer.GetSubaddressIndex())
                {
                    aTransfer.Merge(transfer);
                    return;
                }
            }
            transfers.Add(transfer);
        }

        public override MoneroTxWallet SetInTxPool(bool? inTxPool)
        {
            base.SetInTxPool(inTxPool);
            return this;
        }

        public MoneroTxSet? GetTxSet()
        {
            return _txSet;
        }

        public virtual MoneroTxWallet SetTxSet(MoneroTxSet txSet)
        {
            _txSet = txSet;
            return this;
        }

        public virtual bool? IsIncoming()
        {
            return _isIncoming;
        }

        public virtual MoneroTxWallet SetIsIncoming(bool? isIncoming)
        {
            _isIncoming = isIncoming;
            return this;
        }

        public virtual bool? IsOutgoing()
        {
            return _isOutgoing;
        }

        public virtual MoneroTxWallet SetIsOutgoing(bool? isOutgoing)
        {
            _isOutgoing = isOutgoing;
            return this;
        }

        public ulong? GetIncomingAmount()
        {
            if (GetIncomingTransfers() == null) return null;
            ulong incomingAmt = 0;
            foreach (MoneroIncomingTransfer transfer in GetIncomingTransfers()!) incomingAmt += (ulong)transfer.GetAmount()!;
            return incomingAmt;
        }

        public ulong? GetOutgoingAmount()
        {
            return GetOutgoingTransfer() != null ? GetOutgoingTransfer()!.GetAmount() : null;
        }

        public List<MoneroTransfer> GetTransfers(MoneroTransferQuery? query = null)
        {
            List<MoneroTransfer> transfers = new List<MoneroTransfer>();
            if (GetOutgoingTransfer() != null && (query == null || query.MeetsCriteria(GetOutgoingTransfer()))) transfers.Add(GetOutgoingTransfer()!);
            if (GetIncomingTransfers() != null)
            {
                foreach (MoneroIncomingTransfer transfer in GetIncomingTransfers()!)
                {
                    if (query == null || query.MeetsCriteria(transfer)) transfers.Add(transfer);
                }
            }
            return transfers;
        }

        public List<MoneroTransfer> FilterTransfers(MoneroTransferQuery? query)
        {
            List<MoneroTransfer> transfers = new List<MoneroTransfer>();

            // collect outgoing transfer or erase if filtered
            if (GetOutgoingTransfer() != null && (query == null || query.MeetsCriteria(GetOutgoingTransfer()))) transfers.Add(GetOutgoingTransfer()!);
            else SetOutgoingTransfer(null);

            // collect incoming transfers or erase if filtered
            if (GetIncomingTransfers() != null)
            {
                var toRemoves = new HashSet<MoneroTransfer>();
                foreach (MoneroIncomingTransfer transfer in GetIncomingTransfers()!)
                {
                    if (query == null || query.MeetsCriteria(transfer)) transfers.Add(transfer);
                    else toRemoves.Add(transfer);
                }
                
                GetIncomingTransfers()!.RemoveAll(x => toRemoves.Contains(x));
                if (GetIncomingTransfers()!.Count == 0) SetIncomingTransfers(null);
            }

            return transfers;
        }

        public List<MoneroIncomingTransfer>? GetIncomingTransfers()
        {
            return _incomingTransfers;
        }

        public virtual MoneroTxWallet SetIncomingTransfers(List<MoneroIncomingTransfer>? incomingTransfers)
        {
            _incomingTransfers = incomingTransfers;
            return this;
        }

        public MoneroOutgoingTransfer? GetOutgoingTransfer()
        {
            return _outgoingTransfer;
        }

        public virtual MoneroTxWallet SetOutgoingTransfer(MoneroOutgoingTransfer? outgoingTransfer)
        {
            _outgoingTransfer = outgoingTransfer;
            return this;
        }


        public override MoneroTxWallet SetInputs(List<MoneroOutput>? inputs)
        {

            // Validate that all inputs are wallet inputs
            if (inputs != null)
            {
                foreach (MoneroOutput input in inputs)
                {
                    MoneroOutputWallet inputw = (MoneroOutputWallet)input;
                    if (inputw == null) throw new MoneroError("Wallet transaction inputs must be of type MoneroOutputWallet");
                }
            }

            base.SetInputs(inputs);
            return this;
        }
  
        public virtual MoneroTxWallet SetInputsWallet(List<MoneroOutputWallet> inputs)
        {
            return SetInputs([..inputs]);
        }

        public List<MoneroOutputWallet> GetInputsWallet(MoneroOutputQuery? query = null)
        {
            List<MoneroOutputWallet> inputsWallet = new List<MoneroOutputWallet>();
            List<MoneroOutput>? inputs = GetInputs();
            if (inputs == null) return inputsWallet;
            foreach (MoneroOutput output in inputs)
            {
                if (query == null || query.MeetsCriteria((MoneroOutputWallet)output)) inputsWallet.Add((MoneroOutputWallet)output);
            }
            return inputsWallet;
        }

        public override MoneroTxWallet SetOutputs(List<MoneroOutput>? outputs)
        {

            // Validate that all outputs are wallet outputs
            if (outputs != null)
            {
                foreach (MoneroOutput output in outputs)
                {
                    MoneroOutputWallet outw = (MoneroOutputWallet)output;
                    if (outw == null) throw new MoneroError("Wallet transaction outputs must be of type MoneroOutputWallet");
                }
            }
            base.SetOutputs(outputs);
            return this;
        }


        public virtual MoneroTxWallet SetOutputsWallet(List<MoneroOutputWallet> outputs)
        {
            return SetOutputs([.. outputs]);
        }

        public List<MoneroOutputWallet> GetOutputsWallet(MoneroOutputQuery? query = null)
        {
            List<MoneroOutputWallet> outputsWallet = new List<MoneroOutputWallet>();
            List<MoneroOutput>? outputs = GetOutputs();
            if (outputs == null) return outputsWallet;
            foreach (MoneroOutput output in outputs)
            {
                if (query == null || query.MeetsCriteria((MoneroOutputWallet)output)) outputsWallet.Add((MoneroOutputWallet)output);
            }
            return outputsWallet;
        }

        public List<MoneroOutputWallet> FilterOutputsWallet(MoneroOutputQuery query)
        {
            List<MoneroOutputWallet> outputs = new List<MoneroOutputWallet>();
            if (GetOutputs() != null)
            {
                var toRemoves = new HashSet<MoneroOutput>();
                foreach (MoneroOutput output in GetOutputs()!)
                {
                    if (query == null || query.MeetsCriteria((MoneroOutputWallet)output)) outputs.Add((MoneroOutputWallet)output);
                    else toRemoves.Add(output);
                }
                
                GetOutputs()!.RemoveAll(x => toRemoves.Contains(x));
                
                if (GetOutputs()!.Count == 0) SetOutputs(null);
            }
            return outputs;
        }

        public string? GetNote()
        {
            return _note;
        }

        public virtual MoneroTxWallet SetNote(string? note)
        {
            _note = note;
            return this;
        }

        public bool? IsLocked()
        {
            return _isLocked;
        }

        public virtual MoneroTxWallet SetIsLocked(bool? isLocked)
        {
            _isLocked = isLocked;
            return this;
        }

        public ulong? GetInputSum()
        {
            return _inputSum;
        }

        public virtual MoneroTxWallet SetInputSum(ulong? inputSum)
        {
            _inputSum = inputSum;
            return this;
        }

        public ulong? GetOutputSum()
        {
            return _outputSum;
        }

        public virtual MoneroTxWallet SetOutputSum(ulong? outputSum)
        {
            _outputSum = outputSum;
            return this;
        }

        public string? GetChangeAddress()
        {
            return _changeAddress;
        }

        public virtual MoneroTxWallet SetChangeAddress(string? changeAddress)
        {
            _changeAddress = changeAddress;
            return this;
        }

        public ulong? GetChangeAmount()
        {
            return _changeAmount;
        }

        public virtual MoneroTxWallet SetChangeAmount(ulong? changeAmount)
        {
            _changeAmount = changeAmount;
            return this;
        }

        public uint? GetNumDummyOutputs()
        {
            return _numDummyOutputs;
        }

        public virtual MoneroTxWallet SetNumDummyOutputs(uint? numDummyOutputs)
        {
            _numDummyOutputs = numDummyOutputs;
            return this;
        }

        public string? GetExtraHex()
        {
            return _extraHex;
        }

        public virtual MoneroTxWallet SetExtraHex(string? extraHex)
        {
            _extraHex = extraHex;
            return this;
        }
    }
}
