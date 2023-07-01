using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace FileEncrypt;

/// <summary>
///     VersionWindow.xaml の相互作用ロジック
/// </summary>
public partial class VersionWindow : Window
{
    public VersionWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            LabelVersion.Content = version.ToString();
        }
    }

    private void ButtonClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
