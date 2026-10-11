using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a RPC payment information.
/// </summary>
public class MoneroRpcPaymentInfo
{
    /// <summary>
    /// Number of credits available to the requesting RPC client.
    /// </summary>
    [JsonPropertyName("credits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Credits { get; set; }
    
    /// <summary>
    /// Hash of last block added to blockchain.
    /// </summary>
    [JsonPropertyName("topBlockHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TopBlockHash { get; set; }
}