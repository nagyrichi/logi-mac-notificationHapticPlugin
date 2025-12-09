#!/bin/bash

# NotificationHaptic Plugin Installer
# Copyright (c) 2025 hyunjun.kim

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== NotificationHaptic Plugin Installer ===${NC}"
echo "Installing plugin for Logi Options+..."

# Define paths
PLUGIN_NAME="NotificationHaptic.lplug4"
PLUGIN_SRC_DIR="$(dirname "$0")"
PLUGIN_FILE="$PLUGIN_SRC_DIR/$PLUGIN_NAME"
DEST_DIR="$HOME/Library/Application Support/Logi/LogiPluginService/Plugins"

# Check if plugin file exists
if [ ! -f "$PLUGIN_FILE" ]; then
    echo -e "${RED}Error: $PLUGIN_NAME not found in the current directory.${NC}"
    echo "Please make sure the installer is in the same folder as the .lplug4 file."
    
    # Try to find it in bin/Release if running from source root
    if [ -f "$PLUGIN_SRC_DIR/bin/Release/$PLUGIN_NAME" ]; then
        echo -e "${YELLOW}Found plugin in bin/Release, using that instead...${NC}"
        PLUGIN_FILE="$PLUGIN_SRC_DIR/bin/Release/$PLUGIN_NAME"
    else
        read -p "Press Enter to exit..."
        exit 1
    fi
fi

# Create destination directory if it doesn't exist
if [ ! -d "$DEST_DIR" ]; then
    echo "Creating plugins directory..."
    mkdir -p "$DEST_DIR"
fi

# Remove existing plugin
if [ -d "$DEST_DIR/$PLUGIN_NAME" ] || [ -f "$DEST_DIR/$PLUGIN_NAME" ]; then
    echo "Removing existing version..."
    rm -rf "$DEST_DIR/$PLUGIN_NAME"
fi

# Copy new plugin
echo "Copying plugin files..."
cp -r "$PLUGIN_FILE" "$DEST_DIR/"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Successfully copied plugin to:$DEST_DIR${NC}"
else
    echo -e "${RED}Failed to copy plugin files.${NC}"
    read -p "Press Enter to exit..."
    exit 1
fi

# Restart Logi Options+ to load the plugin
echo -e "${YELLOW}Restarting Logi Options+ to apply changes...${NC}"

# Kill existing processes
pkill -f "Logi Options+"
pkill -f "LogiPluginService"

# Wait a moment
sleep 2

# Start Logi Options+
open -a "Logi Options+"

echo -e "${GREEN}=== Installation Complete! ===${NC}"
echo "Logi Options+ is restarting."
echo "1. Please wait for Logi Options+ to open."
echo "2. Go to your device settings."
echo "3. Check if 'Notification Haptic' action is available."

# Show a dialog
osascript -e 'display notification "Plugin installed successfully. Logi Options+ is restarting." with title "NotificationHaptic Installed"'

read -p "Press Enter to close this window..."

