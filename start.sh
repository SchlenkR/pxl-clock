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

# Load .env to check settings
PXL_SEND_TO_SIMULATOR="true"
if [ -f .env ]; then
    while IFS='=' read -r key value; do
        key=$(echo "$key" | xargs)
        [[ -z "$key" || "$key" == \#* ]] && continue
        value=$(echo "$value" | xargs)
        if [ "$key" = "PXL_SEND_TO_SIMULATOR" ]; then
            PXL_SEND_TO_SIMULATOR="$value"
        fi
    done < .env
fi

dotnet tool restore

# Start the Simulator if enabled
if [ "$PXL_SEND_TO_SIMULATOR" = "true" ]; then
    dotnet Pxl.Simulator &

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
        echo "  Try running: dotnet tool restore && dotnet Pxl.Simulator"
        echo "  Or check: ./build/setup-check.sh"
        echo ""
    fi

    # Open browser (only if simulator is ready)
    if [ "$SIMULATOR_READY" = true ]; then
        if command -v open &> /dev/null; then
            open http://127.0.0.1:5001
        elif command -v xdg-open &> /dev/null; then
            xdg-open http://127.0.0.1:5001
        elif command -v wslview &> /dev/null; then
            wslview http://127.0.0.1:5001
        fi
    fi
else
    echo "Simulator disabled (PXL_SEND_TO_SIMULATOR=$PXL_SEND_TO_SIMULATOR)"
fi

# Start the C# Watcher in the background (.env is loaded by the watcher itself)
dotnet fsi ./build/csFsxWatcher.fsx &

echo ""
echo "Save any .cs or .fsx file in the apps/ folder to start"
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
