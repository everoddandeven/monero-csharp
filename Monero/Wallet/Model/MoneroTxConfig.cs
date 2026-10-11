using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Configures a transaction to send, to sweep or to make a payment URI for. The JSON is the one that monero-c reads.
/// </summary>
public class MoneroTxConfig
{
    /// <summary>
    /// Initialize a new transaction config.
    /// </summary>
    public MoneroTxConfig()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new transaction config with a destination.
    /// </summary>
    /// <param name="address">the address to send to.</param>
    /// <param name="amount">the amount to send, in atomic units, or <c>null</c> for none.</param>
    public MoneroTxConfig(string address, ulong? amount = null)
    {
        AddDestination(address, amount);
    }

    /// <summary>
    /// Construct this config as a deep copy of the given config.
    /// </summary>
    /// <param name="config">the config to copy.</param>
    public MoneroTxConfig(MoneroTxConfig config)
    {
        PaymentId = config.PaymentId;
        Priority = config.Priority;
        RingSize = config.RingSize;
        Fee = config.Fee;
        AccountIndex = config.AccountIndex;
        CanSplit = config.CanSplit;
        Relay = config.Relay;
        Note = config.Note;
        RecipientName = config.RecipientName;
        BelowAmount = config.BelowAmount;
        SweepEachSubaddress = config.SweepEachSubaddress;
        KeyImage = config.KeyImage;

        if (config.Destinations != null)
        {
            Destinations = [.. config.Destinations.Select(destination => destination?.Copy()!)];
        }

        if (config.SubtractFeeFrom != null)
        {
            SubtractFeeFrom = [.. config.SubtractFeeFrom];
        }

        if (config.SubaddressIndices != null)
        {
            SubaddressIndices = [.. config.SubaddressIndices];
        }
    }

    /// <summary>
    /// Copy this config.
    /// </summary>
    /// <returns>a deep copy of the config.</returns>
    public virtual MoneroTxConfig Copy()
    {
        return new MoneroTxConfig(this);
    }

    /// <summary>
    /// Add a destination to send to.
    /// </summary>
    /// <param name="address">the address to send to.</param>
    /// <param name="amount">the amount to send, in atomic units, or <c>null</c> for none.</param>
    /// <returns>this config, to chain the calls.</returns>
    public MoneroTxConfig AddDestination(string address, ulong? amount = null)
    {
        Destinations ??= [];
        Destinations.Add(new MoneroDestination(address, amount));
        return this;
    }

    /// <summary>
    /// Destinations to send to.
    /// </summary>
    [JsonPropertyName("destinations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroDestination>? Destinations { get; set; }

    /// <summary>
    /// Address to send to, which is the address of the only destination. Setting it sets the address of that destination, which is added if
    /// there is none.
    /// </summary>
    /// <exception cref="InvalidOperationException">the config has more than one destination, so the address is not one.</exception>
    [JsonIgnore]
    public string? Address
    {
        get => Destinations is { Count: 1 } ? Destinations[0].Address : null;
        set
        {
            if (Destinations is { Count: > 1 }) throw new InvalidOperationException("The address can't be set because the config has more than one destination");
            if (Destinations is not { Count: 1 }) Destinations = [new MoneroDestination()];
            Destinations[0].Address = value;
        }
    }

    /// <summary>
    /// Amount to send, which is the amount of the only destination, in atomic units. Setting it sets the amount of that destination, which is
    /// added if there is none.
    /// </summary>
    /// <exception cref="InvalidOperationException">the config has more than one destination, so the amount is not one.</exception>
    [JsonIgnore]
    public ulong? Amount
    {
        get => Destinations is { Count: 1 } ? Destinations[0].Amount : null;
        set
        {
            if (Destinations is { Count: > 1 }) throw new InvalidOperationException("The amount can't be set because the config has more than one destination");
            if (Destinations is not { Count: 1 }) Destinations = [new MoneroDestination()];
            Destinations[0].Amount = value;
        }
    }

    /// <summary>
    /// Indices of the destinations that pay the fee, which is split among them.
    /// </summary>
    [JsonPropertyName("subtractFeeFrom")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<uint>? SubtractFeeFrom { get; set; }

    /// <summary>
    /// Payment id of the transaction, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("paymentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Priority of the fee of the transaction.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroTxPriority? Priority { get; set; }

    /// <summary>
    /// Ring size of the transaction. monero-cpp ignores it, and the network decides it.
    /// </summary>
    [JsonPropertyName("ringSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? RingSize { get; set; }

    /// <summary>
    /// Fee of the transaction, in atomic units. The wallet decides it when it is not set.
    /// </summary>
    [JsonPropertyName("fee")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Fee { get; set; }

    /// <summary>
    /// Index of the account to send from.
    /// </summary>
    [JsonPropertyName("accountIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? AccountIndex { get; set; }

    /// <summary>
    /// Indices of the subaddresses of the account to send from.
    /// </summary>
    [JsonPropertyName("subaddressIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<uint>? SubaddressIndices { get; set; }

    /// <summary>
    /// Whether the wallet can split the transfer in several transactions, when one is too big.
    /// </summary>
    [JsonPropertyName("canSplit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanSplit { get; set; }

    /// <summary>
    /// Whether to relay the transaction to the network. It is not relayed when it is not set.
    /// </summary>
    [JsonPropertyName("relay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Relay { get; set; }

    /// <summary>
    /// Note to keep for the transaction.
    /// </summary>
    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; set; }

    /// <summary>
    /// Name of the recipient, for a payment URI.
    /// </summary>
    [JsonPropertyName("recipientName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RecipientName { get; set; }

    /// <summary>
    /// Amount below which the outputs are swept, in atomic units.
    /// </summary>
    [JsonPropertyName("belowAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BelowAmount { get; set; }

    /// <summary>
    /// Whether to sweep each subaddress to its own transaction.
    /// </summary>
    [JsonPropertyName("sweepEachSubaddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SweepEachSubaddress { get; set; }

    /// <summary>
    /// Key image of the output to sweep, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("keyImage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyImage { get; set; }
}
