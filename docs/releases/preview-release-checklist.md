# NuGet Prerelease Checklist

This checklist defines the release process for RuleGate NuGet previews and
release candidates.

NuGet packages are immutable. A published version must never be overwritten,
and a published release tag must never be moved or recreated.

## Release package inventory

The current release family contains:

1. `Fotbiler.RuleGate.Abstractions`
2. `Fotbiler.RuleGate.Core`
3. `Fotbiler.RuleGate.Manifest`
4. `Fotbiler.RuleGate.AspNetCore`
5. `Fotbiler.RuleGate.Cli`
6. `Fotbiler.RuleGate.Keycloak`

Release verification produces one `.nupkg` and one `.snupkg` for every package
in the tagged source. Every package in the release family uses the same version
and is published for every NuGet release, including packages without code
changes. The current synchronized NuGet version is `1.0.0-rc.1`.

## Release workflow

Use this order:

1. Complete feature work through normal feature pull requests.
2. Update `main`.
3. Create a dedicated release-preparation branch.
4. Update version, changelog, workflow guardrails, documentation, roadmap, and
   every package README source.
5. Run all local validation.
6. Push the release branch and open a release pull request.
7. Wait for all required checks and manual review.
8. Merge the release pull request.
9. Delete the local and remote release branch.
10. Update and verify clean `main`.
11. Create and push an annotated release tag.
12. Manually dispatch the NuGet publish workflow with the existing tag.
13. Verify the workflow artifact and every package on NuGet.org.
14. Create the GitHub prerelease from the verified workflow artifacts.

Do not tag or publish directly from a feature branch.

## Prepare the release branch

Start from current and clean `main`:

```bash
git fetch \
  --prune \
  --tags \
  origin

git switch main
git pull \
  --ff-only \
  origin \
  main

git switch \
  --create \
  "chore/prepare-${VERSION}-release"
```

Use the exact intended version in the branch name.

## Release metadata gate

Before committing the release preparation, verify:

- [ ] `Directory.Build.props` contains the intended shared NuGet
      `VersionPrefix` and `VersionSuffix`.
- [ ] No package project declares a package-specific `VersionPrefix` or
      `VersionSuffix`.
- [ ] `CHANGELOG.md` contains a dated section for the release.
- [ ] The `Unreleased` comparison link begins at the new release tag.
- [ ] The release comparison link begins at the previous release tag.
- [ ] `.github/workflows/publish-nuget.yml` expects the exact tag.
- [ ] The workflow publishes the exact version.
- [ ] The workflow includes all six package IDs, even when a package has no
      code changes in the release.
- [ ] Every package contains the embedded `rulegate-icon.png` and declares it
      through NuGet `<icon>` metadata.
- [ ] Normal CI does not publish packages or create releases.

## Documentation gate

Documentation is part of the release and must be updated on the release branch
before tagging.

Review and update:

- [ ] Root `README.md`.
- [ ] `docs/README.md`.
- [ ] Getting-started guide.
- [ ] Capability-specific guides affected by the release.
- [ ] Roadmap status and milestone descriptions.
- [ ] This release checklist when the workflow changes.
- [ ] Every `packaging/nuget/*/README.md` source.
- [ ] Package tables contain the complete release family.
- [ ] Installation commands use the intended exact prerelease version.
- [ ] Delivered capabilities are not still described as planned.
- [ ] Local Markdown links resolve.
- [ ] Markdown code fences are balanced.
- [ ] NuGet README rendering is reviewed.

A release must not be tagged while repository documentation describes an older
package inventory, version, or capability state.

## Local validation gate

Run the complete release verification:

```bash
./scripts/verify-preview-release.sh
```

The verification must cover:

- clean repository state;
- restore;
- formatting;
- Release build;
- complete tests for every target framework;
- package creation;
- package count;
- package metadata and contents;
- source commit metadata;
- package-only consumer builds across every declared target;
- isolated execution on .NET Core 3.1 and .NET 5–7 plus installed .NET 8–10
  runtimes;
- packaged CLI tool installation and execution;
- generated C# output, stale detection, compilation, and execution on every
  supported framework;
- confirmation that normal CI does not publish.

Do not open or merge the release pull request while any validation is failing.

## Release pull request

Stage only intended release-preparation files.

Verify:

```bash
git diff \
  --cached \
  --check

git diff \
  --cached \
  --name-status

git diff \
  --cached \
  --stat
```

Push the release branch and open a pull request into `main`.

The pull request must pass all required checks before merge.

## Post-merge branch cleanup

After merge:

```bash
git fetch \
  --prune \
  origin

git switch main
git pull \
  --ff-only \
  origin \
  main

git branch \
  --delete \
  "chore/prepare-${VERSION}-release"

git push \
  origin \
  --delete \
  "chore/prepare-${VERSION}-release"
```

RuleGate repository cleanup should leave only local `main` and remote
`origin/main`.

Run the complete release verification again from the final merge commit.

## Pre-publication uniqueness checks

Before creating the tag, confirm that the version is absent from:

- local tags;
- remote tags;
- GitHub Releases;
- the `Fotbiler.RuleGate.Keycloak` NuGet.org package index.

Never rely on `--skip-duplicate` as a release strategy. A duplicate version
indicates that the release state must be inspected rather than republished.

## Create the annotated tag

Create the tag from the verified final `main` commit:

```bash
git tag \
  --annotate \
  "$TAG" \
  "$EXPECTED_COMMIT" \
  --message "RuleGate $VERSION"

git push \
  origin \
  "refs/tags/$TAG:refs/tags/$TAG"
```

Verify both the tag object and peeled commit.

Do not use a lightweight release tag.

## Trusted Publishing

RuleGate publishes to NuGet.org through GitHub Actions Trusted Publishing.

The workflow must use:

- GitHub environment `nuget-production`;
- `permissions: id-token: write`;
- `NuGet/login@v1`;
- the temporary `steps.login.outputs.NUGET_API_KEY`;
- `https://api.nuget.org/v3/index.json`.

A persistent NuGet API key must not be stored in the repository or required by
the release process.

## Dispatch the publish workflow

The release workflow is manually dispatched from `main` and receives the
already-created tag as input:

```bash
gh workflow run \
  publish-nuget.yml \
  --repo fotbiler-lab/rulegate \
  --ref main \
  --field "tag=$TAG"
```

Dispatch the workflow exactly once.

Do not trigger a second run while the first run is queued, in progress, or has
already published any package.

## Verify the publish workflow

Verify that the run:

- [ ] Uses `.github/workflows/publish-nuget.yml`.
- [ ] Was dispatched from `main`.
- [ ] Uses the expected final source commit.
- [ ] Validates the annotated tag.
- [ ] Runs the complete release verification.
- [ ] Completes successfully.
- [ ] Uploads one workflow artifact containing six verified `.nupkg` and six
      verified `.snupkg` files.
- [ ] Publishes every RuleGate NuGet package at `1.0.0-rc.1`.

Record:

- workflow run ID;
- job ID;
- artifact ID;
- tag object ID;
- tagged commit;
- artifact SHA-256 hashes.

## Verify NuGet.org publication

NuGet.org indexing may take several minutes after a successful push.

Wait until every package index contains the exact release version.

For every published `.nupkg`, verify:

- [ ] Package ID.
- [ ] Exact version.
- [ ] Repository URL.
- [ ] Source commit.
- [ ] License.
- [ ] Dependencies.
- [ ] Supported target frameworks.
- [ ] README presence.
- [ ] Symbol package publication.
- [ ] Package installation and the applicable smoke test using only NuGet.org.

## Create the GitHub prerelease

Create the GitHub prerelease only after NuGet publication and workflow artifact
verification succeed.

Use the existing annotated tag and upload the package files downloaded from the
successful workflow artifact.

The release must be created as a draft first.

Before publishing the draft, verify:

- [ ] Correct tag.
- [ ] Correct title.
- [ ] `prerelease` is enabled.
- [ ] Release notes match the changelog.
- [ ] Exactly six `.nupkg` assets exist.
- [ ] Exactly six `.snupkg` assets exist.
- [ ] Uploaded asset sizes match the workflow artifact.
- [ ] Downloaded release assets match the workflow artifact hashes.
- [ ] The release is not marked as latest stable.

Publish the draft only after every check succeeds.

## Final confirmation

- [ ] Local `main` equals `origin/main`.
- [ ] Working tree is clean.
- [ ] Only local `main` remains.
- [ ] Only remote `origin/main` remains.
- [ ] Annotated tag points to the intended release commit.
- [ ] The NuGet publish workflow completed successfully.
- [ ] The workflow artifact contains all six `.nupkg` and six `.snupkg` files.
- [ ] All six synchronized NuGet package versions are visible.
- [ ] Public package metadata is correct.
- [ ] GitHub release is marked as a prerelease.
- [ ] GitHub release contains all twelve verified assets.
- [ ] No package or release artifact is committed to Git.
- [ ] No persistent NuGet publishing credential was introduced.

## Failure handling

When publication fails:

- Do not move or delete a published tag merely to retry.
- Do not republish a package version already accepted by NuGet.org.
- Determine which registry accepted an artifact before taking another action.
- Resume only with a new version when immutability requires it.
- Keep failed or partial release evidence for diagnosis.
- Never hide a partial publication with duplicate-skipping behavior.

When documentation is discovered to be stale after package publication, update
it through an explicit documentation pull request. The existing tag and
immutable package version must not be changed.
