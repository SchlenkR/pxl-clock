#!/bin/bash
# PXL Tool - Publish apps & configure devices
#
# Usage:
#   ./pxl-tool.sh                          # uses NuGet tool
#   ./pxl-tool.sh --project <path.fsproj>  # uses local project

cd "$(dirname "$0")"

if [ "$1" = "--project" ] && [ -n "$2" ]; then
    dotnet run --project "$2" -- --clock-repo .
else
    dotnet tool restore
    dotnet Pxl.Tool --clock-repo .
fi
