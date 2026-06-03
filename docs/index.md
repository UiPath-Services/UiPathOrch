---
title: Home
layout: home
nav_order: 1
---

# UiPathOrch

PowerShell drives and cmdlets for UiPath Orchestrator. Mount tenants as PSDrives, navigate folders with `cd` / `dir`, manage entities in bulk, and migrate between tenants — all from a PowerShell 7 console.

[Install from PowerShell Gallery](https://www.powershellgallery.com/packages/UiPathOrch){: .btn .btn-primary }
[View on GitHub](https://github.com/UiPath-Services/UiPathOrch){: .btn }

---

## Quick Start

```powershell
Install-PSResource UiPathOrch
Import-OrchConfig
Get-OrchHelp
```

## For Users

- [Getting Started](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/00-GettingStarted.md) — install, OAuth setup, first connection
- [Cmdlet Reference]({{ '/help/' | relative_url }}) — search 262 cmdlets, jump to any cmdlet's page
- [CSV Export & Import](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/03-CsvExportImport.md) — bulk import / export patterns
- [Migration & Copy Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/04-MigrationGuide.md) — lift-and-shift between tenants
- [Other Providers (DU & Test Manager)](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/07-OtherProvidersGuide.md) — the Document Understanding and Test Manager drives

## For AI Agents

UiPathOrch ships with documentation written for AI agents that automate Orchestrator operations on the user's behalf.

- [Essential Guide for AI](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/01-Essentials.md) — execution rules, decision flow, error protocol
- [Troubleshooting Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/05-Troubleshooting.md) — verbose logging, raw API access via Invoke-OrchApi
- [Contributing Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/06-ContributingGuide.md) — bug reporting, building from source

## Need Help?

| | |
|---|---|
| Bug reports / feature requests | [GitHub Issues](https://github.com/UiPath-Services/UiPathOrch/issues) |
| Usage questions | [GitHub Discussions](https://github.com/UiPath-Services/UiPathOrch/discussions) |
| UiPath broader topics | [UiPath Community Forum](https://forum.uipath.com/) |
| Inline help | `Get-Help <CmdletName> -Examples` |
| Online help | `Get-Help <CmdletName> -Online` |
| Module documentation summary | `Get-OrchHelp` |
