# i18n Commit Guidelines

## Overview

This document defines the commit message standards for internationalization (i18n) related changes in CSP2 project, following GitHub and Conventional Commits best practices.

## Commit Message Format

```
<type>(i18n): <subject>

[optional body]

[optional footer]
```

### Type

Use the following types for i18n-related commits:

| Type | Description | Example |
|------|-------------|---------|
| `feat` | Add a new language or major i18n feature | `feat(i18n): add Japanese language support` |
| `fix` | Fix translation errors or i18n bugs | `fix(i18n): correct server status translations in zh-CN` |
| `docs` | Update i18n documentation | `docs(i18n): update language addition guide` |
| `refactor` | Refactor i18n code structure | `refactor(i18n): reorganize locale file structure` |
| `perf` | Performance improvements for i18n | `perf(i18n): optimize locale loading mechanism` |
| `chore` | Maintenance tasks for i18n | `chore(i18n): update translation keys format` |

### Scope

The scope should always be `i18n` for internationalization-related commits. For more specific changes, you can use sub-scopes:

- `i18n:locale` - Changes to locale files (en.json, zh-CN.json, etc.)
- `i18n:service` - Changes to localization service
- `i18n:converter` - Changes to localization converters
- `i18n:docs` - Changes to i18n documentation

### Subject

The subject should:
- Be written in English
- Use imperative mood ("add" not "added" or "adds")
- Not capitalize the first letter
- Not end with a period
- Clearly describe what changed

## Examples

### Adding a New Language

```
feat(i18n): add Japanese language support

- Add ja-JP.json locale file
- Add Japanese option to settings page
- Update LocalizationHelper with Japanese locale code

Closes #42
```

### Fixing Translation Errors

```
fix(i18n): correct server management translations in zh-CN

- Fix typo in "ServerMgmt.InstallServer" key
- Update "ServerOps.DeleteConfirmTitle" wording for clarity
- Align button text with English version
```

### Adding New Translation Keys

```
feat(i18n): add plugin update notification messages

- Add "PluginUpdate.Available" key to all locales
- Add "PluginUpdate.Changelog" key to all locales
- Add "PluginUpdate.SkipVersion" key to all locales

Related to #56
```

### Updating Existing Translations

```
chore(i18n): improve consistency in button labels

- Standardize use of emoji in action buttons
- Align terminology across all pages
- Update tooltip descriptions for clarity

Affects: en.json, zh-CN.json
```

### Refactoring i18n Code

```
refactor(i18n): extract localization logic to service layer

- Move LocalizationConverter logic to JsonLocalizationService
- Add caching mechanism for loaded locale files
- Update ViewModels to use new service methods

Breaking change: LocalizationConverter constructor signature changed
```

## Best Practices

### 1. Atomic Commits

Each commit should represent a single logical change:

✅ **Good:**
```
feat(i18n): add French language support
fix(i18n): correct German error messages
```

❌ **Bad:**
```
feat(i18n): add French and fix German translations
```

### 2. Complete Language Updates

When adding new keys, update all language files in the same commit:

✅ **Good:**
```
feat(i18n): add server backup messages

- Add "Backup.InProgress" to en.json, zh-CN.json
- Add "Backup.Completed" to en.json, zh-CN.json
- Add "Backup.Failed" to en.json, zh-CN.json
```

❌ **Bad:**
```
feat(i18n): add server backup messages to English
(Missing other languages)
```

### 3. Reference Issues

Always reference related issues or pull requests:

```
fix(i18n): resolve text overflow in long translations

Fixes #123
```

### 4. Breaking Changes

Clearly mark breaking changes in i18n:

```
refactor(i18n): change locale key naming convention

BREAKING CHANGE: All locale keys now use dot notation instead of underscore.
Migration required for custom locale files.

Before: "Server_Management_Title"
After: "ServerMgmt.Title"
```

### 5. Language Code Format

Use the correct language code format in commit messages:

| Language | Code | ✅ Correct | ❌ Incorrect |
|----------|------|-----------|-------------|
| English | en | `en.json` | `english.json`, `EN.json` |
| Simplified Chinese | zh-CN | `zh-CN.json` | `zh-cn.json`, `chinese.json` |
| Japanese | ja-JP | `ja-JP.json` | `jp.json`, `japanese.json` |
| Korean | ko-KR | `ko-KR.json` | `kr.json`, `korean.json` |
| French | fr-FR | `fr-FR.json` | `fr.json`, `french.json` |
| German | de-DE | `de-DE.json` | `de.json`, `german.json` |

## Commit Categories

### New Language Addition

When adding a complete new language:

```
feat(i18n): add [Language] language support

- Create [locale-code].json with complete translations
- Add language option to Settings page
- Update LocalizationHelper
- Add language to README.md

Translators: @username
Reviewers: @native-speaker-username
```

### Translation Updates

For routine translation updates:

```
chore(i18n): update [locale-code] translations

- Improve [section] descriptions
- Fix grammatical errors in [section]
- Align with latest English version
```

### i18n Infrastructure

For changes to i18n system itself:

```
feat(i18n): implement dynamic locale switching without restart

- Add real-time locale change notification
- Update all ViewModels to observe locale changes
- Implement resource dictionary merging strategy
```

## Multiple Commit Types

If a change affects multiple areas, split into separate commits:

### Example Workflow:

```bash
# Commit 1: Add infrastructure
git commit -m "feat(i18n): add locale hot-reload service"

# Commit 2: Update existing locales
git commit -m "chore(i18n): update en.json and zh-CN.json structure"

# Commit 3: Update documentation
git commit -m "docs(i18n): document hot-reload feature"
```

## Pull Request Title Format

For i18n-related pull requests:

```
[i18n] Brief description of the change
```

Examples:
- `[i18n] Add Russian language support`
- `[i18n] Fix Spanish translation errors`
- `[i18n] Refactor locale loading mechanism`

## Labels

Use the following labels for i18n-related PRs and issues:

- `i18n` - General internationalization
- `translation` - Translation updates
- `new-language` - New language addition
- `translation-error` - Translation bugs
- `i18n-infrastructure` - i18n system changes

## Review Checklist

Before committing i18n changes, ensure:

- [ ] All language files are updated (if adding keys)
- [ ] JSON syntax is valid (no trailing commas, proper escaping)
- [ ] Keys follow the project naming convention
- [ ] Strings use proper placeholders (`{0}`, `{1}`, etc.)
- [ ] Commit message follows this guide
- [ ] No hardcoded strings remain in code
- [ ] UI tested with both short and long translations

## Resources

- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Flow](https://guides.github.com/introduction/flow/)
- CSP2 Project Structure: `docs/02-项目结构说明.md`
- CSP2 Locale Files: `src/CSP2.Desktop/Resources/Locales/`

## Version

**Document Version:** 1.0  
**Last Updated:** 2025-10-25  
**Maintainer:** CSP2 Team

---

## Quick Reference

### Most Common Commit Formats:

```bash
# Add new language
feat(i18n): add [language] support

# Fix translation
fix(i18n): correct [section] in [locale]

# Update translations
chore(i18n): update [locale] translations

# Add new keys
feat(i18n): add [feature] translation keys

# Refactor structure
refactor(i18n): reorganize locale file structure
```

---

<p align="center">
  For questions or suggestions, please open an issue with the <code>i18n</code> label.
</p>

