# Monero C# Library

[![Build](https://github.com/libmonero/monero-csharp/actions/workflows/build.yml/badge.svg)](https://github.com/libmonero/monero-csharp/actions/workflows/build.yml)
[![Unit tests](https://github.com/libmonero/monero-csharp/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/libmonero/monero-csharp/actions/workflows/unit-tests.yml)
[![Integration tests](https://github.com/libmonero/monero-csharp/actions/workflows/integration-tests.yml/badge.svg)](https://github.com/libmonero/monero-csharp/actions/workflows/integration-tests.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/222e7e7926d44eb1907ec37b6abdf65d)](https://app.codacy.com/gh/libmonero/monero-csharp/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Codacy Badge](https://app.codacy.com/project/badge/Coverage/222e7e7926d44eb1907ec37b6abdf65d)](https://app.codacy.com/gh/libmonero/monero-csharp/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_coverage)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)

> [!WARNING]
>
> monero-csharp is currently under development and unfunded, expect bugs and breaking changes. Only the wallet and daemon RPC clients (`MoneroWalletRpc` and `MoneroDaemonRpc`) are implemented. The client-side wallets (`MoneroWalletFull` and `MoneroWalletKeys`) are not implemented yet, and the multisig, view-only and offline wallets are not tested yet.

A C# library for creating Monero applications using RPC and a C ABI to [monero v0.18.5.3 'Fluorine Fermi'](https://github.com/monero-project/monero/tree/v0.18.5.3).

* Supports wallet and daemon RPC clients.
* Supports client-side wallets using native bindings.
* Supports multisig, view-only, and offline wallets.
* Wallet types are interchangeable by conforming to a common interface, `MoneroWallet`.
* Uses a clearly defined [data model and API specification](https://github.com/everoddandeven/monero-python/blob/main/docs/monero-spec.pdf) intended to be intuitive and robust.
* Query wallet transactions, transfers, and outputs by their properties.
* Fetch and process binary data from the daemon (e.g. raw blocks).
* Receive notifications when blocks are added to the chain or when wallets sync, send, or receive.

## Architecture

<p align="center">
	<img width="85%" height="auto" src="https://raw.githubusercontent.com/libmonero/monero-csharp/main/docs/architecture.png"/><br>
	<i>Build C# applications using RPC or the C ABI of <a href="https://github.com/libmonero/monero-c">monero-c</a> to <a href="https://github.com/monero-project/monero">monero-project/monero</a>. Wallet implementations are interchangeable by conforming to a common abstract class, <code>MoneroWallet</code>.</i>
</p>

Every call is `async` and takes a `CancellationToken`. The call itself is made by monero-c, and the library runs the calls of a daemon or a wallet one after the other on a thread of its own, so an `await` never blocks a thread of the pool. [Native interop and async design](https://github.com/libmonero/monero-csharp/blob/main/docs/native-interop.md) explains how.

## Sample code

```csharp
using Monero.Common;
using Monero.Daemon;
using Monero.Wallet;
using Monero.Wallet.Model;

// connect to daemon
MoneroDaemon daemon = new MoneroDaemonRpc("http://localhost:38081", "superuser", "abctesting123");
ulong height = await daemon.GetHeightAsync();      // 1523651
var txsInPool = await daemon.GetTxPoolAsync();     // get transactions in the pool

// connect to wallet RPC and open wallet
await using var wallet = new MoneroWalletRpc("http://localhost:38083", "rpc_user", "abc123");
await wallet.OpenWalletAsync("sample_wallet_rpc", "supersecretpassword123");
string primaryAddress = await wallet.GetPrimaryAddressAsync();  // 555zgduFhmKd2o8rPUz...
ulong balance = await wallet.GetBalanceAsync();                 // 533648366742
var txs = await wallet.GetTxsAsync();                           // get transactions containing transfers to/from the wallet

// receive notifications when funds are received, confirmed, and unlocked
wallet.AddListener(new MyWalletListener());

// synchronize in the background every 5 seconds
await wallet.StartSyncingAsync(5000);

// send funds to the second subaddress of the wallet
var txConfig = new MoneroTxConfig
{
    AccountIndex = 0,
    Address = await wallet.GetAddressAsync(1, 0),
    Amount = 250000000000, // send 0.25 XMR (denominated in atomic units)
    Relay = false          // create transaction and relay to the network if true
};

MoneroTxWallet createdTx = await wallet.CreateTxAsync(txConfig);
ulong? fee = createdTx.Fee;               // "Are you sure you want to send... ?"
await wallet.RelayTxAsync(createdTx);     // relay the transaction

// save and close wallet
await wallet.CloseAsync(true);

class MyWalletListener : MoneroWalletListener
{
    public override void OnOutputReceived(MoneroOutputWallet output)
    {
        ulong? amount = output.Amount;
    }
}
```

## Documentation

* [Native interop and async design](https://github.com/libmonero/monero-csharp/blob/main/docs/native-interop.md): how the classes call monero-c, and how they expose async methods.
* [API documentation of monero-python](https://everoddandeven.github.io/monero-python/mkdocs/public/) and [monero-ts](https://github.com/woodser/monero-ts#documentation) translate to C#, since the model and the method names are the same.
* [Data model and API specification](https://github.com/everoddandeven/monero-python/blob/main/docs/monero-spec.pdf).

## Using monero-csharp in your project

The package is `Monero.NET`. It depends on `Monero.Native`, the package with the native library for Linux, Windows and macOS. Neither is on nuget.org yet, so until then build them from source as described below.

You need a daemon and a wallet RPC server to connect to:

1. Download and install [Monero CLI](https://web.getmonero.org/downloads/).
2. Start monerod, e.g.: `./monerod --stagenet` (or use a remote daemon).
3. Start monero-wallet-rpc, e.g.: `./monero-wallet-rpc --daemon-address http://localhost:38081 --stagenet --rpc-bind-port 38083 --rpc-login rpc_user:abc123 --wallet-dir ./`

`MoneroWalletRpc` can't do everything that `MoneroWallet` declares, because monero-wallet-rpc can't. The methods that throw a `MoneroError` on it are listed in the documentation of the class.

## Building monero-csharp from source

You need the .NET 10 SDK.

```sh
git clone --recurse-submodules https://github.com/libmonero/monero-csharp.git
cd monero-csharp
dotnet build
```

`external/monero-c` is [monero-c](https://github.com/libmonero/monero-c), the C library that `Monero` calls. `Monero` references its project `Monero.Native`, which builds the library before `Monero` and copies it to the output of `Monero` and of the tests. The first build takes hours, and the next ones only what changed. It needs the dependencies of the [.NET build guide](https://github.com/libmonero/monero-c/blob/main/docs/dotnet.md).

To skip the native build, for example when the library of the submodule is already built, use `-p:MoneroCSkipNative=true`.

To pack the library, and to run a program that uses the package and not the project:

```sh
dotnet pack Monero/Monero.csproj --configuration Release --output Monero.PackageTests/packages
dotnet run --project Monero.PackageTests --configuration Release
```

## Running tests

### Unit tests

The unit tests read and write the JSON of the models, which is the JSON that monero-c writes, in `Monero.UnitTests/Fixtures`. They need no node and no native library.

```sh
dotnet test Monero.UnitTests
```

To get the coverage of the library:

```sh
dotnet test Monero.UnitTests --collect "XPlat Code Coverage" --settings coverlet.runsettings
```

### Integration tests

`Monero.IntegrationTests` tests `MoneroDaemonRpc` and `MoneroWalletRpc` against a regtest network of two nodes and three monero-wallet-rpc servers. They need Docker and the package `Monero.Native`. A wallet test needs about 2 GB of free memory when it mines on the node.

The nodes and the servers run the images of [docker-monero-rpc](https://github.com/libmonero/docker-monero-rpc), `ghcr.io/libmonero/monero-daemon-rpc` and `ghcr.io/libmonero/monero-wallet-rpc`, with the services of the compose file of monero-python, except that the difficulty of the blocks is 25 and not 500. The tag is `master`, and `MONERO_TAG` in the environment or in a `.env` file next to the compose file sets another one.

The tests mirror those of monero-python, with these differences, which come from the speed of mining on a regtest network:

* The difficulty of the blocks is 25 and not 500. At 500 a block took 13 seconds on a runner, and the setup of the tests asks the node for 20 blocks in a request that has a timeout of 3 minutes.
* The blocks that a test needs (`BlockchainUtils.WaitForHeight` and `ConfirmAndUnlock`) are generated with `generateblocks`, which is slower than the miner of the node, where the tests of monero-python start the miner and wait for the next block headers.
* `TestCreateAndReceive` doesn't start the miner. A wallet is notified of a received output only if one of its polls finds the tx confirmed and locked, and at this difficulty the miner makes several blocks per second, so the tx is unlocked by the time the wallet polls. The test generates the one block that confirms the tx, and it doesn't test the notification while the node is mining, as the test of monero-python does. The comment of the test has the details.

1. Set up the docker test environment:
    ```sh
    docker compose -f Monero.IntegrationTests/docker-compose.yml up -d
    ```
2. Configure the RPC endpoints, authentication, and other settings in [config.json](https://github.com/libmonero/monero-csharp/blob/main/Monero.IntegrationTests/Config/config.json). The daemon URIs can be overridden with `MONERO_DAEMON_RPC_URI` and `MONERO_MINING_DAEMON_RPC_URI`.
3. Run the tests:
    ```sh
    dotnet test Monero.IntegrationTests
    ```
4. Clean up the docker test environment:
    ```sh
    docker compose -f Monero.IntegrationTests/docker-compose.yml down -v
    ```

The first run mines the blocks that the tests need, and funds the test wallet from the mining wallet. `down -v` gives the next run a new chain.

## Continuous integration

* **Build** builds `Monero.Native` from the submodule (**Native**), then the library and the tests, collects the coverage, and calls **Package**, which packs the NuGet package, checks what is in it and uploads it. A tag `v<version>` has to match the version in `Version.props`.
* **Unit tests** waits for the Build run of the same commit, and runs the unit tests that Build made on Linux (x86_64 and arm64), Windows and macOS (arm64 and x86_64), without building them again.
* **Integration tests** waits for the Build run, and runs `Monero.IntegrationTests` that Build made on Linux x86_64, against the regtest network of `docker-compose.yml`.
* **Package tests** waits for the Build run, and installs the package that Build made in `Monero.PackageTests`, on Linux, Windows and macOS.
* **Codacy Coverage Reporter** sends the coverage of Build to Codacy, when the repository has the `CODACY_PROJECT_TOKEN` secret.

## Related projects

* [monero-c](https://github.com/libmonero/monero-c)
* [monero-cpp](https://github.com/woodser/monero-cpp)
* [monero-python](https://github.com/everoddandeven/monero-python)
* [monero-java](https://github.com/woodser/monero-java)
* [monero-ts](https://github.com/woodser/monero-ts)

## License

This project is licensed under MIT.

## Donations

If this library has been valuable to you, please consider donating to support its continued development. 🙏

<p align="center">
  <code>XMR</code><br>
  <img src="donate.png" style="margin-top: 5px" width="115" height="115"/><br>
  <code>8BBjopSDammd96czUaBSirDgkFhoEXm3xXwqerDob1mb4kcQArRQs1fjNhPrNokJGtPkebVxX1rE5DnVF4PzSxopHZw8QpE</code>
</p>
