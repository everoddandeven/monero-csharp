using System.Text.Json;
using System.Text.Json.Nodes;
using Monero.Common;
using Monero.Daemon.Model;

// Checks that the package has what a program needs: the models, and the JSON of the models with and without reflection.
// The exit code is 0 if everything is as expected.

var failures = new List<string>();

void Check(bool condition, string what)
{
    if (!condition) failures.Add(what);
}

string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"));

// reflection
var info = MoneroJson.Deserialize<MoneroDaemonInfo>("""{"height":102,"networkType":2,"difficultyLow":1,"difficultyHigh":1}""");
Check(info.Height == 102, "height");
Check(info.NetworkType == MoneroNetworkType.StageNet, "network type");
Check(info.Difficulty == (UInt128.One << 64) + 1, "128-bit difficulty");
Check(JsonNode.DeepEquals(JsonNode.Parse(MoneroJson.Serialize(info)), JsonNode.Parse("""{"height":102,"networkType":2,"difficultyLow":1,"difficultyHigh":1}""")), "JSON of the daemon info");

// the code that the compiler generates
var generated = JsonSerializer.Deserialize(Fixture("daemon_info"), MoneroDaemonJsonContext.Default.MoneroDaemonInfo);
Check(generated?.IsRegtest == true, "daemon info of the fixture, generated code");

// a block with its transactions, and the parents of them
var blocksJson = Fixture("blocks");
var blocks = MoneroJson.Deserialize<List<MoneroBlock>>(blocksJson);
var tx = blocks.Last().Txs?.SingleOrDefault();
Check(blocks.Count == 3, "blocks of the fixture");
Check(tx != null && ReferenceEquals(tx.Block, blocks.Last()) && tx.GetHeight() == 101, "block of a tx");
Check(tx?.Inputs?.All(input => ReferenceEquals(input.Tx, tx)) == true, "tx of an input");
Check(JsonNode.DeepEquals(JsonNode.Parse(blocksJson), JsonNode.Parse(MoneroJson.Serialize(blocks))), "blocks written back unchanged");

foreach (var failure in failures) Console.Error.WriteLine($"FAILED: {failure}");
Console.WriteLine(failures.Count == 0 ? $"Monero {typeof(MoneroTx).Assembly.GetName().Version}: package ok" : $"{failures.Count} checks failed");
return failures.Count == 0 ? 0 : 1;
