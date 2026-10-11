using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an account of a wallet.
/// </summary>
public class MoneroAccount
{
    /// <summary>
    /// Initialize a new account.
    /// </summary>
    public MoneroAccount()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new account.
    /// </summary>
    /// <param name="index">the index of the account.</param>
    /// <param name="primaryAddress">the primary address of the account.</param>
    public MoneroAccount(uint index, string primaryAddress)
    {
        Index = index;
        PrimaryAddress = primaryAddress;
    }

    /// <summary>
    /// Construct this account as a deep copy of the given account.
    /// </summary>
    /// <param name="account">the account to copy.</param>
    public MoneroAccount(MoneroAccount account)
    {
        Index = account.Index;
        PrimaryAddress = account.PrimaryAddress;
        Balance = account.Balance;
        UnlockedBalance = account.UnlockedBalance;
        Tag = account.Tag;

        if (account.Subaddresses != null)
        {
            Subaddresses = [.. account.Subaddresses.Select(subaddress => subaddress?.Copy()!)];
        }
    }

    /// <summary>
    /// Copy this account.
    /// </summary>
    /// <returns>a deep copy of the account.</returns>
    public virtual MoneroAccount Copy()
    {
        return new MoneroAccount(this);
    }

    /// <summary>
    /// Index of the account.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Index { get; set; }

    /// <summary>
    /// Primary address of the account.
    /// </summary>
    [JsonPropertyName("primaryAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrimaryAddress { get; set; }

    /// <summary>
    /// Balance of the account, in atomic units.
    /// </summary>
    [JsonPropertyName("balance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Balance { get; set; }

    /// <summary>
    /// Balance of the account that can be spent now, in atomic units.
    /// </summary>
    [JsonPropertyName("unlockedBalance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? UnlockedBalance { get; set; }

    /// <summary>
    /// Tag of the account.
    /// </summary>
    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }

    /// <summary>
    /// Subaddresses of the account, when they are fetched.
    /// </summary>
    [JsonPropertyName("subaddresses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroSubaddress>? Subaddresses { get; set; }
}
