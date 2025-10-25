using CommunityToolkit.Mvvm.ComponentModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "就绪 - CSP2 v0.1.0 开发中";

    public MainWindowViewModel()
    {
        // 初始化
    }
}

