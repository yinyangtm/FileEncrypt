using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using Yinyang.FileEncrypt;

namespace FileEncrypt;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Decrypt _decrypt = new(256, 128, 1024, 64);
    private readonly Encrypt _encrypt = new(256, 128, 1024, 64);
    private CancellationTokenSource? _cancellationTokenSource;


    public MainWindow() => InitializeComponent();

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    private void ControlEnable(bool enable)
    {
        ProgressBar.Value = 0;
        ProgressLabel.Content = "0 %";

        if (enable)
        {
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Collapsed;
            MenuExit.IsEnabled = true;
            EncryptButton.IsEnabled = true;
            DecryptButton.IsEnabled = true;
            PasswordBox.IsEnabled = true;
            EncryptButton.Visibility = Visibility.Visible;
            DecryptButton.Visibility = Visibility.Visible;
        }
        else
        {
            CancelButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Visible;
            MenuExit.IsEnabled = false;
            EncryptButton.IsEnabled = false;
            DecryptButton.IsEnabled = false;
            PasswordBox.IsEnabled = false;
            EncryptButton.Visibility = Visibility.Collapsed;
            DecryptButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        ControlEnable(false);

        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            MessageBox.Show(this, "Please enter your password.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ControlEnable(true);
            return;
        }

        var password = PasswordBox.Password;

        OpenFileDialog ofd = new()
        {
            Filter = $"Encrypt File|*{_encrypt.GetExtension}",
            Title = "Please select the file to encrypt",
            RestoreDirectory = true
        };

        if (ofd.ShowDialog() != true)
        {
            ControlEnable(true);
            return;
        }

        var file = ofd.FileName;

        var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog(GetWindow(this)) != FileEncrypt.DialogResult.OK)
        {
            ControlEnable(true);
            return;
        }

        var outdir = fbd.SelectedPath;

        try
        {
            FileHeader? fh = null;
            try
            {
                fh = _decrypt.GetFileHeader(file, password);
            }
            catch (NotValidPasswordException)
            {
                MessageBox.Show(this, "Invalid password or different file format.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ControlEnable(true);
                return;
            }

            if (File.Exists(Path.Combine(outdir, fh.Name)))
            {
                if (MessageBox.Show(
                        this,
                        "A file with the same name already exists in the destination." + Environment.NewLine +
                        "Do you want to overwrite it?",
                        "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    ControlEnable(true);
                    return;
                }
            }

            var progress = new Progress<int>();
            progress.ProgressChanged += Progress_ProgressChanged;
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;

            try
            {
                await _decrypt.DecodeFileAsync(file, outdir, password, progress, ct);
            }
            catch (NotValidPasswordException)
            {
                File.Delete(Path.Combine(outdir, fh.Name));
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                MessageBox.Show(this, "Invalid password or different file format.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ControlEnable(true);
                return;
            }
            catch (OperationCanceledException)
            {
                File.Delete(Path.Combine(outdir, fh.Name));
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                MessageBox.Show(this, "Cancelled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                ControlEnable(true);
                return;
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            MessageBox.Show(this, "Completed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ControlEnable(true);
        }
        catch (Exception err)
        {
            MessageBox.Show(this, err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ControlEnable(true);
        }
    }

    private async void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        ControlEnable(false);

        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            MessageBox.Show(this, "Please enter your password.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ControlEnable(true);
            return;
        }

        var password = PasswordBox.Password;

        OpenFileDialog ofd = new()
        {
            Filter = "All Files(*.*)|*.*", Title = "Please select the file to encrypt", RestoreDirectory = true
        };

        if (ofd.ShowDialog() != true)
        {
            ControlEnable(true);
            return;
        }

        var file = ofd.FileName;

        var outfilepath = ShowSaveDialog(file + _encrypt.GetExtension, true);
        if (string.IsNullOrEmpty(outfilepath))
        {
            ControlEnable(true);
            return;
        }

        try
        {
            var progress = new Progress<int>();
            progress.ProgressChanged += Progress_ProgressChanged;

            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;


            try
            {
                await _encrypt.EncodeFileAsync(file, outfilepath, password, progress, ct);
            }
            catch (OperationCanceledException)
            {
                File.Delete(outfilepath);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                MessageBox.Show(this, "Cancelled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                ControlEnable(true);
                return;
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            MessageBox.Show(this, "Completed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ControlEnable(true);
        }
        catch (Exception err)
        {
            MessageBox.Show(this, err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ControlEnable(true);
        }
    }

    private void MenuExit_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MenuVersion_OnClick(object sender, RoutedEventArgs e)
    {
        var win = new VersionWindow();
        win.ShowDialog();
    }

    private void MenuWebSite_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/yinyangtm/FileEncrypt") {UseShellExecute = true});
        e.Handled = true;
    }

    private void Progress_ProgressChanged(object? sender, int e)
    {
        ProgressBar.Value = e;
        ProgressLabel.Content = string.Format("{0} %", e);
    }

    private string ShowSaveDialog(string filename, bool encrypt)
    {
        var dialog = new SaveFileDialog();
        dialog.OverwritePrompt = true;
        dialog.FileName = filename;
        dialog.Filter = encrypt ? $"Encrypt File|*{_encrypt.GetExtension}" : "All File(*.*)|*.*";

        var result = dialog.ShowDialog() ?? false;

        return !result ? string.Empty : dialog.FileName;
    }
}
