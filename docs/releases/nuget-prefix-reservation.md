# NuGet Package ID Prefix Reservation

The [`Prefix Reserved` badge](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation)
is assigned by NuGet.org. It is not package metadata and cannot be enabled in a
project file or publishing workflow.

RuleGate package metadata already uses a consistent `Fotbiler` author, the
Apache-2.0 license expression, repository URL, and an embedded 128×128 PNG
package icon. Every package-verification run checks the icon file and `<icon>`
metadata before publication.

## Requested reservation

- NuGet.org owner display name: `fotbiler` — confirm the exact casing on the
  owner profile before sending
- Requested prefix: `Fotbiler.RuleGate`
- Repository: `https://github.com/fotbiler-lab/rulegate`
- Intended mode: private reservation, not a public prefix

The request covers:

- `Fotbiler.RuleGate.Abstractions`
- `Fotbiler.RuleGate.Core`
- `Fotbiler.RuleGate.Manifest`
- `Fotbiler.RuleGate.AspNetCore`
- `Fotbiler.RuleGate.Cli`
- `Fotbiler.RuleGate.Keycloak`

## Email draft

Send the following from the email address associated with the NuGet.org owner
account to `account@nuget.org`:

```text
Subject: Package ID prefix reservation request for Fotbiler.RuleGate

Hello NuGet.org team,

I would like to request a private package ID prefix reservation for
Fotbiler.RuleGate.

NuGet.org owner display name: fotbiler
Requested prefix: Fotbiler.RuleGate
Project repository: https://github.com/fotbiler-lab/rulegate

The owner publishes the RuleGate package family under this prefix. The
packages use consistent Fotbiler author metadata, an Apache-2.0 license
expression, an embedded RuleGate icon, and the same source repository.

Current packages:
- Fotbiler.RuleGate.Abstractions
- Fotbiler.RuleGate.Core
- Fotbiler.RuleGate.Manifest
- Fotbiler.RuleGate.AspNetCore
- Fotbiler.RuleGate.Cli
- Fotbiler.RuleGate.Keycloak

Please let me know if you need any additional ownership or identity details.

Kind regards,
Eren Gaygusuz
```

NuGet.org may ask for additional identity or ownership evidence. Once approved,
the badge applies to matching existing and future packages owned by the
reserved-prefix owner; no package rebuild is required.
