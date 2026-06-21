#!/bin/bash
# Run once after installing SecureVault on macOS to register the native messaging host

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HOST_JSON="$SCRIPT_DIR/com.securevault.host.json"

# Chrome
CHROME_DIR="$HOME/Library/Application Support/Google/Chrome/NativeMessagingHosts"
mkdir -p "$CHROME_DIR"
cp "$HOST_JSON" "$CHROME_DIR/com.securevault.host.json"
echo "Registered for Chrome"

# Firefox
FF_DIR="$HOME/Library/Application Support/Mozilla/NativeMessagingHosts"
mkdir -p "$FF_DIR"
cp "$HOST_JSON" "$FF_DIR/com.securevault.host.json"
echo "Registered for Firefox"

echo "Done. Restart your browser."
