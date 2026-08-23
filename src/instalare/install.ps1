# Instalează/actualizează NT to SAGA pe acest calculator.
# Rulat de obicei prin dublu-clic pe Instaleaza.bat, nu direct.
$ErrorActionPreference = "Stop"

function Opreste($mesaj) {
    Write-Host ""
    Write-Host "EROARE: $mesaj" -ForegroundColor Red
    Write-Host ""
    Read-Host "Apasati Enter pentru a inchide"
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sursaExe = Join-Path $scriptDir "NttoSaga.exe"
$tinta = "D:\nttosaga"

Write-Host "Instalare NT to SAGA" -ForegroundColor Cyan
Write-Host "====================="
Write-Host ""

if (-not (Test-Path "D:\")) {
    Opreste "Acest calculator nu are o unitate D:. Aplicatia necesita o unitate D: disponibila. Contactati persoana care a pregatit instalarea."
}

if (-not (Test-Path $sursaExe)) {
    Opreste "Nu gasesc fisierul NttoSaga.exe langa acest script. Copiati intregul folder de instalare, nu doar acest fisier."
}

New-Item -ItemType Directory -Force -Path $tinta | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $tinta "files") | Out-Null

try {
    Copy-Item -Path $sursaExe -Destination (Join-Path $tinta "NttoSaga.exe") -Force
}
catch {
    Opreste "Nu am putut copia aplicatia. Daca NT to SAGA este deschisa in acest moment, inchideti-o si rulati din nou instalarea. Detalii tehnice: $($_.Exception.Message)"
}

try {
    $desktop = [Environment]::GetFolderPath('Desktop')
    $wsh = New-Object -ComObject WScript.Shell
    $scurtatura = $wsh.CreateShortcut((Join-Path $desktop "NT to SAGA.lnk"))
    $scurtatura.TargetPath = Join-Path $tinta "NttoSaga.exe"
    $scurtatura.WorkingDirectory = $tinta
    $scurtatura.Description = "NT to SAGA"
    $scurtatura.Save()
    $scurtaturaCreata = $true
}
catch {
    $scurtaturaCreata = $false
}

Write-Host "Instalare finalizata cu succes." -ForegroundColor Green
Write-Host ""
Write-Host "Aplicatia a fost copiata in: $tinta\NttoSaga.exe"
if ($scurtaturaCreata) {
    Write-Host "A fost creata o comanda rapida pe desktop: 'NT to SAGA'."
} else {
    Write-Host "Nu am putut crea comanda rapida pe desktop — porniti aplicatia direct din $tinta\NttoSaga.exe"
}
Write-Host ""
Write-Host "Fisierele CSV de import se pun in: $tinta\files"
Write-Host ""
Write-Host "Datele deja introduse (daca aceasta e o actualizare) nu au fost atinse."
Write-Host ""
Read-Host "Apasati Enter pentru a inchide"
