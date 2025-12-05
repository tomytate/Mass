#!/bin/bash
set -e

echo -e "\033[0;36m🚀 Starting Mass Suite Developer Bootstrap...\033[0m"

# 1. Check Prerequisites
echo -e "\n[1/5] Checking prerequisites..."
if ! command -v dotnet &> /dev/null; then
    echo "dotnet SDK is required but not found."
    exit 1
fi
echo -e "  - dotnet SDK: $(dotnet --version)"

if ! command -v git &> /dev/null; then
    echo "git is required but not found."
    exit 1
fi
echo -e "  - git: Found"

# 2. Restore Solution
echo -e "\n[2/5] Restoring solution..."
dotnet restore Mass.sln
echo -e "  - Restore complete"

# 3. Setup Local NuGet Feed
echo -e "\n[3/5] Setting up local NuGet feed..."
FEED_PATH="../../.nupkg-feed"
mkdir -p "$FEED_PATH"
echo -e "  - Feed path: $FEED_PATH"

# Pack Mass.Spec
echo -e "  - Packing Mass.Spec..."
dotnet pack src/Mass.Spec/Mass.Spec.csproj -o "$FEED_PATH" -c Release
echo -e "  - Mass.Spec packed"

# 4. Build Solution
echo -e "\n[4/5] Building solution..."
dotnet build Mass.sln -c Debug
echo -e "  - Build complete"

# 5. Setup Dev Data
echo -e "\n[5/5] Setting up dev data..."
DEV_DATA_PATH="../../dev-data"
mkdir -p "$DEV_DATA_PATH"

# Create sample settings
SETTINGS_PATH="$DEV_DATA_PATH/settings.json"
if [ ! -f "$SETTINGS_PATH" ]; then
    echo '{
        "Logging": { "Level": "Debug" },
        "Telemetry": { "Enabled": false }
    }' > "$SETTINGS_PATH"
    echo -e "  - Created sample settings.json"
fi

echo -e "\n\033[0;36m✅ Bootstrap Complete! You are ready to code.\033[0m"
echo "Run 'dotnet run --project src/Mass.Launcher' to start the app."
