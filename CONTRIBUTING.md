# Contributing to CSP2 / 为CSP2做贡献

[English](#english) | [简体中文](#简体中文)

---

## English

Thank you for your interest in contributing to CSP2! This document provides guidelines and instructions for contributing to the project.

### 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Translation / i18n Contributions](#translation--i18n-contributions)

### 🤝 Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

### 🚀 Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/csp2.git
   cd csp2
   ```
3. **Set up the commit template**:
   ```bash
   git config commit.template .gitmessage
   ```
4. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

### 💡 How to Contribute

There are many ways to contribute to CSP2:

- 🐛 **Report bugs** - Submit detailed bug reports
- ✨ **Suggest features** - Propose new features or improvements
- 🌍 **Translate** - Help translate the UI to other languages
- 📝 **Improve documentation** - Fix typos, add examples, clarify explanations
- 💻 **Submit code** - Fix bugs or implement new features
- 🧪 **Write tests** - Improve test coverage
- 🎨 **Design** - Improve UI/UX

### 🛠️ Development Setup

#### Prerequisites

- Windows 10/11 (64-bit)
- .NET 8.0 SDK or later
- Visual Studio 2022 or Rider (recommended)
- Git

#### Build Instructions

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src/CSP2.Desktop/CSP2.Desktop.csproj
```

### 📝 Commit Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.

#### Commit Message Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

#### Quick Reference

**Common commit types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**

```bash
feat(i18n): add Japanese language support
fix(core): resolve server startup crash
docs: update installation instructions
chore(deps): update NuGet packages
```

**Detailed guidelines:**
- [Git Setup Guide](docs/git-setup-guide.md) - How to configure the commit template
- [i18n Commit Guidelines](docs/i18n-commit-guide.md) - Specific guidelines for translation contributions

### 🔄 Pull Request Process

1. **Ensure your code builds** without errors
2. **Test your changes** thoroughly
3. **Update documentation** if needed
4. **Follow the commit message format**
5. **Fill out the PR template** completely
6. **Link related issues** using keywords like "Fixes #123"
7. **Request review** from maintainers

#### PR Checklist

- [ ] Code follows project style guidelines
- [ ] All tests pass
- [ ] Documentation is updated
- [ ] Commit messages follow the convention
- [ ] PR template is filled out
- [ ] No merge conflicts

### 🌍 Translation / i18n Contributions

We welcome translations to make CSP2 accessible to more users!

#### Quick Start for Translators

1. Check if your language already exists in `src/CSP2.Desktop/Resources/Locales/`
2. If adding a new language:
   - Copy `en.json` to `[your-locale].json` (e.g., `ja-JP.json`)
   - Translate all strings
   - Keep the JSON structure and keys unchanged
   - Use proper placeholders (`{0}`, `{1}`, etc.)

3. For translation fixes:
   - Edit the relevant locale file
   - Test your changes in the application

4. Commit using the i18n template:
   ```bash
   feat(i18n): add Japanese language support
   
   - Create ja-JP.json with complete translations
   - Add Japanese to language selection
   - Update documentation
   
   Closes #42
   ```

**Detailed i18n guidelines:** [i18n Commit Guidelines](docs/i18n-commit-guide.md)

#### Translation Issues

Use our translation issue template: [Create Translation Issue](https://github.com/yichen11818/csp2/issues/new?template=translation.md)

### 📚 Additional Resources

- [Technical Design Document](docs/01-技术设计文档.md)
- [Project Structure](docs/02-项目结构说明.md)
- [Development Roadmap](docs/03-开发路线图.md)
- [Git Setup Guide](docs/git-setup-guide.md)
- [i18n Commit Guidelines](docs/i18n-commit-guide.md)

### 🆘 Need Help?

- 💬 [GitHub Discussions](https://github.com/yichen11818/csp2/discussions) - Ask questions, share ideas
- 🐛 [GitHub Issues](https://github.com/yichen11818/csp2/issues) - Report bugs, request features
- 📖 [Documentation](docs/) - Read the docs

### 🎉 Thank You!

Every contribution, no matter how small, is valuable. Thank you for helping make CSP2 better!

---

## 简体中文

感谢你有兴趣为CSP2做贡献！本文档提供了为项目做贡献的指南和说明。

### 📋 目录

- [行为准则](#行为准则)
- [开始贡献](#开始贡献)
- [如何贡献](#如何贡献)
- [开发环境配置](#开发环境配置)
- [提交规范](#提交规范)
- [Pull Request流程](#pull-request流程)
- [翻译/国际化贡献](#翻译国际化贡献)

### 🤝 行为准则

参与此项目即表示你同意为所有贡献者维护一个尊重和包容的环境。

### 🚀 开始贡献

1. 在GitHub上 **Fork此仓库**
2. **克隆你的fork** 到本地：
   ```bash
   git clone https://github.com/YOUR-USERNAME/csp2.git
   cd csp2
   ```
3. **设置提交模板**：
   ```bash
   git config commit.template .gitmessage
   ```
4. 为你的更改 **创建分支**：
   ```bash
   git checkout -b feature/your-feature-name
   ```

### 💡 如何贡献

有很多方式可以为CSP2做贡献：

- 🐛 **报告bug** - 提交详细的bug报告
- ✨ **建议功能** - 提出新功能或改进建议
- 🌍 **翻译** - 帮助将UI翻译成其他语言
- 📝 **改进文档** - 修正错别字、添加示例、澄清说明
- 💻 **提交代码** - 修复bug或实现新功能
- 🧪 **编写测试** - 提高测试覆盖率
- 🎨 **设计** - 改进UI/UX

### 🛠️ 开发环境配置

#### 前置要求

- Windows 10/11 (64位)
- .NET 8.0 SDK或更高版本
- Visual Studio 2022或Rider（推荐）
- Git

#### 构建说明

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用程序
dotnet run --project src/CSP2.Desktop/CSP2.Desktop.csproj
```

### 📝 提交规范

我们遵循 [Conventional Commits](https://www.conventionalcommits.org/) 规范。

#### 提交信息格式

```
<type>(<scope>): <subject>

[可选的正文]

[可选的页脚]
```

#### 快速参考

**常见提交类型：**
- `feat`: 新功能
- `fix`: 问题修复
- `docs`: 文档更改
- `style`: 代码样式更改（格式化等）
- `refactor`: 代码重构
- `perf`: 性能改进
- `test`: 添加或更新测试
- `chore`: 维护任务

**示例：**

```bash
feat(i18n): add Japanese language support
fix(core): resolve server startup crash
docs: update installation instructions
chore(deps): update NuGet packages
```

**详细指南：**
- [Git配置指南](docs/git-setup-guide.md) - 如何配置提交模板
- [i18n提交规范](docs/i18n-commit-guide.md) - 翻译贡献的具体指南

### 🔄 Pull Request流程

1. **确保你的代码** 无错误地构建
2. **彻底测试** 你的更改
3. **更新文档** （如需要）
4. **遵循提交信息格式**
5. **完整填写PR模板**
6. **关联相关issue**，使用"Fixes #123"等关键词
7. **请求维护者审查**

#### PR检查清单

- [ ] 代码遵循项目样式指南
- [ ] 所有测试通过
- [ ] 文档已更新
- [ ] 提交信息遵循规范
- [ ] PR模板已填写
- [ ] 无合并冲突

### 🌍 翻译/国际化贡献

我们欢迎翻译，让更多用户能使用CSP2！

#### 翻译者快速开始

1. 检查你的语言是否已存在于 `src/CSP2.Desktop/Resources/Locales/`
2. 如果添加新语言：
   - 将 `en.json` 复制为 `[你的语言代码].json`（如 `ja-JP.json`）
   - 翻译所有字符串
   - 保持JSON结构和键名不变
   - 使用正确的占位符（`{0}`, `{1}` 等）

3. 修复翻译：
   - 编辑相关的语言文件
   - 在应用程序中测试你的更改

4. 使用i18n模板提交：
   ```bash
   feat(i18n): add Japanese language support
   
   - Create ja-JP.json with complete translations
   - Add Japanese to language selection
   - Update documentation
   
   Closes #42
   ```

**详细i18n指南：** [i18n提交规范](docs/i18n-commit-guide.md)

#### 翻译问题

使用我们的翻译issue模板：[创建翻译Issue](https://github.com/yichen11818/csp2/issues/new?template=translation.md)

### 📚 其他资源

- [技术设计文档](docs/01-技术设计文档.md)
- [项目结构说明](docs/02-项目结构说明.md)
- [开发路线图](docs/03-开发路线图.md)
- [Git配置指南](docs/git-setup-guide.md)
- [i18n提交规范](docs/i18n-commit-guide.md)

### 🆘 需要帮助？

- 💬 [GitHub Discussions](https://github.com/yichen11818/csp2/discussions) - 提问、分享想法
- 🐛 [GitHub Issues](https://github.com/yichen11818/csp2/issues) - 报告bug、请求功能
- 📖 [文档](docs/) - 阅读文档

### 🎉 感谢！

每一个贡献，无论多小，都是宝贵的。感谢你帮助改进CSP2！

---

<p align="center">
  <strong>Happy Contributing! 祝贡献愉快！</strong> 🚀
</p>

