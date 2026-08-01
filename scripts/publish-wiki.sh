#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPOSITORY_ROOT"

node scripts/verify-documentation.mjs
node scripts/build-wiki.mjs

WIKI_WORKTREE="$(mktemp --directory /tmp/rulegate-wiki-publish-XXXXXX)"

cleanup()
{
  rm -rf "$WIKI_WORKTREE"
}

trap cleanup EXIT

if ! git clone \
  "https://github.com/fotbiler-lab/rulegate.wiki.git" \
  "$WIKI_WORKTREE"
then
  cat >&2 <<'EOF'
The GitHub Wiki repository is not initialized yet.

Create the first page once at:
https://github.com/fotbiler-lab/rulegate/wiki/_new

After GitHub creates rulegate.wiki.git, run this command again. The generated
Home, sidebar, footer, and chapter pages will replace the bootstrap page.
EOF
  exit 1
fi

find "$WIKI_WORKTREE" \
  -mindepth 1 \
  -maxdepth 1 \
  -type f \
  -name '*.md' \
  -delete

cp "$REPOSITORY_ROOT"/artifacts/wiki/*.md "$WIKI_WORKTREE"/

git -C "$WIKI_WORKTREE" add --all

if git -C "$WIKI_WORKTREE" diff --cached --quiet
then
  echo 'GitHub Wiki is already synchronized.'
  exit 0
fi

git -C "$WIKI_WORKTREE" \
  -c user.name='RuleGate Documentation' \
  -c user.email='noreply@fotbiler.dev' \
  commit \
  -m 'docs: synchronize RuleGate guide'

git -C "$WIKI_WORKTREE" push origin HEAD:master

echo 'GitHub Wiki synchronization completed.'
