$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "BeaconV2Flasher\BeaconV2Flasher.csproj"
$publish = Join-Path $root "artifacts\publish"
$script = Join-Path $PSScriptRoot "BeaconV2Flasher.iss"
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

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $publish

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish gagal dengan exit code $LASTEXITCODE."
}

& $iscc $script
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup gagal dengan exit code $LASTEXITCODE."
}
