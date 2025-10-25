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
    public void Initialize(Services.LocalizationService localizationService)
    {
        localizationService.LanguageChanged += (s, e) =>
        {
            // 当语言改变时，通知所有属性更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            });
        };
    }

    // 资源访问属性
    public string this[string key]
    {
        get
        {
            try
            {
                var value = Resources.Strings.ResourceManager.GetString(key, Resources.Strings.Culture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }
    }

    // 应用程序标题
    public string App_Title => this["App_Title"];
    public string App_Name => this["App_Name"];
    public string App_Description => this["App_Description"];
    public string App_Version => this["App_Version"];
    public string App_DevVersion => this["App_DevVersion"];

    // 导航菜单
    public string Nav_ServerManagement => this["Nav_ServerManagement"];
    public string Nav_LogConsole => this["Nav_LogConsole"];
    public string Nav_PluginMarket => this["Nav_PluginMarket"];
    public string Nav_DownloadManager => this["Nav_DownloadManager"];
    public string Nav_Settings => this["Nav_Settings"];
    public string Nav_DebugConsole => this["Nav_DebugConsole"];

    // 状态
    public string Status_Ready => this["Status_Ready"];
    public string Status_ReadyText => this["Status_ReadyText"];

    // 服务器管理
    public string ServerMgmt_Title => this["ServerMgmt_Title"];
    public string ServerMgmt_Subtitle => this["ServerMgmt_Subtitle"];
    public string ServerMgmt_InstallServer => this["ServerMgmt_InstallServer"];
    public string ServerMgmt_AddServer => this["ServerMgmt_AddServer"];
    public string ServerMgmt_Refresh => this["ServerMgmt_Refresh"];
    public string ServerMgmt_Port => this["ServerMgmt_Port"];
    public string ServerMgmt_Map => this["ServerMgmt_Map"];
    public string ServerMgmt_MaxPlayers => this["ServerMgmt_MaxPlayers"];
    public string ServerMgmt_TickRate => this["ServerMgmt_TickRate"];
    public string ServerMgmt_Start => this["ServerMgmt_Start"];
    public string ServerMgmt_Stop => this["ServerMgmt_Stop"];
    public string ServerMgmt_Restart => this["ServerMgmt_Restart"];
    public string ServerMgmt_ViewLog => this["ServerMgmt_ViewLog"];
    public string ServerMgmt_Settings => this["ServerMgmt_Settings"];
    public string ServerMgmt_Uninstall => this["ServerMgmt_Uninstall"];
    public string ServerMgmt_Delete => this["ServerMgmt_Delete"];
    public string ServerMgmt_NoServers => this["ServerMgmt_NoServers"];
    public string ServerMgmt_NoServersHint => this["ServerMgmt_NoServersHint"];

    // 插件市场
    public string PluginMarket_Title => this["PluginMarket_Title"];
    public string PluginMarket_Subtitle => this["PluginMarket_Subtitle"];
    public string PluginMarket_TargetServer => this["PluginMarket_TargetServer"];
    public string PluginMarket_Category => this["PluginMarket_Category"];
    public string PluginMarket_Install => this["PluginMarket_Install"];
    public string PluginMarket_Featured => this["PluginMarket_Featured"];
    public string PluginMarket_NoPlugins => this["PluginMarket_NoPlugins"];
    public string PluginMarket_NoPluginsHint => this["PluginMarket_NoPluginsHint"];
    public string PluginMarket_Loading => this["PluginMarket_Loading"];
    public string PluginMarket_PleaseWait => this["PluginMarket_PleaseWait"];

    // 设置
    public string Settings_Title => this["Settings_Title"];
    public string Settings_Subtitle => this["Settings_Subtitle"];
    public string Settings_Appearance => this["Settings_Appearance"];
    public string Settings_AppearanceDesc => this["Settings_AppearanceDesc"];
    public string Settings_Theme => this["Settings_Theme"];
    public string Settings_ThemeDesc => this["Settings_ThemeDesc"];
    public string Settings_Language => this["Settings_Language"];
    public string Settings_LanguageDesc => this["Settings_LanguageDesc"];
    public string Settings_Update => this["Settings_Update"];
    public string Settings_UpdateDesc => this["Settings_UpdateDesc"];
    public string Settings_AutoCheckUpdates => this["Settings_AutoCheckUpdates"];
    public string Settings_AutoCheckUpdatesDesc => this["Settings_AutoCheckUpdatesDesc"];
    public string Settings_SteamCmd => this["Settings_SteamCmd"];
    public string Settings_SteamCmdDesc => this["Settings_SteamCmdDesc"];
    public string Settings_Status => this["Settings_Status"];
    public string Settings_CheckStatus => this["Settings_CheckStatus"];
    public string Settings_InstallSteamCmd => this["Settings_InstallSteamCmd"];
    public string Settings_UninstallSteamCmd => this["Settings_UninstallSteamCmd"];
    public string Settings_InstallPath => this["Settings_InstallPath"];
    public string Settings_InstallPathDesc => this["Settings_InstallPathDesc"];
    public string Settings_Browse => this["Settings_Browse"];
    public string Settings_AutoDownloadSteamCmd => this["Settings_AutoDownloadSteamCmd"];
    public string Settings_AutoDownloadSteamCmdDesc => this["Settings_AutoDownloadSteamCmdDesc"];
    public string Settings_RestoreDefaults => this["Settings_RestoreDefaults"];
    public string Settings_Save => this["Settings_Save"];

    // 下载管理
    public string Download_Title => this["Download_Title"];
    public string Download_Subtitle => this["Download_Subtitle"];
    public string Download_NoTasks => this["Download_NoTasks"];
    public string Download_NoTasksHint => this["Download_NoTasksHint"];

    // 日志控制台
    public string Log_Title => this["Log_Title"];
    public string Log_Subtitle => this["Log_Subtitle"];
    public string Log_SelectServer => this["Log_SelectServer"];
    public string Log_SendCommand => this["Log_SendCommand"];
    public string Log_Clear => this["Log_Clear"];
    public string Log_Export => this["Log_Export"];

    // Debug控制台
    public string Debug_Title => this["Debug_Title"];
    public string Debug_Subtitle => this["Debug_Subtitle"];

    // 通用
    public string Common_OK => this["Common_OK"];
    public string Common_Cancel => this["Common_Cancel"];
    public string Common_Refresh => this["Common_Refresh"];
}

