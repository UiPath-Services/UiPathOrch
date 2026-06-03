# UiPathOrch

[![PowerShell](https://img.shields.io/badge/PowerShell-7.4+-blue.svg)](https://github.com/PowerShell/PowerShell)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-brightgreen.svg)](#prerequisites)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/UiPathOrch)](https://www.powershellgallery.com/packages/UiPathOrch)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/UiPathOrch)](https://www.powershellgallery.com/packages/UiPathOrch)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-yellow.svg)](LICENSE)

PowerShell Drive and Cmdlets for managing UiPath Orchestrator.

> **Note:** UiPathOrch is an open-source PowerShell module for managing UiPath Orchestrator. It is not an official abbreviation of, or part of, the UiPath Orchestrator product.

## Overview

UiPathOrch is a PowerShell module that enables you to mount multiple Orchestrator tenants as PSDrives, allowing you to navigate through their folders using familiar commands like `cd`, `dir`, `mkdir`, `rmdir` within the PowerShell console. Beyond simple folder navigation, it facilitates the manipulation of various entities through a range of cmdlets and wildcards.

This module uses the Orchestrator API to mount modern folders as local drives, making them accessible from the PowerShell console with file system-like operations.

Works against **Automation Cloud**, **Automation Suite**, and **on-premises Orchestrator** deployments — the deployment kind is auto-detected from the drive's Root URL (or pinned explicitly via the `Edition` config field).

**Note:** This module functions even in environments where Studio/Assistant is not installed.

## Key Features

### Bulk Processing
Execute bulk operations across multiple folders and tenants with a single command, eliminating the need for scripting or CSV preparation.

### Advanced Scripting
Perform advanced operations like downloading only active packages or detecting long-pending jobs to trigger email notifications.

### Tenant Migration
Facilitate tenant migrations by copying various entities between tenants efficiently.

### Entity Management
- **Tenant entities:** Manage libraries, packages, assets, queues, triggers, webhooks, and more
- **Folder entities:** Handle processes, jobs, machines, robots, schedules, and other folder-specific items
- **Platform Management:** User, group, license, and authentication management
- **Document Understanding:** Project and queue management
- **Test Manager:** Test set operations

## Installation

### Prerequisites
- PowerShell 7.4.2 or later
- UiPath Orchestrator API access with appropriate OAuth scopes

### Install from PowerShell Gallery

```powershell
Install-PSResource UiPathOrch
Import-OrchConfig
```

Run `Get-OrchHelp` for a getting started guide, or see the `Docs` folder in the module directory.

## Quick Start

### Basic Navigation

```powershell
# Navigate to an Orchestrator drive
cd Orch1:\

# List folders
dir

# Navigate to a shared folder
cd Shared

# List assets in current folder
Get-OrchAsset
```

### Working with Multiple Tenants

```powershell
# Target multiple folders across tenants
Get-OrchAsset -Path Orch1:\Shared,Orch2:\Shared

# Operate on all folder entities recursively
Get-OrchAsset -Recurse
```

### Listing Available Commands

```powershell
# List all cmdlets in UiPathOrch
Get-Command -Module UiPathOrch

# Get help for a specific cmdlet
Get-Help Get-OrchAsset -Full

# Open the cmdlet's online reference page in a browser
Get-Help Get-OrchAsset -Online
```

## Documentation

Guides (also published, searchable, at [uipath-services.github.io/UiPathOrch](https://uipath-services.github.io/UiPathOrch)):

- [Getting Started](docs/00-GettingStarted.md) — install, OAuth setup, first connection
- [Essential Guide for AI](docs/01-Essentials.md) — execution rules, decision flow, error protocol
- [Cmdlet Reference](docs/02-CmdletReference.md) — cmdlets by area, with examples
- [CSV Export & Import](docs/03-CsvExportImport.md) — bulk import / export patterns
- [Migration & Copy Guide](docs/04-MigrationGuide.md) — lift-and-shift between tenants
- [Other Providers (DU & Test Manager)](docs/05-OtherProvidersGuide.md) — the Document Understanding and Test Manager drives
- [Troubleshooting](docs/90-Troubleshooting.md) — verbose logging, raw API access via `Invoke-OrchApi`
- [Contributing](docs/99-ContributingGuide.md) — bug reporting, building from source

Also:

- **Inline help:** `Get-Help <cmdlet-name>` for OAuth scopes and usage; `Get-OrchHelp` for a summary (the same guides ship in the module's `Docs` folder)
- **Marketplace:** [UiPathOrch on UiPath Marketplace](https://marketplace.uipath.com/listings/uipathorch)

## AI Agent Integration

UiPathOrch works with AI agents through [PowerShell.MCP](https://www.powershellgallery.com/packages/PowerShell.MCP), which exposes a PowerShell console as an MCP server. Agents can navigate Orchestrator folders, manage entities, and use tab completion programmatically via `TabExpansion2`. The `Docs` folder in the module directory includes documentation written for both human users and AI agents.

## Headless / CI Use

In non-interactive environments (GitHub Actions, other CI runners, containers, scheduled jobs), the default first-import behavior — creating a template config file and opening it in Notepad — is undesirable. Set the `UIPATHORCH_SUPPRESS_CONFIG_CREATION` environment variable to `1` to skip both, then mount drives directly with `New-OrchPSDrive` so no config file is needed.

```yaml
# GitHub Actions example
- shell: pwsh
  env:
    UIPATHORCH_SUPPRESS_CONFIG_CREATION: 1
    ORCH_URL:    ${{ secrets.ORCH_URL }}        # https://cloud.uipath.com/<org>/<tenant>
    ORCH_APPID:  ${{ secrets.ORCH_APPID }}
    ORCH_SECRET: ${{ secrets.ORCH_APPSECRET }}
  run: |
    Install-PSResource UiPathOrch -TrustRepository
    Import-Module UiPathOrch
    New-OrchPSDrive -Name Orch -Root $env:ORCH_URL `
      -AppId $env:ORCH_APPID -AppSecret $env:ORCH_SECRET `
      -OAuthScope 'OR.Folders OR.Assets OR.Settings'
    Get-OrchAsset -Path Orch:\Shared
```

Notes:

- A **confidential application** (`-AppId` + `-AppSecret`, OAuth `client_credentials` grant) is the recommended auth for CI. PKCE requires an interactive browser and cannot be used headlessly. Personal Access Tokens (`-AccessToken`) work but tie the pipeline to an individual user and inherit that user's full permission set, so prefer a confidential app per pipeline with the minimum OAuth scopes required.
- Pass secrets via `env:` rather than inline command arguments so GitHub Actions log masking covers them and they don't appear in command echoes.
- For on-premises Orchestrator or self-hosted Automation Suite that isn't reachable from GitHub-hosted runners, use a self-hosted runner inside the network where the deployment is accessible.
- For ad-hoc API calls in CI (e.g., calling an endpoint with no dedicated cmdlet), use `Invoke-OrchApi` rather than extracting the bearer token. It reuses the drive's authenticated session, so the access token never leaves the module — no risk of leaking a live token into pipeline logs:

  ```powershell
  Invoke-OrchApi -Path Orch: -Uri '/odata/Folders?$select=Id,DisplayName'
  ```

- **Avoid piping `Get-OrchPSDrive` output to logs in CI.** Its output exposes a live `AccessToken` property as a documented diagnostic feature (see `Docs/90-Troubleshooting.md`); commands like `Get-OrchPSDrive | Format-List *` or `ConvertTo-Json` would emit it. GitHub Actions log masking covers only the registered `AppSecret`, not derived tokens. `Invoke-OrchApi` removes the need for this pattern in nearly all cases.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Support

**Important**: This is a community-driven tool and is not officially supported by UiPath Technical Support. For issues related to this module, please use the channels below instead of contacting UiPath Technical Support.

For support and questions, you can reach out through any of these channels:

- **UiPath Marketplace Q&A**: [Ask questions on the Marketplace listing](https://marketplace.uipath.com/listings/uipathorch/questions)
- **GitHub Discussions**: [Start a discussion](https://github.com/UiPath-Services/UiPathOrch/discussions) for general questions and ideas
- **GitHub Issues**: [Report bugs or request features](https://github.com/UiPath-Services/UiPathOrch/issues)
- **UiPath Community Forum**: [Get help from the community](https://forum.uipath.com/)

## Publisher

Internal Labs - UiPath

---

**Note:** Please test in non-production environments before use in production.
