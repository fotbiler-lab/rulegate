#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPO_ROOT"

VERSION="${1:-1.0.0-rc.1}"

PYTHON_SCRIPT="scripts/verify-nuget-payload-reproducibility.py"

PROJECTS=(
  "src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj"
  "src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj"
  "src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj"
  "src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj"
  "src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj"
  "src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj"
)

PACKAGE_IDS=(
  "Fotbiler.RuleGate.Abstractions"
  "Fotbiler.RuleGate.Core"
  "Fotbiler.RuleGate.Manifest"
  "Fotbiler.RuleGate.AspNetCore"
  "Fotbiler.RuleGate.Cli"
  "Fotbiler.RuleGate.Keycloak"
)

test -x "$PYTHON_SCRIPT"

TMP_DIR="$(mktemp -d)"

RUN_ONE="$TMP_DIR/run-one"
RUN_TWO="$TMP_DIR/run-two"

clean_production_outputs()
{
  find src \
    -type d \
    \( \
      -name bin \
      -o \
      -name obj \
    \) \
    -prune \
    -exec rm -rf {} +
}

cleanup()
{
  clean_production_outputs
  rm -rf "$TMP_DIR"
}

trap cleanup EXIT

restore_solution()
{
  dotnet restore \
    Fotbiler.RuleGate.slnx \
    --locked-mode
}

pack_all()
{
  local output="$1"

  mkdir -p "$output"

  for project in "${PROJECTS[@]}"
  do
    dotnet pack \
      "$project" \
      --configuration Release \
      --no-restore \
      -p:Version="$VERSION" \
      -p:ContinuousIntegrationBuild=true \
      -p:Deterministic=true \
      -p:DebugType=portable \
      --output "$output"
  done
}

python3 \
  "$PYTHON_SCRIPT" \
  --self-test

clean_production_outputs
restore_solution
pack_all "$RUN_ONE"

clean_production_outputs
restore_solution
pack_all "$RUN_TWO"

python3 \
  "$PYTHON_SCRIPT" \
  "$RUN_ONE" \
  "$RUN_TWO" \
  "$VERSION" \
  "${PACKAGE_IDS[@]}"

cleanup
trap - EXIT

if find src \
  -type d \
  \( \
    -name bin \
    -o \
    -name obj \
  \) \
  -print \
  -quit |
grep -q .
then
  echo 'Production build outputs remain after cleanup.' >&2
  exit 1
fi

echo 'NUGET_REPRODUCIBILITY_BUILD_STATE_CLEANED'
