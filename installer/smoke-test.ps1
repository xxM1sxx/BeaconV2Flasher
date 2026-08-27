$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root "artifacts\publish"
$openocd = Join-Path $publish "openocd\bin\openocd.exe"
$config = Join-Path $publish "openocd-stlink.cfg"
$installer = Join-Path $root "artifacts\installer\Qimtronics-Beacon-V2-Flasher-Setup-1.0.0.exe"

@(
    (Join-Path $publish "BeaconV2Flasher.exe")
    $openocd
    $config
    $installer
) | ForEach-Object {
    if (-not (Test-Path $_)) { throw "File paket tidak ditemukan: $_" }
}

& $openocd -f $config -c "adapter serial SMOKETEST" -c "shutdown"
if ($LASTEXITCODE -ne 0) {
    throw "Konfigurasi OpenOCD gagal dimuat."
}

Write-Host "Package smoke test passed."
