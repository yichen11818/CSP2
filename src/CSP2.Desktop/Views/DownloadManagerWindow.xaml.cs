using System.Windows;

namespace CSP2.Desktop.Views;

/// <summary>
/// DownloadManagerWindow.xaml 的交互逻辑
/// </summary>
public partial class DownloadManagerWindow : Window
{
    public DownloadManagerWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

