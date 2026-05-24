#!/usr/bin/env bash
set -euo pipefail
APK="${1:?Usage: ./provision.sh path/to/app-debug.apk}"
PKG="com.anzone.mdm"
ADMIN="$PKG/.device.AnzoneDeviceAdminReceiver"
echo "Ensure the device is factory-reset, has NO accounts, and USB debugging is on."
adb install -r "$APK"
adb shell dpm set-device-owner "$ADMIN"
echo "Success. Reboot the device to enter Kiosk."
