#!/bin/bash
# Publish a PXL Clock app to a device using Pxl.Compiler.Cli

cd "$(dirname "$0")"

dotnet tool restore
dotnet Pxl.Compiler.Cli --clock-repo .
