#!/usr/bin/env bash
# Run the pure-rule tests outside Unity: tools/cstest/run.sh [--filter Name]
set -euo pipefail
cd "$(dirname "$0")"
dotnet test cstest.csproj --nologo -v q "$@"
