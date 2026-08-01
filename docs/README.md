# RuleGate Documentation

The documentation has two connected layers:

1. **The guide** teaches RuleGate from first principles through production in
   one ordered path.
2. **Reference documents** provide exhaustive contracts, operators, failure
   behavior, compatibility, and maintainer procedures.

Repository Markdown is the canonical source. The GitHub Wiki edition is built
from the guide so the two forms do not drift.

## Start with the guide

[Open The RuleGate Guide](guide/README.md)

| Stage               | Chapters                                                                                                                                             |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| Learn               | [Foundations](guide/01-Authorization-Foundations.md) · [Packages](guide/02-Packages-and-Installation.md)                                             |
| Build               | [First API](guide/03-First-Protected-API.md) · [Policy language](guide/04-Policy-Language.md) · [ASP.NET Core](guide/05-ASP.NET-Core-Integration.md) |
| Supply trusted data | [Attributes and context](guide/06-Trusted-Attributes-and-Context.md) · [Identity and Keycloak](guide/07-Identity-and-Keycloak.md)                    |
| Add frontend        | [Frontend integration](guide/08-Frontend-Integration.md)                                                                                             |
| Automate            | [CLI lifecycle](guide/09-CLI-and-Policy-Lifecycle.md) · [Testing and diagnostics](guide/10-Testing-and-Diagnostics.md)                               |
| Operate and extend  | [Policy sources](guide/11-Policy-Sources-and-Reload.md) · [Extensibility](guide/12-Extensibility.md)                                                 |
| Apply               | [Real-world recipes](guide/13-Real-World-Recipes.md) · [Production checklist](guide/14-Production-Checklist.md)                                      |

Use the [glossary](guide/Glossary.md) whenever an authorization term is
unfamiliar.

## Reference library

| Need                                                       | Reference                                                         |
| ---------------------------------------------------------- | ----------------------------------------------------------------- |
| Complete subject/resource/action/context model             | [Authorization model](authorization-model.md)                     |
| Every YAML member, operator, type, and validation rule     | [Manifest reference](manifests.md)                                |
| Minimal API, MVC, imperative, HTTP, and engine integration | [ASP.NET Core reference](aspnetcore.md)                           |
| Subject, resource, and context providers                   | [Enrichment reference](enrichment.md)                             |
| Modern Angular guards, directives, and generation          | [Angular reference](angular.md)                                   |
| Angular 9–22 package selection                             | [Frontend compatibility](frontend-compatibility.md)               |
| Keycloak backend and frontend mapping                      | [Keycloak reference](keycloak.md)                                 |
| CLI commands and exit codes                                | [CLI reference](cli.md)                                           |
| Deterministic policy fixtures                              | [Policy testing](policy-testing.md)                               |
| Decision explanation and manifest linting                  | [Explain and lint](explain-and-lint.md)                           |
| Deterministic C# constants                                 | [C# generation](code-generation.md)                               |
| Local sources and atomic reload                            | [Policy sources](policy-sources.md)                               |
| Logging and custom sinks                                   | [Diagnostics](diagnostics.md)                                     |
| Activities, metrics, benchmarks, and concurrency           | [Telemetry and performance](telemetry-performance-concurrency.md) |
| Full trust and failure boundaries                          | [Security model](security.md)                                     |
| .NET and Angular support matrix                            | [Platform compatibility](platform-compatibility.md)               |
| Runnable package-consuming applications                    | [Reference applications](reference-applications.md)               |
| Preview-to-stable upgrade                                  | [Migration to 1.0](migration-to-1.0.md)                           |
| Released and planned capabilities                          | [Roadmap](roadmap.md)                                             |

## Published package families

All six NuGet packages and all three npm packages have a stable `1.0.0`
release. Use the [package selection chapter](guide/02-Packages-and-Installation.md)
for package links, dependency roles, install commands, and framework ranges.

## Samples

- [Minimal ASP.NET Core](../samples/aspnetcore-minimal/README.md)
- [Document approval](../samples/document-approval/README.md)
- [Document approval verification](../samples/document-approval/verification.md)
- [All samples](../samples/README.md)

## Maintainer documentation

- [NuGet release checklist](releases/preview-release-checklist.md)
- [npm release checklist](releases/npm-preview-release-checklist.md)
- [NuGet prefix reservation](releases/nuget-prefix-reservation.md)

Maintainer release procedures are intentionally outside the beginner learning
path.
