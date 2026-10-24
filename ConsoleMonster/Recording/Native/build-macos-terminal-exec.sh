#!/bin/sh
# Maintainer-only regeneration; ordinary .NET builds embed the checked-in executable.
set -eu
cd "$(dirname "$0")"
xcrun clang -Os -Wall -Wextra -Werror -arch arm64 -arch x86_64 \
    -mmacosx-version-min=11.0 \
    macos-terminal-exec.c -o macos-terminal-exec
