#!/bin/bash
# Start PXL Clock development environment
# The simulator now includes the file watcher and config management.

cd "$(dirname "$0")"

# Run setup check
./build/setup-check.sh
if [ $? -ne 0 ]; then
    exit 1
fi

# Clean up on exit
cleanup() {
    echo ""
    echo "Stopping PXL Clock development environment..."
    kill 0 2>/dev/null
    wait 2>/dev/null
}
trap cleanup EXIT

echo ""
echo "Starting PXL Clock development environment..."
echo ""

dotnet tool restore

# Start the simulator (includes file watcher, config API, and web UI)
dotnet Pxl.Simulator --clock-repo "$(pwd)" &

# Wait for the simulator to be reachable (max 15 seconds)
echo "Waiting for simulator..."
SIMULATOR_READY=false
for i in {1..30}; do
    if curl -s --head http://127.0.0.1:5001 > /dev/null 2>&1; then
        SIMULATOR_READY=true
        echo "Simulator ready at http://127.0.0.1:5001"
        break
    fi
    sleep 0.5
done

if [ "$SIMULATOR_READY" = false ]; then
    echo ""
    echo "WARNING: Simulator did not start within 15 seconds."
    echo "  Try running: dotnet tool restore && dotnet Pxl.Simulator --clock-repo ."
    echo "  Or check: ./build/setup-check.sh"
    echo ""
fi

# Open browser (skip when PXL_USE_IDE_BROWSER is set, e.g. from VS Code task)
if [ "$SIMULATOR_READY" = true ] && [ "$PXL_USE_IDE_BROWSER" != "1" ]; then
    if command -v open &> /dev/null; then
        open http://127.0.0.1:5001
    elif command -v xdg-open &> /dev/null; then
        xdg-open http://127.0.0.1:5001
    elif command -v wslview &> /dev/null; then
        wslview http://127.0.0.1:5001
    fi
fi

echo ""
echo "Save any .cs file in the apps/ folder to start"
echo "Config and scripts are now managed in the browser at http://127.0.0.1:5001"
echo ""

# Wait for simulator process
wait
