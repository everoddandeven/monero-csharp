using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models the result of adding auxiliary proofs of work to a block template.
/// </summary>
public class MoneroAddAuxiliaryPowResult : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Block template with the auxiliary proofs of work, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("blockTemplateBlob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BlockTemplateBlob { get; set; }

    /// <summary>
    /// Blob to hash for the proof of work of the block template, in hexadecimal format.
    /// </summary>
    [JsonPropertyName("blockHashingBlob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BlockHashingBlob { get; set; }

    /// <summary>
    /// Merkle root of the auxiliary proofs of work.
    /// </summary>
    [JsonPropertyName("merkleRoot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MerkleRoot { get; set; }

    /// <summary>
    /// Depth of the merkle tree of the auxiliary proofs of work.
    /// </summary>
    [JsonPropertyName("merkleTreeDepth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? MerkleTreeDepth { get; set; }

    /// <summary>
    /// The auxiliary proofs of work that were added.
    /// </summary>
    [JsonPropertyName("auxPow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoneroAuxiliaryPow>? AuxPow { get; set; }
}
