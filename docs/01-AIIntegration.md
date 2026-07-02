---
title: AI Integration (PowerShell.MCP)
nav_order: 1
permalink: /ai-integration/
---

# UiPathOrch Module - AI Integration with PowerShell.MCP

UiPathOrch is a PowerShell module, so the way to let an AI assistant operate Orchestrator through
it is to give the assistant a **live PowerShell 7 session** that has UiPathOrch loaded. The cleanest
bridge for that is **[PowerShell.MCP](https://www.powershellgallery.com/packages/PowerShell.MCP)** —
a general-purpose MCP server that exposes one shared PowerShell console to an MCP client (Claude
Code, Claude Desktop, …). The AI runs the same `Import-OrchConfig`, `Get-OrchProcess`,
`Start-OrchJob -WhatIf`, … that you would, in a console you can watch.

This page is about **wiring an AI to UiPathOrch**. For the rules the AI should follow once connected
(safety, `-WhatIf`, error handling, the `.Path` property, architecture), see the
[Essentials for AI](02-Essentials.md).

- [Why PowerShell.MCP](#why-powershellmcp)
- [Setup](#setup)
- [The six tools](#the-six-tools)
- [How the AI drives UiPathOrch](#how-the-ai-drives-uipathorch)
- [Authentication in a shared session](#authentication-in-a-shared-session)
- [Transparency, auditing, and safety](#transparency-auditing-and-safety)
- [Sharing UiPathOrch docs back to the user](#sharing-uipathorch-docs-back-to-the-user)
- [Limitations and gotchas](#limitations-and-gotchas)

## Why PowerShell.MCP

A service-specific MCP server would have to wrap each cmdlet as a bespoke tool. PowerShell.MCP takes
the opposite approach: it hands the AI a real shell, so **all UiPathOrch cmdlets — plus
`Get-Help`, the providers, tab-completion metadata, and any other module or CLI on the box — are
available immediately**, with no per-cmdlet tool definitions to maintain.

For driving a **live production Orchestrator**, three properties matter most:

- **Shared, visible console.** You and the AI work in the *same* PowerShell session. Every command
  the AI runs appears in your console in real time and is saved to history — so a destructive
  Orchestrator operation is never invisible. This pairs naturally with the module's `-WhatIf`
  discipline.
- **Persistent session state.** Authenticate once and stay authenticated: `Import-OrchConfig`
  mounts the drives, the OAuth tokens are cached, and modules/variables/functions persist across
  every subsequent command. There is no per-command re-initialization or re-login.
- **Secrets stay with you, not the AI.** UiPathOrch's recommended login is interactive PKCE (a
  browser sign-in). PowerShell.MCP routes interactive prompts (`Read-Host -AsSecureString`,
  `Get-Credential`, the PKCE browser flow, an app secret, an MFA code) to the **console**, where you
  complete them — the keystrokes never enter the AI's output stream. The AI orchestrates the
  workflow; you supply the credential at the right moment.

## Setup

1. **Install PowerShell 7.4+** ([guide](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)).
2. **Install both modules** (PowerShell Gallery):
   ```powershell
   Install-PSResource PowerShell.MCP
   Install-PSResource UiPathOrch
   ```
3. **Register the MCP server** with your client (run once):
   ```powershell
   Register-PwshToClaudeCode       # Claude Code
   Register-PwshToClaudeDesktop    # Claude Desktop
   ```
   For other MCP clients, run `Get-MCPProxyPath -Escape` to get the JSON-escaped proxy path and add
   it to that client's MCP configuration manually. (On Linux/macOS also run
   `chmod +x "$(Get-MCPProxyPath)"` once.)
4. **Restart the MCP client.**
5. **Connect UiPathOrch in the shared console.** The AI (or you) runs:
   ```powershell
   Import-OrchConfig      # mount the configured tenants as drives
   Get-OrchPSDrive        # verify which drives are available
   ```
   On first run, complete any browser sign-in in the console window. After that the session stays
   authenticated.

## The six tools

PowerShell.MCP exposes six tools — the AI uses `execute_command` for essentially every
UiPathOrch command:

| Tool | Purpose |
|------|---------|
| `execute_command` | Run any PowerShell command — `Get-OrchProcess`, `Start-OrchJob`, a pipeline, a CLI tool. `var1`–`var4` inject literal strings past the parser (safe for `$`, backticks, quotes) |
| `start_console` | Ensure a console is available, reusing a standby one; a `reason` forces an additional console |
| `get_current_location` | Report the current directory and available drives (incl. `Orch1:` …) |
| `wait_for_completion` | Wait for a long-running command and retrieve its cached result |
| `cancel` | Interrupt the running command (pipeline stop + Ctrl+C for native CLIs) |
| `close_console` | Force-close a console by PID — the escape hatch for a stuck prompt or a non-cooperative command |

A single client can launch **multiple** consoles (each titled e.g. `#12345 Taxi`), but only **one is
active at a time** — the MCP server switches to the right console automatically when needed, and
routes around a busy console by spawning a new one in the same call. Sub-agents get isolated
consoles of their own via `is_subagent=true` / `agent_id`. `Get-MCPOwner` shows which client owns
the current console. (Separate client instances also run in parallel, each with its own console.)

## How the AI drives UiPathOrch

Because the AI is in a real shell, it should use the module the way a PowerShell user would:

- **Discover with help, not guesswork.** `Get-OrchHelp` for the doc map, `Get-Command -Module
  UiPathOrch` to list cmdlets, and `Get-Help <Cmdlet> -Examples` before composing a command.
- **Work with objects, not screen-scraping.** Cmdlets emit real objects — pipe them:
  ```powershell
  Get-OrchAsset -Path Orch1:\ -Recurse | Select-Object Path, Name, ValueType, Value
  Get-OrchJob -Path Orch1:\ -Recurse -Last 7d | Where-Object State -eq Faulted
  ```
- **Complete values without a keyboard.** An AI can't press `[Tab]`, so it retrieves the same
  candidates programmatically with `TabExpansion2`:
  ```powershell
  (TabExpansion2 'Get-OrchAsset -Path Orch1:\Finance -Name ').CompletionMatches |
      Select-Object CompletionText
  ```
  Put `-Path` (and `-Recurse`) **before** the parameter being completed, so the completer knows the
  folder context.
- **Stay safe by default.** Preview destructive operations with `-WhatIf`, show the result, and only
  re-run without `-WhatIf` after the user confirms. The shared console makes that confirmation
  natural — the user sees exactly what is about to run.
- **Don't block on long jobs.** Kick off the command and use `wait_for_completion` rather than
  polling.

The full operational contract (session start-up, error-classification protocol, the `.Path`
property, the permission model) lives in the [Essentials for AI](02-Essentials.md); this page only
covers the integration mechanics.

## Authentication in a shared session

UiPathOrch supports interactive PKCE (Non-Confidential App — recommended for interactive/AI use),
Confidential App (app secret, for unattended automation), and Personal Access Token. With
PowerShell.MCP:

- **Interactive PKCE** fits well: the AI triggers `Import-OrchConfig` / the first cmdlet, **you**
  complete the browser sign-in, and the cached token serves the rest of the session — the AI never
  sees your credentials.
- `Import-OrchConfig` only *mounts* the drives; the token is fetched lazily on first use, so
  `Get-OrchPSDrive` showing `HasToken = False` right after import is normal.
- Use `Switch-OrchCurrentUser` to sign in as a different account (opens an InPrivate window and
  clears cached data).
- For fully **unattended** AI runs, prefer a Confidential App or PAT so no interactive sign-in is
  needed; note that some user-info cmdlets (e.g. `Get-OrchCurrentUser`) are unavailable under a
  Confidential App — that is expected.

## Transparency, auditing, and safety

Operating production Orchestrator through an AI is only acceptable if every action is visible and
reversible-by-confirmation. The shared-console model gives you that:

- Every AI command is printed in your console and added to history.
- Interactive prompts are answered **by you**, in the console.
- `-WhatIf` previews, the two-step confirm flow, and `Clear-OrchCache`-then-retry (see
  [Essentials](02-Essentials.md)) all work unchanged.
- Commands **you** type in the console are private — they are not shown to the AI.

## Sharing UiPathOrch docs back to the user

When the AI needs to *show* you a page from this `Docs/` folder (rather than paraphrase it), it can
render the Markdown with **[MarkdownPointer](https://www.powershellgallery.com/packages/MarkdownPointer)**:

```powershell
Install-Module MarkdownPointer        # once
mdp <path to a .md file>              # open a doc
```

MarkdownPointer can itself be registered as an MCP server, after which the AI calls the
`show_markdown` tool instead of `mdp`:

```powershell
Register-MdpToClaudeCode       # Claude Code
Register-MdpToClaudeDesktop    # Claude Desktop
```

## Limitations and gotchas

- **Interrupting a running command.** The `cancel` tool stops a runaway pipeline and sends Ctrl+C
  to native CLIs. What it can't cancel is a PowerShell **host prompt** (`Read-Host`,
  `Get-Credential`, a missing mandatory parameter) — answer it in the console, or abandon the
  console with `close_console`.
- **Multiple consoles, one active.** A client can launch several consoles, but only one is operable
  at a time; the MCP server switches to the needed console automatically. (Truly simultaneous work
  uses separate client instances.)
- **Rebuilding the module from source?** Windows locks a loaded DLL, so every `pwsh` that imported
  UiPathOrch (including the PowerShell.MCP console) must be stopped before redeploying — see
  [Contributing Guide → DLL Lock](99-ContributingGuide.md). PowerShell.MCP prints its `pwsh` PID for
  exactly this.

---

See also: [Essentials for AI](02-Essentials.md) (operational rules) ·
[Getting Started](00-GettingStarted.md) (install & authentication) ·
[Troubleshooting](90-Troubleshooting.md) (verbose logging, raw `Invoke-OrchApi`).
