# Publica aplicatia si asambleaza pachetul de instalare gata de copiat pe stick USB / retea,
# in d:\nttosaga\instalare\ (Instaleaza.bat + install.ps1 + Ghid_instalare.txt + NttoSaga.exe).
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptDir "publish.ps1")

$pachetDir = "d:\nttosaga\instalare"
New-Item -ItemType Directory -Force -Path $pachetDir | Out-Null

Copy-Item -Path (Join-Path $scriptDir "instalare\install.ps1") -Destination $pachetDir -Force
Copy-Item -Path (Join-Path $scriptDir "instalare\Instaleaza.bat") -Destination $pachetDir -Force
Copy-Item -Path (Join-Path $scriptDir "instalare\Ghid_instalare.txt") -Destination $pachetDir -Force
Copy-Item -Path "d:\nttosaga\NttoSaga.exe" -Destination $pachetDir -Force

Write-Host ""
Write-Host "Pachet de instalare gata in: $pachetDir" -ForegroundColor Green
Write-Host "Copiati intregul folder pe statia tinta si rulati Instaleaza.bat acolo."
