using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace BeaconV2Flasher;

public sealed class OpenOcdRunner(string executablePath, string configPath)
{
    private static readonly Regex SerialPattern = new(
        "^[A-Za-z0-9]+$",
        RegexOptions.CultureInvariant);

    public async Task<int> FlashAsync(
        string firmwarePath,
        string? adapterSerial,
        bool chipEraseBeforeFlash,
        Action<string> writeLog,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(executablePath))
            throw new FileNotFoundException("OpenOCD tidak ditemukan.", executablePath);
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Konfigurasi OpenOCD tidak ditemukan.", configPath);
        if (!File.Exists(firmwarePath))
            throw new FileNotFoundException("Firmware tidak ditemukan.", firmwarePath);
        if (adapterSerial is not null && !SerialPattern.IsMatch(adapterSerial))
            throw new ArgumentException("Serial ST-Link tidak valid.", nameof(adapterSerial));

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(configPath);
        if (adapterSerial is not null)
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add($"adapter serial {adapterSerial}");
        }

        if (chipEraseBeforeFlash)
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("init");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("nrf52_recover");
        }

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"program {QuoteTcl(firmwarePath)} verify reset exit");

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) => WriteLine(args.Data, writeLog);
        process.ErrorDataReceived += (_, args) => WriteLine(args.Data, writeLog);

        if (!process.Start())
            throw new InvalidOperationException("OpenOCD gagal dijalankan.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw;
        }
    }

    internal static string QuoteTcl(string value)
    {
        var normalized = Path.GetFullPath(value).Replace('\\', '/');
        return $"\"{normalized
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static void WriteLine(string? line, Action<string> writeLog)
    {
        if (!string.IsNullOrWhiteSpace(line))
            writeLog(line);
    }
}
