#!/bin/bash

# PXL Clock - Setup Verification Script
# Checks all prerequisites for macOS, Linux, and Windows (WSL/Git Bash)

set -u

# --- Colors (disabled if not a terminal) ---
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    CYAN='\033[0;36m'
    BOLD='\033[1m'
    NC='\033[0m'
else
    RED='' GREEN='' YELLOW='' CYAN='' BOLD='' NC=''
fi

# --- State ---
ERRORS=0
WARNINGS=0

# --- Helpers ---
ok()   { echo -e "  ${GREEN}[OK]${NC}    $1"; }
warn() { echo -e "  ${YELLOW}[WARN]${NC}  $1"; WARNINGS=$((WARNINGS + 1)); }
fail() { echo -e "  ${RED}[FAIL]${NC}  $1"; ERRORS=$((ERRORS + 1)); }
info() { echo -e "  ${CYAN}[INFO]${NC}  $1"; }
hint() { echo -e "          $1"; }

detect_platform() {
    if [ -f /proc/version ] && grep -qi microsoft /proc/version 2>/dev/null; then
        echo "wsl"
    elif [ "$(uname -s)" = "Darwin" ]; then
        echo "macos"
    elif [ "$(uname -s)" = "Linux" ]; then
        echo "linux"
    else
        echo "unknown"
    fi
}

version_ge() {
    # Returns 0 if $1 >= $2 (major version comparison)
    local actual=$1
    local required=$2
    local actual_major="${actual%%.*}"
    local required_major="${required%%.*}"
    [ "$actual_major" -ge "$required_major" ] 2>/dev/null
}

# --- Main ---
PLATFORM=$(detect_platform)

echo ""
echo -e "${BOLD}PXL Clock - Setup Check${NC}"
echo "========================================"
case "$PLATFORM" in
    macos) info "Platform: macOS ($(uname -m))" ;;
    linux) info "Platform: Linux ($(uname -m))" ;;
    wsl)   info "Platform: Windows (WSL)" ;;
    *)     info "Platform: $(uname -s) ($(uname -m))" ;;
esac
echo ""

# ------------------------------------------
# 1. .NET SDK
# ------------------------------------------
echo -e "${BOLD}Required${NC}"
echo "--------"

if command -v dotnet &>/dev/null; then
    DOTNET_VERSION=$(dotnet --version 2>/dev/null)
    if version_ge "$DOTNET_VERSION" "10"; then
        ok ".NET SDK $DOTNET_VERSION"
    else
        fail ".NET SDK $DOTNET_VERSION found, but 10.0+ is required"
        hint "Download: https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
    fi
else
    fail ".NET SDK not found"
    case "$PLATFORM" in
        macos)
            hint "Install: brew install dotnet"
            hint "    or:  https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
            ;;
        wsl|linux)
            hint "Install: https://learn.microsoft.com/dotnet/core/install/linux"
            hint "    or:  sudo apt-get install dotnet-sdk-10.0  (Ubuntu/Debian)"
            ;;
        *)
            hint "Download: https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
            ;;
    esac
fi

# ------------------------------------------
# 2. curl (used by start.sh to wait for simulator)
# ------------------------------------------
if command -v curl &>/dev/null; then
    ok "curl"
else
    fail "curl not found (needed to detect simulator startup)"
    case "$PLATFORM" in
        macos) hint "curl should be pre-installed on macOS — check your PATH" ;;
        *)     hint "Install: sudo apt-get install curl" ;;
    esac
fi

# ------------------------------------------
# 3. .NET local tools
# ------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
TOOLS_JSON="$SCRIPT_DIR/.config/dotnet-tools.json"

if [ -f "$TOOLS_JSON" ]; then
    ok ".NET tool manifest found"
else
    fail ".config/dotnet-tools.json not found — repo might be incomplete"
    hint "Try: git checkout .config/dotnet-tools.json"
fi

echo ""

# ------------------------------------------
# 4. Optional: VS Code
# ------------------------------------------
echo -e "${BOLD}Recommended${NC}"
echo "-----------"

if command -v code &>/dev/null; then
    ok "Visual Studio Code"

    # Check extensions (non-blocking)
    EXTENSIONS=$(code --list-extensions 2>/dev/null || true)

    if echo "$EXTENSIONS" | grep -q "ms-dotnettools.csdevkit"; then
        ok "VS Code extension: C# Dev Kit"
    else
        warn "VS Code extension: C# Dev Kit not installed"
        hint "Install: code --install-extension ms-dotnettools.csdevkit"
    fi
else
    warn "VS Code not found (optional, but makes development easier)"
    hint "Download: https://code.visualstudio.com/"
fi

# ------------------------------------------
# 5. Browser opener (for simulator auto-open)
# ------------------------------------------
case "$PLATFORM" in
    macos)
        if command -v open &>/dev/null; then
            ok "Browser opener (open)"
        else
            warn "No browser opener found"
        fi
        ;;
    wsl)
        if command -v wslview &>/dev/null; then
            ok "Browser opener (wslview)"
        elif command -v xdg-open &>/dev/null; then
            ok "Browser opener (xdg-open)"
        else
            warn "No browser opener found — simulator won't auto-open"
            hint "Install wslu: sudo apt-get install wslu"
        fi
        ;;
    linux)
        if command -v xdg-open &>/dev/null; then
            ok "Browser opener (xdg-open)"
        else
            warn "No browser opener found — simulator won't auto-open"
            hint "Install xdg-utils: sudo apt-get install xdg-utils"
        fi
        ;;
esac

# ------------------------------------------
# Summary
# ------------------------------------------
echo ""
echo "========================================"

if [ "$ERRORS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
    echo -e "${GREEN}${BOLD}All checks passed!${NC}"
    echo ""
    echo "  Get started:"
    echo "    ./start.sh                           (macOS/Linux/WSL)"
    echo "    Cmd+Shift+B / Ctrl+Shift+B           (VS Code)"
    echo "    Open http://localhost:5001            (Simulator)"
    echo ""
elif [ "$ERRORS" -eq 0 ]; then
    echo -e "${YELLOW}${BOLD}Ready with $WARNINGS warning(s)${NC}"
    echo ""
    echo "  All required tools are installed. Warnings are optional."
    echo "  Run ./start.sh to get started."
    echo ""
else
    echo -e "${RED}${BOLD}$ERRORS required tool(s) missing${NC}"
    echo ""
    echo "  Please install the missing tools above and re-run:"
    echo "    ./build/setup-check.sh"
    echo ""
    exit 1
fi
