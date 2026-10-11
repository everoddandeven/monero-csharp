using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models a subaddress of an account of a wallet. The primary address of an account is its subaddress 0.
/// </summary>
public class MoneroSubaddress
{
    /// <summary>
    /// Initialize a new subaddress.
    /// </summary>
    public MoneroSubaddress()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new subaddress.
    /// </summary>
    /// <param name="address">the address.</param>
    public MoneroSubaddress(string address)
    {
        Address = address;
    }

    /// <summary>
    /// Initialize a new subaddress.
    /// </summary>
    /// <param name="accountIndex">the index of the account of the subaddress.</param>
    /// <param name="index">the index of the subaddress in its account.</param>
    public MoneroSubaddress(uint accountIndex, uint index)
    {
        AccountIndex = accountIndex;
        Index = index;
    }

    /// <summary>
    /// Construct this subaddress as a copy of the given subaddress.
    /// </summary>
    /// <param name="subaddress">the subaddress to copy.</param>
    public MoneroSubaddress(MoneroSubaddress subaddress)
    {
        AccountIndex = subaddress.AccountIndex;
        Index = subaddress.Index;
        Address = subaddress.Address;
        Label = subaddress.Label;
        Balance = subaddress.Balance;
        UnlockedBalance = subaddress.UnlockedBalance;
        NumUnspentOutputs = subaddress.NumUnspentOutputs;
        IsUsed = subaddress.IsUsed;
        NumBlocksToUnlock = subaddress.NumBlocksToUnlock;
    }

    /// <summary>
    /// Copy this subaddress.
    /// </summary>
    /// <returns>a copy of the subaddress.</returns>
    public virtual MoneroSubaddress Copy()
    {
        return new MoneroSubaddress(this);
    }

    /// <summary>
    /// Index of the account that has the subaddress.
    /// </summary>
    [JsonPropertyName("accountIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? AccountIndex { get; set; }

    /// <summary>
    /// Index of the subaddress in its account.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Index { get; set; }

    /// <summary>
    /// The address.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Label of the subaddress.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    /// <summary>
    /// Balance of the subaddress, in atomic units.
    /// </summary>
    [JsonPropertyName("balance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Balance { get; set; }

    /// <summary>
    /// Balance of the subaddress that can be spent now, in atomic units.
    /// </summary>
    [JsonPropertyName("unlockedBalance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnlockedBalance { get; set; }

    /// <summary>
    /// Number of unspent outputs of the subaddress.
    /// </summary>
    [JsonPropertyName("numUnspentOutputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumUnspentOutputs { get; set; }

    /// <summary>
    /// Whether the subaddress received funds.
    /// </summary>
    [JsonPropertyName("isUsed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsUsed { get; set; }

    /// <summary>
    /// Number of blocks until the funds of the subaddress that are locked can be spent.
    /// </summary>
    [JsonPropertyName("numBlocksToUnlock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumBlocksToUnlock { get; set; }
}
