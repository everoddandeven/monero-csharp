using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a Monero transaction output.
/// </summary>
public class MoneroOutput
{
    /// <summary>
    /// Initialize a new output.
    /// </summary>
    public MoneroOutput()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this output as a deep copy of the given output. The copy has no transaction: the transaction that holds it sets it.
    /// </summary>
    /// <param name="output">the output to copy.</param>
    public MoneroOutput(MoneroOutput output)
    {
        KeyImage = output.KeyImage?.Copy();
        Amount = output.Amount;
        Index = output.Index;
        if (output.RingOutputIndices != null)
        {
            RingOutputIndices = [.. output.RingOutputIndices];
        }
        StealthPublicKey = output.StealthPublicKey;
        Mask = output.Mask;
    }

    /// <summary>
    /// Copy this output.
    /// </summary>
    /// <returns>a copy of the output, which has no transaction.</returns>
    public virtual MoneroOutput Copy()
    {
        return new MoneroOutput(this);
    }

    /// <summary>
    /// Transaction associated to output. It is not part of the JSON, since the transaction holds its outputs.
    /// A transaction sets it when the outputs are assigned to it.
    /// </summary>
    [JsonIgnore]
    public MoneroTx? Tx { get; set; }
    
    /// <summary>
    /// Key image associated to transaction output.
    /// </summary>
    [JsonPropertyName("keyImage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroKeyImage? KeyImage { get; set; }

    /// <summary>
    /// Output amount (atomic units).
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Amount { get; set; }

    /// <summary>
    /// Index of the output in the blockchain, among the outputs of its amount.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Index { get; set; }

    /// <summary>
    /// Ring output indices.
    /// </summary>
    [JsonPropertyName("ringOutputIndices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? RingOutputIndices { get; set; }

    /// <summary>
    /// The stealth public key of the receiver.
    /// Whoever owns the private key associated with this key controls this transaction output.
    /// </summary>
    [JsonPropertyName("stealthPublicKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StealthPublicKey { get; set; }

    /// <summary>
    /// The RingCT mask of the output, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("mask")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mask { get; set; }
}
