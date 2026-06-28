#!/bin/bash
# Thin wrapper around the FAKE build project.
# Usage:
#   ./build.sh           – run the full pipeline (CheckFormat → Lint → Build → Test)
#   ./build.sh Format    – reformat code with Fantomas
#   ./build.sh <Target>  – run any named FAKE target
set -e
if [ -n "$1" ]; then
    exec dotnet run --project src/PNobre.NortonGuides.Build -- --target "$1"
else
    exec dotnet run --project src/PNobre.NortonGuides.Build
fi
