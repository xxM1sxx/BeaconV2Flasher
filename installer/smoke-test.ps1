param(
    [switch]$SkipDeviceTest
)

$ErrorActionPreference = "Stop"

# smoke-test.ps1 [-SkipDeviceTest]
#   Memeriksa bahwa paket publish + installer untuk versi saat ini lengkap, dan
#   (secara default) mencoba sekali memuat konfigurasi OpenOCD terhadap ST-Link
#   yang terpasang.

$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root "artifacts\publish"
$openocd = Join-Path $publish "openocd\bin\openocd.exe"
$config = Join-Path $publish "openocd-stlink.cfg"
$appExe = Join-Path $publish "NRF52840Flasher.exe"

. (Join-Path $PSScriptRoot "Version.ps1")
$version = Get-AppVersion -RepoRoot $root
$installer = Join-Path $root "artifacts\installer\Qimtronics-NRF52840-Flasher-Setup-$version.exe"

@(
    $appExe
    $openocd
    $config
    $installer
) | ForEach-Object {
    if (-not (Test-Path -LiteralPath $_)) { throw "File paket tidak ditemukan: $_" }
}

# Versi executable hasil publish harus sama dengan versi yang minta dipaketkan.
$exeVersion = (Get-Item -LiteralPath $appExe).VersionInfo
$expectedVersion = "$version.0"
if ($exeVersion.FileVersion -ne $expectedVersion) {
    throw "Versi executable hasil publish tidak sesuai: $($exeVersion.FileVersion), diharapkan $expectedVersion."
}
if ($exeVersion.ProductVersion -notlike "$version*") {
    throw "ProductVersion executable tidak sesuai: $($exeVersion.ProductVersion), diharapkan berawalan $version."
}
if ($exeVersion.ProductName -ne "NRF52840 Flasher") {
    throw "ProductName executable tidak sesuai: '$($exeVersion.ProductName)'."
}
if ($exeVersion.CompanyName -ne "Qimtronics") {
    throw "CompanyName executable tidak sesuai: '$($exeVersion.CompanyName)'."
}

Write-Host "Versi paket $version terverifikasi (FileVersion $($exeVersion.FileVersion), ProductName $($exeVersion.ProductName))."

if ($SkipDeviceTest) {
    Write-Host "Package smoke test passed (uji perangkat dilewati)."
    return
}

# Argumen serial dummy membuat OpenOCD berhenti setelah memuat konfigurasi.
# Tanpa ST-Link terpasang ini gagal; jalankan dengan -SkipDeviceTest bila memang
# tidak ada perangkat.
& $openocd -f $config -c "adapter serial SMOKETEST" -c "shutdown"
if ($LASTEXITCODE -ne 0) {
    throw "Konfigurasi OpenOCD gagal dimuat. Jalankan ulang dengan -SkipDeviceTest bila ST-Link tidak terpasang."
}

Write-Host "Package smoke test passed."
