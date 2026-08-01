# Security Policy

RuleGate is an authorization framework. Security reports are taken
seriously, especially when they involve authorization bypasses, fail-open
behavior, information disclosure, policy confusion, unsafe diagnostics, or
package integrity.

This file describes vulnerability reporting. For runtime and integration
security guidance, read the [RuleGate security model](docs/security.md).

## Supported versions

RuleGate 1.0 is the current stable release.

Only the latest supported stable line in each package ecosystem receives
security fixes.

| Package family                      | Version                                                                            | Supported |
| ----------------------------------- | ---------------------------------------------------------------------------------- | --------- |
| `@fotbiler/rulegate-client`         | [`1.0.0`](https://www.npmjs.com/package/@fotbiler/rulegate-client/v/1.0.0)         | Yes       |
| `@fotbiler/rulegate-angular`        | [`1.0.0`](https://www.npmjs.com/package/@fotbiler/rulegate-angular/v/1.0.0)        | Yes       |
| `@fotbiler/rulegate-angular-legacy` | [`1.0.0`](https://www.npmjs.com/package/@fotbiler/rulegate-angular-legacy/v/1.0.0) | Yes       |
| NuGet packages                      | [`1.0.0`](https://github.com/fotbiler-lab/rulegate/releases/tag/v1.0.0)            | Yes       |
| Release candidates and previews     | —                                                                                  | No        |

This table is updated when a new supported stable version is published.

## Reporting a vulnerability

Do not open a public GitHub issue for a suspected vulnerability.

Use
[GitHub private vulnerability reporting](https://github.com/fotbiler-lab/rulegate/security/advisories/new).

Include as much of the following information as possible:

- Affected package and version
- Affected target framework
- Vulnerability category
- Preconditions required to reproduce it
- Minimal reproduction or proof of concept
- Expected authorization decision
- Actual authorization decision
- Potential impact
- Suggested mitigation, when known

Do not include real credentials, access tokens, private keys, personal data, or
production policy data.

## What to expect

The maintainers will attempt to:

1. Acknowledge the report.
2. Confirm whether the behavior is reproducible.
3. Assess severity and affected versions.
4. Prepare a fix and regression tests.
5. Coordinate disclosure and release notes.

Response and resolution times depend on severity, reproducibility, maintainer
availability, and release complexity.

## Security-sensitive areas

Reports are especially valuable when they involve:

- Authorization bypass
- Fail-open behavior
- Incorrect policy selection
- Requirement evaluator confusion
- Resource type or action mismatches
- Subject, resource, or context attribute confusion
- Manifest parsing or compilation inconsistencies
- Sensitive information in HTTP responses
- Sensitive information in logs, traces, or diagnostics
- Unsafe dependency-injection replacement
- Cancellation or exception behavior that changes authorization outcomes
- Package or release supply-chain integrity

## Disclosure

Allow the maintainers reasonable time to investigate and publish a fix before
publicly disclosing a vulnerability.

Do not publish proof-of-concept material that exposes users before a fix or
mitigation is available.
