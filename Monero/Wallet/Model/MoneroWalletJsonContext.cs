using System.Text.Json.Serialization;
using Monero.Common;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// The JSON of the wallet models, generated at compile time. It reads and writes the same JSON as <see cref="MoneroJson"/>, without
/// reflection, so it works in trimmed and ahead of time compiled programs, and it is faster.
/// </summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MoneroAccount))]
[JsonSerializable(typeof(MoneroAccountTag))]
[JsonSerializable(typeof(MoneroAddressBookEntry))]
[JsonSerializable(typeof(MoneroBlockWallet))]
[JsonSerializable(typeof(MoneroCheck))]
[JsonSerializable(typeof(MoneroCheckReserve))]
[JsonSerializable(typeof(MoneroCheckTx))]
[JsonSerializable(typeof(MoneroDecodedAddress))]
[JsonSerializable(typeof(MoneroDestination))]
[JsonSerializable(typeof(MoneroIncomingTransfer))]
[JsonSerializable(typeof(MoneroIntegratedAddress))]
[JsonSerializable(typeof(MoneroKeyImage))]
[JsonSerializable(typeof(MoneroKeyImageExportResult))]
[JsonSerializable(typeof(MoneroKeyImageImportResult))]
[JsonSerializable(typeof(MoneroMessageSignatureResult))]
[JsonSerializable(typeof(MoneroMultisigInfo))]
[JsonSerializable(typeof(MoneroMultisigInitResult))]
[JsonSerializable(typeof(MoneroMultisigSignResult))]
[JsonSerializable(typeof(MoneroOutgoingTransfer))]
[JsonSerializable(typeof(MoneroOutputQuery))]
[JsonSerializable(typeof(MoneroOutputWallet))]
[JsonSerializable(typeof(MoneroRpcConnection))]
[JsonSerializable(typeof(MoneroSubaddress))]
[JsonSerializable(typeof(MoneroSyncResult))]
[JsonSerializable(typeof(MoneroTransferQuery))]
[JsonSerializable(typeof(MoneroTxConfig))]
[JsonSerializable(typeof(MoneroTxQuery))]
[JsonSerializable(typeof(MoneroTxSet))]
[JsonSerializable(typeof(MoneroTxSetsResult))]
[JsonSerializable(typeof(MoneroTxWallet))]
[JsonSerializable(typeof(MoneroVersion))]
[JsonSerializable(typeof(MoneroWalletBlocksResult))]
[JsonSerializable(typeof(MoneroWalletConfig))]
[JsonSerializable(typeof(List<MoneroAccount>))]
[JsonSerializable(typeof(List<MoneroAccountTag>))]
[JsonSerializable(typeof(List<MoneroAddressBookEntry>))]
[JsonSerializable(typeof(List<MoneroKeyImage>))]
[JsonSerializable(typeof(List<MoneroSubaddress>))]
[JsonSerializable(typeof(List<MoneroTxSet>))]
[JsonSerializable(typeof(List<MoneroTxWallet>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<uint>))]
public partial class MoneroWalletJsonContext : JsonSerializerContext
{
}
