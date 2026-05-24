# Usage: .\provision.ps1 -Apk path\to\app-debug.apk
param([Parameter(Mandatory=$true)][string]$Apk)
$pkg = "com.anzone.mdm"
$admin = "$pkg/.device.AnzoneDeviceAdminReceiver"
Write-Host "Ensure the device is factory-reset, has NO accounts added, and USB debugging is on."
adb install -r $Apk
if ($LASTEXITCODE -ne 0) { Write-Error "Install failed"; exit 1 }
adb shell dpm set-device-owner $admin
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to set Device Owner (device must have no existing accounts)"; exit 1 }
Write-Host "Success. Reboot the device to enter Kiosk."
