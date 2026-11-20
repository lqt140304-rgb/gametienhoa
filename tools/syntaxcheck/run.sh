#!/usr/bin/env bash
# Syntax check of the C# files changed since a git ref (default: the first project commit's parent is not needed; pass a ref), for when Unity is not available.
# Usage: tools/syntaxcheck/run.sh [base-ref]      e.g. tools/syntaxcheck/run.sh origin/claude/youthful-mendel-yw0yza
# SYNTAX lines are real errors (Unity would refuse to compile). SEMANTIC lines are mostly noise: Unity's own types are missing here,
# so only read the ones that name a project type or a wrong argument count/member (CS1501, CS0117).
set -euo pipefail
cd "$(dirname "$0")/../.."
base="${1:-origin/claude/youthful-mendel-yw0yza}"
list=$(mktemp); trap 'rm -f "$list"' EXIT
git diff --name-only "$base" HEAD -- 'Assets/_Project/*.cs' > "$list"
cd tools/syntaxcheck && dotnet run -c Release -- "$(cd ../.. && pwd)" "$list" | grep -E "^SYNTAX|^syntax errors|CS1501|CS0117"
