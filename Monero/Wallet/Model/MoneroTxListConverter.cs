using System.Text.Json;
using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Reads the transactions of a block of a wallet as the class of a wallet transaction, and writes each as the class that it has. The list
/// stays a list of <see cref="MoneroTx"/>, which is what the block of the daemon has, so that the block of a wallet is one too.
/// </summary>
/// <typeparam name="TTx">the class of the transactions.</typeparam>
internal sealed class MoneroTxListConverter<TTx> : JsonConverter<List<MoneroTx>> where TTx : MoneroTx
{
    public override List<MoneroTx>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("The transactions are expected as an array");

        var txs = new List<MoneroTx>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) return txs;
            txs.Add(JsonSerializer.Deserialize<TTx>(ref reader, options)!);
        }

        throw new JsonException("The array of the transactions is not complete");
    }

    public override void Write(Utf8JsonWriter writer, List<MoneroTx> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var tx in value)
        {
            if (tx == null) writer.WriteNullValue();
            else JsonSerializer.Serialize(writer, tx, tx.GetType(), options);
        }

        writer.WriteEndArray();
    }
}
