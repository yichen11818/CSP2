using CSP2.Core.Models;
using System.Text.RegularExpressions;

namespace CSP2.Core.Utilities;

/// <summary>
/// 启动参数构建器（用于服务器启动和UI预览）
/// </summary>
public static class LaunchArgumentsBuilder
{
    /// <summary>
    /// 构建服务器启动参数
    /// </summary>
    public static string BuildStartupArguments(ServerConfig config)
    {
        var args = new List<string>
        {
            // ========== 核心必需参数 ==========
            "-dedicated",
            "-norestart",
            "-console",  // 默认总是启用控制台
            $"-ip {config.IpAddress}",
            $"-port {config.Port}",
            $"-maxplayers {config.MaxPlayers}",
            $"-tickrate {config.TickRate}",
            $"+game_type {config.GameType}",
            $"+game_mode {config.GameMode}",
            $"+mapgroup {config.MapGroup}",
            $"+map {config.Map}"
        };

        // ========== 常用选项 ==========
        
        // BOT 设置
        if (config.DisableBots)
        {
            args.Add("+bot_quota 0");
        }
        
        // Insecure 模式（用于自定义地图）
        if (config.InsecureMode)
        {
            args.Add("-insecure");
        }
        
        // 局域网模式
        if (config.IsLanMode)
        {
            args.Add("+sv_lan 1");
        }
        else
        {
            args.Add("+sv_lan 0");
        }
        
        // 应用内控制台模式
        if (config.OpenConsoleInApp)
        {
            // 启用调试日志，确保日志输出到stdout（被应用捕获）
            args.Add("-condebug");
        }

        // ========== 高级配置（可选，建议使用 autoexec.cfg）==========
        
        // 服务器名称（如果配置了）
        if (!string.IsNullOrEmpty(config.ServerName))
        {
            args.Add($"+hostname \"{config.ServerName}\"");
        }

        // 服务器密码（如果配置了）
        if (!string.IsNullOrEmpty(config.ServerPassword))
        {
            args.Add($"+sv_password \"{config.ServerPassword}\"");
        }

        // RCON密码（如果配置了）
        if (!string.IsNullOrEmpty(config.RconPassword))
        {
            args.Add($"+rcon_password \"{config.RconPassword}\"");
        }

        // Steam令牌（如果配置了）
        if (!string.IsNullOrEmpty(config.SteamToken))
        {
            args.Add($"+sv_setsteamaccount {config.SteamToken}");
        }

        // ========== 日志设置（默认启用）==========
        args.Add("+log on");
        args.Add("+sv_logfile 1");
        args.Add("+mp_logdetail 3");
        args.Add("+sv_logecho 1");

        // ========== 自定义参数（用户完全控制）==========
        
        // 自定义参数字符串（新方式）
        if (!string.IsNullOrWhiteSpace(config.CustomParameters))
        {
            args.Add(config.CustomParameters.Trim());
        }

        return string.Join(" ", args);
    }

    /// <summary>
    /// 构建启动参数列表（用于UI显示）
    /// </summary>
    public static List<string> BuildStartupArgumentsList(ServerConfig config)
    {
        var args = new List<string>
        {
            // ========== 核心必需参数 ==========
            "-dedicated",
            "-norestart",
            "-console",
            $"-ip {config.IpAddress}",
            $"-port {config.Port}",
            $"-maxplayers {config.MaxPlayers}",
            $"-tickrate {config.TickRate}",
            $"+game_type {config.GameType}",
            $"+game_mode {config.GameMode}",
            $"+mapgroup {config.MapGroup}",
            $"+map {config.Map}"
        };

        // ========== 常用选项 ==========
        
        if (config.DisableBots)
        {
            args.Add("+bot_quota 0");
        }
        
        if (config.InsecureMode)
        {
            args.Add("-insecure");
        }
        
        if (config.IsLanMode)
        {
            args.Add("+sv_lan 1");
        }
        else
        {
            args.Add("+sv_lan 0");
        }
        
        if (config.OpenConsoleInApp)
        {
            args.Add("-condebug");
        }

        // ========== 高级配置 ==========
        
        if (!string.IsNullOrEmpty(config.ServerName))
        {
            args.Add($"+hostname \"{config.ServerName}\"");
        }

        if (!string.IsNullOrEmpty(config.ServerPassword))
        {
            args.Add($"+sv_password \"{config.ServerPassword}\"");
        }

        if (!string.IsNullOrEmpty(config.RconPassword))
        {
            args.Add($"+rcon_password \"{config.RconPassword}\"");
        }

        if (!string.IsNullOrEmpty(config.SteamToken))
        {
            args.Add($"+sv_setsteamaccount {config.SteamToken}");
        }

        // ========== 日志设置 ==========
        args.Add("+log on");
        args.Add("+sv_logfile 1");
        args.Add("+mp_logdetail 3");
        args.Add("+sv_logecho 1");

        // ========== 自定义参数 ==========
        
        if (!string.IsNullOrWhiteSpace(config.CustomParameters))
        {
            // 将自定义参数拆分为单独的项目
            var customArgs = config.CustomParameters.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            args.AddRange(customArgs);
        }

        return args;
    }

    /// <summary>
    /// 从启动参数字符串解析配置
    /// </summary>
    public static void ParseArgumentsToConfig(string arguments, ServerConfig config)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return;

        var args = arguments.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].Trim();
            
            // 跳过固定参数
            if (arg == "-dedicated" || arg == "-norestart" || arg == "-console" || arg == "-condebug")
                continue;

            // 解析 -参数 值 格式
            if (arg.StartsWith("-"))
            {
                if (i + 1 < args.Length)
                {
                    var value = args[i + 1];
                    switch (arg)
                    {
                        case "-ip":
                            config.IpAddress = value;
                            i++;
                            break;
                        case "-port":
                            if (int.TryParse(value, out int port))
                                config.Port = port;
                            i++;
                            break;
                        case "-maxplayers":
                            if (int.TryParse(value, out int maxPlayers))
                                config.MaxPlayers = maxPlayers;
                            i++;
                            break;
                        case "-tickrate":
                            if (int.TryParse(value, out int tickRate))
                                config.TickRate = tickRate;
                            i++;
                            break;
                        case "-insecure":
                            config.InsecureMode = true;
                            break;
                    }
                }
            }
            // 解析 +参数 值 格式
            else if (arg.StartsWith("+"))
            {
                if (i + 1 < args.Length)
                {
                    var value = args[i + 1];
                    
                    // 处理带引号的值
                    if (value.StartsWith("\""))
                    {
                        var quotedValue = value;
                        // 查找结束引号
                        while (!value.EndsWith("\"") && i + 2 < args.Length)
                        {
                            i++;
                            value = args[i + 1];
                            quotedValue += " " + value;
                        }
                        value = quotedValue.Trim('"');
                    }
                    
                    switch (arg)
                    {
                        case "+game_type":
                            if (int.TryParse(value, out int gameType))
                                config.GameType = gameType;
                            i++;
                            break;
                        case "+game_mode":
                            if (int.TryParse(value, out int gameMode))
                                config.GameMode = gameMode;
                            i++;
                            break;
                        case "+map":
                            config.Map = value;
                            i++;
                            break;
                        case "+bot_quota":
                            if (value == "0")
                                config.DisableBots = true;
                            i++;
                            break;
                        case "+sv_lan":
                            config.IsLanMode = value == "1";
                            i++;
                            break;
                        case "+hostname":
                            config.ServerName = value;
                            i++;
                            break;
                        case "+sv_password":
                            config.ServerPassword = value;
                            i++;
                            break;
                        case "+rcon_password":
                            config.RconPassword = value;
                            i++;
                            break;
                        case "+sv_setsteamaccount":
                            config.SteamToken = value;
                            i++;
                            break;
                        // 忽略日志相关参数（总是启用）
                        case "+log":
                        case "+sv_logfile":
                        case "+mp_logdetail":
                        case "+sv_logecho":
                            i++;
                            break;
                    }
                }
            }
        }
    }
}

