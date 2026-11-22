#!/usr/bin/env bash
# Printed into every Claude Code session when it starts (see .claude/settings.json): the project board and what the other sessions pushed lately,
# so each session knows what the others are doing and stays off their files. Read-only: it fetches but never merges or changes the working tree.
cd "$(git rev-parse --show-toplevel 2>/dev/null)" 2>/dev/null || exit 0
echo "=== BẢNG VIỆC CHUNG (docs/PROJECT_BOARD.md) ==="
sed -n 1,80p docs/PROJECT_BOARD.md 2>/dev/null
timeout 10 git fetch -q origin claude/unity-test-maze 2>/dev/null
echo
echo "=== Commit của phiên khác trên origin/claude/unity-test-maze mà bạn CHƯA kéo về ==="
git log --oneline HEAD..origin/claude/unity-test-maze 2>/dev/null | head -20
echo
echo "=== File đổi trong 12 commit gần nhất (tránh sửa chồng) ==="
git log -12 --name-only --pretty=format:'-- %h %s' origin/claude/unity-test-maze 2>/dev/null | head -50
echo
echo "QUY TẮC: kéo (git pull --no-rebase origin claude/unity-test-maze) trước khi sửa; chỉ sửa file thuộc phần việc của bạn; xong thì sửa dòng của bạn trong bảng việc; chạy tools/cstest/run.sh trước khi push."
