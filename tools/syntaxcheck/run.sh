#!/usr/bin/env bash
# Syntax check of the C# files changed since a git ref (default: the first project commit's parent is not needed; pass a ref), for when Unity is not available.
# Usage: tools/syntaxcheck/run.sh [base-ref]      e.g. tools/syntaxcheck/run.sh origin/claude/youthful-mendel-yw0yza
# SYNTAX lines are real errors (Unity would refuse to compile). SEMANTIC lines are mostly noise: Unity's own types are missing here,
# so only the codes that point at real mistakes are shown: wrong argument count/member (CS1501, CS0117), null with a struct (CS0037, CS0019 with <null>).
# For everything else run: cd tools/syntaxcheck && dotnet run -c Release -- <repo> <list-of-files> and read the rare codes (CS0019, CS0165, CS1503 naming a project type); CS0246/CS0103 on Unity names are noise.
set -euo pipefail
cd "$(dirname "$0")/../.."
base="${1:-origin/claude/youthful-mendel-yw0yza}"
list=$(mktemp); trap 'rm -f "$list"' EXIT
git diff --name-only "$base" HEAD -- 'Assets/_Project/*.cs' > "$list"
cd tools/syntaxcheck && dotnet run -c Release -- "$(cd ../.. && pwd)" "$list" | grep -E "^SYNTAX|^syntax errors|CS1501|CS0117|CS0037|CS0019.*<null>"
