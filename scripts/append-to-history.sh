#!/bin/bash
# Script to append work to HISTORY.md in standardized format
# Usage: bash scripts/append-to-history.sh "Stage Title" "What was changed" "Why it was changed" "Impact on codebase"

set -e

HISTORY_FILE="docs/HISTORY.md"
DATE=$(date +%Y-%m-%d)

# Check if HISTORY.md exists
if [ ! -f "$HISTORY_FILE" ]; then
    echo "Error: $HISTORY_FILE not found"
    exit 1
fi

# Parse arguments
STAGE_TITLE="$1"
WHAT_CHANGED="$2"
WHY_CHANGED="$3"
IMPACT="$4"

if [ -z "$STAGE_TITLE" ]; then
    echo "Error: Stage title is required"
    echo "Usage: bash scripts/append-to-history.sh \"Stage Title\" \"What changed\" \"Why changed\" \"Impact\""
    exit 1
fi

# Create the new entry
ENTRY="
---

## $DATE - $STAGE_TITLE

### What was changed

$WHAT_CHANGED

### Why it was changed

$WHY_CHANGED

### Impact on the codebase

$IMPACT
"

# Append to HISTORY.md
echo "$ENTRY" >> "$HISTORY_FILE"

echo "✅ Successfully appended to $HISTORY_FILE"
echo "Entry: $STAGE_TITLE"
