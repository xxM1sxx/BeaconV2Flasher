$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "NRF52840Flasher\NRF52840Flasher.csproj"
$publish = Join-Path $root "artifacts\publish"
$script = Join-Path $PSScriptRoot "NRF52840Flasher.iss"

. (Join-Path $PSScriptRoot "Version.ps1")
$version = Get-AppVersion -RepoRoot $root
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not (Test-Path (Join-Path $root "vendor\openocd\bin\openocd.exe"))) {
    throw "Bundled OpenOCD tidak ditemukan di vendor\openocd."
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup 6 tidak ditemukan. Install: winget install JRSoftware.InnoSetup"
}

# Bersihkan hasil publish lama supaya executable dengan nama versi sebelumnya
# tidak ikut terpaketkan ke installer baru.
if (Test-Path -LiteralPath $publish) {
    Remove-Item -LiteralPath $publish -Recurse -Force
}

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $publish

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish gagal dengan exit code $LASTEXITCODE."
}

& $iscc "/DMyAppVersion=$version" $script
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup gagal dengan exit code $LASTEXITCODE."
}

$installer = Join-Path $root "artifacts\installer\Qimtronics-NRF52840-Flasher-Setup-$version.exe"
if (-not (Test-Path -LiteralPath $installer)) {
    throw "Installer selesai tetapi hasilnya tidak ditemukan: $installer"
}

Write-Host "Installer selesai: $installer (versi $version)"
