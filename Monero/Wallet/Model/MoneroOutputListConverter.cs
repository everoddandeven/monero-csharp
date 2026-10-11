using System.Text.Json;
using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Reads the inputs or the outputs of a transaction of a wallet as the class of a wallet output, and writes each as the class that it has.
/// The list stays a list of <see cref="MoneroOutput"/>, which is what the transaction of the daemon has, so that the transaction of a wallet
/// is one too.
/// </summary>
/// <typeparam name="TOutput">the class of the outputs.</typeparam>
internal sealed class MoneroOutputListConverter<TOutput> : JsonConverter<List<MoneroOutput>> where TOutput : MoneroOutput
{
    public override List<MoneroOutput>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("The outputs are expected as an array");

        var outputs = new List<MoneroOutput>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) return outputs;
            outputs.Add(JsonSerializer.Deserialize<TOutput>(ref reader, options)!);
        }

        throw new JsonException("The array of the outputs is not complete");
    }

    public override void Write(Utf8JsonWriter writer, List<MoneroOutput> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var output in value)
        {
            if (output == null) writer.WriteNullValue();
            else JsonSerializer.Serialize(writer, output, output.GetType(), options);
        }

        writer.WriteEndArray();
    }
}
