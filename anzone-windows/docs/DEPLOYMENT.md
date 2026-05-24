# anzone-windows Deployment Guide

## Prerequisites
- Daily users on target PCs must be **standard accounts (NOT local administrators)**, otherwise self-protection can be bypassed.
- Installation requires one-time administrator rights.

## Publish (on a build machine)
    dotnet publish AnzoneService -c Release -r win-x64 --self-contained -o publish\service
    dotnet publish AnzoneTray   -c Release -r win-x64 --self-contained -o publish\tray
Copy `publish\service\*` to the target PC at `C:\Program Files\anzone\`, and `publish\tray\*` to the same folder.

## Install (admin PowerShell)
1. `scripts\install-service.ps1` (creates + starts the service: auto-start + SCM crash recovery).
2. Make `AnzoneTray.exe` launch at user logon (Startup folder or Run registry key).
3. First run: tray "Admin login" -> set password -> add whitelist -> "Resume enforcement" (enforcement is paused by default on a fresh install).

## Releasing / uninstalling
- Tray login -> Pause enforcement (to install software); or run `uninstall-service.ps1` to remove entirely.

## Known limitations
- Reactive interception: a blocked process flashes briefly before being killed.
- If a user is a local administrator, they can stop/uninstall the service — cannot be prevented in user mode.
- Installer blocking is heuristic (msiexec / setup* / *install*), not exhaustive.
- Windows-only; tested against mainstream stock Windows.
