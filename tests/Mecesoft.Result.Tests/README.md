# Mecesoft.Result characterization tests

These tests pin the 1.0.2 public API and the corrected 1.1.0 pagination behavior.

## Run

```bash
dotnet test tests/Mecesoft.Result.Tests/Mecesoft.Result.Tests.csproj -c Release -p:GeneratePackageOnBuild=false
```

The test project targets `net8.0`, `net9.0` and `net10.0`; the matching runtime must be
installed to execute each target's tests.

`GeneratePackageOnBuild=false` prevents test runs from producing NuGet packages in the production project's output directory.

## API baseline

`Api/PublicApiBaseline.1.0.2.txt` was generated from the published NuGet 1.0.2 DLL:

- Published DLL SHA-256: `42f106a4a93c2e8c57090d74996db659f8881ee33a687904bba18be516f8d2c3`
- The local `bin/Debug/net9.0/Mecesoft.Result.dll` was byte-identical to the published DLL at the time of the audit.

`CurrentPublicApi_MatchesPublished_1_0_2_Baseline` compares the current production assembly's public surface against this file. If the production API intentionally changes, update the baseline as part of a reviewed release change, not as an incidental test fix.

The baseline captures:

- public types and type kind/base type
- constructors, properties, methods, fields, events
- implicit operators
- generic constraints
- optional parameters and parameter names
- `init` accessors and the `JsonIgnore` marker

Nullable reference annotations are not captured. They should be added to the baseline in a later phase if source-level nullability compatibility is required.

## Known-limitation tests

Tests whose names contain `ValueType` or `IsNotSupported` pin known limitations.
The pagination tests now pin the corrected 1.1.0 behavior (empty metadata, out-of-range
pages and long-offset handling). Do not change behavioral expectations without an explicit
release decision and release note.

## Runtime coverage

The test project targets `net8.0`, `net9.0` and `net10.0` with matching EF Core InMemory
versions. CI runs the suite independently on the .NET 8, .NET 9 and .NET 10 runtimes.
