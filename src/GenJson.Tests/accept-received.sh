#!/bin/bash

# Get the absolute path to the directory containing this script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Use the SCRIPT_DIR variable to target the files
mv "$SCRIPT_DIR/TestsSourceGenerator.Generated.received.txt" "$SCRIPT_DIR/TestsSourceGenerator.Generated.verified.txt"