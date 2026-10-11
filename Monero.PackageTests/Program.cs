using System.Text.Json;
using System.Text.Json.Nodes;
using Monero.Common;
using Monero.Daemon.Model;
using Monero.Wallet.Model;

// Checks that the package has what a program needs: the models of the daemon and of the wallet, and their JSON with and without reflection.
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

// the wallet: its blocks hold transactions of a wallet, with their transfers, and a spent output is in the inputs of its transaction
var walletBlocks = MoneroJson.Deserialize<MoneroWalletBlocksResult>(Fixture("wallet_txs_miner"));
var walletTx = walletBlocks.GetTxs().FirstOrDefault();
Check(walletBlocks.Blocks?.Count == 2 && walletTx != null && walletTx.GetHeight() == 1, "wallet blocks of the fixture");
var incoming = walletTx?.IncomingTransfers?.SingleOrDefault();
Check(incoming != null && ReferenceEquals(incoming.Tx, walletTx) && incoming.Amount == 35184338534400UL, "incoming transfer of a wallet tx");
Check(JsonNode.DeepEquals(JsonNode.Parse(Fixture("wallet_txs_miner")), JsonNode.Parse(MoneroJson.Serialize(walletBlocks))), "wallet blocks written back unchanged");

var txSets = JsonSerializer.Deserialize(Fixture("wallet_tx_sets_created"), MoneroWalletJsonContext.Default.MoneroTxSetsResult);
var created = txSets?.GetTxs().SingleOrDefault();
Check(created?.TxSet == txSets?.TxSets?.Single() && created?.GetInputsWallet().Count == 2, "tx set of the fixture, generated code");

var spent = MoneroJson.Deserialize<MoneroWalletBlocksResult>(Fixture("wallet_listener_output_spent_first")).GetInputs().SingleOrDefault();
Check(spent?.GetTxWallet()?.GetHeight() == 101 && spent.Amount == 35184338534400UL, "spent output of a listener notification");

var config = new MoneroWalletConfig { NetworkType = MoneroNetworkType.Regtest, Server = new MoneroRpcConnection("http://127.0.0.1:18081"), IsTrustedDaemon = true };
Check(JsonNode.DeepEquals(JsonNode.Parse(MoneroJson.Serialize(config)), JsonNode.Parse("""{"networkType":3,"server":{"uri":"http://127.0.0.1:18081"},"isTrustedDaemon":true}""")), "wallet config");

foreach (var failure in failures) Console.Error.WriteLine($"FAILED: {failure}");
Console.WriteLine(failures.Count == 0 ? $"Monero {typeof(MoneroTx).Assembly.GetName().Version}: package ok" : $"{failures.Count} checks failed");
return failures.Count == 0 ? 0 : 1;
