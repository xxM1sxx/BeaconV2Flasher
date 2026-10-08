# Build installer NRF52840 Flasher

Panduan singkat membuat installer Windows (`.exe` setup) untuk aplikasi
**NRF52840 Flasher** buatan **Qimtronics**.

## 1. Yang perlu terpasang

| Kebutuhan | Versi dipakai saat ini | Cek cepat |
|---|---|---|
| .NET SDK | 10.0.401 | `dotnet --version` |
| Inno Setup 6 | 6.x | `Test-Path "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"` |
| Payload OpenOCD | sudah ada di repo | `Test-Path vendor\openocd\bin\openocd.exe` |

Kalau Inno Setup belum ada: `winget install JRSoftware.InnoSetup`.

## 2. Naikkan versi (satu tempat saja)

Versi hanya ditulis di **`NRF52840Flasher\Directory.Build.props`**:

```xml
<Version>1.1.0</Version>
```

Ubah angka itu, lalu `Version`, `FileVersion`, `AssemblyVersion`, metadata
assembly aplikasi, teks "Versi ..." di jendela aplikasi, `AppVersion` installer,
dan nama file setup semuanya ikut berubah. `installer\Version.ps1` membaca file
yang sama, jadi tidak ada versi kedua yang bisa lupa diperbarui.

## 3. Build

Dari root repo:

```powershell
.\installer\build-installer.ps1
```

Skrip itu menjalankan dua tahap:

1. menghapus `artifacts\publish` lama, lalu `dotnet publish` self-contained
   single-file (`win-x64`) ke folder tersebut;
2. `ISCC.exe` mengompilasi `installer\NRF52840Flasher.iss` menjadi setup.

Hasilnya:

```
artifacts\publish\NRF52840Flasher.exe                                (aplikasi + runtime)
artifacts\publish\openocd\...                                        (payload OpenOCD)
artifacts\installer\Qimtronics-NRF52840-Flasher-Setup-<versi>.exe    (installer)
```

`NRF52840Flasher.iss` menolak dikompilasi kalau executable hasil publish bukan
versi yang diminta, jadi installer tidak bisa memaketkan binary versi lama.

## 4. Uji paket

```powershell
.\installer\smoke-test.ps1                 # dengan ST-Link terpasang
.\installer\smoke-test.ps1 -SkipDeviceTest # tanpa perangkat
```

Uji ini memastikan file paket ada, `FileVersion`, `ProductName`, dan
`CompanyName` executable sudah benar, serta konfigurasi OpenOCD berhasil dimuat.

## 5. Identitas aplikasi

| | |
|---|---|
| Nama aplikasi | `NRF52840 Flasher` |
| Pembuat / penerbit | `Qimtronics` |
| Nama executable | `NRF52840Flasher.exe` |
| Nama file setup | `Qimtronics-NRF52840-Flasher-Setup-<versi>.exe` |
| Ikon | `NRF52840Flasher\Assets\NRF52840 Flasher Pixel Icon.ico` (16–256 px) |
| Folder aplikasi | `C:\Program Files\Qimtronics\NRF52840 Flasher` |
| Shortcut Start Menu | `NRF52840 Flasher` |
| Shortcut Desktop | opsional, tidak dicentang secara default |
| AppId | `{7B799A18-E576-4BB3-B289-CC43A78E301B}` — tidak diubah sejak versi Beacon V2, sehingga installer baru tetap terbaca sebagai upgrade |
| Hak akses | `PrivilegesRequired=admin` (menulis ke Program Files) |

Nama assembly tetap `NRF52840Flasher`, sedangkan namespace C# di dalam kode masih
`BeaconV2Flasher` (nama internal dari versi awal, tidak terlihat pengguna).

## 6. Catatan rilis

- Installer belum ditandatangani secara digital, jadi Windows SmartScreen bisa
  menampilkan peringatan saat setup dijalankan. Untuk menghilangkannya perlu
  sertifikat code signing (`SignTool` pada `[Setup]`).
- Nama folder pemasangan berubah dari `Beacon V2 Flasher` menjadi
  `NRF52840 Flasher`. Karena AppId dan nama shortcut ikut berubah, pemasangan
  1.1.0 muncul sebagai entri terpisah dari versi 1.0.x; versi lama sebaiknya
  di-uninstall lewat Apps & Features.
- Semua isi `vendor\openocd` ikut dipaketkan, jadi ukuran setup sekitar 45 MB.
- Kalau `vendor\openocd` diperbarui, jalankan ulang `build-installer.ps1` supaya
  payload di `artifacts\publish\openocd` ikut tersegarkan.
