using System.Text.Json.Serialization;

namespace Monero.Daemon.Model;

/// <summary>
/// Models a Monero key image.
/// </summary>
public class MoneroKeyImage
{
    /// <summary>
    /// Initialize a new key image.
    /// </summary>
    public MoneroKeyImage()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this key image as a copy of the given one.
    /// </summary>
    /// <param name="keyImage">the key image to copy.</param>
    public MoneroKeyImage(MoneroKeyImage keyImage)
    {
        Hex = keyImage.Hex;
        Signature = keyImage.Signature;
    }

    /// <summary>
    /// Copy this key image.
    /// </summary>
    /// <returns>a copy of the key image.</returns>
    public virtual MoneroKeyImage Copy()
    {
        return new MoneroKeyImage(this);
    }

    /// <summary>
    /// Key image in hexadecimal format.
    /// </summary>
    [JsonPropertyName("hex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hex { get; set; }
    
    /// <summary>
    /// Key image wallet signature.
    /// </summary>
    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }
}
