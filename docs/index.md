# UiPathOrch

PowerShell drives and cmdlets for UiPath Orchestrator. Mount tenants as PSDrives, navigate folders with `cd` / `dir`, manage entities in bulk, and migrate between tenants — all from a PowerShell 7 console.

[Install from PowerShell Gallery](https://www.powershellgallery.com/packages/UiPathOrch) · [View on GitHub](https://github.com/UiPath-Services/UiPathOrch)

---

## Quick Start

```powershell
Install-PSResource UiPathOrch
Import-OrchConfig
Get-OrchHelp
```

## For Users

- [Getting Started](00-GettingStarted.md) — install, OAuth setup, first connection
- [Cmdlet Reference](02-CmdletReference.md) — cmdlets by area, with examples
- [CSV Export & Import](03-CsvExportImport.md) — bulk import / export patterns
- [Migration & Copy Guide](04-MigrationGuide.md) — lift-and-shift between tenants
- [Incremental-Cache Cmdlets](05-IncrementalCacheCmdlets.md) — drive `Get-OrchJob`, `Get-OrchLog`, `Get-OrchAuditLog`, `Get-OrchQueueItem` and friends efficiently
- [Licensing Guide](06-LicensingGuide.md) — license cmdlets at the organization (`Pm*`) and tenant (`Orch*`) layers
- [Credentials & Secrets](07-CredentialsAndSecrets.md) — the write-only-secrets rule and how to re-set each entity's secret
- [Other Providers (DU & Test Manager)](60-OtherProvidersGuide.md) — the Document Understanding and Test Manager drives

## For AI Agents

UiPathOrch ships with documentation written for AI agents that automate Orchestrator operations on the user's behalf.

- [Essential Guide for AI](01-Essentials.md) — execution rules, decision flow, error protocol
- [Troubleshooting Guide](90-Troubleshooting.md) — verbose logging, raw API access via `Invoke-OrchApi`
- [Contributing Guide](99-ContributingGuide.md) — bug reporting, building from source

## Need Help?

| | |
|---|---|
| Bug reports / feature requests | [GitHub Issues](https://github.com/UiPath-Services/UiPathOrch/issues) |
| Usage questions | [GitHub Discussions](https://github.com/UiPath-Services/UiPathOrch/discussions) |
| UiPath broader topics | [UiPath Community Forum](https://forum.uipath.com/) |
| Inline help | `Get-Help <CmdletName> -Examples` |
| Online help | `Get-Help <CmdletName> -Online` (opens the cmdlet's help on GitHub) |
| Module documentation summary | `Get-OrchHelp` |
