using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the tag of accounts: a name that groups accounts, and the label that describes the group.
/// </summary>
public class MoneroAccountTag
{
    /// <summary>
    /// Initialize a new account tag.
    /// </summary>
    public MoneroAccountTag()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new account tag.
    /// </summary>
    /// <param name="tag">the tag.</param>
    /// <param name="label">the label of the tag.</param>
    /// <param name="accountIndices">the indices of the accounts that have the tag.</param>
    public MoneroAccountTag(string tag, string label, List<uint>? accountIndices = null)
    {
        Tag = tag;
        Label = label;
        AccountIndices = accountIndices;
    }

    /// <summary>
    /// The tag.
    /// </summary>
    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }

    /// <summary>
    /// Label of the tag.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    /// <summary>
    /// Indices of the accounts that have the tag.
    /// </summary>
    [JsonPropertyName("accountIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<uint>? AccountIndices { get; set; }
}
