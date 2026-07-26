#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIRECTORY="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &&
  pwd
)"

REPOSITORY_ROOT="$(
  cd -- "$SCRIPT_DIRECTORY/.." &&
  pwd
)"

cd "$REPOSITORY_ROOT"

ALLOW_DIRTY="false"

case "${1:-}" in
  "")
    ;;

  --allow-dirty)
    ALLOW_DIRTY="true"
    ;;

  *)
    echo "Usage: $0 [--allow-dirty]"
    exit 2
    ;;
esac

EXPECTED_VERSION_PREFIX="0.1.0"
EXPECTED_VERSION_SUFFIX="preview.1"
EXPECTED_VERSION="$EXPECTED_VERSION_PREFIX-$EXPECTED_VERSION_SUFFIX"

EXPECTED_REPOSITORY_URL="https://github.com/fotbiler-lab/rulegate"
EXPECTED_LICENSE="Apache-2.0"
EXPECTED_AUTHOR="Fotbiler"
EXPECTED_FRAMEWORK="net10.0"

PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"

PACKAGE_IDS=(
  "Fotbiler.RuleGate.Abstractions"
  "Fotbiler.RuleGate.Core"
  "Fotbiler.RuleGate.Manifest"
)

read_property()
{
  local property_name="$1"

  sed -n \
    "s:.*<$property_name>\\(.*\\)</$property_name>.*:\\1:p" \
    Directory.Build.props |
    head -n 1
}

assert_contains()
{
  local content="$1"
  local expected="$2"
  local description="$3"

  if ! grep -F "$expected" \
    <<<"$content" \
    >/dev/null
  then
    printf 'ERROR: %s\n' "$description"
    printf 'Expected: %s\n' "$expected"
    exit 1
  fi
}

printf '\n== Verify repository state ==\n'

if [[ "$ALLOW_DIRTY" == "false" ]] &&
   [[ -n "$(git status --porcelain)" ]]
then
  echo "ERROR: Working tree is not clean."
  git status --short
  exit 1
fi

if [[ "$ALLOW_DIRTY" == "true" ]]
then
  echo "Dirty working tree is allowed for this verification run."
else
  echo "Working tree is clean."
fi

HEAD_COMMIT="$(
  git rev-parse HEAD
)"

printf 'Commit: %s\n' "$HEAD_COMMIT"

printf '\n== Verify central version ==\n'

ACTUAL_VERSION_PREFIX="$(
  read_property VersionPrefix
)"

ACTUAL_VERSION_SUFFIX="$(
  read_property VersionSuffix
)"

if [[ "$ACTUAL_VERSION_PREFIX" != "$EXPECTED_VERSION_PREFIX" ]]
then
  echo "ERROR: Unexpected VersionPrefix."
  echo "Expected: $EXPECTED_VERSION_PREFIX"
  echo "Actual:   $ACTUAL_VERSION_PREFIX"
  exit 1
fi

if [[ "$ACTUAL_VERSION_SUFFIX" != "$EXPECTED_VERSION_SUFFIX" ]]
then
  echo "ERROR: Unexpected VersionSuffix."
  echo "Expected: $EXPECTED_VERSION_SUFFIX"
  echo "Actual:   $ACTUAL_VERSION_SUFFIX"
  exit 1
fi

printf 'Version: %s\n' "$EXPECTED_VERSION"

printf '\n== Show .NET SDK ==\n'

dotnet --version

printf '\n== Restore ==\n'

dotnet restore \
  Fotbiler.RuleGate.slnx

printf '\n== Verify formatting ==\n'

dotnet format \
  Fotbiler.RuleGate.slnx \
  --verify-no-changes \
  --no-restore

printf '\n== Build ==\n'

dotnet build \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-restore

printf '\n== Test ==\n'

rm -rf TestResults

dotnet test \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-build \
  --logger trx \
  --results-directory TestResults

printf '\n== Pack ==\n'

rm -rf "$PACKAGE_DIRECTORY"

dotnet pack \
  Fotbiler.RuleGate.slnx \
  --configuration Release \
  --no-build

printf '\n== Verify package count ==\n'

NUPKG_COUNT="$(
  find "$PACKAGE_DIRECTORY" \
    -maxdepth 1 \
    -type f \
    -name '*.nupkg' \
    ! -name '*.snupkg' |
    wc -l
)"

SNUPKG_COUNT="$(
  find "$PACKAGE_DIRECTORY" \
    -maxdepth 1 \
    -type f \
    -name '*.snupkg' |
    wc -l
)"

printf 'nupkg:  %s\n' "$NUPKG_COUNT"
printf 'snupkg: %s\n' "$SNUPKG_COUNT"

if [[ "$NUPKG_COUNT" -ne 3 ]]
then
  echo "ERROR: Expected exactly three nupkg files."
  exit 1
fi

if [[ "$SNUPKG_COUNT" -ne 3 ]]
then
  echo "ERROR: Expected exactly three snupkg files."
  exit 1
fi

printf '\n== Verify package contents and metadata ==\n'

for package_id in "${PACKAGE_IDS[@]}"
do
  package_path="$PACKAGE_DIRECTORY/$package_id.$EXPECTED_VERSION.nupkg"
  symbol_path="$PACKAGE_DIRECTORY/$package_id.$EXPECTED_VERSION.snupkg"

  if [[ ! -f "$package_path" ]]
  then
    echo "ERROR: Missing package: $package_path"
    exit 1
  fi

  if [[ ! -f "$symbol_path" ]]
  then
    echo "ERROR: Missing symbol package: $symbol_path"
    exit 1
  fi

  printf '\nPACKAGE: %s\n' "$package_id"

  package_files="$(
    unzip -Z1 "$package_path"
  )"

  assert_contains \
    "$package_files" \
    "README.md" \
    "$package_id does not contain README.md."

  assert_contains \
    "$package_files" \
    "lib/$EXPECTED_FRAMEWORK/$package_id.dll" \
    "$package_id does not contain its framework assembly."

  nuspec_path="$(
    grep -E '\.nuspec$' \
      <<<"$package_files" |
      head -n 1
  )"

  if [[ -z "$nuspec_path" ]]
  then
    echo "ERROR: No nuspec found in $package_id."
    exit 1
  fi

  nuspec_content="$(
    unzip -p \
      "$package_path" \
      "$nuspec_path"
  )"

  assert_contains \
    "$nuspec_content" \
    "<id>$package_id</id>" \
    "$package_id has an unexpected package identifier."

  assert_contains \
    "$nuspec_content" \
    "<version>$EXPECTED_VERSION</version>" \
    "$package_id has an unexpected package version."

  assert_contains \
    "$nuspec_content" \
    "<authors>$EXPECTED_AUTHOR</authors>" \
    "$package_id has unexpected author metadata."

  assert_contains \
    "$nuspec_content" \
    "<license type=\"expression\">$EXPECTED_LICENSE</license>" \
    "$package_id has unexpected license metadata."

  assert_contains \
    "$nuspec_content" \
    "<readme>README.md</readme>" \
    "$package_id does not declare README.md."

  assert_contains \
    "$nuspec_content" \
    "<projectUrl>$EXPECTED_REPOSITORY_URL</projectUrl>" \
    "$package_id has an unexpected project URL."

  assert_contains \
    "$nuspec_content" \
    "url=\"$EXPECTED_REPOSITORY_URL\"" \
    "$package_id has an unexpected repository URL."

  assert_contains \
    "$nuspec_content" \
    "commit=\"$HEAD_COMMIT\"" \
    "$package_id does not reference the current commit."

  symbol_files="$(
    unzip -Z1 "$symbol_path"
  )"

  assert_contains \
    "$symbol_files" \
    "lib/$EXPECTED_FRAMEWORK/$package_id.pdb" \
    "$package_id symbol package does not contain its portable PDB."

  case "$package_id" in
    Fotbiler.RuleGate.Abstractions)
      ;;

    Fotbiler.RuleGate.Core)
      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.Abstractions\" version=\"$EXPECTED_VERSION\"" \
        "Core does not depend on the expected Abstractions version."
      ;;

    Fotbiler.RuleGate.Manifest)
      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.Abstractions\" version=\"$EXPECTED_VERSION\"" \
        "Manifest does not depend on the expected Abstractions version."

      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"YamlDotNet\" version=\"18.1.0\"" \
        "Manifest does not depend on YamlDotNet 18.1.0."
      ;;
  esac

  echo "Verified: $package_id"
done

printf '\n== Run package consumer smoke test ==\n'

./scripts/test-nuget-consumer-smoke.sh \
  --packages-ready

printf '\n== Verify normal CI does not publish ==\n'

if grep -RInE \
  'dotnet nuget push|NUGET_API_KEY|packages: write|contents: write|gh release create' \
  .github/workflows
then
  echo "ERROR: Release or publishing configuration exists in the normal CI workflow."
  exit 1
fi

echo "No release or publishing configuration found in the normal CI workflow."

printf '\n== Release verification succeeded ==\n'

printf 'Version: %s\n' "$EXPECTED_VERSION"
printf 'Commit:  %s\n' "$HEAD_COMMIT"
printf 'Packages: 3 nupkg + 3 snupkg\n'
printf 'Tests:    completed successfully\n'
printf 'Consumer: completed successfully\n'
