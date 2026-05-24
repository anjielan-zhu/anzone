# Run from an ADMIN PowerShell. Param: path to the published AnzoneService.exe
param([string]$ServiceExe = "C:\Program Files\anzone\AnzoneService.exe")

# Data dir: SYSTEM + Administrators full control, standard Users read-only.
$dataDir = "C:\ProgramData\anzone"
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
icacls $dataDir /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "Users:(OI)(CI)RX" | Out-Null

sc.exe create AnzoneService binPath= "`"$ServiceExe`"" start= auto
sc.exe failure AnzoneService reset= 86400 actions= restart/5000/restart/5000/restart/5000
sc.exe start AnzoneService
Write-Host "AnzoneService installed and started (auto-start + crash auto-restart). Data dir locked down."
