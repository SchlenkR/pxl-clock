#!/bin/bash
# sync-up.sh — pxl-clock is now the SOURCE of truth for app files.
# This pushes apps from pxl-clock → pxl-software, transforming the
# package reference back to the project reference used during local dev:
#
#   #:package Pxl@*   →   #:project ../../Pxl.Ui.CSharp
#
# Default behaviour (no args): syncs the three managed subdirs
#   apps/clockFaces, apps/demos, apps/llm-demos
# from pxl-clock to pxl-software/src/apps/...
#
# With an argument, syncs just that file or directory:
#   ./build/sync-up.sh apps/clockFaces/Foo.cs
#   ./build/sync-up.sh apps/clockFaces
#
# Always shows a diff against the target before writing.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CLOCK_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="$(cd "$CLOCK_ROOT/.." && pwd)"
SOFTWARE_ROOT="$WORKSPACE_ROOT/pxl-software"

if [ ! -d "$SOFTWARE_ROOT" ]; then
    echo "ERROR: pxl-software not found at $SOFTWARE_ROOT"
    exit 1
fi

# ---- Build the list of files to sync ---------------------------------------
FILES=()
if [ $# -eq 0 ]; then
    # No arg → sync all managed subdirs
    for SUB in clockFaces demos llm-demos; do
        DIR="$CLOCK_ROOT/apps/$SUB"
        [ -d "$DIR" ] || continue
        while IFS= read -r -d '' FILE; do
            FILES+=("${FILE#$CLOCK_ROOT/}")
        done < <(find "$DIR" -name "*.cs" -print0)
    done
else
    REL_INPUT="$1"
    SRC_PATH="$CLOCK_ROOT/$REL_INPUT"
    if [ ! -e "$SRC_PATH" ]; then
        echo "ERROR: source not found: $SRC_PATH"
        exit 1
    fi
    if [ -f "$SRC_PATH" ]; then
        FILES+=("$REL_INPUT")
    else
        while IFS= read -r -d '' FILE; do
            FILES+=("${FILE#$CLOCK_ROOT/}")
        done < <(find "$SRC_PATH" -name "*.cs" -print0)
    fi
fi

if [ ${#FILES[@]} -eq 0 ]; then
    echo "No .cs files found."
    exit 0
fi

echo "Syncing ${#FILES[@]} file(s) UP: pxl-clock → pxl-software"
echo ""

WROTE=0
for REL in "${FILES[@]}"; do
    SRC="$CLOCK_ROOT/$REL"
    # apps/...  →  src/apps/...
    SOFT_REL="${REL/#apps\//src/apps/}"
    DEST="$SOFTWARE_ROOT/$SOFT_REL"

    # Transform on the fly
    TMP=$(mktemp)
    sed 's|^#:package Pxl@.*|#:project ../../Pxl.Ui.CSharp|' "$SRC" > "$TMP"

    if [ -f "$DEST" ] && cmp -s "$TMP" "$DEST"; then
        echo "OK:   $REL (no change)"
        rm -f "$TMP"
        continue
    fi

    if [ -f "$DEST" ]; then
        echo "DIFF: $REL"
        diff -u "$DEST" "$TMP" | sed 's/^/      /' | head -40 || true
    else
        echo "NEW:  $REL  →  $SOFT_REL"
    fi

    mkdir -p "$(dirname "$DEST")"
    mv "$TMP" "$DEST"
    WROTE=$((WROTE + 1))
done

echo ""
echo "Wrote $WROTE file(s)."
echo "Review with: git -C $SOFTWARE_ROOT diff"
