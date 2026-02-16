#!/bin/bash
# Start Simulator and C# Watcher concurrently

cd "$(dirname "$0")"

# Run setup check (only on first start, skip on .env restart)
if [ "$PXL_RESTART" != "1" ]; then
    ./build/setup-check.sh
    if [ $? -ne 0 ]; then
        exit 1
    fi
fi

# Clean up all child processes on exit
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

# Restore and start the Simulator in the background
dotnet tool restore
dotnet Pxl.Simulator &

# Wait for the simulator to be reachable (max 10 seconds)
echo "Waiting for simulator..."
for i in {1..20}; do
    if curl -s --head http://127.0.0.1:5001 > /dev/null 2>&1; then
        echo "Simulator ready at http://127.0.0.1:5001"
        break
    fi
    sleep 0.5
done

# Open browser
if command -v open &> /dev/null; then
    open http://127.0.0.1:5001
elif command -v xdg-open &> /dev/null; then
    xdg-open http://127.0.0.1:5001
elif command -v wslview &> /dev/null; then
    wslview http://127.0.0.1:5001
fi

# Start the C# Watcher in the background (.env is loaded by the watcher itself)
dotnet fsi ./build/csFsxWatcher.fsx &

echo ""
echo "Save any .cs or .fsx file in the apps/ folder to send it to the simulator"
echo ""

# Watch .env for changes and restart everything if it changes
if [ -f .env ]; then
    ENV_HASH=$(md5sum .env 2>/dev/null || md5 -q .env 2>/dev/null)
    (
        while true; do
            sleep 2
            if [ -f .env ]; then
                NEW_HASH=$(md5sum .env 2>/dev/null || md5 -q .env 2>/dev/null)
                if [ "$NEW_HASH" != "$ENV_HASH" ]; then
                    echo ""
                    echo ".env changed — restarting development environment..."
                    echo ""
                    kill $$
                fi
            fi
        done
    ) &
fi

# Wait for all child processes
wait

# If we got here via the .env watcher signal, restart
PXL_RESTART=1 exec "$0" "$@"
