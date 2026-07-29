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

MINIMAL_PROJECT="$REPOSITORY_ROOT/samples/aspnetcore-minimal/RuleGate.MinimalApi.Sample.csproj"
DOCUMENT_PROJECT="$REPOSITORY_ROOT/samples/document-approval/api/RuleGate.DocumentApproval.Api.csproj"
MINIMAL_URL="http://127.0.0.1:5098"
DOCUMENT_URL="http://127.0.0.1:5099"
SAMPLE_TEMP_DIRECTORY="$(mktemp -d)"
MINIMAL_PID=""
DOCUMENT_PID=""

cleanup() {
  if [[ -n "$MINIMAL_PID" ]]; then
    kill "$MINIMAL_PID" 2>/dev/null || true
  fi
  if [[ -n "$DOCUMENT_PID" ]]; then
    kill "$DOCUMENT_PID" 2>/dev/null || true
  fi
  rm -rf -- "$SAMPLE_TEMP_DIRECTORY"
}

trap cleanup EXIT

if [[ "${1:-}" != "--no-build" ]]; then
  dotnet build "$MINIMAL_PROJECT" --configuration Release
  dotnet build "$DOCUMENT_PROJECT" --configuration Release
fi

ASPNETCORE_URLS="$MINIMAL_URL" \
  dotnet run --project "$MINIMAL_PROJECT" --configuration Release --no-build --no-launch-profile \
  >"$SAMPLE_TEMP_DIRECTORY/minimal.log" 2>&1 &
MINIMAL_PID="$!"

for _ in {1..30}; do
  if curl --fail --silent "$MINIMAL_URL/" >/dev/null; then
    break
  fi
  sleep 1
done

curl --fail --silent \
  -H "X-Demo-User: sample-user" \
  -H "X-Demo-Permissions: DOC.READ" \
  "$MINIMAL_URL/documents/doc-1" >/dev/null

denied_status="$(
  curl --silent --output /dev/null --write-out "%{http_code}" \
    -H "X-Demo-User: sample-user" \
    "$MINIMAL_URL/documents/doc-1"
)"

if [[ "$denied_status" != "403" ]]; then
  echo "Expected the minimal sample to deny with 403; received $denied_status." >&2
  exit 1
fi

ASPNETCORE_URLS="$DOCUMENT_URL" \
ConnectionStrings__SampleDatabase="Data Source=$SAMPLE_TEMP_DIRECTORY/sample.db" \
  dotnet run --project "$DOCUMENT_PROJECT" --configuration Release --no-build --no-launch-profile \
  >"$SAMPLE_TEMP_DIRECTORY/document.log" 2>&1 &
DOCUMENT_PID="$!"

for _ in {1..30}; do
  if curl --fail --silent "$DOCUMENT_URL/api/health" >/dev/null; then
    echo "Reference application smoke tests passed."
    exit 0
  fi
  sleep 1
done

cat "$SAMPLE_TEMP_DIRECTORY/document.log" >&2
exit 1
