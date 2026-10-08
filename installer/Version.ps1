# Sumber tunggal versi aplikasi untuk seluruh skrip installer.
# Nilai aslinya dibaca dari <Version> di NRF52840Flasher\Directory.Build.props,
# file yang sama yang dipakai MSBuild untuk metadata assembly aplikasi.

function Get-AppVersion {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent)
    )

    $propsPath = Join-Path $RepoRoot 'NRF52840Flasher\Directory.Build.props'
    if (-not (Test-Path -LiteralPath $propsPath)) {
        throw "Directory.Build.props tidak ditemukan: $propsPath"
    }

    # Hanya mengambil elemen <Version> yang berisi angka, bukan referensi seperti
    # <FileVersion>$(Version).0</FileVersion>.
    $match = [regex]::Match(
        (Get-Content -LiteralPath $propsPath -Raw),
        '<Version>\s*(?<value>\d[0-9A-Za-z.\-]*)\s*</Version>')
    if (-not $match.Success) {
        throw "Tidak menemukan <Version> di $propsPath"
    }

    $version = $match.Groups['value'].Value
    Write-Verbose "Versi aplikasi dari Directory.Build.props: $version"
    return $version
}
