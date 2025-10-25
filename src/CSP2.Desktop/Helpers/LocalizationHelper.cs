using System;
using System.ComponentModel;
using System.Windows;

namespace CSP2.Desktop.Helpers;

/// <summary>
/// 本地化辅助类 - 用于在XAML中实现动态资源绑定
/// </summary>
public class LocalizationHelper : INotifyPropertyChanged
{
    private static LocalizationHelper? _instance;
    private Services.JsonLocalizationService? _localizationService;
    
    public static LocalizationHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LocalizationHelper();
            }
            return _instance;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationHelper()
    {
    }

    /// <summary>
    /// 初始化本地化助手（从主窗口调用）
    /// </summary>
    public void Initialize(Services.JsonLocalizationService localizationService)
    {
        _localizationService = localizationService;
        localizationService.LanguageChanged += (s, e) =>
        {
            // 当语言改变时，通知所有属性更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            });
        };
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    private string GetString(string key, params object[] args)
    {
        if (_localizationService == null)
        {
            return $"[{key}]";
        }
        return _localizationService.GetString(key, args);
    }

    // 资源访问属性
    public string this[string key]
    {
        get => GetString(key);
    }

    // 应用程序标题
    public string App_Title => GetString("App.Title");
    public string App_Name => GetString("App.Name");
    public string App_Description => GetString("App.Description");
    public string App_Version => GetString("App.Version");
    public string App_DevVersion => GetString("App.DevVersion");

    // 导航菜单
    public string Nav_ServerManagement => GetString("Nav.ServerManagement");
    public string Nav_LogConsole => GetString("Nav.LogConsole");
    public string Nav_PluginMarket => GetString("Nav.PluginMarket");
    public string Nav_DownloadManager => GetString("Nav.DownloadManager");
    public string Nav_Settings => GetString("Nav.Settings");
    public string Nav_DebugConsole => GetString("Nav.DebugConsole");

    // 状态
    public string Status_Ready => GetString("Status.Ready");
    public string Status_ReadyText => GetString("Status.ReadyText");

    // 服务器管理
    public string ServerMgmt_Title => GetString("ServerMgmt.Title");
    public string ServerMgmt_Subtitle => GetString("ServerMgmt.Subtitle");
    public string ServerMgmt_InstallServer => GetString("ServerMgmt.InstallServer");
    public string ServerMgmt_AddServer => GetString("ServerMgmt.AddServer");
    public string ServerMgmt_Refresh => GetString("ServerMgmt.Refresh");
    public string ServerMgmt_Port => GetString("ServerMgmt.Port");
    public string ServerMgmt_Map => GetString("ServerMgmt.Map");
    public string ServerMgmt_MaxPlayers => GetString("ServerMgmt.MaxPlayers");
    public string ServerMgmt_TickRate => GetString("ServerMgmt.TickRate");
    public string ServerMgmt_Start => GetString("ServerMgmt.Start");
    public string ServerMgmt_Stop => GetString("ServerMgmt.Stop");
    public string ServerMgmt_Restart => GetString("ServerMgmt.Restart");
    public string ServerMgmt_ViewLog => GetString("ServerMgmt.ViewLog");
    public string ServerMgmt_Settings => GetString("ServerMgmt.Settings");
    public string ServerMgmt_Uninstall => GetString("ServerMgmt.Uninstall");
    public string ServerMgmt_Delete => GetString("ServerMgmt.Delete");
    public string ServerMgmt_NoServers => GetString("ServerMgmt.NoServers");
    public string ServerMgmt_NoServersHint => GetString("ServerMgmt.NoServersHint");

    // 插件市场
    public string PluginMarket_Title => GetString("PluginMarket.Title");
    public string PluginMarket_Subtitle => GetString("PluginMarket.Subtitle");
    public string PluginMarket_TargetServer => GetString("PluginMarket.TargetServer");
    public string PluginMarket_Category => GetString("PluginMarket.Category");
    public string PluginMarket_Install => GetString("PluginMarket.Install");
    public string PluginMarket_Featured => GetString("PluginMarket.Featured");
    public string PluginMarket_NoPlugins => GetString("PluginMarket.NoPlugins");
    public string PluginMarket_NoPluginsHint => GetString("PluginMarket.NoPluginsHint");
    public string PluginMarket_Loading => GetString("PluginMarket.Loading");
    public string PluginMarket_PleaseWait => GetString("PluginMarket.PleaseWait");

    // 设置
    public string Settings_Title => GetString("Settings.Title");
    public string Settings_Subtitle => GetString("Settings.Subtitle");
    public string Settings_Appearance => GetString("Settings.Appearance");
    public string Settings_AppearanceDesc => GetString("Settings.AppearanceDesc");
    public string Settings_Theme => GetString("Settings.Theme");
    public string Settings_ThemeDesc => GetString("Settings.ThemeDesc");
    public string Settings_Language => GetString("Settings.Language");
    public string Settings_LanguageDesc => GetString("Settings.LanguageDesc");
    public string Settings_Update => GetString("Settings.Update");
    public string Settings_UpdateDesc => GetString("Settings.UpdateDesc");
    public string Settings_AutoCheckUpdates => GetString("Settings.AutoCheckUpdates");
    public string Settings_AutoCheckUpdatesDesc => GetString("Settings.AutoCheckUpdatesDesc");
    public string Settings_SteamCmd => GetString("Settings.SteamCmd");
    public string Settings_SteamCmdDesc => GetString("Settings.SteamCmdDesc");
    public string Settings_Status => GetString("Settings.Status");
    public string Settings_CheckStatus => GetString("Settings.CheckStatus");
    public string Settings_InstallSteamCmd => GetString("Settings.InstallSteamCmd");
    public string Settings_UninstallSteamCmd => GetString("Settings.UninstallSteamCmd");
    public string Settings_InstallPath => GetString("Settings.InstallPath");
    public string Settings_InstallPathDesc => GetString("Settings.InstallPathDesc");
    public string Settings_Browse => GetString("Settings.Browse");
    public string Settings_AutoDownloadSteamCmd => GetString("Settings.AutoDownloadSteamCmd");
    public string Settings_AutoDownloadSteamCmdDesc => GetString("Settings.AutoDownloadSteamCmdDesc");
    public string Settings_RestoreDefaults => GetString("Settings.RestoreDefaults");
    public string Settings_Save => GetString("Settings.Save");

    // 下载管理
    public string Download_Title => GetString("Download.Title");
    public string Download_Subtitle => GetString("Download.Subtitle");
    public string Download_NoTasks => GetString("Download.NoTasks");
    public string Download_NoTasksHint => GetString("Download.NoTasksHint");

    // 日志控制台
    public string Log_Title => GetString("Log.Title");
    public string Log_Subtitle => GetString("Log.Subtitle");
    public string Log_SelectServer => GetString("Log.SelectServer");
    public string Log_SendCommand => GetString("Log.SendCommand");
    public string Log_Clear => GetString("Log.Clear");
    public string Log_Export => GetString("Log.Export");

    // Debug控制台
    public string Debug_Title => GetString("Debug.Title");
    public string Debug_Subtitle => GetString("Debug.Subtitle");

    // 通用
    public string Common_OK => GetString("Common.OK");
    public string Common_Cancel => GetString("Common.Cancel");
    public string Common_Refresh => GetString("Common.Refresh");
}

