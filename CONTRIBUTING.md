# Contributing to Fotbiler RuleGate

Thank you for considering a contribution to Fotbiler RuleGate.

RuleGate is a security-sensitive authorization framework. Contributions must
preserve default-deny and fail-closed behavior, stable public contracts,
deterministic policy evaluation, and the provider-independent core.

## Before opening an issue

Search existing issues before creating a new report or proposal.

Use the provided forms for:

- Reproducible bugs
- Usage and troubleshooting questions
- Feature proposals
- Documentation problems

Do not report security vulnerabilities through public issues. Follow
[SECURITY.md](SECURITY.md).

## Development requirements

Use the .NET SDK selected by [`global.json`](global.json).

The repository targets:

- .NET 8
- .NET 9
- .NET 10

Clone and restore the repository:

```bash
git clone https://github.com/fotbiler-lab/rulegate.git
cd rulegate

dotnet restore Fotbiler.RuleGate.slnx
```

## Branches

Create a focused branch from the latest `main`.

Recommended branch prefixes:

- `feat/` for new behavior
- `fix/` for corrections
- `docs/` for documentation
- `test/` for test-only work
- `chore/` for maintenance and repository work

Keep each branch limited to one coherent change.

## Coding expectations

Contributions should:

- Preserve default-deny and fail-closed authorization behavior.
- Avoid coupling the core engine to an identity provider or remote service.
- Keep public APIs intentional, stable, and documented.
- Avoid exposing claims, roles, permissions, attributes, subject identifiers,
  resource identifiers, or policy internals through public responses or
  diagnostics by default.
- Keep matching ordinal and case-sensitive unless a public contract explicitly
  states otherwise.
- Preserve cancellation through APIs that accept a cancellation token.
- Keep singleton extension points thread-safe.
- Keep generated and serialized output deterministic.
- Include successful, denied, malformed, and indeterminate tests where
  applicable.
- Add package-only consumer coverage for new public integration APIs.

Read the [security model](docs/security.md) before changing authorization,
mapping, diagnostics, HTTP result, manifest, or extension-point behavior.

## Formatting, build, tests, and packages

Run the same primary validation sequence used by CI:

```bash
dotnet restore \
  Fotbiler.RuleGate.slnx

dotnet format \
  Fotbiler.RuleGate.slnx \
  --verify-no-changes \
  --no-restore

dotnet build \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-restore

dotnet test \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-build

dotnet pack \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-build

git diff --check
```

Run relevant focused tests while developing, but complete the full validation
sequence before opening a pull request.

Before proposing a release-related change, create a local commit so the working
tree is clean, then run:

```bash
./scripts/verify-preview-release.sh
```

The release verification script requires a clean working tree.

## Pull requests

A pull request should:

- Explain the problem and intended outcome.
- Describe public API or behavioral changes.
- State the authorization and security impact.
- Include relevant regression tests.
- Update documentation and the changelog when appropriate.
- Keep unrelated refactoring out of the change.
- Pass all repository CI checks.
- Be manually reviewed as a complete consumer workflow where applicable.

Preview APIs may change, but breaking changes must still be deliberate,
documented, and tested.

## Commit messages

Use short, descriptive commit messages.

Conventional Commit-style subjects are preferred:

```text
feat(core): add requirement evaluator
fix(manifest): reject duplicate policy identifiers
docs: improve authorization examples
test(aspnetcore): cover challenge behavior
chore(release): prepare preview release
```

## Code of Conduct

Participation in the project is governed by
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## License

By contributing, you agree that your contributions will be licensed under the
repository's [Apache License 2.0](LICENSE).
