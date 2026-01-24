#!/usr/bin/env bash

# =========================================
# NUKE Build Script for Linux/macOS
# =========================================

set -eo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BUILD_PROJECT="$SCRIPT_DIR/build/_build.csproj"
TEMP_DIR="$SCRIPT_DIR/.nuke/temp"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    exit 1
fi

# Check if Nuke.GlobalTool is installed
if ! dotnet tool list --global | grep -q "nuke.globaltool"; then
    echo "Installing Nuke.GlobalTool..."
    dotnet tool install Nuke.GlobalTool --global
fi

# Create temp directory if it doesn't exist
mkdir -p "$TEMP_DIR"

# Run Nuke with all passed arguments
dotnet run --project "$BUILD_PROJECT" --no-launch-profile -- "$@"
