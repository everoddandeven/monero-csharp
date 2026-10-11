using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monero.Wallet.Model;

/// <summary>
/// Enumerates the keys that sign a message. The numbers are the ones that monero-c takes to sign, and the JSON of a result names the key
/// as <c>spend</c> or <c>view</c>.
/// </summary>
[JsonConverter(typeof(MoneroMessageSignatureTypeConverter))]
public enum MoneroMessageSignatureType
{
    /// <summary>
    /// The message is signed with the spend key.
    /// </summary>
    SignWithSpendKey = 0,

    /// <summary>
    /// The message is signed with the view key.
    /// </summary>
    SignWithViewKey
}

/// <summary>
/// Reads and writes the type of a signature of a message as <c>spend</c> or <c>view</c>.
/// </summary>
internal sealed class MoneroMessageSignatureTypeConverter : JsonConverter<MoneroMessageSignatureType>
{
    public override MoneroMessageSignatureType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("The type of the signature is expected as the text spend or view");

        return reader.GetString() switch
        {
            "spend" => MoneroMessageSignatureType.SignWithSpendKey,
            "view" => MoneroMessageSignatureType.SignWithViewKey,
            var other => throw new JsonException($"Unknown type of signature: {other}")
        };
    }

    public override void Write(Utf8JsonWriter writer, MoneroMessageSignatureType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            MoneroMessageSignatureType.SignWithSpendKey => "spend",
            MoneroMessageSignatureType.SignWithViewKey => "view",
            _ => throw new JsonException($"Unknown type of signature: {value}")
        });
    }
}
