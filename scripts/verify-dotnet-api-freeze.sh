#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &&
    pwd
)"

REPOSITORY_ROOT="$(
  cd -- "$SCRIPT_DIR/.." &&
    pwd
)"

cd "$REPOSITORY_ROOT"

SOLUTION="Fotbiler.RuleGate.slnx"

BASELINE_VERSION="1.0.0-rc.1"
BASELINE_ROOT="api-baselines/dotnet/$BASELINE_VERSION"

TOOL_PROJECT="tools/Fotbiler.RuleGate.ApiSnapshot/Fotbiler.RuleGate.ApiSnapshot.csproj"
TOOL_DLL="tools/Fotbiler.RuleGate.ApiSnapshot/bin/Release/net10.0/Fotbiler.RuleGate.ApiSnapshot.dll"

UPDATE=false
NO_BUILD=false

for argument in "$@"
do
  case "$argument" in
    --update)
      UPDATE=true
      ;;

    --no-build)
      NO_BUILD=true
      ;;

    *)
      echo "ERROR: Unknown argument: $argument" >&2
      echo "Usage: $0 [--update] [--no-build]" >&2
      exit 2
      ;;
  esac
done

if [[ "$NO_BUILD" == false ]]
then
  echo "Building RuleGate Release outputs..."

  dotnet build \
    "$SOLUTION" \
    --configuration Release
fi

if [[ ! -f "$TOOL_DLL" ]]
then
  echo "ERROR: API snapshot tool is not built:" >&2
  echo "$TOOL_DLL" >&2
  echo "Build the Release solution or omit --no-build." >&2
  exit 3
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

MATRIX="$TMP_DIR/matrix.txt"

cat >"$MATRIX" <<'TXT'
Fotbiler.RuleGate.Abstractions|src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj|netstandard2.0
Fotbiler.RuleGate.Abstractions|src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj|net8.0
Fotbiler.RuleGate.Abstractions|src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj|net9.0
Fotbiler.RuleGate.Abstractions|src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj|net10.0
Fotbiler.RuleGate.Core|src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj|netstandard2.0
Fotbiler.RuleGate.Core|src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj|net8.0
Fotbiler.RuleGate.Core|src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj|net9.0
Fotbiler.RuleGate.Core|src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj|net10.0
Fotbiler.RuleGate.Manifest|src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj|netstandard2.0
Fotbiler.RuleGate.Manifest|src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj|net8.0
Fotbiler.RuleGate.Manifest|src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj|net9.0
Fotbiler.RuleGate.Manifest|src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj|net10.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|netcoreapp3.1
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net5.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net6.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net7.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net8.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net9.0
Fotbiler.RuleGate.AspNetCore|src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj|net10.0
Fotbiler.RuleGate.Cli|src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj|net8.0
Fotbiler.RuleGate.Cli|src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj|net9.0
Fotbiler.RuleGate.Cli|src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj|net10.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|netcoreapp3.1
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net5.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net6.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net7.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net8.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net9.0
Fotbiler.RuleGate.Keycloak|src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj|net10.0
TXT

if [[ "$(wc -l <"$MATRIX")" -ne 29 ]]
then
  echo "ERROR: .NET API matrix must contain exactly 29 surfaces." >&2
  exit 4
fi

REFERENCE_TARGETS="$TMP_DIR/ReferencePathDump.targets"
REFERENCE_ROOT="$TMP_DIR/references"
ACTUAL_ROOT="$TMP_DIR/actual"

mkdir -p \
  "$REFERENCE_ROOT" \
  "$ACTUAL_ROOT"

cat >"$REFERENCE_TARGETS" <<'XML'
<Project>

  <Target
    Name="DumpRuleGateReferencePath"
    DependsOnTargets="ResolveReferences">

    <WriteLinesToFile
      File="$(RuleGateReferenceDump)"
      Lines="@(ReferencePath->'%(FullPath)')"
      Overwrite="true" />

  </Target>

</Project>
XML

echo "Exporting .NET public API surfaces..."

while IFS='|' read -r package project tfm
do
  assembly="src/$package/bin/Release/$tfm/$package.dll"
  references="$REFERENCE_ROOT/$package/$tfm.txt"
  actual="$ACTUAL_ROOT/$package/$tfm.api.txt"

  if [[ ! -f "$assembly" ]]
  then
    echo "ERROR: Release assembly is missing:" >&2
    echo "$assembly" >&2
    exit 5
  fi

  mkdir -p \
    "$(dirname "$references")" \
    "$(dirname "$actual")"

  dotnet msbuild \
    "$project" \
    -nologo \
    -verbosity:quiet \
    -p:Configuration=Release \
    -p:TargetFramework="$tfm" \
    -p:CustomAfterMicrosoftCommonTargets="$REFERENCE_TARGETS" \
    -p:RuleGateReferenceDump="$references" \
    -t:DumpRuleGateReferencePath

  if [[ ! -s "$references" ]]
  then
    echo "ERROR: ReferencePath is empty:" >&2
    echo "$package / $tfm" >&2
    exit 6
  fi

  sort -u \
    "$references" \
    -o "$references"

  if grep -q \
    '/bin/Debug/' \
    "$references"
  then
    echo "ERROR: Debug reference leaked into Release API graph:" >&2
    echo "$package / $tfm" >&2

    grep \
      '/bin/Debug/' \
      "$references" \
      >&2

    exit 7
  fi

  while IFS= read -r reference
  do
    if [[ ! -f "$reference" ]]
    then
      echo "ERROR: resolved reference does not exist:" >&2
      echo "$reference" >&2
      exit 8
    fi
  done <"$references"

  dotnet "$TOOL_DLL" \
    "$assembly" \
    "$references" \
    >"$actual"

  if [[ ! -s "$actual" ]]
  then
    echo "ERROR: API snapshot is empty:" >&2
    echo "$package / $tfm" >&2
    exit 9
  fi

  printf 'Exported %-38s %s\n' \
    "$package" \
    "$tfm"

done <"$MATRIX"

ACTUAL_COUNT="$(
  find "$ACTUAL_ROOT" \
    -type f \
    -name '*.api.txt' |
    wc -l
)"

if [[ "$ACTUAL_COUNT" -ne 29 ]]
then
  echo "ERROR: Expected 29 generated API snapshots, found $ACTUAL_COUNT." >&2
  exit 10
fi

for noise in \
  AsyncStateMachineAttribute \
  IteratorStateMachineAttribute \
  CompilerGeneratedAttribute \
  NullableAttribute \
  NullableContextAttribute \
  RefSafetyRulesAttribute \
  PreserveBaseOverridesAttribute
do
  if grep -R -q \
    "$noise" \
    "$ACTUAL_ROOT"
  then
    echo "ERROR: Compiler-only metadata leaked into API snapshot: $noise" >&2
    exit 11
  fi
done

if [[ "$UPDATE" == true ]]
then
  replacement="$TMP_DIR/replacement"

  mkdir -p "$replacement"

  cp -a \
    "$ACTUAL_ROOT"/. \
    "$replacement"/

  rm -rf "$BASELINE_ROOT"

  mkdir -p \
    "$(dirname "$BASELINE_ROOT")"

  mv \
    "$replacement" \
    "$BASELINE_ROOT"

  echo
  echo "Updated .NET API approval baseline:"
  echo "$BASELINE_ROOT"
  echo "Review the generated diff before committing."

  exit 0
fi

if [[ ! -d "$BASELINE_ROOT" ]]
then
  echo "ERROR: .NET API baseline directory is missing:" >&2
  echo "$BASELINE_ROOT" >&2
  exit 12
fi

EXPECTED_FILES="$TMP_DIR/expected-files.txt"
BASELINE_FILES="$TMP_DIR/baseline-files.txt"

while IFS='|' read -r package project tfm
do
  printf '%s/%s.api.txt\n' \
    "$package" \
    "$tfm"
done <"$MATRIX" |
  sort \
  >"$EXPECTED_FILES"

find "$BASELINE_ROOT" \
  -type f \
  -name '*.api.txt' \
  -printf '%P\n' |
  sort \
  >"$BASELINE_FILES"

if ! cmp -s \
  "$EXPECTED_FILES" \
  "$BASELINE_FILES"
then
  echo "ERROR: .NET API baseline file set does not match the 29-surface matrix." >&2

  echo >&2
  echo "===== Expected =====" >&2
  cat "$EXPECTED_FILES" >&2

  echo >&2
  echo "===== Found =====" >&2
  cat "$BASELINE_FILES" >&2

  exit 13
fi

failed=false

while IFS='|' read -r package project tfm
do
  expected="$BASELINE_ROOT/$package/$tfm.api.txt"
  actual="$ACTUAL_ROOT/$package/$tfm.api.txt"

  if cmp -s \
    "$expected" \
    "$actual"
  then
    printf 'API unchanged %-38s %s\n' \
      "$package" \
      "$tfm"

    continue
  fi

  failed=true

  echo >&2
  echo "ERROR: .NET public API changed: $package / $tfm" >&2
  echo "Baseline: $expected" >&2
  echo >&2

  diff -u \
    "$expected" \
    "$actual" \
    | sed -n '1,200p' \
    >&2 \
    || true

done <"$MATRIX"

if [[ "$failed" == true ]]
then
  echo >&2
  echo "ERROR: .NET public API freeze verification failed." >&2
  echo "Review the public contract before running:" >&2
  echo "  ./scripts/verify-dotnet-api-freeze.sh --update" >&2
  exit 14
fi

echo
echo ".NET public API freeze verified against $BASELINE_VERSION."
