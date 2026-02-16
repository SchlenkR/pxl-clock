#!/bin/bash
# filepath: /Users/ronald/repos/github.CnP/PXL-Clock/start.sh
# Start Simulator and C# Watcher concurrently

# Load environment variables from .env file (if it exists)
if [ -f .env ]; then
    set -a
    source .env
    set +a
fi

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
    echo "🛑 Stopping PXL Clock development environment..."
    kill 0 2>/dev/null
    wait 2>/dev/null
}
trap cleanup EXIT

echo ""
echo "🚀 Starting PXL Clock development environment..."
echo ""

# Start the Simulator in the background
./build/start-simulator.sh &

# Start the C# Watcher in the background
./build/start-watcher.sh &

echo ""
echo "💡 Save any .cs or .fsx file in the apps/ folder to send it to the simulator"
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
                    echo "🔄 .env changed — restarting development environment..."
                    echo ""
                    # Kill the parent process group, then re-exec start.sh
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
