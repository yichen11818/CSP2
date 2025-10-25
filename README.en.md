# CSP2 - Counter-Strike 2 Server Panel

> 🎮 Open-source CS2 server management panel that makes server management simple and efficient

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-green)](https://www.microsoft.com/windows)

**English** | [简体中文](README.md)

---

## ✨ Introduction

CSP2 is an open-source desktop management tool for CS2 server administrators, inspired by Minecraft's PCL2 launcher. It provides an intuitive graphical interface that makes server management, plugin installation, log viewing, and other operations simple and quick.

### 🌟 Core Features

- ⚡ **One-Click Launch**: Quickly start and manage CS2 dedicated servers
- 📊 **Real-time Monitoring**: View server logs and status in real-time
- 🔌 **Plugin Management**: Browse, install, and update plugins with support for multiple frameworks
- 🖥️ **Multi-Server**: Manage multiple server instances simultaneously
- 🔧 **Extensible**: Based on Provider mechanism, community can contribute new features
- 🎨 **Modern UI**: Clean and beautiful user interface

### 👥 Target Audience

- CS2 community server administrators
- Server operators
- Plugin developers
- Players who want to quickly set up a CS2 server

---

## 🖼️ Preview

> Project is still under development

---

## 🚀 Quick Start

### System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **.NET Runtime**: .NET 8.0 or higher
- **Disk Space**: At least 100MB
- **Memory**: Recommended 4GB or more

### Installation

#### Option 1: Download Pre-built Version (Recommended)

1. Go to [Releases](https://github.com/yichen11818/csp2/releases) page
2. Download the latest version of `CSP2-vX.X.X-Windows.zip`
3. Extract to any directory
4. Run `CSP2.Desktop.exe`

#### Option 2: Build from Source

**Manual Build**:

```bash
# 1. Clone repository
git clone https://github.com/yichen11818/csp2.git
cd csp2

# 2. Restore dependencies
dotnet restore

# 3. Build project
dotnet build --configuration Release

# 4. Run
cd src/CSP2.Desktop/bin/Release/net8.0-windows
./CSP2.Desktop.exe
```

---

## 📖 User Guide

### Basic Workflow

1. **Add Server**
   - Select an existing CS2 installation path
   - Or download dedicated server via SteamCMD (in development)

2. **Install Plugin Framework**
   - One-click install Metamod
   - One-click install CounterStrikeSharp

3. **Browse Plugin Market**
   - Search and install required plugins
   - Manage installed plugins

4. **Start Server**
   - Configure server parameters
   - Start and view logs in real-time

### Detailed Documentation

- 📚 [User Manual](docs/用户手册.md) (Coming soon)
- 🔧 [Developer Documentation](docs/01-技术设计文档.md)
- 🎓 [WPF Quick Start](docs/04-WPF快速入门.md)

---

## 🔌 Supported Plugin Frameworks

| Framework | Status | Description |
|-----------|--------|-------------|
| Metamod:Source | ✅ Supported | CS2 plugin loader foundation |
| CounterStrikeSharp | ✅ Supported | C# plugin development framework |
| Swiftly | 🚧 Planned | Emerging plugin framework |

*Community can add new framework support by implementing the `IFrameworkProvider` interface*

---

## 🤝 Contributing

We welcome all forms of contributions! Whether it's reporting bugs, suggesting features, improving documentation, or submitting code.

### How to Contribute

1. **Report Bugs**: Submit in [Issues](https://github.com/yichen11818/csp2/issues)
2. **Feature Requests**: Discuss in [Discussions](https://github.com/yichen11818/csp2/discussions)
3. **Submit Code**: Fork the project, create a Pull Request
4. **Translate**: Help translate the interface to other languages



### Developer Guide

Check out these documents to start contributing:

- [Technical Design Document](docs/01-技术设计文档.md)
- [Project Structure](docs/02-项目结构说明.md)
- [Development Roadmap](docs/03-开发路线图.md)


---

## 📁 Project Structure

```
csp2/
├── src/
│   ├── CSP2.Core/              # Core library (interfaces and services)
│   ├── CSP2.Providers/         # Official Provider implementations
│   ├── CSP2.Desktop/           # WPF desktop application
│   └── CSP2.SDK/               # Extension development SDK
├── tests/                      # Unit tests
├── docs/                       # Documentation
├── .github/                    # GitHub configuration
└── README.md
```

---

## 🛠️ Tech Stack

- **Frontend**: WPF (.NET 8.0)
- **Architecture**: MVVM (CommunityToolkit.Mvvm)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog
- **HTTP**: HttpClient + Polly
- **JSON**: System.Text.Json

Future plans to migrate to **Avalonia UI** for cross-platform support.

---

## ❓ FAQ

### Q: Which operating systems are supported?
A: Current version only supports Windows. Linux support will be available in v2.0.

### Q: Where does the plugin data come from?
A: From our maintained [plugin repository](https://github.com/yichen11818/csp2-plugins) (planned)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

```
MIT License

Copyright (c) 2025 CSP2 Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
...
```

---

## 📧 Contact

- **Issues**: [GitHub Issues](https://github.com/yichen11818/csp2/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yichen11818/csp2/discussions)
- **Email**: your.email@example.com (optional)

---

## ⭐ Star History

If this project helps you, please give us a Star ⭐!

[![Star History Chart](https://api.star-history.com/svg?repos=yichen11818/csp2&type=Date)](https://star-history.com/#yichen11818/csp2&Date)

---

<p align="center">
  Made with ❤️ by CSP2 Community
</p>

