#!/usr/bin/env bash
# Builds a release NotchBeat.app into ./build
set -euo pipefail

cd "$(dirname "$0")/.."

APP_NAME="NotchBeat"
APP="build/$APP_NAME.app"

swift build -c release

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS"
cp "$(swift build -c release --show-bin-path)/$APP_NAME" "$APP/Contents/MacOS/"
cp Resources/Info.plist "$APP/Contents/"

# Ad-hoc signing so macOS remembers the Spotify Automation permission between launches.
codesign --force --sign - "$APP"

echo "Built $APP"
