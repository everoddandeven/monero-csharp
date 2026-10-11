using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Monero daemon connection span.
/// </summary>
public class MoneroConnectionSpan
{
    /// <summary>
    /// Unique id of span connection.
    /// </summary>
    [JsonPropertyName("connectionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Number of blocks in current span.
    /// </summary>
    [JsonPropertyName("numBlocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumBlocks { get; set; }
    
    /// <summary>
    /// Peer address from which the node is downloading or has downloaded the span.
    /// </summary>
    [JsonPropertyName("remoteAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RemoteAddress { get; set; }
    
    /// <summary>
    /// Connection rate.
    /// </summary>
    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Rate { get; set; }
    
    /// <summary>
    /// Connection speed.
    /// </summary>
    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Speed { get; set; }
    
    /// <summary>
    /// Total size in bytes of all blocks (including transactions) included in current span.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Size { get; set; }

    /// <summary>
    /// Block height of the first block in current span.
    /// </summary>
    [JsonPropertyName("startHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? StartHeight { get; set; }

}