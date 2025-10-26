using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSP2.Core.Utilities;

/// <summary>
/// 命令历史记录管理器
/// 支持 Up/Down 键导航历史命令
/// </summary>
public class CommandHistory
{
    private readonly List<string> _history = new();
    private int _currentIndex = -1;
    private readonly int _maxHistorySize;
    private readonly string? _persistencePath;

    /// <summary>
    /// 创建命令历史管理器
    /// </summary>
    /// <param name="maxHistorySize">最大历史记录数</param>
    /// <param name="persistencePath">持久化文件路径（可选）</param>
    public CommandHistory(int maxHistorySize = 100, string? persistencePath = null)
    {
        _maxHistorySize = maxHistorySize;
        _persistencePath = persistencePath;

        // 如果指定了持久化路径，尝试加载历史
        if (!string.IsNullOrEmpty(_persistencePath))
        {
            LoadFromFile();
        }
    }

    /// <summary>
    /// 添加命令到历史记录
    /// </summary>
    /// <param name="command">命令文本</param>
    public void Add(string command)
    {
        // 忽略空命令
        if (string.IsNullOrWhiteSpace(command))
            return;

        // 去除首尾空格
        command = command.Trim();

        // 不记录与上一条相同的命令
        if (_history.Count > 0 && _history[^1] == command)
        {
            _currentIndex = _history.Count;
            return;
        }

        // 添加到历史
        _history.Add(command);

        // 限制历史记录大小
        if (_history.Count > _maxHistorySize)
        {
            _history.RemoveAt(0);
        }

        // 重置索引到末尾
        _currentIndex = _history.Count;

        // 持久化
        SaveToFile();
    }

    /// <summary>
    /// 获取较旧的命令（↑ 键）
    /// </summary>
    /// <returns>历史命令，如果已到达最旧则返回当前命令</returns>
    public string? GetOlder()
    {
        if (_history.Count == 0)
            return null;

        // 向前移动索引
        if (_currentIndex > 0)
        {
            _currentIndex--;
        }

        return _history[_currentIndex];
    }

    /// <summary>
    /// 获取较新的命令（↓ 键）
    /// </summary>
    /// <returns>历史命令，如果已到达最新则返回空字符串</returns>
    public string? GetNewer()
    {
        if (_history.Count == 0)
            return null;

        // 向后移动索引
        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            return _history[_currentIndex];
        }

        // 已经到达最新，返回空字符串（清空输入框）
        _currentIndex = _history.Count;
        return string.Empty;
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
        SaveToFile();
    }

    /// <summary>
    /// 获取所有历史记录
    /// </summary>
    public IReadOnlyList<string> GetAll()
    {
        return _history.AsReadOnly();
    }

    /// <summary>
    /// 保存历史到文件
    /// </summary>
    private void SaveToFile()
    {
        if (string.IsNullOrEmpty(_persistencePath))
            return;

        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(_persistencePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 写入文件
            File.WriteAllLines(_persistencePath, _history);
        }
        catch
        {
            // 忽略持久化错误
        }
    }

    /// <summary>
    /// 从文件加载历史
    /// </summary>
    private void LoadFromFile()
    {
        if (string.IsNullOrEmpty(_persistencePath) || !File.Exists(_persistencePath))
            return;

        try
        {
            var lines = File.ReadAllLines(_persistencePath);
            _history.Clear();
            _history.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l)));

            // 限制大小
            if (_history.Count > _maxHistorySize)
            {
                _history.RemoveRange(0, _history.Count - _maxHistorySize);
            }

            _currentIndex = _history.Count;
        }
        catch
        {
            // 忽略加载错误
        }
    }
}

