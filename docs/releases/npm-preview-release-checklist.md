# npm Preview Release Checklist

This checklist defines the release process for the public
`@fotbiler/rulegate-angular` npm package.

npm package versions and release tags are immutable. Never overwrite a
published version or move an existing release tag.

## Release inventory

The `0.5.0-preview.1` npm release contains one package:

```text
@fotbiler/rulegate-angular
```

The package is public and uses both the `latest` and `preview` distribution
tags. npm packages have an independent version line from the synchronized
NuGet package family, which is currently `0.6.0-preview.2`.

## Required security configuration

- npm organization: `fotbiler`
- Public package: `@fotbiler/rulegate-angular`
- Maintainer npm account with two-factor authentication enabled
- GitHub repository: `fotbiler-lab/rulegate`
- GitHub environment: `npm-production`
- Trusted Publishing workflow: `publish-npm.yml`
- Allowed Trusted Publishing action: `npm stage publish` only
- GitHub-hosted runner with `id-token: write`
- npm CLI 11.15.0 or newer for staged publishing

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

- [ ] `src/Fotbiler.RuleGate.Angular/package.json` contains the exact version.
- [ ] Package name is `@fotbiler/rulegate-angular`.
- [ ] `publishConfig.access` is `public`.
- [ ] Repository URL and directory are correct.
- [ ] License is `Apache-2.0`.
- [ ] Angular peer dependencies match the supported major version.
- [ ] The Keycloak secondary entrypoint is present without making
      `keycloak-js` a dependency or peer dependency.
- [ ] Changelog contains a dated release section.
- [ ] Roadmap describes the Angular SDK as available.
- [ ] Installation examples contain the exact preview version.
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
production build, tests, package metadata, tarball contents, exported public
APIs, a package-only Angular consumer, registry uniqueness, and normal-CI
publication isolation. The consumer installs real `keycloak-js` separately and
compiles against the optional secondary entrypoint.

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

## First-package bootstrap

Trusted Publishing and staged publishing require the package to exist on npm.
Therefore `0.4.0-preview.1` is a one-time bootstrap publication.

Before the first publish:

1. Enable 2FA on the maintainer npm account.
2. Authenticate locally with `npm login --auth-type=web`.
3. Verify `npm whoami` and `npm org ls fotbiler`.
4. Check out the exact annotated tag commit.
5. Run the npm release verifier and compare the tarball hash.
6. Install npm CLI 12.0.1 or another reviewed compatible version.

Publish the already-verified tarball:

```bash
npm publish \
  artifacts/npm/fotbiler-rulegate-angular-0.4.0-preview.1.tgz \
  --access public \
  --tag preview \
  --provenance=false
```

Local bootstrap publication cannot generate GitHub Actions provenance. This
exception applies only to the first package version. Do not create a temporary
automation token to work around the bootstrap constraint.

## Configure Trusted Publishing

After the first package is visible, open its npm settings and create a GitHub
Actions Trusted Publisher with these exact values:

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

The `0.4.0-preview.1` workflow is installed for trust configuration but must
not be dispatched after the manual bootstrap publish because that immutable
version already exists.

## Future staged publication

For later releases, update the exact version and tag guardrails on the release
branch. After merging and tagging, dispatch from `main`:

```bash
gh workflow run \
  publish-npm.yml \
  --repo fotbiler-lab/rulegate \
  --ref main \
  --field "tag=$TAG"
```

Verify the workflow artifact and staged package, then approve it with 2FA on
npmjs.com. Do not approve a staged package until its metadata, contents, source
commit, provenance, and SHA-256 evidence match the verified release.

## Public-package verification

After publication, verify:

- [ ] Exact name and version.
- [ ] Public visibility.
- [ ] `preview` distribution tag.
- [ ] Repository URL and directory.
- [ ] License and README rendering.
- [ ] Angular peer dependencies.
- [ ] FESM and TypeScript declaration assets.
- [ ] Keycloak secondary-entrypoint FESM and declaration assets.
- [ ] No `keycloak-js` dependency or peer dependency in the RuleGate package.
- [ ] Package installation into a clean Angular application.
- [ ] No unexpected files or lifecycle scripts.

## GitHub prerelease

Create a draft GitHub prerelease only after npm and NuGet verification succeed.
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
