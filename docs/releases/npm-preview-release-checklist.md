# npm Prerelease Checklist

This checklist defines the release process for the public RuleGate npm package
family.

npm package versions and release tags are immutable. Never overwrite a
published version or move an existing release tag.

## Release inventory

The compatibility package family contains:

```text
@fotbiler/rulegate-client
@fotbiler/rulegate-angular-legacy
@fotbiler/rulegate-angular
```

All three packages are public, share one npm version, and are published
together even when only one package has code changes. npm packages retain an
independent version line from the synchronized NuGet package family. The
current synchronized npm prerelease version is `1.0.0-rc.1`.

## Required security configuration

- npm organization: `fotbiler`
- Public packages: all three packages in the release inventory
- Maintainer npm account with two-factor authentication enabled
- GitHub repository: `fotbiler-lab/rulegate`
- GitHub environment: `npm-production`
- Trusted Publishing workflow: `publish-npm.yml`
- Allowed Trusted Publishing action: `npm stage publish` only
- GitHub-hosted runner with `id-token: write`
- npm CLI 11.5.1 or newer and Node.js 22.14.0 or newer for Trusted
  Publishing; use the reviewed versions pinned in the workflow

Do not store an npm password, session token, granular access token, recovery
code, or one-time password in the repository or GitHub Actions.

## Release preparation

Use the normal release order:

1. Complete feature work through a feature pull request.
2. Create a dedicated release-preparation branch from clean `main`.
3. Update the changelog, roadmap, package README, public documentation,
   workflow guardrails, and this checklist.
4. Run npm validation and the relevant repository validation.
5. Push the release branch and open a pull request.
6. Merge only after every required check passes.
7. Delete the release branch and update clean `main`.
8. Re-run release verification from the final merge commit.
9. Create and push an annotated release tag.
10. Publish and verify the npm artifact before creating its GitHub prerelease.
    For a coordinated cross-ecosystem release, verify both registries first.

Do not publish from a feature or release-preparation branch.

## Metadata gate

Before tagging, verify:

- [ ] All three source `package.json` files contain the same exact version.
- [ ] Package names and repository directories match the release inventory.
- [ ] `publishConfig.access` is `public`.
- [ ] Repository URL and directory are correct.
- [ ] License is `MIT`.
- [ ] Modern Angular peer dependencies cover Angular 20–22.
- [ ] Legacy Angular peer dependencies cover Angular 12–19.
- [ ] Both Angular adapters require the exact package-family client version.
- [ ] The Keycloak secondary entrypoint is present without making
      `keycloak-js` a dependency or peer dependency.
- [ ] Changelog contains a dated release section.
- [ ] Roadmap describes the Angular SDK as available.
- [ ] Installation examples contain the exact prerelease version.
- [ ] Normal CI builds packages but cannot publish them.
- [ ] `publish-npm.yml` expects the exact annotated release tag.

## Local validation

Create a local commit, then run:

```bash
./scripts/verify-npm-preview-release.sh \
  --check-registry-availability

./scripts/verify-preview-release.sh
```

The npm verifier checks repository cleanliness, locked dependencies, format,
production builds, tests, metadata for all three packages, tarball contents,
exported public APIs, package-only Angular consumers, registry uniqueness, and
normal-CI publication isolation. The CI compatibility matrix builds real
consumers on Angular 9, 11, 12, 15, 16, 19, 20, 21, and 22.

Record the produced tarball name and SHA-256 hash.

## Create the annotated tag

Create the release tag only from verified final `main`:

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

Verify the tag object and its peeled commit before publishing.

## New-package bootstrap

Staged publishing and Trusted Publishing configuration both require the package
to already exist on npm. A brand-new package therefore needs a one-time
maintainer bootstrap before it can join the normal staged family workflow.

For a brand-new package:

1. Enable 2FA on the maintainer npm account.
2. Authenticate locally with `npm login --auth-type=web`.
3. Verify `npm whoami` and the maintainer's access to the `fotbiler` scope.
4. Check out the exact verified release/tag commit that produced the tarball.
5. Run the npm release verifier and compare the tarball hash.
6. Publish the verified tarball directly with public access, the intended
   distribution tag, and provenance explicitly disabled because the operation
   is not running in GitHub Actions.
7. Verify the public registry version and `dist.shasum` against the exact
   verified tarball.
8. Configure the package's GitHub Actions Trusted Publisher before its next
   release.

Prefer a dedicated bootstrap version when planning a new package ahead of a
coordinated family release. This keeps the intended coordinated version
available for the normal staged workflow.

If the coordinated release version itself must be used as an exceptional
bootstrap version, treat that version as immutable immediately after npm
accepts it. Do not then dispatch a release workflow that expects the same
version to still be unpublished.

Local bootstrap publication cannot generate GitHub Actions provenance. This
exception applies only to the bootstrap publication. Do not create a temporary
automation token to work around the bootstrap constraint.

## Configure Trusted Publishing

For each package, open npm settings and create a GitHub Actions Trusted
Publisher with these exact values:

| Field                | Value                    |
| -------------------- | ------------------------ |
| Organization or user | `fotbiler-lab`           |
| Repository           | `rulegate`               |
| Workflow filename    | `publish-npm.yml`        |
| Environment          | `npm-production`         |
| Allowed action       | `npm stage publish` only |

Then set package publishing access to require 2FA and disallow traditional
tokens. Future GitHub Actions publications use short-lived OIDC credentials
and automatic provenance.

## Future staged publication

For later releases, update the exact version and tag guardrails on the release
branch. After merging, final-main verification, and annotated tagging, dispatch
from `main`:

```bash
gh workflow run \
  publish-npm.yml \
  --repo fotbiler-lab/rulegate \
  --ref main \
  --field "tag=$TAG"
```

The workflow verifies and stages all three packages through GitHub Actions OIDC.
It intentionally does not approve the staged packages.

Review every staged package before approval:

```bash
npm stage list @fotbiler/rulegate-client
npm stage list @fotbiler/rulegate-angular-legacy
npm stage list @fotbiler/rulegate-angular

npm stage view <stage-id>
npm stage download <stage-id>
```

Compare package metadata, tarball contents, source commit, provenance, and
artifact hashes with the verified release evidence. Only then approve each
stage with an authenticated maintainer account:

```bash
npm stage approve <stage-id>
```

Every approval requires maintainer proof-of-presence/2FA. GitHub Actions OIDC
must not be used to approve or reject staged packages.

The workflow stages this release candidate with the `rc` distribution tag.
Until RuleGate has a stable release, also align `latest` to the newly verified
release candidate after all three packages are publicly visible:

```bash
npm dist-tag add "@fotbiler/rulegate-client@$VERSION" latest
npm dist-tag add "@fotbiler/rulegate-angular-legacy@$VERSION" latest
npm dist-tag add "@fotbiler/rulegate-angular@$VERSION" latest
```

Verify both `rc` and `latest` on all three packages. After the first stable
RuleGate release, reserve `latest` for the stable line and keep release
candidates on `rc`.

## Public-package verification

After publication, verify:

- [ ] Exact names and one aligned version across all three packages.
- [ ] Public visibility.
- [ ] `rc` distribution tag points to the intended release-candidate version.
- [ ] Before the first stable release, `latest` is deliberately aligned to the
      intended release-candidate version; after stable release, `latest` remains on the
      stable line.
- [ ] Repository URL and directory.
- [ ] License and README rendering.
- [ ] Modern and legacy Angular peer dependencies.
- [ ] Exact client peer dependency alignment.
- [ ] FESM and TypeScript declaration assets.
- [ ] Keycloak secondary-entrypoint FESM and declaration assets.
- [ ] No `keycloak-js` dependency or peer dependency in the RuleGate package.
- [ ] Package installation into the compatibility consumer matrix.
- [ ] No unexpected files or lifecycle scripts.

## GitHub prerelease

Create the GitHub prerelease only after npm and NuGet verification succeed.
Use the existing annotated tag, attach the verified `.tgz`, `.nupkg`, and
`.snupkg`, include the release notes, and leave the release marked as a
prerelease rather than the latest stable release.

## Failure handling

- Never retry publication by moving or deleting a published tag.
- Never republish an accepted package version.
- Do not use duplicate-skipping as a recovery strategy.
- Inspect registry and staged-package state before retrying a failed workflow.
- Use a new version when npm immutability requires it.
- Preserve failed workflow logs and artifact hashes for diagnosis.
