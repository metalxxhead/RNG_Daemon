#!/bin/bash

# Resolve script and project root directories
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( dirname "$SCRIPT_DIR" )"

cd "$ROOT_DIR"

EXE="$ROOT_DIR/build/rng.exe"

if [ ! -f "$EXE" ]; then
    echo "Executable not found: $EXE"
    echo "Did you run: bash scripts/build.sh ?"
    exit 1
fi

echo "Running application..."
mono "$EXE" "$@"

