using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace CSP2.Desktop.Services;

/// <summary>
/// 主题管理服务
/// </summary>
public class ThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private string _currentTheme = "Light";

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 当前主题
    /// </summary>
    public string CurrentTheme => _currentTheme;

    /// <summary>
    /// 主题变更事件
    /// </summary>
    public event EventHandler<string>? ThemeChanged;

    /// <summary>
    /// 应用主题
    /// </summary>
    /// <param name="theme">主题名称：Light, Dark, Auto</param>
    public void ApplyTheme(string theme)
    {
        try
        {
            var actualTheme = theme;
            
            // 如果是Auto模式，根据系统主题决定
            if (theme == "Auto")
            {
                actualTheme = IsSystemDarkTheme() ? "Dark" : "Light";
            }

            if (_currentTheme == actualTheme)
                return;

            _currentTheme = actualTheme;
            
            // 更新应用程序资源
            UpdateApplicationResources(actualTheme);
            
            // 触发主题变更事件
            ThemeChanged?.Invoke(this, actualTheme);
            
            _logger.LogInformation("主题已切换到: {Theme}", actualTheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用主题失败: {Theme}", theme);
        }
    }

    /// <summary>
    /// 检测系统是否为深色主题
    /// </summary>
    private bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false; // 默认浅色主题
        }
    }

    /// <summary>
    /// 更新应用程序资源
    /// </summary>
    private void UpdateApplicationResources(string theme)
    {
        var app = Application.Current;
        if (app?.Resources == null) return;

        // 定义主题颜色
        var colors = GetThemeColors(theme);
        
        // 更新 Color 资源（这些是在 Colors.xaml 中定义的）
        foreach (var color in colors)
        {
            if (app.Resources.Contains(color.Key))
            {
                app.Resources[color.Key] = color.Value;
            }
            
            // 同时更新对应的 Brush 资源
            var brushKey = color.Key.Replace("Color", "Brush");
            if (app.Resources.Contains(brushKey))
            {
                app.Resources[brushKey] = new SolidColorBrush(color.Value);
            }
        }
    }

    /// <summary>
    /// 获取主题颜色定义
    /// </summary>
    private Dictionary<string, Color> GetThemeColors(string theme)
    {
        return theme switch
        {
            "Dark" => new Dictionary<string, Color>
            {
                // 背景色
                ["BackgroundColor"] = Color.FromRgb(0x1e, 0x1e, 0x1e),
                ["SurfaceColor"] = Color.FromRgb(0x2d, 0x2d, 0x2d),
                ["CardBackgroundColor"] = Color.FromRgb(0x25, 0x25, 0x25),
                ["HoverBackgroundColor"] = Color.FromRgb(0x3c, 0x3c, 0x3c),
                ["SidebarBackgroundColor"] = Color.FromRgb(0x0f, 0x17, 0x2a),
                ["SidebarDarkBackgroundColor"] = Color.FromRgb(0x0c, 0x14, 0x21),
                
                // 文字色
                ["TextPrimaryColor"] = Color.FromRgb(0xe5, 0xe7, 0xeb),
                ["TextSecondaryColor"] = Color.FromRgb(0x9c, 0xa3, 0xaf),
                ["TextTertiaryColor"] = Color.FromRgb(0x6b, 0x72, 0x80),
                ["TextDisabledColor"] = Color.FromRgb(0x4b, 0x55, 0x63),
                
                // 边框色
                ["BorderColor"] = Color.FromRgb(0x37, 0x41, 0x51),
                ["BorderLightColor"] = Color.FromRgb(0x4b, 0x55, 0x63),
                ["BorderDarkColor"] = Color.FromRgb(0x1f, 0x29, 0x37),
                
                // 主题色
                ["PrimaryPaleColor"] = Color.FromRgb(0x31, 0x2e, 0x81),
                
                // 功能色浅色版本（深色主题）
                ["SuccessLightColor"] = Color.FromRgb(0x06, 0x4e, 0x3b),
                ["WarningLightColor"] = Color.FromRgb(0x78, 0x35, 0x0f),
                ["DangerLightColor"] = Color.FromRgb(0x7f, 0x1d, 0x1d),
                ["InfoLightColor"] = Color.FromRgb(0x16, 0x4e, 0x63),
            },
            _ => new Dictionary<string, Color>
            {
                // 浅色主题（默认）
                ["BackgroundColor"] = Color.FromRgb(0xfa, 0xfa, 0xfa),
                ["SurfaceColor"] = Color.FromRgb(0xff, 0xff, 0xff),
                ["CardBackgroundColor"] = Color.FromRgb(0xff, 0xff, 0xff),
                ["HoverBackgroundColor"] = Color.FromRgb(0xf8, 0xfa, 0xfc),
                ["SidebarBackgroundColor"] = Color.FromRgb(0x1e, 0x29, 0x3b),
                ["SidebarDarkBackgroundColor"] = Color.FromRgb(0x0f, 0x17, 0x2a),
                
                // 文字色
                ["TextPrimaryColor"] = Color.FromRgb(0x1f, 0x29, 0x37),
                ["TextSecondaryColor"] = Color.FromRgb(0x6b, 0x72, 0x80),
                ["TextTertiaryColor"] = Color.FromRgb(0x9c, 0xa3, 0xaf),
                ["TextDisabledColor"] = Color.FromRgb(0xd1, 0xd5, 0xdb),
                
                // 边框色
                ["BorderColor"] = Color.FromRgb(0xe5, 0xe7, 0xeb),
                ["BorderLightColor"] = Color.FromRgb(0xf3, 0xf4, 0xf6),
                ["BorderDarkColor"] = Color.FromRgb(0xd1, 0xd5, 0xdb),
                
                // 主题色
                ["PrimaryPaleColor"] = Color.FromRgb(0xe0, 0xe7, 0xff),
                
                // 功能色浅色版本（浅色主题）- 使用浅色背景
                ["SuccessLightColor"] = Color.FromRgb(0xd1, 0xfa, 0xe5),  // 浅绿色
                ["WarningLightColor"] = Color.FromRgb(0xfe, 0xf3, 0xc7),  // 浅黄色
                ["DangerLightColor"] = Color.FromRgb(0xfe, 0xe2, 0xe2),   // 浅红色
                ["InfoLightColor"] = Color.FromRgb(0xd1, 0xf5, 0xff),     // 浅蓝色
            }
        };
    }
}
