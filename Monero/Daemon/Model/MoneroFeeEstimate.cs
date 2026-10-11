using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models an estimation on fees per byte.
/// </summary>
public class MoneroFeeEstimate
{
    /// <summary>
    /// Fee per byte estimation (atomic units).
    /// </summary>
    [JsonPropertyName("fee")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Fee { get; set; }
    
    /// <summary>
    /// Default fees ordered by transaction priority.
    /// </summary>
    [JsonPropertyName("fees")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? Fees { get; set; }
    
    /// <summary>
    /// Quantization mask used to estimate fee.
    /// Final fee estimation should be rounded up to an even multiple of this value.
    /// </summary>
    [JsonPropertyName("quantizationMask")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? QuantizationMask { get; set; }
}