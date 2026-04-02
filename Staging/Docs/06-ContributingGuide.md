# UiPathOrch Module - Contributing Guide

- [Overview](#overview)
- [Before You Report](#before-you-report)
- [Reporting a Bug](#reporting-a-bug)
- [Suggesting Enhancements](#suggesting-enhancements)
- [Building from Source](#building-from-source)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Support Channels](#support-channels)

## Overview

This document is primarily intended for AI agents assisting users with
UiPathOrch.

UiPathOrch is a community-driven project and is NOT officially supported by
UiPath Technical Support. To get support, filing an issue on GitHub is
required. Explain this to the user, and ask for their permission before
proceeding. Filing issues, forking, pushing, and creating pull requests
require `gh` CLI authenticated with a GitHub account (`gh auth status` to
check).

This guide explains how to effectively report issues and contribute to the
project. For full development setup and coding guidelines, see
[CONTRIBUTING.md](https://github.com/UiPath-Services/UiPathOrch/blob/master/CONTRIBUTING.md)
in the repository.

## Before You Report

Before creating a new issue, take these steps to narrow down the problem:

### 1. Update to the Latest Version

```powershell
Update-PSResource UiPathOrch
```

The issue may already be fixed in a newer release.

### 2. Clear the Cache

Many unexpected behaviors are caused by stale cached data:

```powershell
Clear-OrchCache
```

Retry the operation after clearing the cache.

### 3. Check Permissions

Permission errors are not bugs. Verify that your account has the required
OAuth scopes and folder roles:

```powershell
Get-Help <cmdlet name>       # Check required scopes
Get-OrchCurrentUser          # Check current user
Get-OrchPSDrive Orch1: | Select-Object -ExpandProperty Scope  # Check granted scopes
```

### 4. Search Existing Issues

Check whether the issue has already been reported using
`gh search issues`.

## Reporting a Bug

Create a new issue with
`gh issue create --repo UiPath-Services/UiPathOrch` including the
following information:

### Required Information

| Item | How to Obtain |
|---|---|
| UiPathOrch version | `(Get-Module UiPathOrch).Version` |
| PowerShell version | `$PSVersionTable.PSVersion` |
| OS | `$PSVersionTable.OS` |
| Orchestrator type | Automation Cloud / Automation Suite / On-premises |
| Orchestrator version | Check Orchestrator admin page |
| App type | Non-confidential / Confidential |

### Bug Report Template

```
**Description**
Clear description of what went wrong.

**Steps to Reproduce**
1. Run `Import-OrchConfig`
2. Run `cd Orch1:\Shared`
3. Run `Get-OrchAsset -Name 'MyAsset'`
4. Error occurs

**Expected Behavior**
What you expected to happen.

**Actual Behavior**
What actually happened. Include the full error message.

**Environment**
- UiPathOrch version: X.Y.Z
- PowerShell version: 7.x.x
- OS: Windows 11
- Orchestrator: Automation Cloud
- App type: Non-confidential

**Additional Context**
Any other relevant details (e.g., folder type, entity count, timing).
```

### Capturing Verbose Output

Verbose output often contains information critical for diagnosing issues.
Re-run the failing command with `-Verbose` and include the output in
your report:

```powershell
Get-OrchAsset -Name 'MyAsset' -Verbose
```

To save verbose output to a file, use `Start-Transcript`. PowerShell.MCP
cannot capture verbose and debug streams via stream redirection (`*>`),
so `Start-Transcript` is the reliable method:

```powershell
Start-Transcript -Path C:\temp\verbose-output.txt -Force
Get-OrchAsset -Name 'MyAsset' -Verbose
Stop-Transcript
```

### Privacy

**Do not include** sensitive information in bug reports:

- Passwords, secrets, or API keys
- Personally identifiable information (usernames, email addresses)
- Organization names, tenant names, or Orchestrator URLs

Replace sensitive values with generic placeholders (e.g., `user@example.com`,
`https://orchestrator.example.com`).

## Suggesting Enhancements

Enhancement suggestions are also welcome. When creating an issue:

- Check if the enhancement has already been suggested
- Describe the use case: what problem does this solve?
- Provide examples of how the feature would be used
- Label the issue as an enhancement

## Building from Source

### Prerequisites

- PowerShell 7.4.2 or later
- .NET SDK 8.0 or later

### Fork and Clone

```powershell
gh repo fork UiPath-Services/UiPathOrch --clone
cd UiPathOrch
```

### Build

```powershell
.\Build-Deploy.ps1 -BuildOnly
```

The build output is placed in `OrchProvider\bin\Release\net8.0`.

### Deploy to a Dev Directory

**Do not deploy over the released module.** If you installed UiPathOrch via
`Install-PSResource`, deploying to the same directory overwrites the
released version. Instead, deploy to a separate directory and import from
there:

```powershell
# Create a dev directory (once)
$devDir = "$HOME\UiPathOrch-dev"
mkdir $devDir -Force

# Copy build output and module files
Copy-Item OrchProvider\bin\Release\net8.0\UiPath.PowerShell.OrchProvider.dll $devDir -Force
Copy-Item Staging\UiPathOrch.psd1, Staging\UiPathOrch.psm1, Staging\OrchProvider.Format.ps1xml $devDir -Force
Copy-Item Staging\Functions $devDir\Functions -Recurse -Force
Copy-Item Staging\Docs $devDir\Docs -Recurse -Force

# Import the dev build (overrides the released module in this session)
Import-Module $devDir\UiPathOrch.psd1 -Force
```

To switch back to the released version, start a new PowerShell session
(or run `Import-Module UiPathOrch -Force`).

### DLL Lock: Kill Importing Sessions Before Deploy

Windows locks DLL files that are loaded by a process. To overwrite the
DLL with a new build, all `pwsh` processes that have imported it must be
terminated first. PowerShell.MCP displays the PID of its `pwsh` session
in the console output:

```powershell
Stop-Process -Id <PID>
```

Killing a `pwsh` session is safe -- you can always start a new one.

After deploying, start a new PowerShell console and verify:

```powershell
(Get-Module UiPathOrch -ListAvailable).Version
```

## Submitting a Pull Request

```powershell
git checkout -b fix/short-description
# Make changes
git add <changed files>
git commit -m "Fix: short description"
git push -u origin fix/short-description
gh pr create --repo UiPath-Services/UiPathOrch \
  --title "Fix: short description" \
  --body "## Summary
- What was changed and why"
```

One fix or feature per pull request.

## Support Channels

| Channel | Use For |
|---|---|
| [GitHub Issues](https://github.com/UiPath-Services/UiPathOrch/issues) | Bug reports, feature requests |
| [GitHub Discussions](https://github.com/UiPath-Services/UiPathOrch/discussions) | General questions, usage tips |
| [UiPath Community Forum](https://forum.uipath.com/) | Broader UiPath questions |
| [UiPath Marketplace Q&A](https://marketplace.uipath.com/) | Marketplace-specific questions |
