using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an output of a wallet, which is an output that the wallet has or had the ability to spend. It is in its transaction, as an input
/// when the wallet spent it and as an output when the wallet received it.
/// </summary>
public class MoneroOutputWallet : MoneroOutput
{
    /// <summary>
    /// Initialize a new wallet output.
    /// </summary>
    public MoneroOutputWallet()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this output as a deep copy of the given output. The transaction is not copied, and the copy has none.
    /// </summary>
    /// <param name="output">the output to copy.</param>
    public MoneroOutputWallet(MoneroOutputWallet output) : base(output)
    {
        AccountIndex = output.AccountIndex;
        SubaddressIndex = output.SubaddressIndex;
        IsSpent = output.IsSpent;
        IsFrozen = output.IsFrozen;
    }

    /// <summary>
    /// Copy this output.
    /// </summary>
    /// <returns>a deep copy of the output, with no transaction.</returns>
    public override MoneroOutputWallet Copy()
    {
        return new MoneroOutputWallet(this);
    }

    /// <summary>
    /// Get the transaction of the output as a transaction of a wallet.
    /// </summary>
    /// <returns>the transaction, or <c>null</c> if the output has none or it is not one of a wallet.</returns>
    public MoneroTxWallet? GetTxWallet()
    {
        return Tx as MoneroTxWallet;
    }

    /// <summary>
    /// Index of the account that has the output.
    /// </summary>
    [JsonPropertyName("accountIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? AccountIndex { get; set; }

    /// <summary>
    /// Index of the subaddress that has the output, in its account.
    /// </summary>
    [JsonPropertyName("subaddressIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? SubaddressIndex { get; set; }

    /// <summary>
    /// Whether the output is spent.
    /// </summary>
    [JsonPropertyName("isSpent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSpent { get; set; }

    /// <summary>
    /// Whether the output is frozen, which keeps the wallet from spending it.
    /// </summary>
    [JsonPropertyName("isFrozen")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsFrozen { get; set; }
}
