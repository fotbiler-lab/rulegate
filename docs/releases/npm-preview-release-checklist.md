# npm Preview Release Checklist

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
independent version line from the synchronized NuGet package family.

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
- [ ] License is `Apache-2.0`.
- [ ] Modern Angular peer dependencies cover Angular 20–22.
- [ ] Legacy Angular peer dependencies cover Angular 12–19.
- [ ] Both Angular adapters require the exact package-family client version.
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

Trusted Publishing configuration requires a package to exist on npm. The
existing `@fotbiler/rulegate-angular` package is already configured. Before the
first family release, bootstrap `@fotbiler/rulegate-client` and
`@fotbiler/rulegate-angular-legacy` with an authenticated maintainer operation,
then configure their Trusted Publishers. Do not dispatch the three-package
workflow until all three trust relationships exist.

Before the first publish:

1. Enable 2FA on the maintainer npm account.
2. Authenticate locally with `npm login --auth-type=web`.
3. Verify `npm whoami` and `npm org ls fotbiler`.
4. Check out the exact annotated tag commit.
5. Run the npm release verifier and compare the tarball hash.
6. Install npm CLI 12.0.1 or another reviewed compatible version.

Use a dedicated bootstrap version that is not the intended family release
version, or follow the current npm staged-package bootstrap process if it
exposes package settings before approval. Never consume the intended family
version during bootstrap. Record the exact command, artifact hash, and npm
result in the release evidence.

Local bootstrap publication cannot generate GitHub Actions provenance. This
exception applies only to each package's bootstrap version. Do not create a
temporary automation token to work around the bootstrap constraint.

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
branch. After merging and tagging, dispatch from `main`:

```bash
gh workflow run \
  publish-npm.yml \
  --repo fotbiler-lab/rulegate \
  --ref main \
  --field "tag=$TAG"
```

Verify the workflow artifact and all three staged packages, then approve each
with 2FA on npmjs.com. Do not approve any staged package until the complete
family metadata, contents, source commit, provenance, and SHA-256 evidence
match the verified release.

## Public-package verification

After publication, verify:

- [ ] Exact names and one aligned version across all three packages.
- [ ] Public visibility.
- [ ] `preview` distribution tag.
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
