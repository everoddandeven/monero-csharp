using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result from submitting a tx to a daemon.
/// </summary>
public class MoneroSubmitTxResult : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Indicates if transaction submit was successful.
    /// </summary>
    [JsonPropertyName("isGood")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsGood { get; set; }
    
    /// <summary>
    /// Indicates if transaction was relayed.
    /// </summary>
    [JsonPropertyName("isRelayed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRelayed { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to double spending.
    /// </summary>
    [JsonPropertyName("isDoubleSpend")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsDoubleSpend { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to too low fees.
    /// </summary>
    [JsonPropertyName("isFeeTooLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsFeeTooLow { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to too low mixin.
    /// </summary>
    [JsonPropertyName("isMixinTooLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsMixinTooLow { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to an invalid input.
    /// </summary>
    [JsonPropertyName("hasInvalidInput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasInvalidInput { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to an invalid output.
    /// </summary>
    [JsonPropertyName("hasInvalidOutput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasInvalidOutput { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to too few outputs.
    /// </summary>
    [JsonPropertyName("hasTooFewOutputs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasTooFewOutputs { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to overspend.
    /// </summary>
    [JsonPropertyName("isOverspend")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOverspend { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to transaction size too big.
    /// </summary>
    [JsonPropertyName("isTooBig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTooBig { get; set; }
    
    /// <summary>
    /// Indicates that submit failed sanity check.
    /// </summary>
    [JsonPropertyName("sanityCheckFailed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SanityCheckFailed { get; set; }
    
    /// <summary>
    /// Reason of failed submit.
    /// </summary>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to too big transaction extra.
    /// </summary>
    [JsonPropertyName("isTxExtraTooBig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTxExtraTooBig { get; set; }
    
    /// <summary>
    /// Indicates if submit failed due to non-zero transaction unlock time.
    /// </summary>
    [JsonPropertyName("isNonZeroUnlockTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsNonZeroUnlockTime { get; set; }
}