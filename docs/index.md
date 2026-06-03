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

- [Getting Started]({{ '/getting-started/' | relative_url }}) — install, OAuth setup, first connection
- [Cmdlet Reference]({{ '/help/' | relative_url }}) — search 262 cmdlets, jump to any cmdlet's page
- [CSV Export & Import]({{ '/csv-export-import/' | relative_url }}) — bulk import / export patterns
- [Migration & Copy Guide]({{ '/migration/' | relative_url }}) — lift-and-shift between tenants
- [Other Providers (DU & Test Manager)]({{ '/other-providers/' | relative_url }}) — the Document Understanding and Test Manager drives

## For AI Agents

UiPathOrch ships with documentation written for AI agents that automate Orchestrator operations on the user's behalf.

- [Essential Guide for AI]({{ '/essentials/' | relative_url }}) — execution rules, decision flow, error protocol
- [Troubleshooting Guide]({{ '/troubleshooting/' | relative_url }}) — verbose logging, raw API access via Invoke-OrchApi
- [Contributing Guide]({{ '/contributing/' | relative_url }}) — bug reporting, building from source

## Need Help?

| | |
|---|---|
| Bug reports / feature requests | [GitHub Issues](https://github.com/UiPath-Services/UiPathOrch/issues) |
| Usage questions | [GitHub Discussions](https://github.com/UiPath-Services/UiPathOrch/discussions) |
| UiPath broader topics | [UiPath Community Forum](https://forum.uipath.com/) |
| Inline help | `Get-Help <CmdletName> -Examples` |
| Online help | `Get-Help <CmdletName> -Online` |
| Module documentation summary | `Get-OrchHelp` |
