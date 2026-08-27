using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace BeaconV2Flasher;

public partial class MainWindow : Window
{
    private static readonly Brush IdleBrush = BrushFrom("#98A2B3");
    private static readonly Brush BusyBrush = BrushFrom("#2563EB");
    private static readonly Brush SuccessBrush = BrushFrom("#16A34A");
    private static readonly Brush ErrorBrush = BrushFrom("#DC2626");

    private readonly OpenOcdRunner _openOcd = new(
        Path.Combine(AppContext.BaseDirectory, "openocd", "bin", "openocd.exe"),
        Path.Combine(AppContext.BaseDirectory, "openocd-stlink.cfg"));

    private CancellationTokenSource? _flashCancellation;
    private bool _isFlashing;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshStLinks();
        Closing += MainWindow_Closing;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshStLinks();

    private void StLinkComboBox_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateFlashButton();

    private void RefreshStLinks()
    {
        var previousDeviceId = (StLinkComboBox.SelectedItem as StLinkProbe)?.DeviceId;

        try
        {
            var probes = StLinkDiscovery.GetConnected();
            StLinkComboBox.ItemsSource = probes;
            StLinkComboBox.SelectedItem =
                probes.FirstOrDefault(probe => probe.DeviceId == previousDeviceId)
                ?? probes.FirstOrDefault();

            SetStatus(
                probes.Count == 0 ? "ST-Link tidak ditemukan" : $"{probes.Count} ST-Link ditemukan",
                probes.Count == 0 ? ErrorBrush : IdleBrush);
        }
        catch (Exception ex)
        {
            StLinkComboBox.ItemsSource = null;
            SetStatus("Gagal membaca perangkat USB", ErrorBrush);
            AppendLog($"ERROR: {ex.Message}");
        }

        UpdateFlashButton();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pilih firmware merged.hex",
            Filter = "Intel HEX (*.hex)|*.hex",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            FirmwarePathTextBox.Text = dialog.FileName;
            SetStatus("Firmware siap di-flash", IdleBrush);
            UpdateFlashButton();
        }
    }

    private async void FlashButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isFlashing ||
            StLinkComboBox.SelectedItem is not StLinkProbe probe ||
            !IsValidFirmware(FirmwarePathTextBox.Text))
            return;

        var connectedProbe = StLinkDiscovery.GetConnected()
            .Any(candidate => candidate.DeviceId == probe.DeviceId);
        if (!connectedProbe)
        {
            AppendLog("ERROR: ST-Link terpilih sudah tidak tersambung.");
            RefreshStLinks();
            return;
        }

        _flashCancellation = new CancellationTokenSource();
        SetBusy(true);
        LogTextBox.Clear();
        AppendLog($"ST-Link : {probe.AdapterSerial ?? probe.DeviceId}");
        AppendLog($"Firmware: {FirmwarePathTextBox.Text}");
        AppendLog("Memulai OpenOCD...");

        try
        {
            var exitCode = await _openOcd.FlashAsync(
                FirmwarePathTextBox.Text,
                probe.AdapterSerial,
                line => Dispatcher.BeginInvoke(() => AppendLog(line)),
                _flashCancellation.Token);

            AppendLog($"OpenOCD selesai dengan exit code {exitCode}.");
            SetStatus(
                exitCode == 0 ? "Flash berhasil" : "Flash gagal",
                exitCode == 0 ? SuccessBrush : ErrorBrush);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Flash dibatalkan.");
            SetStatus("Flash dibatalkan", ErrorBrush);
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            SetStatus("Flash gagal", ErrorBrush);
        }
        finally
        {
            _flashCancellation.Dispose();
            _flashCancellation = null;
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        _isFlashing = isBusy;
        StLinkComboBox.IsEnabled = !isBusy;
        RefreshButton.IsEnabled = !isBusy;
        BrowseButton.IsEnabled = !isBusy;
        FlashProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        if (isBusy)
            SetStatus("Flashing dan verifying...", BusyBrush);
        UpdateFlashButton();
    }

    private void UpdateFlashButton() =>
        FlashButton.IsEnabled = !_isFlashing
                                && StLinkComboBox.SelectedItem is StLinkProbe
                                && IsValidFirmware(FirmwarePathTextBox.Text);

    private static bool IsValidFirmware(string path) =>
        File.Exists(path) &&
        Path.GetExtension(path).Equals(".hex", StringComparison.OrdinalIgnoreCase);

    private void AppendLog(string line)
    {
        LogTextBox.AppendText($"{line}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private void SetStatus(string text, Brush color)
    {
        StatusTextBlock.Text = text;
        StatusIndicator.Fill = color;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e) =>
        _flashCancellation?.Cancel();

    private static Brush BrushFrom(string hex) =>
        (Brush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.Transparent);
}
