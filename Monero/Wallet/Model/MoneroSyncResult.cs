using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Models the result of synchronizing a wallet.
/// </summary>
public class MoneroSyncResult
{
    /// <summary>
    /// Initialize a new sync result.
    /// </summary>
    public MoneroSyncResult()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new sync result.
    /// </summary>
    /// <param name="numBlocksFetched">the number of blocks that the sync fetched.</param>
    /// <param name="receivedMoney">whether the wallet received funds during the sync.</param>
    public MoneroSyncResult(ulong numBlocksFetched, bool receivedMoney)
    {
        NumBlocksFetched = numBlocksFetched;
        ReceivedMoney = receivedMoney;
    }

    /// <summary>
    /// Number of blocks that the sync fetched.
    /// </summary>
    [JsonPropertyName("numBlocksFetched")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumBlocksFetched { get; set; }

    /// <summary>
    /// Whether the wallet received funds during the sync.
    /// </summary>
    [JsonPropertyName("receivedMoney")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ReceivedMoney { get; set; }
}
