using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views;

public partial class MapHistoryView : UserControl
{
    public MapHistoryView()
    {
        InitializeComponent();
        Loaded += MapHistoryView_OnLoaded;
    }

    /// <summary>
    /// 当视图加载时，加载地图历史
    /// </summary>
    private void MapHistoryView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MapHistoryViewModel viewModel)
        {
            // 使用 Execute 而不是 ExecuteAsync
            if (viewModel.LoadHistoryCommand.CanExecute(null))
            {
                viewModel.LoadHistoryCommand.Execute(null);
            }
        }
    }
}

