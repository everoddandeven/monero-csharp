using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models transaction pool statistics.
/// </summary>
public class MoneroTxPoolStats
{
    /// <summary>
    /// Number of transactions currently in the pool.
    /// </summary>
    [JsonPropertyName("numTxs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumTxs { get; set; }
    
    /// <summary>
    /// Number of transactions in poll but still not relayed to the network.
    /// </summary>
    [JsonPropertyName("numNotRelayed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumNotRelayed { get; set; }
    
    /// <summary>
    /// Number of failing transactions currently in pool.
    /// </summary>
    [JsonPropertyName("numFailing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumFailing { get; set; }
    
    /// <summary>
    /// Number of double spend transactions currently in the pool.
    /// </summary>
    [JsonPropertyName("numDoubleSpends")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumDoubleSpends { get; set; }
    
    /// <summary>
    /// Number of transactions in pool for 10 minutes or more.
    /// </summary>
    [JsonPropertyName("num10m")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Num10M { get; set; }
    
    /// <summary>
    /// Sum of fees paid in pool transactions (atomic units).
    /// </summary>
    [JsonPropertyName("feeTotal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? FeeTotal { get; set; }
    
    /// <summary>
    /// Size in bytes of the largest transaction currently in the pool.
    /// </summary>
    [JsonPropertyName("bytesMax")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BytesMax { get; set; }
    
    /// <summary>
    /// Average size in bytes of transactions currently in the pool.
    /// </summary>
    [JsonPropertyName("bytesMed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BytesMed { get; set; }

    /// <summary>
    /// Size in bytes of the smallest transaction currently in the pool.
    /// </summary>
    [JsonPropertyName("bytesMin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BytesMin { get; set; }
    
    /// <summary>
    /// Total size in bytes of all transactions currently in the pool.
    /// </summary>
    [JsonPropertyName("bytesTotal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BytesTotal { get; set; }

    /// <summary>
    /// Histogram of current transaction pool.
    /// Key: number of bytes.
    /// Value: number of transactions.
    /// </summary>
    [JsonPropertyName("histo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<ulong, ulong>? Histo { get; set; }
    
    /// <summary>
    /// The time 98% of transactions are "younger" than.
    /// </summary>
    [JsonPropertyName("histo98pc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Histo98Pc { get; set; }

    /// <summary>
    /// Unix time of the oldest transaction in the pool.
    /// </summary>
    [JsonPropertyName("oldestTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? OldestTimestamp { get; set; }
    
}