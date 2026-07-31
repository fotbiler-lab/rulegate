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

EXPECTED_VERSION_PREFIX="0.9.0"
EXPECTED_VERSION_SUFFIX="preview.1"
EXPECTED_VERSION="$EXPECTED_VERSION_PREFIX-$EXPECTED_VERSION_SUFFIX"

EXPECTED_REPOSITORY_URL="https://github.com/fotbiler-lab/rulegate"
EXPECTED_LICENSE="Apache-2.0"
EXPECTED_AUTHOR="Fotbiler"
EXPECTED_FRAMEWORKS_VALUE="net8.0;net9.0;net10.0"

EXPECTED_FRAMEWORKS=(
  "net8.0"
  "net9.0"
  "net10.0"
)

FRAMEWORK_COUNT="${#EXPECTED_FRAMEWORKS[@]}"

PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"

PACKAGE_IDS=(
  "Fotbiler.RuleGate.Abstractions"
  "Fotbiler.RuleGate.Core"
  "Fotbiler.RuleGate.Manifest"
  "Fotbiler.RuleGate.AspNetCore"
  "Fotbiler.RuleGate.Cli"
  "Fotbiler.RuleGate.Keycloak"
)

declare -A EXPECTED_PACKAGE_TITLES=(
  ["Fotbiler.RuleGate.Abstractions"]="RuleGate Abstractions"
  ["Fotbiler.RuleGate.Core"]="RuleGate Core"
  ["Fotbiler.RuleGate.Manifest"]="RuleGate Manifest"
  ["Fotbiler.RuleGate.AspNetCore"]="RuleGate ASP.NET Core"
  ["Fotbiler.RuleGate.Cli"]="RuleGate CLI"
  ["Fotbiler.RuleGate.Keycloak"]="RuleGate Keycloak Integration"
)

EXPECTED_PACKAGE_COUNT="${#PACKAGE_IDS[@]}"

read_property()
{
  local property_name="$1"

  sed -n \
    "s:.*<$property_name>\\(.*\\)</$property_name>.*:\\1:p" \
    Directory.Build.props |
    head -n 1
}

count_expected_test_runs()
{
  local expected_count=0
  local project
  local target_framework
  local target_frameworks
  local target_count
  local -a declared_frameworks

  while IFS= read -r project
  do
    target_frameworks="$(
      sed -n \
        's:.*<TargetFrameworks>\(.*\)</TargetFrameworks>.*:\1:p' \
        "$project" |
        head -n 1
    )"

    target_framework="$(
      sed -n \
        's:.*<TargetFramework>\(.*\)</TargetFramework>.*:\1:p' \
        "$project" |
        head -n 1
    )"

    if [[ "$target_frameworks" == '$(RuleGateTargetFrameworks)' ]]
    then
      target_count="$FRAMEWORK_COUNT"
    elif [[ -n "$target_frameworks" ]]
    then
      IFS=';' read -r -a declared_frameworks <<< "$target_frameworks"
      target_count="${#declared_frameworks[@]}"
    elif [[ -n "$target_framework" ]]
    then
      target_count=1
    else
      echo "ERROR: Test project does not declare a target framework: $project" >&2
      return 1
    fi

    expected_count=$((expected_count + target_count))
  done < <(
    find tests \
      -type f \
      -name '*.Tests.csproj' \
      -print |
      sort
  )

  printf '%s\n' "$expected_count"
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

if grep -RInE \
  --include='*.csproj' \
  '<Version(Prefix|Suffix)>' \
  src
then
  echo "ERROR: Package-specific versions are not allowed."
  echo "All RuleGate NuGet packages must inherit the central version."
  exit 1
fi

if grep -RInF \
  --include='README.md' \
  --include='*.csproj' \
  'Fotbiler RuleGate' \
  packaging/nuget \
  src
then
  echo "ERROR: NuGet package metadata and READMEs must use RuleGate as the product name."
  exit 1
fi

ACTUAL_VERSION_PREFIX="$(
  read_property VersionPrefix
)"

ACTUAL_VERSION_SUFFIX="$(
  read_property VersionSuffix
)"

ACTUAL_TARGET_FRAMEWORKS="$(
  read_property RuleGateTargetFrameworks
)"

if [[ "$ACTUAL_TARGET_FRAMEWORKS" != "$EXPECTED_FRAMEWORKS_VALUE" ]]
then
  echo "ERROR: Unexpected RuleGateTargetFrameworks."
  echo "Expected: $EXPECTED_FRAMEWORKS_VALUE"
  echo "Actual:   $ACTUAL_TARGET_FRAMEWORKS"
  exit 1
fi

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
printf 'Frameworks: %s\n' "$EXPECTED_FRAMEWORKS_VALUE"

printf '\n== Show .NET SDK ==\n'

dotnet --version

INSTALLED_RUNTIMES="$(
  dotnet --list-runtimes
)"

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  runtime_major="${framework#net}"
  runtime_major="${runtime_major%%.*}"

  assert_contains \
    "$INSTALLED_RUNTIMES" \
    "Microsoft.NETCore.App $runtime_major." \
    "Microsoft.NETCore.App runtime is missing for $framework."

  assert_contains \
    "$INSTALLED_RUNTIMES" \
    "Microsoft.AspNetCore.App $runtime_major." \
    "Microsoft.AspNetCore.App runtime is missing for $framework."
done

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

TRX_COUNT="$(
  find TestResults \
    -type f \
    -name '*.trx' |
    wc -l
)"

EXPECTED_TRX_COUNT="$(count_expected_test_runs)"

if [[ "$TRX_COUNT" -ne "$EXPECTED_TRX_COUNT" ]]
then
  echo "ERROR: Unexpected total TRX count."
  echo "Expected: $EXPECTED_TRX_COUNT"
  echo "Actual:   $TRX_COUNT"
  exit 1
fi

printf 'TRX files: %s\n' "$TRX_COUNT"

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

if [[ "$NUPKG_COUNT" -ne "$EXPECTED_PACKAGE_COUNT" ]]
then
  echo "ERROR: Expected exactly $EXPECTED_PACKAGE_COUNT nupkg files."
  exit 1
fi

if [[ "$SNUPKG_COUNT" -ne "$EXPECTED_PACKAGE_COUNT" ]]
then
  echo "ERROR: Expected exactly $EXPECTED_PACKAGE_COUNT snupkg files."
  exit 1
fi

printf '\n== Verify package contents and metadata ==\n'

for package_id in "${PACKAGE_IDS[@]}"
do
  package_version="$EXPECTED_VERSION"

  package_path="$PACKAGE_DIRECTORY/$package_id.$package_version.nupkg"
  symbol_path="$PACKAGE_DIRECTORY/$package_id.$package_version.snupkg"

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

  for framework in "${EXPECTED_FRAMEWORKS[@]}"
  do
    if [[ "$package_id" == "Fotbiler.RuleGate.Cli" ]]
    then
      assert_contains \
        "$package_files" \
        "tools/$framework/any/DotnetToolSettings.xml" \
        "$package_id does not contain its $framework tool settings."
      assert_contains \
        "$package_files" \
        "tools/$framework/any/Fotbiler.RuleGate.Cli.dll" \
        "$package_id does not contain its $framework CLI assembly."
      assert_contains \
        "$package_files" \
        "tools/$framework/any/Fotbiler.RuleGate.Manifest.dll" \
        "$package_id does not contain its $framework manifest dependency."
    else
      assert_contains \
        "$package_files" \
        "lib/$framework/$package_id.dll" \
        "$package_id does not contain its $framework assembly."
    fi
  done

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
    "<version>$package_version</version>" \
    "$package_id has an unexpected package version."

  assert_contains \
    "$nuspec_content" \
    "<title>${EXPECTED_PACKAGE_TITLES[$package_id]}</title>" \
    "$package_id has an unexpected package title."

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

  for framework in "${EXPECTED_FRAMEWORKS[@]}"
  do
    if [[ "$package_id" == "Fotbiler.RuleGate.Cli" ]]
    then
      assert_contains \
        "$symbol_files" \
        "tools/$framework/any/Fotbiler.RuleGate.Cli.pdb" \
        "$package_id symbol package does not contain its $framework portable PDB."
    else
      assert_contains \
        "$symbol_files" \
        "lib/$framework/$package_id.pdb" \
        "$package_id symbol package does not contain its $framework portable PDB."
    fi
  done

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

    Fotbiler.RuleGate.AspNetCore)
      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.Abstractions\" version=\"$EXPECTED_VERSION\"" \
        "AspNetCore does not depend on the expected Abstractions version."

      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.Core\" version=\"$EXPECTED_VERSION\"" \
        "AspNetCore does not depend on the expected Core version."

      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.Manifest\" version=\"$EXPECTED_VERSION\"" \
        "AspNetCore does not depend on the expected Manifest version."

      assert_contains \
        "$nuspec_content" \
        "<frameworkReference name=\"Microsoft.AspNetCore.App\"" \
        "AspNetCore does not reference Microsoft.AspNetCore.App."

      assert_contains \
        "$nuspec_content" \
        "<readme>README.md</readme>" \
        "AspNetCore does not declare its package README."

      assert_contains \
        "$package_files" \
        "README.md" \
        "AspNetCore package does not contain README.md."
      ;;

    Fotbiler.RuleGate.Cli)
      assert_contains \
        "$nuspec_content" \
        "<packageType name=\"DotnetTool\"" \
        "CLI package is not declared as a .NET tool."

      for framework in "${EXPECTED_FRAMEWORKS[@]}"
      do
        assert_contains \
          "$package_files" \
          "tools/$framework/any/System.CommandLine.dll" \
          "CLI package does not contain System.CommandLine for $framework."
        assert_contains \
          "$package_files" \
          "tools/$framework/any/YamlDotNet.dll" \
          "CLI package does not contain YamlDotNet for $framework."
      done
      ;;

    Fotbiler.RuleGate.Keycloak)
      assert_contains \
        "$nuspec_content" \
        "<dependency id=\"Fotbiler.RuleGate.AspNetCore\" version=\"$EXPECTED_VERSION\"" \
        "Keycloak does not depend on the expected AspNetCore version."

      assert_contains \
        "$nuspec_content" \
        "<frameworkReference name=\"Microsoft.AspNetCore.App\"" \
        "Keycloak does not reference Microsoft.AspNetCore.App."
      ;;
  esac

  echo "Verified: $package_id"
done

printf '\n== Run package consumer smoke test ==\n'

./scripts/test-nuget-consumer-smoke.sh \
  --packages-ready

printf '\n== Run Keycloak package consumer smoke test ==\n'

./scripts/test-keycloak-nuget-consumer-smoke.sh \
  --packages-ready

printf '\n== Run packaged CLI tool smoke test ==\n'

./scripts/test-cli-tool-smoke.sh \
  --packages-ready

printf '\n== Run generated C# compilation smoke test ==\n'

./scripts/test-generated-code-smoke.sh \
  --packages-ready

printf '\n== Verify normal CI does not publish ==\n'

if grep -RInE \
  'dotnet nuget push|NUGET_API_KEY|packages: write|contents: write|gh release create' \
  .github/workflows/ci.yml
then
  echo "ERROR: Release or publishing configuration exists in the normal CI workflow."
  exit 1
fi

echo "No release or publishing configuration found in the normal CI workflow."

printf '\n== Release verification succeeded ==\n'

printf 'Version: %s\n' "$EXPECTED_VERSION"
printf 'Commit:  %s\n' "$HEAD_COMMIT"
printf 'Packages: %s nupkg + %s snupkg\n' \
  "$EXPECTED_PACKAGE_COUNT" \
  "$EXPECTED_PACKAGE_COUNT"
printf 'Frameworks: %s\n' "$EXPECTED_FRAMEWORKS_VALUE"
printf 'Tests:    completed successfully\n'
printf 'Consumer: completed successfully\n'
printf 'CLI tool: completed successfully\n'
