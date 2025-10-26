# cs2servergui 功能分析与移植方案

> 分析 cs2servergui 项目的有价值功能，并规划移植到 CSP2

**创建时间**: 2025-10-26  
**状态**: 进行中

---

## 📋 项目对比

### cs2servergui (C++/Qt)
- **定位**: 轻量级本地服务器管理工具
- **技术栈**: C++, Qt 6.7, QML, Conan
- **特点**: 简洁、快速、专注于本地服务器

### CSP2 (C#/.NET)
- **定位**: 全功能服务器管理面板
- **技术栈**: C# .NET 8, WPF, MVVM
- **特点**: 插件管理、多服务器、扩展性强

---

## ✨ cs2servergui 有价值的功能

### 1. 🎮 RCON 客户端支持 ⭐⭐⭐⭐⭐

**功能描述**:
- 通过 RCON 协议远程连接服务器
- 支持发送命令到远程服务器
- 异步连接，不阻塞主线程

**当前状态**: CSP2 **缺失** 此功能

**技术实现** (cs2servergui):
```cpp
// 使用 rconpp 库
m_client = std::make_shared<rconpp::rcon_client>(
    ip, port, password);
m_client->send_data(cmd, 999, SERVERDATA_EXECCOMMAND);
```

**移植价值**: ⭐⭐⭐⭐⭐
- RCON 是 CS2 服务器管理的标准协议
- 支持远程管理（不仅限于本地服务器）
- 可以在服务器关闭时仍然连接其他服务器

**移植方案**:
1. 在 `CSP2.Core` 中创建 `IRCONClient` 接口
2. 实现 C# RCON 客户端 (使用 Socket/TcpClient)
3. 在 `LogConsoleViewModel` 中集成 RCON
4. UI 增加 RCON 连接配置（IP、端口、密码）

---

### 2. ⬆️⬇️ 命令历史记录导航 ⭐⭐⭐⭐⭐

**功能描述**:
- 按 ↑ 键显示上一条命令
- 按 ↓ 键显示下一条命令
- 类似 Linux Shell 的体验

**当前状态**: CSP2 **缺失** 此功能

**技术实现** (cs2servergui):
```cpp
class CommandHistory {
    QString getNewer();  // ↓ 键
    QString getOlder();  // ↑ 键
    void add(const QString& cmd);
private:
    qsizetype m_currentIndex;
    QList<QString> m_history;
};
```

**移植价值**: ⭐⭐⭐⭐⭐
- 极大提升用户体验
- 标准 Shell 操作习惯
- 实现简单，收益巨大

**移植方案**:
1. 创建 `CommandHistory` 类 (C#)
2. 在 `LogConsolePage.xaml.cs` 中监听 KeyDown 事件
3. 支持 Up/Down 键切换历史命令
4. 将历史持久化到配置文件

---

### 3. ⚡ 快捷命令 (Quick Commands) ⭐⭐⭐⭐

**功能描述**:
- 用户可添加常用命令按钮
- 一键执行，无需输入
- 支持动态添加/删除
- 自动保存到配置

**当前状态**: CSP2 **缺失** 此功能

**UI 示例** (cs2servergui):
```qml
[Execute] [mp_restartgame 1] [Delete]
[Execute] [sv_cheats 1]       [Delete]
[+Add quick command]
```

**移植价值**: ⭐⭐⭐⭐
- 常用命令一键执行
- 降低输入错误
- 提高管理效率

**移植方案**:
1. 在 `ServerConfig` 中添加 `QuickCommands: List<string>`
2. 在 `LogConsolePage` 中增加快捷命令区域
3. 使用 `ItemsControl` 动态显示命令按钮
4. 支持添加、删除、执行操作

---

### 4. 🗺️ Workshop 地图助手 ⭐⭐⭐⭐

**功能描述**:
- 专用输入框用于加载 Workshop 地图
- 支持输入地图 ID 或 URL
- 自动从 URL 中提取地图 ID
- 执行 `host_workshop_map <id>` 命令

**当前状态**: CSP2 **缺失** 此功能

**技术实现** (cs2servergui):
```cpp
void hostWorkshopMap(const QString& map) {
    // 支持:
    // 1. 直接输入 ID: 123456789
    // 2. Workshop URL: https://steamcommunity.com/workshop/filedetails/?id=123456789
    
    QString mapId = extractMapId(map);
    execCommand("host_workshop_map " + mapId);
    MapHistory::Add(mapId);  // 记录到历史
}
```

**移植价值**: ⭐⭐⭐⭐
- Workshop 地图是社区服务器的核心
- 简化地图切换流程
- 提升用户体验

**移植方案**:
1. 在 `LogConsolePage` 增加 "加载 Workshop 地图" 输入框
2. 实现 URL 解析逻辑
3. 自动发送 `host_workshop_map` 命令
4. (可选) 记录地图历史

---

### 5. 📜 地图历史记录 ⭐⭐⭐

**功能描述**:
- 记录加载过的 Workshop 地图
- 显示地图预览图
- 显示地图名称、作者、下载时间
- 通过 Steam API 获取地图信息

**当前状态**: CSP2 **缺失** 此功能

**技术实现** (cs2servergui):
```cpp
class MapHistory {
    void Add(const std::string& mapId);
    
    // 1. 调用 Steam API 获取地图信息
    // 2. 下载地图预览图
    // 3. 保存到 map_history.json
    // 4. 在 UI 中显示
};
```

**移植价值**: ⭐⭐⭐
- 方便管理已使用的地图
- 快速重新加载地图
- 展示历史记录

**移植方案**:
1. 创建 `MapHistory` 模型和服务
2. 集成 Steam Web API
3. 创建地图历史页面 (可选)
4. 在配置中保存地图历史

---

### 6. 🎨 主题切换 (Light/Dark) ⭐⭐

**功能描述**:
- 支持亮色和暗色主题
- 实时切换，无需重启

**当前状态**: CSP2 **已有** 类似功能 (Settings 页面)

**移植价值**: ⭐ (已存在)
- CSP2 已有设置页面
- 可参考 cs2servergui 的主题配色方案

---

### 7. 📏 UI 缩放设置 ⭐⭐

**功能描述**:
- 用户可调整界面缩放比例
- 适配不同分辨率和 DPI

**当前状态**: CSP2 **部分支持** (WPF 自动 DPI 缩放)

**移植价值**: ⭐⭐
- 可选实现，WPF 已有一定 DPI 支持
- 如需要，可在设置中添加缩放选项

---

### 8. 🔄 多命令历史类型 ⭐⭐⭐

**功能描述**:
- 不同输入框有独立的命令历史
  - 通用控制台命令历史
  - Exec 脚本命令历史
  - Workshop 地图历史

**当前状态**: CSP2 **缺失** 此功能

**移植价值**: ⭐⭐⭐
- 提升专业性
- 分类管理历史记录

**移植方案**:
1. 创建多个 `CommandHistory` 实例
2. 每个输入框绑定对应的历史

---

## 🚀 实施计划

### 优先级 P0 (必须实现)

1. **命令历史导航** ⬆️⬇️
   - 工作量: 2 小时
   - 收益: 极高
   - 无依赖

2. **RCON 客户端** 🎮
   - 工作量: 6-8 小时
   - 收益: 极高
   - 依赖: 需要实现 RCON 协议

3. **快捷命令** ⚡
   - 工作量: 4 小时
   - 收益: 高
   - 依赖: 无

### 优先级 P1 (应该实现)

4. **Workshop 地图助手** 🗺️
   - 工作量: 3 小时
   - 收益: 高
   - 依赖: 无

5. **多命令历史类型** 🔄
   - 工作量: 2 小时
   - 收益: 中
   - 依赖: 命令历史导航

### 优先级 P2 (可选实现)

6. **地图历史记录** 📜
   - 工作量: 6-8 小时
   - 收益: 中
   - 依赖: Steam API 集成

7. **UI 缩放设置** 📏
   - 工作量: 2 小时
   - 收益: 低
   - 依赖: 无

---

## 📝 技术实现细节

### RCON 协议实现

**RCON 协议结构** (Source Engine):
```
Packet = Size (4 bytes) + ID (4 bytes) + Type (4 bytes) + Body (N bytes) + Null (2 bytes)

Types:
- SERVERDATA_AUTH (3): 认证请求
- SERVERDATA_AUTH_RESPONSE (2): 认证响应
- SERVERDATA_EXECCOMMAND (2): 执行命令
- SERVERDATA_RESPONSE_VALUE (0): 命令响应
```

**C# 实现参考**:
```csharp
public class RCONClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    
    public async Task<bool> ConnectAsync(string host, int port, string password);
    public async Task<string> SendCommandAsync(string command);
    private async Task<RCONPacket> ReceivePacketAsync();
}
```

**已有 .NET RCON 库**:
- `CoreRCON` (推荐) - https://github.com/ScottKaye/CoreRCON
- `Rcon.Net` - https://github.com/Timeraa/Rcon.Net

**推荐方案**: 使用 `CoreRCON` 库，成熟稳定。

---

### 命令历史实现

```csharp
public class CommandHistory
{
    private List<string> _history = new();
    private int _currentIndex = -1;
    
    public void Add(string command)
    {
        // 不记录重复命令
        if (_history.Count > 0 && _history[^1] == command)
            return;
            
        _history.Add(command);
        _currentIndex = _history.Count;
    }
    
    public string? GetOlder()
    {
        if (_history.Count == 0) return null;
        
        if (_currentIndex > 0)
            _currentIndex--;
            
        return _history[_currentIndex];
    }
    
    public string? GetNewer()
    {
        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            return _history[_currentIndex];
        }
        
        _currentIndex = _history.Count;
        return string.Empty;
    }
    
    public void SaveToFile(string path)
    {
        File.WriteAllLines(path, _history);
    }
    
    public void LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            _history = File.ReadAllLines(path).ToList();
            _currentIndex = _history.Count;
        }
    }
}
```

---

### Workshop 地图 URL 解析

```csharp
public static class WorkshopMapHelper
{
    public static string ExtractMapId(string input)
    {
        // 已经是纯数字 ID
        if (long.TryParse(input, out _))
            return input;
        
        // 解析 URL
        // https://steamcommunity.com/sharedfiles/filedetails/?id=123456789
        var match = Regex.Match(input, @"[?&]id=(\d+)");
        if (match.Success)
            return match.Groups[1].Value;
        
        throw new ArgumentException("无法从输入中提取地图 ID");
    }
    
    public static string BuildHostCommand(string input)
    {
        var mapId = ExtractMapId(input);
        return $"host_workshop_map {mapId}";
    }
}
```

---

## 📊 对比总结

| 功能 | cs2servergui | CSP2 | 移植优先级 |
|------|--------------|------|-----------|
| 服务器启动/停止 | ✅ | ✅ | - |
| 日志查看 | ✅ | ✅ | - |
| 命令发送 | ✅ | ✅ | - |
| RCON 支持 | ✅ | ❌ | P0 |
| 命令历史 ↑↓ | ✅ | ❌ | P0 |
| 快捷命令 | ✅ | ❌ | P0 |
| Workshop 助手 | ✅ | ❌ | P1 |
| 地图历史 | ✅ | ❌ | P2 |
| 插件管理 | ❌ | ✅ | - |
| 多服务器 | 有限 | ✅ | - |
| 主题切换 | ✅ | ✅ | - |

---

## 🎯 移植后的效果

完成所有 P0 和 P1 功能后，CSP2 的日志控制台将具备:

✅ 服务器日志实时显示  
✅ 命令输入与发送  
✅ **命令历史导航 (↑↓)**  
✅ **RCON 远程连接**  
✅ **快捷命令一键执行**  
✅ **Workshop 地图快速加载**  
✅ 日志导出与复制  
✅ 多服务器切换  

这将使 CSP2 在保持强大插件管理能力的同时，拥有 cs2servergui 的便捷操作体验。

---

## 📖 参考资料

- cs2servergui 源码: `cs2servergui/src/`
- RCON 协议文档: https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
- Steam Web API: https://steamcommunity.com/dev
- CoreRCON 库: https://github.com/ScottKaye/CoreRCON

---

**文档结束**

