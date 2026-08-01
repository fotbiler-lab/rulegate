# Platform Compatibility

RuleGate distinguishes vendor-supported platforms from legacy targets that are
verified only to help existing applications migrate.

## .NET package matrix

| Package                          | Target frameworks                               |
| -------------------------------- | ----------------------------------------------- |
| `Fotbiler.RuleGate.Abstractions` | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| `Fotbiler.RuleGate.Core`         | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| `Fotbiler.RuleGate.Manifest`     | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| `Fotbiler.RuleGate.AspNetCore`   | `netcoreapp3.1`, `net5.0` through `net10.0`     |
| `Fotbiler.RuleGate.Keycloak`     | `netcoreapp3.1`, `net5.0` through `net10.0`     |
| `Fotbiler.RuleGate.Cli`          | `net8.0`, `net9.0`, `net10.0`                   |

[.NET 8, .NET 9, and .NET 10 are supported by Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
as of August 2026.
.NET Core 3.1 and .NET 5–7 are end-of-life. RuleGate cannot provide runtime or
framework security maintenance for those releases.

The legacy matrix restores and builds consumers against the actual packed
NuGet files, then executes their compiled applications inside isolated official
.NET Core 3.1 and .NET 5–7 runtime containers. Current targets execute on the
installed .NET 8–10 runtimes.

Package compatibility checks keep package TFM-support warnings enabled.
RuleGate does not treat warning suppression as proof of legacy compatibility;
the resolved dependency graph itself must restore and compile cleanly for the
declared target.

Authorization evaluation, manifest compilation, strict configuration binding,
and default-deny behavior remain aligned. The optional
`AddHttpAuthorizationResultMapping` API is unavailable on .NET Core 3.1 because
that framework does not expose `IAuthorizationMiddlewareResultHandler`; hosts
retain the standard ASP.NET Core 3.1 challenge and forbid behavior.

## Angular package matrix

| Angular version | Package                                             | Support level |
| --------------- | --------------------------------------------------- | ------------- |
| 20–22           | `@fotbiler/rulegate-angular`                        | Current       |
| 12–19           | `@fotbiler/rulegate-angular-legacy`                 | Legacy-tested |
| 9–11            | `@fotbiler/rulegate-client` in a host-owned service | Legacy-tested |

[Angular 20–22 are supported by Angular](https://angular.dev/reference/releases)
as of August 2026. Angular 9–19 are end-of-life. See
[Frontend compatibility](frontend-compatibility.md) for the adapter APIs and
installation paths.

## Support-level definitions

- **Current:** the framework vendor still supplies fixes and RuleGate CI
  verifies package-only consumers.
- **Legacy-tested:** the vendor no longer supplies fixes, but RuleGate CI
  verifies installation, compilation, and representative authorization use.
- **Unsupported:** below .NET Core 3.1, below Angular 9, or outside the declared
  package targets.

Passing RuleGate compatibility tests does not make an end-of-life framework
safe for production. Teams remain responsible for upgrading runtimes,
frameworks, operating systems, and transitive dependencies.

## RC package-consumer verification boundary

The .NET package-only compatibility gate verifies two independent package
graphs:

- the current `1.0.0-rc.1` candidate, packed from the current repository and
  restored from an isolated local feed;
- the published `0.9.0-preview.4` compatibility baseline, restored directly
  from NuGet.org.

Both ASP.NET Core and Keycloak consumers contain no `ProjectReference` entries.
They restore, build, and execute on `net8.0`, `net9.0`, and `net10.0`. Their
`netcoreapp3.1`, `net5.0`, `net6.0`, and `net7.0` outputs execute in isolated,
read-only Docker runtime images with networking disabled.

The verifier checks exact package versions and package sources. For the current
candidate it also verifies that each restored RuleGate package hash matches the
locally packed `.nupkg`. This prevents a package already present in a global
cache or registry from silently satisfying the current-candidate matrix.

The current CLI candidate is installed from the same local feed and executed on
its `net8.0`, `net9.0`, and `net10.0` tool targets.
