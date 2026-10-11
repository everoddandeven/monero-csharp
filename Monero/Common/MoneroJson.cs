using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monero.Common;

/// <summary>
/// JSON of the Monero models. It is the one that monero-cpp and monero-c write: camel case names, a field that is not set is left out,
/// numbers for the amounts and the enumerations.
/// </summary>
public static class MoneroJson
{
    /// <summary>
    /// The options of the serializer for the models.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serialize a model to JSON.
    /// </summary>
    /// <param name="model">the model.</param>
    /// <typeparam name="T">type of the model.</typeparam>
    /// <returns>the JSON.</returns>
    public static string Serialize<T>(T model)
    {
        return JsonSerializer.Serialize(model, Options);
    }

    /// <summary>
    /// Serialize a model to UTF-8 JSON.
    /// </summary>
    /// <param name="model">the model.</param>
    /// <typeparam name="T">type of the model.</typeparam>
    /// <returns>the JSON, as UTF-8 bytes.</returns>
    public static byte[] SerializeToUtf8Bytes<T>(T model)
    {
        return JsonSerializer.SerializeToUtf8Bytes(model, Options);
    }

    /// <summary>
    /// Deserialize a model from JSON.
    /// </summary>
    /// <param name="json">the JSON.</param>
    /// <typeparam name="T">type of the model.</typeparam>
    /// <returns>the model.</returns>
    /// <exception cref="JsonException">the JSON is not valid, or it is not a model of that type.</exception>
    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException($"The JSON is null, and a {typeof(T).Name} is needed");
    }

    /// <summary>
    /// Deserialize a model from UTF-8 JSON, without a string in between. The span is not kept.
    /// </summary>
    /// <param name="utf8Json">the JSON, as UTF-8 bytes.</param>
    /// <typeparam name="T">type of the model.</typeparam>
    /// <returns>the model.</returns>
    /// <exception cref="JsonException">the JSON is not valid, or it is not a model of that type.</exception>
    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<T>(utf8Json, Options) ?? throw new JsonException($"The JSON is null, and a {typeof(T).Name} is needed");
    }
}
