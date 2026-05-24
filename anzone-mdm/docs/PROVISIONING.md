# anzone Device Owner Deployment Guide

## Prerequisites
- Device is **factory reset**; during setup **skip / do not add any Google or vendor account** (any existing account makes `set-device-owner` fail).
- Developer options -> enable **USB debugging**.
- A computer with `adb` (Android platform-tools) installed.

## Steps
1. Connect the device; run `adb devices` and confirm it is authorized.
2. Windows: `scripts\provision.ps1 -Apk app-debug.apk`
   macOS/Linux: `./scripts/provision.sh app-debug.apk`
3. After "Success", reboot the device.
4. The device boots into the anzone Kiosk. On first launch, follow the wizard to create the admin and normal-user accounts.
5. Admin configures the whitelist, then switches to the normal user.

## Releasing management
Admin login -> Settings -> Release management -> confirm with password. The device then returns to normal; re-provisioning is required to re-enable management.

## Known limitations
- Tested against mainstream stock Android only; aggressive OEM background-killing is not specifically adapted.
- Cannot prevent a fastboot/flash-level wipe.
- HarmonyOS Next (5.0+) is not supported.
