# Git Setup Guide / Git配置指南

## 📋 Overview / 概述

This guide helps you configure Git to use CSP2's commit message template and contribution workflows.

本指南帮助你配置Git以使用CSP2的提交信息模板和贡献工作流。

---

## 🔧 Setup Commit Template / 配置提交模板

### Option 1: Local Configuration (Recommended) / 本地配置（推荐）

Configure the commit template for this repository only:

仅为此仓库配置提交模板：

```bash
cd /path/to/csp2
git config commit.template .gitmessage
```

### Option 2: Global Configuration / 全局配置

Configure the commit template for all your repositories:

为你所有的仓库配置提交模板：

```bash
git config --global commit.template ~/.gitmessage
cp .gitmessage ~/.gitmessage
```

### Verify Configuration / 验证配置

```bash
git config commit.template
```

---

## ✍️ Using the Commit Template / 使用提交模板

### Method 1: Command Line / 命令行

When you run `git commit` without `-m`, your configured editor will open with the template:

当你运行 `git commit` 而不带 `-m` 时，会打开配置的编辑器并显示模板：

```bash
git add .
git commit
# Your editor opens with the template pre-filled
```

### Method 2: With Message / 带消息提交

You can still use `-m` to commit directly:

你仍然可以使用 `-m` 直接提交：

```bash
git commit -m "feat(i18n): add Japanese language support"
```

### Method 3: Using VS Code / Git UI / 使用VS Code/Git图形界面

Most Git GUI tools will automatically use the template. In VS Code:

大多数Git图形工具会自动使用模板。在VS Code中：

1. Open Source Control panel / 打开源代码管理面板
2. Click the commit message box / 点击提交信息框
3. The template will appear / 模板会出现

---

## 📝 Commit Message Examples / 提交信息示例

### i18n Commits / 国际化提交

#### Adding a new language / 添加新语言

```
feat(i18n): add Japanese language support

- Create ja-JP.json with complete translations
- Add Japanese option to Settings page
- Update LocalizationHelper with ja-JP locale
- Add Japanese to language selection dropdown

Closes #42
```

#### Fixing translations / 修复翻译

```
fix(i18n): correct server management translations in zh-CN

- Fix typo in "ServerMgmt.InstallServer" key
- Update "ServerOps.DeleteConfirmTitle" wording
- Align button text with English version
```

#### Adding new translation keys / 添加新翻译键

```
feat(i18n): add plugin update notification messages

Add new translation keys for plugin update feature:
- PluginUpdate.Available
- PluginUpdate.Changelog
- PluginUpdate.SkipVersion

Updated in all locale files: en.json, zh-CN.json

Related to #56
```

### Other Commits / 其他提交

#### Feature / 新功能

```
feat(core): add automatic server backup functionality

- Implement backup scheduling service
- Add backup management UI
- Support multiple backup strategies
- Add backup restoration feature

Fixes #123
```

#### Bug Fix / 问题修复

```
fix(ui): resolve server list refresh issue

Server list was not updating after adding new server.
Fixed by forcing observable collection refresh.

Fixes #234
```

#### Documentation / 文档

```
docs: update installation guide with troubleshooting section

- Add common installation issues
- Add solutions for .NET runtime errors
- Add FAQ for Windows Defender warnings
```

---

## 🔍 Commit Message Validation / 提交信息验证

### Recommended Tools / 推荐工具

#### commitlint (Optional) / commitlint（可选）

You can optionally set up commitlint to validate commit messages:

你可以选择设置commitlint来验证提交信息：

```bash
npm install --save-dev @commitlint/{cli,config-conventional}

echo "module.exports = {extends: ['@commitlint/config-conventional']}" > commitlint.config.js
```

Add to `.husky/commit-msg`:

添加到 `.husky/commit-msg`：

```bash
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

npx --no-install commitlint --edit $1
```

---

## 🎯 Quick Reference / 快速参考

### Commit Types / 提交类型

| Type | When to Use | Example |
|------|------------|---------|
| `feat` | New feature | `feat(i18n): add Korean support` |
| `fix` | Bug fix | `fix(i18n): correct German translations` |
| `docs` | Documentation | `docs: update i18n guide` |
| `style` | Formatting | `style: format JSON locale files` |
| `refactor` | Code restructuring | `refactor(i18n): extract locale loader` |
| `perf` | Performance | `perf(i18n): cache loaded locales` |
| `test` | Tests | `test(i18n): add locale validation tests` |
| `chore` | Maintenance | `chore(i18n): update translations` |

### Common Scopes / 常用作用域

| Scope | Description | Example |
|-------|-------------|---------|
| `i18n` | Internationalization | `feat(i18n): add new language` |
| `core` | Core library | `fix(core): fix server manager bug` |
| `ui` | User interface | `feat(ui): add dark theme` |
| `provider` | Provider system | `feat(provider): add new framework` |
| `docs` | Documentation | `docs: update README` |
| `config` | Configuration | `chore(config): update build settings` |
| `deps` | Dependencies | `chore(deps): update packages` |
| `ci` | CI/CD | `ci: add GitHub Actions workflow` |

---

## 📚 Best Practices / 最佳实践

### ✅ DO / 应该

- Use English for commit messages / 使用英文编写提交信息
- Keep subject line under 50 characters / 主题行保持在50字符以内
- Use imperative mood ("add" not "added") / 使用祈使语气（"add"而非"added"）
- Reference issues when applicable / 在适用时引用issue
- Write meaningful commit messages / 编写有意义的提交信息
- Make atomic commits / 进行原子化提交

### ❌ DON'T / 不应该

- Don't commit without testing / 不要未测试就提交
- Don't mix multiple changes in one commit / 不要在一个提交中混合多个更改
- Don't use vague messages like "fix bug" / 不要使用模糊的信息如"fix bug"
- Don't capitalize the subject line / 不要大写主题行首字母
- Don't end subject line with a period / 不要在主题行末尾加句号

---

## 🔗 Related Resources / 相关资源

- [i18n Commit Guidelines](i18n-commit-guide.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Pull Request Template](../.github/PULL_REQUEST_TEMPLATE.md)
- [How to Write a Git Commit Message](https://chris.beams.io/posts/git-commit/)

---

## 🆘 Troubleshooting / 故障排除

### Template not showing / 模板未显示

**Problem:** The commit template doesn't appear when I run `git commit`.

**Solution:**

1. Verify the configuration:
   ```bash
   git config commit.template
   ```

2. Check if the template file exists:
   ```bash
   ls -la .gitmessage
   ```

3. Try reconfiguring:
   ```bash
   git config commit.template .gitmessage
   ```

### Editor issues / 编辑器问题

**Problem:** Git opens the wrong editor or an unfamiliar editor.

**Solution:**

Configure your preferred editor:

```bash
# VS Code
git config --global core.editor "code --wait"

# Vim
git config --global core.editor "vim"

# Notepad (Windows)
git config --global core.editor "notepad"

# Notepad++ (Windows)
git config --global core.editor "'C:/Program Files/Notepad++/notepad++.exe' -multiInst -notabbar -nosession -noPlugin"
```

---

## 📞 Need Help? / 需要帮助？

If you have questions about contributing or using these templates:

如果你对贡献或使用这些模板有疑问：

- Open an issue: [GitHub Issues](https://github.com/yichen11818/csp2/issues)
- Start a discussion: [GitHub Discussions](https://github.com/yichen11818/csp2/discussions)

---

<p align="center">
  Thank you for contributing to CSP2! 🎉<br>
  感谢为CSP2做出贡献！🎉
</p>

