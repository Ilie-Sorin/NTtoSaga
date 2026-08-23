# Publică NttoSaga.App ca executabil autonom (single-file) și îl copiază în rădăcina d:\nttosaga.
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $scriptDir "NttoSaga.App\NttoSaga.App.csproj"
$publishDir = Join-Path $scriptDir "NttoSaga.App\bin\Release\net8.0-windows\win-x64\publish"

dotnet publish $proj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

Copy-Item -Path (Join-Path $publishDir "NttoSaga.App.exe") -Destination "d:\nttosaga\NttoSaga.exe" -Force
Write-Host "Executabil publicat: d:\nttosaga\NttoSaga.exe"
