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

SOLUTION="$REPOSITORY_ROOT/Fotbiler.RuleGate.slnx"
PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"

EXPECTED_FRAMEWORKS=(
  "net8.0"
  "net9.0"
  "net10.0"
)

VERSION_PREFIX="$(
  sed -n \
    's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' \
    "$REPOSITORY_ROOT/Directory.Build.props" |
    head -n 1
)"

VERSION_SUFFIX="$(
  sed -n \
    's:.*<VersionSuffix>\(.*\)</VersionSuffix>.*:\1:p' \
    "$REPOSITORY_ROOT/Directory.Build.props" |
    head -n 1
)"

test -n "$VERSION_PREFIX"

if [[ -n "$VERSION_SUFFIX" ]]
then
  PACKAGE_VERSION="$VERSION_PREFIX-$VERSION_SUFFIX"
else
  PACKAGE_VERSION="$VERSION_PREFIX"
fi

PACKAGE_ID="Fotbiler.RuleGate.Cli"
PACKAGE_PATH="$PACKAGE_DIRECTORY/$PACKAGE_ID.$PACKAGE_VERSION.nupkg"

PACKAGES_READY="false"

case "${1:-}" in
  "")
    ;;

  --packages-ready)
    PACKAGES_READY="true"
    ;;

  *)
    echo "Usage: $0 [--packages-ready]"
    exit 2
    ;;
esac

TEMP_DIRECTORY="$(
  mktemp \
    --directory \
    /tmp/rulegate-generated-code-smoke-XXXXXX
)"

cleanup()
{
  rm -rf "$TEMP_DIRECTORY"
}

trap cleanup EXIT

export NUGET_PACKAGES="$TEMP_DIRECTORY/global-packages"
export DOTNET_CLI_HOME="$TEMP_DIRECTORY/dotnet-home"
export DOTNET_NOLOGO=true
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

mkdir -p \
  "$NUGET_PACKAGES" \
  "$DOTNET_CLI_HOME"

if [[ "$PACKAGES_READY" == "false" ]]
then
  printf '\n== Restore solution ==\n'

  dotnet restore \
    "$SOLUTION"

  printf '\n== Build solution ==\n'

  dotnet build \
    "$SOLUTION" \
    --configuration Release \
    --no-restore

  printf '\n== Test solution ==\n'

  dotnet test \
    "$SOLUTION" \
    --configuration Release \
    --no-build

  printf '\n== Pack solution ==\n'

  rm -rf "$PACKAGE_DIRECTORY"

  dotnet pack \
    "$SOLUTION" \
    --configuration Release \
    --no-build
fi

printf '\n== Verify generated-code smoke package ==\n'

test -f "$PACKAGE_PATH"

printf 'Package: %s\n' \
  "$(basename "$PACKAGE_PATH")"

MANIFEST_PATH="$TEMP_DIRECTORY/rulegate.yaml"

cat >"$MANIFEST_PATH" <<'YAML'
schemaVersion: 1

application:
  id: generated-code-smoke
  name: Generated Code Smoke

policies:
  - id: orders.update
    resourceType: order
    action: update
    requirement:
      permission: ORDERS.UPDATE

  - id: documents.read
    resourceType: document
    action: read
    requirement:
      permission: DOCUMENTS.READ
YAML

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  printf '\n========================================\n'
  printf 'GENERATED CODE FRAMEWORK: %s\n' "$framework"
  printf '========================================\n'

  TOOL_DIRECTORY="$TEMP_DIRECTORY/tool-$framework"
  CONSUMER_DIRECTORY="$TEMP_DIRECTORY/consumer-$framework"
  GENERATED_DIRECTORY="$CONSUMER_DIRECTORY/Generated"

  mkdir -p \
    "$TOOL_DIRECTORY" \
    "$GENERATED_DIRECTORY"

  printf '\n== Install exact packaged CLI ==\n'

  dotnet tool install \
    "$PACKAGE_ID" \
    --tool-path "$TOOL_DIRECTORY" \
    --version "$PACKAGE_VERSION" \
    --source "$PACKAGE_DIRECTORY" \
    --framework "$framework" \
    --no-cache

  CLI="$TOOL_DIRECTORY/rulegate"

  test -x "$CLI"

  printf '\n== Verify generation help ==\n'

  "$CLI" \
    generate \
    csharp \
    --help \
    >"$TEMP_DIRECTORY/generate-help-$framework.out" \
    2>"$TEMP_DIRECTORY/generate-help-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/generate-help-$framework.err"

  grep -F \
    -- '--namespace' \
    "$TEMP_DIRECTORY/generate-help-$framework.out" \
    >/dev/null

  grep -F \
    -- '--output' \
    "$TEMP_DIRECTORY/generate-help-$framework.out" \
    >/dev/null

  grep -F \
    -- '--check' \
    "$TEMP_DIRECTORY/generate-help-$framework.out" \
    >/dev/null

  GENERATED_PATH="$GENERATED_DIRECTORY/RuleGate.g.cs"

  printf '\n== Generate C# source ==\n'

  "$CLI" \
    generate \
    csharp \
    "$MANIFEST_PATH" \
    --namespace RuleGate.Generated \
    --output "$GENERATED_PATH" \
    >"$TEMP_DIRECTORY/generate-$framework.out" \
    2>"$TEMP_DIRECTORY/generate-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/generate-$framework.err"

  test -s "$GENERATED_PATH"

  grep -F \
    'RuleGate C# source generated.' \
    "$TEMP_DIRECTORY/generate-$framework.out" \
    >/dev/null

  grep -F \
    'public const string DocumentsRead = "documents.read";' \
    "$GENERATED_PATH" \
    >/dev/null

  grep -F \
    'public const string OrdersUpdate = "orders.update";' \
    "$GENERATED_PATH" \
    >/dev/null

  python3 - \
    "$GENERATED_PATH" <<'PY'
from pathlib import Path
import sys

path = Path(sys.argv[1])
content = path.read_bytes()

assert content
assert not content.startswith(b"\xef\xbb\xbf")
assert b"\r" not in content
assert content.endswith(b"\n")
PY

  GENERATED_HASH="$(
    sha256sum "$GENERATED_PATH" |
      awk '{ print $1 }'
  )"

  printf '\n== Verify generated output is current ==\n'

  "$CLI" \
    generate \
    csharp \
    "$MANIFEST_PATH" \
    --namespace RuleGate.Generated \
    --output "$GENERATED_PATH" \
    --check \
    >"$TEMP_DIRECTORY/check-current-$framework.out" \
    2>"$TEMP_DIRECTORY/check-current-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/check-current-$framework.err"

  grep -F \
    'RuleGate C# output is current.' \
    "$TEMP_DIRECTORY/check-current-$framework.out" \
    >/dev/null

  test "$GENERATED_HASH" = "$(
    sha256sum "$GENERATED_PATH" |
      awk '{ print $1 }'
  )"

  printf '\n== Create generated-code consumer ==\n'

  cat >"$CONSUMER_DIRECTORY/GeneratedCodeSmoke.csproj" <<EOF_PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$framework</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
EOF_PROJECT

  cat >"$CONSUMER_DIRECTORY/Program.cs" <<'CS'
using RuleGate.Generated;

Console.WriteLine(
    $"{RuleGatePolicies.DocumentsRead}|"
    + $"{RuleGateResourceTypes.Document}|"
    + $"{RuleGateActions.Read}");
CS

  printf '\n== Restore generated-code consumer ==\n'

  dotnet restore \
    "$CONSUMER_DIRECTORY/GeneratedCodeSmoke.csproj" \
    --force \
    --no-cache

  printf '\n== Compile generated C# source ==\n'

  dotnet build \
    "$CONSUMER_DIRECTORY/GeneratedCodeSmoke.csproj" \
    --configuration Release \
    --framework "$framework" \
    --no-restore

  printf '\n== Run generated-code consumer ==\n'

  dotnet run \
    --project "$CONSUMER_DIRECTORY/GeneratedCodeSmoke.csproj" \
    --configuration Release \
    --framework "$framework" \
    --no-build \
    --no-restore \
    >"$TEMP_DIRECTORY/consumer-$framework.out"

  grep -Fx \
    'documents.read|document|read' \
    "$TEMP_DIRECTORY/consumer-$framework.out" \
    >/dev/null

  printf '\n== Verify stale output fails without mutation ==\n'

  printf '// stale\n' \
    >>"$GENERATED_PATH"

  STALE_HASH="$(
    sha256sum "$GENERATED_PATH" |
      awk '{ print $1 }'
  )"

  set +e

  "$CLI" \
    generate \
    csharp \
    "$MANIFEST_PATH" \
    --namespace RuleGate.Generated \
    --output "$GENERATED_PATH" \
    --check \
    >"$TEMP_DIRECTORY/check-stale-$framework.out" \
    2>"$TEMP_DIRECTORY/check-stale-$framework.err"

  STALE_EXIT_CODE="$?"

  set -e

  test "$STALE_EXIT_CODE" -eq 1
  test ! -s \
    "$TEMP_DIRECTORY/check-stale-$framework.out"

  grep -F \
    'RuleGate C# output is stale.' \
    "$TEMP_DIRECTORY/check-stale-$framework.err" \
    >/dev/null

  test "$STALE_HASH" = "$(
    sha256sum "$GENERATED_PATH" |
      awk '{ print $1 }'
  )"

  printf '\n== Regenerate and recheck source ==\n'

  "$CLI" \
    generate \
    csharp \
    "$MANIFEST_PATH" \
    --namespace RuleGate.Generated \
    --output "$GENERATED_PATH" \
    >"$TEMP_DIRECTORY/regenerate-$framework.out" \
    2>"$TEMP_DIRECTORY/regenerate-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/regenerate-$framework.err"

  test "$GENERATED_HASH" = "$(
    sha256sum "$GENERATED_PATH" |
      awk '{ print $1 }'
  )"

  "$CLI" \
    generate \
    csharp \
    "$MANIFEST_PATH" \
    --namespace RuleGate.Generated \
    --output "$GENERATED_PATH" \
    --check \
    >"$TEMP_DIRECTORY/recheck-$framework.out" \
    2>"$TEMP_DIRECTORY/recheck-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/recheck-$framework.err"

  printf '\nGenerated C# compilation smoke passed for %s.\n' \
    "$framework"
done

printf '\nGenerated RuleGate C# compiled and ran successfully on all frameworks.\n'
