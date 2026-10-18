# monero-csharp
C# library for using Monero

## Development

You need the .NET 10 SDK.

```sh
git clone https://github.com/libmonero/monero-csharp
cd monero-csharp
git submodule update --init
```

`external/monero-c` is [monero-c](https://github.com/libmonero/monero-c), the C library that `Monero` calls. `Monero` references its project `Monero.Native`, which builds the library before `Monero` and copies it to the output of `Monero` and of the tests. The first build takes hours, and the next ones only what changed. It needs the dependencies of the [.NET build guide](https://github.com/libmonero/monero-c/blob/main/docs/dotnet.md). To skip the native build, for example when the library of the submodule is already built, use `-p:MoneroCSkipNative=true`. CI builds the library in the job **Native**, puts it where the project would leave it and packs the package `Monero.Native`, which `Monero.NET` depends on and which is not on nuget.org yet.

```sh
dotnet build
dotnet test Monero.UnitTests
```

The unit tests read and write the JSON of the models, which is the JSON that monero-c writes, in `Monero.UnitTests/Fixtures`. They need no node and no native library.

To get the coverage of the library:

```sh
dotnet test Monero.UnitTests --collect "XPlat Code Coverage" --settings coverlet.runsettings
```

To pack the library, and to run a program that uses the package and not the project:

```sh
dotnet pack Monero/Monero.csproj --configuration Release --output Monero.PackageTests/packages
dotnet run --project Monero.PackageTests --configuration Release
```

## Integration tests

`Monero.IntegrationTests` tests `MoneroDaemonRpc` against a regtest network. They need Docker and the package `Monero.Native`.

```sh
docker compose -f Monero.IntegrationTests/docker-compose.yml up -d
dotnet test Monero.IntegrationTests
docker compose -f Monero.IntegrationTests/docker-compose.yml down -v
```

The first run mines the blocks that the tests need. `Monero.IntegrationTests/Config/config.json` has the configuration, and the daemon URIs can be overridden with `MONERO_DAEMON_RPC_URI` and `MONERO_MINING_DAEMON_RPC_URI`. `down -v` gives the next run a new chain.

## Continuous integration

* **Build** builds `Monero.Native` from the submodule (**Native**), then the library and the tests, collects the coverage, and calls **Package**, which packs the NuGet package, checks what is in it and uploads it. A tag `v<version>` has to match the version in `Version.props`.
* **Tests** waits for the Build run of the same commit, and runs the unit tests that Build made on Linux (x86_64 and arm64), Windows and macOS (arm64 and x86_64), without building them again. It also installs the package that Build made in `Monero.PackageTests`, on Linux, Windows and macOS.
* **Codacy Coverage Reporter** sends the coverage of Build to Codacy, when the repository has the `CODACY_PROJECT_TOKEN` secret.
