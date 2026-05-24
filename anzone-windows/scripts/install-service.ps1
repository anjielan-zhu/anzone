# Run from an ADMIN PowerShell. Param: path to the published AnzoneService.exe
param([string]$ServiceExe = "C:\Program Files\anzone\AnzoneService.exe")
sc.exe create AnzoneService binPath= "`"$ServiceExe`"" start= auto
sc.exe failure AnzoneService reset= 86400 actions= restart/5000/restart/5000/restart/5000
sc.exe start AnzoneService
Write-Host "AnzoneService installed and started (auto-start + crash auto-restart)."
