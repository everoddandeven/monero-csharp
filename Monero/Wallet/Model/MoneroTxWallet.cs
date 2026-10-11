using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Models a transaction in the context of a wallet: the transfers that it has for the wallet, and the outputs that the wallet spent or
/// received in it. It is in its block, which is the one without a height when the transaction is in the pool or was not relayed, and in its
/// transaction set when the wallet made it.
/// </summary>
public class MoneroTxWallet : MoneroTx
{
    private List<MoneroIncomingTransfer>? _incomingTransfers;
    private MoneroOutgoingTransfer? _outgoingTransfer;

    /// <summary>
    /// Initialize a new transaction of a wallet.
    /// </summary>
    public MoneroTxWallet()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this transaction as a deep copy of the given transaction. The block and the transaction set are not copied, and the copy
    /// has none. The transfers and the outputs are copied, and they are in the new transaction.
    /// </summary>
    /// <param name="tx">the transaction to copy.</param>
    public MoneroTxWallet(MoneroTxWallet tx) : base(tx)
    {
        IsIncoming = tx.IsIncoming;
        IsOutgoing = tx.IsOutgoing;
        Note = tx.Note;
        InputSum = tx.InputSum;
        OutputSum = tx.OutputSum;
        ChangeAddress = tx.ChangeAddress;
        ChangeAmount = tx.ChangeAmount;
        NumDummyOutputs = tx.NumDummyOutputs;
        ExtraHex = tx.ExtraHex;

        if (tx.IncomingTransfers != null)
        {
            IncomingTransfers = [.. tx.IncomingTransfers.Select(transfer => transfer?.Copy()!)];
        }

        OutgoingTransfer = tx.OutgoingTransfer?.Copy();
    }

    /// <summary>
    /// Copy this transaction.
    /// </summary>
    /// <returns>a deep copy of the transaction, with no block and no transaction set.</returns>
    public override MoneroTxWallet Copy()
    {
        return new MoneroTxWallet(this);
    }

    /// <summary>
    /// Transaction set that the transaction is in, when the wallet made it. It is set when the set gets the transaction, and it is not in the
    /// JSON.
    /// </summary>
    [JsonIgnore]
    public MoneroTxSet? TxSet { get; set; }

    /// <summary>
    /// Whether the wallet receives funds in the transaction.
    /// </summary>
    [JsonPropertyName("isIncoming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsIncoming { get; set; }

    /// <summary>
    /// Whether the wallet sends funds in the transaction.
    /// </summary>
    [JsonPropertyName("isOutgoing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOutgoing { get; set; }

    /// <summary>
    /// Incoming transfers of the transaction, one for each subaddress of the wallet that receives. Assigning the list sets the transaction of
    /// each transfer.
    /// </summary>
    [JsonPropertyName("incomingTransfers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroIncomingTransfer>? IncomingTransfers
    {
        get => _incomingTransfers;
        set
        {
            _incomingTransfers = value;
            if (value == null) return;
            foreach (var transfer in value)
            {
                if (transfer != null) transfer.Tx = this;
            }
        }
    }

    /// <summary>
    /// Outgoing transfer of the transaction. Assigning it sets the transaction of the transfer.
    /// </summary>
    [JsonPropertyName("outgoingTransfer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroOutgoingTransfer? OutgoingTransfer
    {
        get => _outgoingTransfer;
        set
        {
            _outgoingTransfer = value;
            if (value != null) value.Tx = this;
        }
    }

    /// <summary>
    /// Note that the wallet has for the transaction.
    /// </summary>
    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; set; }

    /// <summary>
    /// Sum of the inputs of the transaction, in atomic units. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("inputSum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? InputSum { get; set; }

    /// <summary>
    /// Sum of the outputs of the transaction, in atomic units. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("outputSum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? OutputSum { get; set; }

    /// <summary>
    /// Address that receives the change of the transaction. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("changeAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChangeAddress { get; set; }

    /// <summary>
    /// Amount of the change of the transaction, in atomic units. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("changeAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ChangeAmount { get; set; }

    /// <summary>
    /// Number of dummy outputs of the transaction. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("numDummyOutputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? NumDummyOutputs { get; set; }

    /// <summary>
    /// Extra of the transaction, in hexadecimal format. The wallet knows it for a transaction that it creates.
    /// </summary>
    [JsonPropertyName("extraHex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExtraHex { get; set; }

    /// <summary>
    /// Inputs of the transaction that the wallet spent, which are read as wallet outputs. Assigning the list sets the transaction of each input.
    /// </summary>
    [JsonPropertyName("inputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(MoneroOutputListConverter<MoneroOutputWallet>))]
    public override List<MoneroOutput>? Inputs
    {
        get => base.Inputs;
        set => base.Inputs = value;
    }

    /// <summary>
    /// Outputs of the transaction that the wallet received, which are read as wallet outputs. Assigning the list sets the transaction of each
    /// output.
    /// </summary>
    [JsonPropertyName("outputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(MoneroOutputListConverter<MoneroOutputWallet>))]
    public override List<MoneroOutput>? Outputs
    {
        get => base.Outputs;
        set => base.Outputs = value;
    }

    /// <summary>
    /// Get the inputs of the transaction as wallet outputs.
    /// </summary>
    /// <returns>the inputs that are wallet outputs, in order. It is a new list.</returns>
    public List<MoneroOutputWallet> GetInputsWallet()
    {
        return Inputs?.OfType<MoneroOutputWallet>().ToList() ?? [];
    }

    /// <summary>
    /// Get the outputs of the transaction as wallet outputs.
    /// </summary>
    /// <returns>the outputs that are wallet outputs, in order. It is a new list.</returns>
    public List<MoneroOutputWallet> GetOutputsWallet()
    {
        return Outputs?.OfType<MoneroOutputWallet>().ToList() ?? [];
    }

    /// <summary>
    /// Get the transfers of the transaction.
    /// </summary>
    /// <returns>the outgoing transfer, if there is one, and then the incoming transfers. It is a new list.</returns>
    public List<MoneroTransfer> GetTransfers()
    {
        var transfers = new List<MoneroTransfer>();
        if (OutgoingTransfer != null) transfers.Add(OutgoingTransfer);
        if (IncomingTransfers != null) transfers.AddRange(IncomingTransfers.Where(transfer => transfer != null));
        return transfers;
    }

    /// <summary>
    /// Get the amount that the wallet receives in the transaction.
    /// </summary>
    /// <returns>the sum of the amounts of the incoming transfers, in atomic units.</returns>
    public ulong GetIncomingAmount()
    {
        ulong total = 0;
        foreach (var transfer in IncomingTransfers ?? [])
        {
            total += transfer?.Amount ?? 0;
        }

        return total;
    }

    /// <summary>
    /// Get the amount that the wallet sends in the transaction.
    /// </summary>
    /// <returns>the amount of the outgoing transfer, in atomic units, or 0 if there is none.</returns>
    public ulong GetOutgoingAmount()
    {
        return OutgoingTransfer?.Amount ?? 0;
    }
}
