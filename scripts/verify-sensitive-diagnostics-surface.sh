#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPO_ROOT"

VERSION="${1:-1.0.0}"

PYTHON_SCRIPT="scripts/verify-sensitive-diagnostics-surface.py"

CORE_TEST_PROJECT="tests/Fotbiler.RuleGate.Core.Tests/Fotbiler.RuleGate.Core.Tests.csproj"
ASPNET_TEST_PROJECT="tests/Fotbiler.RuleGate.AspNetCore.Tests/Fotbiler.RuleGate.AspNetCore.Tests.csproj"

test -x "$PYTHON_SCRIPT"
test -f "$CORE_TEST_PROJECT"
test -f "$ASPNET_TEST_PROJECT"

PYTHONDONTWRITEBYTECODE=1 \
  python3 \
    "$PYTHON_SCRIPT" \
    --self-test

PYTHONDONTWRITEBYTECODE=1 \
  python3 \
    "$PYTHON_SCRIPT" \
    --check

dotnet test \
  "$CORE_TEST_PROJECT" \
  --configuration Release \
  --no-restore \
  -p:Version="$VERSION" \
  --filter \
    'FullyQualifiedName~RuleGateTelemetryTests'

dotnet test \
  "$ASPNET_TEST_PROJECT" \
  --configuration Release \
  --no-restore \
  -p:Version="$VERSION" \
  --filter \
    'FullyQualifiedName~RuleGateLoggingDiagnosticsTests'

echo
echo 'SENSITIVE_DIAGNOSTICS_REGRESSION_GATE_PASSED'
