#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPO_ROOT"

mapfile -d '' SOURCE_FILES < <(
  find src \
    -type f \
    \( \
      -name '*.cs' \
      -o \
      -name '*.csproj' \
    \) \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -print0 |
    sort -z
)

if [[ "${#SOURCE_FILES[@]}" -eq 0 ]]
then
  echo 'ERROR: no production source files were found.'
  exit 1
fi

PATTERN='System\.Text\.RegularExpressions|RegexOptions|Regex[[:space:]]*\.|new[[:space:]]+Regex[[:space:]]*\('

if grep \
  -nH \
  -E \
  "$PATTERN" \
  "${SOURCE_FILES[@]}"
then
  echo
  echo 'ERROR: production Regex API usage was introduced.'
  echo 'RuleGate 1.0 has no production Regex evaluation surface.'
  echo 'Any future Regex support requires explicit input limits and timeouts.'
  exit 1
fi

echo 'Production Regex surface verified: no Regex API usage found.'
