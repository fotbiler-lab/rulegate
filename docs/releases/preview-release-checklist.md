# Preview Release Checklist

This checklist defines the manual release process for the first Fotbiler RuleGate preview.

## Release

```text
Version: 0.1.0-preview.1
Tag: v0.1.0-preview.1
Target framework: net10.0
```

Packages:

```text
Fotbiler.RuleGate.Abstractions
Fotbiler.RuleGate.Core
Fotbiler.RuleGate.Manifest
```

## Release boundaries

This process is intentionally manual.

The normal CI workflow must:

- restore
- verify formatting
- build
- test
- pack
- run the NuGet package consumer smoke test
- upload package artifacts

The normal CI workflow must not:

- create Git tags
- create GitHub Releases
- push packages to NuGet.org
- contain a NuGet API key

## Before merging the release-readiness pull request

- [ ] Release-readiness branch is based on current `main`.
- [ ] Working tree is clean.
- [ ] Release version is `0.1.0-preview.1`.
- [ ] `CHANGELOG.md` describes the preview contents.
- [ ] Release verification script passes.
- [ ] CI pull-request workflow passes.
- [ ] NuGet package artifacts are downloadable from CI.
- [ ] No NuGet publishing configuration exists in the normal CI workflow.
- [ ] No credentials or API keys are committed.

## Before creating the release

Run from an updated and clean `main` branch:

```bash
git switch main
git pull --ff-only origin main
git fetch origin --prune

git status --short --branch

./scripts/verify-preview-release.sh
```

Confirm that no release with the same version already exists:

```bash
git tag --list 'v0.1.0-preview.1'

git ls-remote \
  --tags \
  origin \
  'refs/tags/v0.1.0-preview.1'

gh release view \
  v0.1.0-preview.1
```

The final command is expected to report that the release does not exist before the first publication.

Recheck the package identifiers:

```bash
for package_id in \
  Fotbiler.RuleGate.Abstractions \
  Fotbiler.RuleGate.Core \
  Fotbiler.RuleGate.Manifest
do
  lowercase_id="$(
    printf '%s' "$package_id" |
      tr '[:upper:]' '[:lower:]'
  )"

  curl \
    --silent \
    --show-error \
    --output /dev/null \
    --write-out "$package_id: HTTP %{http_code}\n" \
    "https://api.nuget.org/v3-flatcontainer/$lowercase_id/index.json"
done
```

A `404` response means that no published version is currently visible. It does not reserve ownership of the package identifier.

## Prepare the release commit

Before release, replace `Unreleased` on the `0.1.0-preview.1` changelog heading with the actual release date.

Example:

```text
## [0.1.0-preview.1] - 2026-07-26
```

Commit the date change and merge it into `main`.

Run the full verification script again from the resulting release commit.

## Create and push the tag

Create an annotated tag from the verified `main` commit:

```bash
git switch main
git pull --ff-only origin main

git tag \
  --annotate \
  v0.1.0-preview.1 \
  --message "Fotbiler RuleGate 0.1.0-preview.1"

git show \
  --no-patch \
  --decorate \
  v0.1.0-preview.1

git push origin \
  v0.1.0-preview.1
```

Do not move or recreate a published version tag.

## NuGet.org API key

Create a scoped NuGet.org API key with only the permissions required to publish these packages.

Keep the key outside the repository:

```bash
read -rsp 'NuGet API key: ' NUGET_API_KEY
export NUGET_API_KEY
echo
```

Never paste the key into:

- repository files
- Git history
- pull-request descriptions
- issue comments
- terminal screenshots
- shell history

## Publish order

Publish packages in dependency order:

1. `Fotbiler.RuleGate.Abstractions`
2. `Fotbiler.RuleGate.Core`
3. `Fotbiler.RuleGate.Manifest`

Both the `.nupkg` and matching `.snupkg` must exist in `artifacts/packages`.

Publish Abstractions:

```bash
dotnet nuget push \
  artifacts/packages/Fotbiler.RuleGate.Abstractions.0.1.0-preview.1.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --symbol-api-key "$NUGET_API_KEY" \
  --symbol-source https://api.nuget.org/v3/index.json
```

Publish Core:

```bash
dotnet nuget push \
  artifacts/packages/Fotbiler.RuleGate.Core.0.1.0-preview.1.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --symbol-api-key "$NUGET_API_KEY" \
  --symbol-source https://api.nuget.org/v3/index.json
```

Publish Manifest:

```bash
dotnet nuget push \
  artifacts/packages/Fotbiler.RuleGate.Manifest.0.1.0-preview.1.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --symbol-api-key "$NUGET_API_KEY" \
  --symbol-source https://api.nuget.org/v3/index.json
```

Do not use `--skip-duplicate` for the first publication. A duplicate version must be investigated rather than ignored.

Clear the key after publication:

```bash
unset NUGET_API_KEY
```

## Verify NuGet.org publication

Confirm all three packages and the exact preview version:

```bash
for package_id in \
  Fotbiler.RuleGate.Abstractions \
  Fotbiler.RuleGate.Core \
  Fotbiler.RuleGate.Manifest
do
  lowercase_id="$(
    printf '%s' "$package_id" |
      tr '[:upper:]' '[:lower:]'
  )"

  printf '\nPACKAGE: %s\n' "$package_id"

  curl \
    --silent \
    --show-error \
    "https://api.nuget.org/v3-flatcontainer/$lowercase_id/index.json"
done
```

Verify that:

- [ ] `0.1.0-preview.1` appears for all three packages.
- [ ] Core depends on Abstractions `0.1.0-preview.1`.
- [ ] Manifest depends on Abstractions `0.1.0-preview.1`.
- [ ] Manifest depends on YamlDotNet `18.1.0`.
- [ ] README renders correctly.
- [ ] License is Apache-2.0.
- [ ] Repository URL and commit are correct.
- [ ] Symbols were accepted.

## Create the GitHub prerelease

Prepare release notes from the `0.1.0-preview.1` section of `CHANGELOG.md`.

Create the GitHub prerelease using the existing tag:

```bash
gh release create \
  v0.1.0-preview.1 \
  artifacts/packages/*.nupkg \
  artifacts/packages/*.snupkg \
  --verify-tag \
  --prerelease \
  --title "Fotbiler RuleGate 0.1.0-preview.1" \
  --notes-file /path/to/release-notes.md
```

Confirm the release:

```bash
gh release view \
  v0.1.0-preview.1
```

## Post-release smoke test

Create a clean directory outside the repository:

```bash
SMOKE_ROOT="$(
  mktemp \
    --directory \
    --tmpdir \
    rulegate-release-smoke.XXXXXX
)"

cd "$SMOKE_ROOT"

dotnet new console \
  --framework net10.0

dotnet add package \
  Fotbiler.RuleGate.Core \
  --version 0.1.0-preview.1

dotnet add package \
  Fotbiler.RuleGate.Manifest \
  --version 0.1.0-preview.1

dotnet restore
dotnet build \
  --configuration Release
```

Verify that the published packages restore from NuGet.org without a local package source.

## Final confirmation

- [ ] Tag points to the intended release commit.
- [ ] GitHub release is marked as a prerelease.
- [ ] Three `.nupkg` assets are attached.
- [ ] Three `.snupkg` assets are attached.
- [ ] Three NuGet.org package versions are visible.
- [ ] Public README renders correctly.
- [ ] Published consumer restore succeeds.
- [ ] No API key remains in the environment.
- [ ] No release artifact is committed to Git.
