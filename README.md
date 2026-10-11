# monero-csharp
C# library for using Monero

## Development

You need the .NET 10 SDK.

```sh
dotnet build
dotnet test
```

The tests read and write the JSON of the models. They use the JSON that [monero-c](https://github.com/libmonero/monero-c) writes, in `Monero.UnitTests/Fixtures`, and need no node and no native library.

To get the coverage of the library:

```sh
dotnet test --collect "XPlat Code Coverage" --settings coverlet.runsettings
```

To pack the library, and to run a program that uses the package and not the project:

```sh
dotnet pack Monero/Monero.csproj --configuration Release --output Monero.PackageTests/packages
dotnet run --project Monero.PackageTests --configuration Release
```

## Continuous integration

* **Build** builds the library and the tests, collects the coverage, and calls **Package**, which packs the NuGet package, checks what is in it and uploads it. A tag `v<version>` has to match the version in `Version.props`.
* **Tests** waits for the Build run of the same commit, and runs the tests that Build made on Linux (x86_64 and arm64), Windows and macOS (arm64 and x86_64), without building them again. It also installs the package that Build made in `Monero.PackageTests`, on Linux, Windows and macOS.
* **Codacy Coverage Reporter** sends the coverage of Build to Codacy, when the repository has the `CODACY_PROJECT_TOKEN` secret.
