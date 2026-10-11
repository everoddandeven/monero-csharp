using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.Daemon.Model;

/// <summary>
/// Model for the summation of miner emissions and fees.
/// </summary>
public class MoneroMinerTxSum
{
    /// <summary>
    /// Least significant 64 bits of the sum of the new coins emitted (atomic units), which has 128 bits.
    /// </summary>
    [JsonPropertyName("emissionSumLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? EmissionSumLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the sum of the new coins emitted (atomic units).
    /// </summary>
    [JsonPropertyName("emissionSumHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? EmissionSumHigh { get; set; }

    /// <summary>
    /// The sum of the new coins emitted (atomic units), joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? EmissionSum
    {
        get => MoneroUInt128.Combine(EmissionSumLow, EmissionSumHigh);
        set => (EmissionSumLow, EmissionSumHigh) = MoneroUInt128.Split(value);
    }

    /// <summary>
    /// Least significant 64 bits of the sum of the fees (atomic units), which has 128 bits.
    /// </summary>
    [JsonPropertyName("feeSumLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? FeeSumLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the sum of the fees (atomic units).
    /// </summary>
    [JsonPropertyName("feeSumHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? FeeSumHigh { get; set; }

    /// <summary>
    /// The sum of the fees (atomic units), joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? FeeSum
    {
        get => MoneroUInt128.Combine(FeeSumLow, FeeSumHigh);
        set => (FeeSumLow, FeeSumHigh) = MoneroUInt128.Split(value);
    }
}