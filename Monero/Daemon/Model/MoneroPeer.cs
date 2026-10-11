using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a daemon peer.
/// </summary>
public class MoneroPeer
{
    /// <summary>
    /// Unique peer id.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
    
    /// <summary>
    /// Peer address.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }
    
    /// <summary>
    /// Peer connection hostname.
    /// </summary>
    [JsonPropertyName("host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Host { get; set; }
    
    /// <summary>
    /// Peer connection port.
    /// </summary>
    [JsonPropertyName("port")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Port { get; set; }
    
    /// <summary>
    /// Indicates if peer is online.
    /// </summary>
    [JsonPropertyName("isOnline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOnline { get; set; }

    /// <summary>
    /// Last timestamp peer was seen by the node.
    /// </summary>
    [JsonPropertyName("lastSeenTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? LastSeenTimestamp { get; set; }
    
    /// <summary>
    /// Peer pruning seed height.
    /// </summary>
    [JsonPropertyName("pruningSeed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PruningSeed { get; set; }
    
    /// <summary>
    /// Peer advertised RPC port.
    /// </summary>
    [JsonPropertyName("rpcPort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RpcPort { get; set; }
    
    /// <summary>
    /// RPC credits reward for each calculated hash.
    /// </summary>
    [JsonPropertyName("rpcCreditsPerHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? RpcCreditsPerHash { get; set; }
    
    /// <summary>
    /// Peer unique hash.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; set; }
    
    /// <summary>
    /// Average bytes of data downloaded by node from this peer.
    /// </summary>
    [JsonPropertyName("avgDownload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? AvgDownload { get; set; }
    
    /// <summary>
    /// Average bytes of data uploaded by node to this peer.
    /// </summary>
    [JsonPropertyName("avgUpload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? AvgUpload { get; set; }
    
    /// <summary>
    /// Current bytes downloaded by node from this peer.
    /// </summary>
    [JsonPropertyName("currentDownload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CurrentDownload { get; set; }

    /// <summary>
    /// Current bytes uploaded by node to this peer.
    /// </summary>
    [JsonPropertyName("currentUpload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CurrentUpload { get; set; }
    
    /// <summary>
    /// Peer block height.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Indicates if peer is pulling data from the node.
    /// </summary>
    [JsonPropertyName("isIncoming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsIncoming { get; set; }
    
    /// <summary>
    /// Peer live time.
    /// </summary>
    [JsonPropertyName("liveTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? LiveTime { get; set; }
    
    /// <summary>
    /// Indicates if peer is from local ip.
    /// </summary>
    [JsonPropertyName("isLocalIp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLocalIp { get; set; }
    
    /// <summary>
    /// Indicates if peer is from local host.
    /// </summary>
    [JsonPropertyName("isLocalHost")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLocalHost { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("numReceives")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumReceives { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("numSends")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumSends { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("receiveIdleTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ReceiveIdleTime { get; set; }
    
    /// <summary>
    /// TODO.
    /// </summary>
    [JsonPropertyName("sendIdleTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? SendIdleTime { get; set; }
    
    /// <summary>
    /// Peer state info.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; set; }
    
    /// <summary>
    /// Number of support flags of the peer.
    /// </summary>
    [JsonPropertyName("numSupportFlags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumSupportFlags { get; set; }
    
    /// <summary>
    /// Peer connection type. The JSON of monero-cpp calls it <c>addressType</c>.
    /// </summary>
    [JsonPropertyName("addressType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroConnectionType? ConnectionType { get; set; }
}