#!/bin/bash

# Always resolve script directory correctly
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( dirname "$SCRIPT_DIR" )"

cd "$ROOT_DIR"

echo "=== Building project using mcs ==="
echo "Project root: $ROOT_DIR"

# ----------------------------------------
# 1. Ensure build directory exists at ROOT/build
# ----------------------------------------
mkdir -p "$ROOT_DIR/build"

# ----------------------------------------
# 2. Collect all .cs source files
# ----------------------------------------
CS_FILES=$(find "$ROOT_DIR/src" -type f -name '*.cs')

if [ -z "$CS_FILES" ]; then
    echo "No .cs files found under src/. Nothing to compile."
    exit 1
fi

# ----------------------------------------
# 3. Module Detection
# ----------------------------------------
REFERENCES=""
echo ""
echo "Detecting optional modules..."

# ----- JSON Module -----
if [ -d "$ROOT_DIR/src/modules/json" ]; then
    echo " - JSON module: present"

    DLL="$ROOT_DIR/lib/Newtonsoft.Json.dll"
    if [ -f "$DLL" ]; then
        REFERENCES="$REFERENCES -r:$DLL"
    else
        echo "   WARNING: JSON module present but newtonsoft.json.dll missing!"
    fi
else
    echo " - JSON module: not present"
fi

# ----- SQLite Module -----
if [ -d "$ROOT_DIR/src/modules/sqlite" ]; then
    echo " - SQLite module: present"
    REFERENCES="$REFERENCES -r:Mono.Data.Sqlite.dll -r:System.Data.dll"
else
    echo " - SQLite module: not present"
fi

echo ""
echo "Compiling..."
echo "mcs -optimize+ -out:build/rng.exe $REFERENCES ..."

# ----------------------------------------
# 4. Actual Build Command
# ----------------------------------------
mcs -optimize+ -out:"$ROOT_DIR/build/rng.exe" $CS_FILES $REFERENCES

STATUS=$?

# ----------------------------------------
# 5. Success/Failure Report
# ----------------------------------------
if [ $STATUS -eq 0 ]; then
    echo ""
    echo "Build successful!"
    echo "Executable: build/rng.exe"
    echo "Run with:"
    echo "  bash scripts/run.sh"
else
    echo ""
    echo "Build FAILED with code $STATUS"
fi

