# Policy Sources and Atomic Reload

RuleGate can compose policies from local application sources and activate the
complete result as one immutable runtime snapshot. A candidate snapshot is
never made visible until every source has loaded successfully and the combined
policy set has passed duplicate-ID and duplicate-route validation.

Policy sources and atomic reload are available starting with
`0.9.0-preview.2`.

## Supported sources

| Source                 | Registration API                    | Automatic reload |
| ---------------------- | ----------------------------------- | ---------------- |
| In-memory policies     | `AddPolicy` / `AddPolicies`         | Manual           |
| YAML file              | `AddYamlPolicyFile`                 | Optional         |
| Embedded YAML resource | `AddEmbeddedPolicyResource`         | No               |
| .NET configuration     | `AddConfigurationPolicySource`      | Optional         |
| Application-defined    | `AddPolicySource` / `IPolicySource` | Manual           |

Database-backed repositories, remote policy servers, and central evaluation
are not part of this milestone. Applications can implement `IPolicySource`,
but source loading and policy evaluation remain local to the process.

## YAML file source

Register a manifest file and enable reload after file changes:

```csharp
var manifestPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "rulegate.yaml");

builder.Services
    .AddRuleGate()
    .AddYamlPolicyFile(
        manifestPath,
        options =>
        {
            options.ReloadOnChange = true;
        });
```

The ASP.NET Core hosted service completes the initial load before host startup
continues. File changes are debounced, then the complete source set is loaded
again. A missing, unreadable, malformed, or invalid file rejects the candidate
snapshot without removing the active policies.

## In-memory policies

Existing in-memory registration remains supported:

```csharp
builder.Services
    .AddRuleGate()
    .AddPolicies(policies);
```

When an additional policy source is registered, these policies are included in
the combined atomic snapshot under the `in-memory` source name. Policy IDs and
resource/action routes must remain unique across every source.

## Embedded resource source

Embed the YAML file in the application assembly:

```xml
<ItemGroup>
  <EmbeddedResource Include="Authorization/rulegate.yaml" />
</ItemGroup>
```

Register its fully qualified resource name:

```csharp
builder.Services
    .AddRuleGate()
    .AddEmbeddedPolicyResource(
        typeof(Program).Assembly,
        "Sample.Authorization.rulegate.yaml");
```

Embedded resources are immutable for the lifetime of the process and therefore
do not use change monitoring. They can still be re-read through the manual
reload service.

## Configuration source

The configuration source binds a structured configuration section to the same
manifest model used by YAML compilation:

```json
{
  "RuleGate": {
    "SchemaVersion": 1,
    "Application": {
      "Id": "sample-api",
      "Name": "Sample API"
    },
    "Policies": [
      {
        "Id": "document-read",
        "ResourceType": "document",
        "Action": "read",
        "Requirement": {
          "Permission": "document.read"
        }
      }
    ]
  }
}
```

Register the section and optionally follow configuration reload tokens:

```csharp
builder.Services
    .AddRuleGate()
    .AddConfigurationPolicySource(
        builder.Configuration,
        "RuleGate",
        options =>
        {
            options.ReloadOnChange = true;
        });
```

Configuration providers decide when a reload token changes. Environment
variables and providers without change notifications can still be reloaded
explicitly through `IPolicyReloadService`.

## Application-defined source

An application source returns either a complete policy collection or one or
more structured diagnostics:

```csharp
public sealed class ApplicationPolicySource : IPolicySource
{
    public string Name => "application";

    public async ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var policies = await LoadPoliciesAsync(
            cancellationToken);

        return PolicySourceLoadResult.Success(policies);
    }
}
```

Register it as a singleton source:

```csharp
builder.Services
    .AddRuleGate()
    .AddPolicySource<ApplicationPolicySource>();
```

Source names are ordinal and must be unique. A source exception is converted
to the stable `POLICY_SOURCE_LOAD_EXCEPTION` diagnostic without exposing the
exception message or stack trace. Cancellation still propagates to the caller.

Manifest-based custom sources can convert compiler output without inventing a
second diagnostic model:

```csharp
var result = compiler
    .CompileFromText(yaml)
    .ToPolicySourceLoadResult();
```

## Manual reload

Resolve `IPolicyReloadService` when deployment or application logic controls
the reload boundary:

```csharp
var reloader = app.Services.GetRequiredService<
    IPolicyReloadService>();

var result = await reloader.ReloadAsync();

if (!result.IsSuccess)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        logger.LogError(
            "Policy source {Source} failed with {Code} at {Path}",
            diagnostic.SourceName,
            diagnostic.Code,
            diagnostic.Path);
    }
}
```

`CurrentSnapshot` exposes only operational metadata:

- monotonically increasing in-process version;
- active policy count;
- sorted source names.

It does not expose subject data, resource data, policy values, or manifest
contents. `LastReload` reports the latest activation or rejection, and
`HasReloaded` distinguishes the initial empty fail-closed state from a completed
load attempt.

## Atomic activation sequence

Every initial load and reload follows the same sequence:

1. Load every registered source.
2. Parse and validate every manifest-based source completely.
3. Combine only complete source results.
4. Reject duplicate source names, policy IDs, or resource/action routes.
5. Build an immutable lookup snapshot.
6. Replace the active snapshot with one atomic reference write.

If any step fails, `IsActivated` is `false`, deterministic diagnostics are
returned, and the last valid snapshot remains active. If no valid snapshot has
ever been activated, lookups return no policy and authorization denies by
default.

Readers do not lock during policy lookup. A request observes either the old
complete snapshot or the new complete snapshot; it cannot observe a partially
constructed collection.

## Operational guidance

- Treat every policy source as security-sensitive configuration.
- Restrict write access to reloadable files and configuration providers.
- Validate, lint, and test manifests before deployment even when runtime reload
  is enabled.
- Monitor rejected reloads and active snapshot versions.
- Use explicit source names in custom operational tooling rather than logging
  policy values.
- Coordinate policy promotion across application instances when identical
  generations are required; snapshot versions are local process counters, not
  distributed version identifiers.

See the [security model](security.md) for trust boundaries and the
[diagnostics guide](diagnostics.md) for runtime logging behavior.
