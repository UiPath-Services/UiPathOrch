---
title: Troubleshooting
nav_order: 7
permalink: /troubleshooting/
---

# UiPathOrch Module - Troubleshooting Guide

- [Overview](#overview)
- [Connection Issues](#connection-issues)
- [Enable Verbose Logging](#enable-verbose-logging)
- [Reading Logs](#reading-logs)
- [Calling Endpoints Directly](#calling-endpoints-directly)
- [API Reference](#api-reference)
- [Common Investigation Workflow](#common-investigation-workflow)
- [Last Resort: Extracting the Access Token](#last-resort-extracting-the-access-token)

## Overview

This document is primarily intended for AI agents investigating issues
with UiPathOrch. It covers how to enable logging, read HTTP logs, and
call Orchestrator API endpoints directly to isolate whether a problem
is in UiPathOrch or in the Orchestrator API itself.

The recommended diagnostic flow uses `Invoke-OrchApi`, which sends
requests through the same authenticated, folder-aware HTTP session as
the cmdlets. This eliminates the need to extract a bearer token into
user space — useful both for security (logs, screenshots, AI agent
contexts) and for accuracy (folder context, OData headers, and
ApiVersion are applied identically to cmdlet calls, so the comparison
isolates only the response-processing layer).

## Connection Issues

When a drive fails to connect, verify the configuration values:

```powershell
Get-OrchPSDrive Orch2: | Select-Object Root, Edition, AppId, Scope, RedirectUrl, IdentityUrl
```

| Property | Notes |
|---|---|
| `Root` | Orchestrator URL (include organization and tenant for Cloud / Automation Suite: `https://{host}/{org}/{tenant}`) |
| `Edition` | Resolved deployment kind: `Cloud`, `AutomationSuite`, or `OnPremises`. Inferred from `Root` (uipath.com host → Cloud, two-segment path → AutomationSuite, otherwise OnPremises) unless pinned in the config. If this column shows the wrong value, that is the root cause — see "Edition mis-detection" below |
| `AppId` | Must match the external application registered in Orchestrator |
| `Scope` | Must not exceed scopes granted to the external application |
| `RedirectUrl` | Auto-configured. Verify it matches the Redirect URL registered in the external application |
| `IdentityUrl` | Auto-configured for all editions. For Automation Suite the canonical pattern is `https://{host}/identity_` at the host root — pin it explicitly in the config if the auto-derived value doesn't reach your AS identity service |

### Edition mis-detection

If `Get-OrchPSDrive` shows the wrong `Edition` for your drive, all the URL
construction (`/orchestrator_/`, `/identity_/`, `/portal_/`, tenant
header vs URL path) is going to be wrong. Pin the correct value
explicitly in `UiPathOrchConfig.json`:

```jsonc
{
  "Name": "MyDrive",
  "Root": "https://...",
  "Edition": "AutomationSuite",   // or "Cloud" / "OnPremises"
  ...
}
```

The known false-positive case is an on-premises Orchestrator behind a
multi-level reverse proxy that produces a two-segment path (`/dept/tenant`)
— the heuristic mis-classifies it as Automation Suite. Pin
`"Edition": "OnPremises"` to restore on-prem URL building.

After editing, reload with `Import-OrchConfig`.

### Diagnosing a sign-in (PKCE) failure

When a non-confidential (PKCE) sign-in fails, UiPathOrch's local listener only
sees that no authorization code arrived — the real reason is on the page UiPath
Identity left the browser on. Copy that URL from the browser's address bar and
pass it to `Resolve-OrchAuthError`, which decodes the `errorCode` / `errorId`
envelope and returns a human-readable `Diagnosis` and `RecommendedAction`:

```powershell
Resolve-OrchAuthError 'https://cloud.uipath.com/identity_/web/login?errorCode=...&returnUrl=...&traceId=...'
```

> **Wrap the URL in single quotes.** These URLs contain `&` (and sometimes `$`),
> which PowerShell would otherwise treat as an operator / a variable, so an
> unquoted URL fails to parse or is silently truncated at the first `&`. Single
> quotes pass the URL through literally — double quotes are not enough, because
> they still expand `$`.

The diagnosis object also surfaces the External App / `ClientId` in use, the
`RedirectUri` and `Scopes` Identity received, a `TraceId` (server-side
correlation id for a support ticket), and the decoded `RawErrorId`.

## Enable Verbose Logging

Logging must be enabled in the configuration file before logs are
generated. Get the config file path and check the current setting:

```powershell
Get-OrchConfigPath
```

In the configuration file, ensure the `Logging` section in the global
settings has `Enabled` set to `true` and `Level` set to `Verbose`:

```jsonc
"Logging": {
  "Level": "Verbose",
  "Enabled": true
}
```

After editing, reload the configuration:

```powershell
Import-OrchConfig
```

Available log levels:

| Level | Description |
|---|---|
| `Error` | Errors only |
| `Info` | Errors and informational messages |
| `Trace` | Adds request/response headers |
| `Verbose` | Adds request/response bodies (most detailed) |

The `Authorization: Bearer ...` request header is masked in the log
file regardless of log level, so verbose logs are safe to attach to
GitHub issues without leaking active tokens.

## Reading Logs

Get the log folder path for the current drive:

```powershell
Get-OrchLogLocation
```

Logs are stored as daily files named `YYYY-MM-DD_DriveName.log`. Each
entry contains:

- Timestamp and sequence number
- HTTP method and URL
- Request headers (Authorization is masked)
- Request body (for POST/PUT/PATCH)
- Response status code
- Response headers
- Response body

Example log entry:

```
23:23:24.171 #0001 GET https://cloud.uipath.com/org/tenant/odata/Folders
  REQ HEAD {"User-Agent":"UiPathOrch/1.0.0.0","Authorization":"Bearer ***"}
  RSP 200 OK (0.234s)
  RSP HEAD {"Content-Type":"application/json; odata.metadata=minimal"}
  RSP BODY {"@odata.context":"...","@odata.count":5,"value":[...]}
```

To find errors in the latest log:

```powershell
$logDir = Get-OrchLogLocation
$latest = Get-ChildItem $logDir | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Show-TextFiles $latest.FullName -Contains 'RSP 4'    # 4xx errors
Show-TextFiles $latest.FullName -Contains 'RSP 5'    # 5xx errors
Show-TextFiles $latest.FullName -Contains 'EXCEPTION' # Exceptions
```

## Calling Endpoints Directly

When a cmdlet produces an unexpected result, reproduce the call with
`Invoke-OrchApi` to determine whether the issue is in UiPathOrch or
in the Orchestrator API. `Invoke-OrchApi` reuses the drive's
authenticated session, the OAuth scopes, the folder context header
(`X-UIPATH-OrganizationUnitId`), and the ApiVersion the cmdlets use,
so a difference between cmdlet and direct call isolates the
response-processing layer rather than the request setup.

### Step 1: Get the endpoint URL from the log

Find the relevant request in the log. Note the HTTP method, URL path,
and any request body.

### Step 2: Call the endpoint

The relative path goes to `-Uri`. The drive in `-Path` selects the
tenant; subfolders inject the folder context header automatically.

**List folders** (equivalent to `dir`):

```powershell
Invoke-OrchApi -Path Orch2: -Uri '/odata/Folders?$select=DisplayName,FolderType'
```

**Get assets in a folder** (equivalent to `Get-OrchAsset`):

```powershell
# Folder path on the drive supplies X-UIPATH-OrganizationUnitId automatically.
Invoke-OrchApi -Path Orch2:\Shared -Uri '/odata/Assets'
```

**Get users** (equivalent to `Get-OrchUser`):

```powershell
Invoke-OrchApi -Path Orch2: -Uri '/odata/Users?$select=UserName,Type'
```

**Get machines** (equivalent to `Get-OrchMachine`):

```powershell
Invoke-OrchApi -Path Orch2: -Uri '/odata/Machines?$select=Name,Type'
```

**Create an asset** (equivalent to `New-OrchAsset`):

```powershell
Invoke-OrchApi -Path Orch2:\Shared -Uri '/odata/Assets' -Method Post -Body @{
    Name        = 'TestAsset'
    ValueScope  = 'Global'
    ValueType   = 'Text'
    StringValue = 'hello'
}
```

`Invoke-OrchApi` honors `-WhatIf` and `-Confirm` for non-GET methods,
so you can preview a destructive call without executing it. Pass
`-Confirm:$false` to bypass the prompt in scripts.

### Step 3: Compare results

Compare the direct API response with what the cmdlet returned. If the
two match, the issue is in Orchestrator. If they differ, the issue
may be in UiPathOrch's processing of the response.

### Useful flags for diagnostics

| Flag | Use |
|---|---|
| `-SkipHttpErrorCheck` | Capture the parsed body of a 4xx / 5xx response instead of throwing. Pair with `-StatusCodeVariable` and `-ResponseHeadersVariable` |
| `-Raw` | Return the raw JSON string without parsing — preserves `@odata.context`, `@odata.nextLink`, and the OData wrapper |
| `-Identity` / `-Portal` | Switch the base URL to the Identity Server / Portal endpoints respectively (used by `Pm` cmdlets and undocumented APIs) |
| `-SkipFolderContext` | Suppress automatic `X-UIPATH-OrganizationUnitId` injection (rare; needed when calling tenant-scope endpoints from a folder context) |
| `-Headers` | Add custom headers. `Authorization` is silently dropped — the drive owns auth |

Example — capture a 404 body for inspection:

```powershell
$body = Invoke-OrchApi -Path Orch2: -Uri '/odata/Folders(99999)' `
    -SkipHttpErrorCheck -StatusCodeVariable sc -ResponseHeadersVariable rh
"$sc"; $body
```

## API Reference

To look up available endpoints, parameters, and response schemas,
fetch the Swagger JSON via `Invoke-OrchApi` (no token plumbing
required).

Orchestrator API (used by `Orch` cmdlets):

```powershell
$swagger = Invoke-OrchApi -Path Orch2: -Uri '/swagger/v20.0/swagger.json' -Raw | ConvertFrom-Json
$swagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*Asset*' }
$swagger.paths.'/odata/Assets'.get
```

Identity Server API (used by `Pm` cmdlets):

```powershell
$idSwagger = Invoke-OrchApi -Path Orch2: -Identity -Uri '/swagger/internal/swagger.json' -Raw | ConvertFrom-Json
$idSwagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*User*' }
```

Test Manager API (used by `Tm` cmdlets):

```powershell
$tmSwagger = Invoke-OrchApi -Path Orch2: -Uri '/testmanager_/swagger/v2/swagger.json' -Raw | ConvertFrom-Json
$tmSwagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*TestSet*' }
```

Document Understanding API (used by `Du` cmdlets):

```powershell
$duSwagger = Invoke-OrchApi -Path Orch2: -Uri '/du_/api/documentmanager/swagger_dm.json' -Raw | ConvertFrom-Json
$duSwagger.paths.PSObject.Properties.Name
```

To open the Swagger UI in the user's browser:

```powershell
$root = (Get-OrchPSDrive Orch2:).Root
Start-Process "$root/swagger"
```

Official documentation:
[UiPath Orchestrator API Reference](https://docs.uipath.com/orchestrator/reference)

## A PmUserPreference Change Doesn't Show in the Web UI

After `Set-PmUserPreference` (or `Copy-PmUserPreference`) changes your
language or theme, an already-open Orchestrator browser tab may keep
showing the old value even after a normal refresh.

This is expected. The cmdlet writes the preference to the identity
Setting store exactly as the web UI does (a single
`PUT /api/identity/Setting` with the `UserLanguage.Language` /
`UserLanguage.Date` pair) — `Get-PmUserPreference` confirms the new
value is stored. But the portal renders the *active* language/theme
from a client-side cache (browser local storage) that is updated only
when you change the setting through the web UI's own switcher. A
server-side write doesn't touch that cache.

To see a cmdlet-set value in the browser:

- Sign out and sign back in, **or**
- Clear the site's data / local storage (DevTools → Application →
  Local Storage), **or**
- Open the org in a new browser profile or a private window.

A fresh sign-in reads the stored preference and applies it. So
`Set-`/`Copy-PmUserPreference` are well suited to auditing, migrating,
or provisioning preferences (which take effect on the next sign-in),
not to live-switching the UI of a session that is already open. The
`UserLanguage.Date` value is just a timestamp the web writes alongside
the language; it is **not** a trigger that forces the cache to refresh.

## Common Investigation Workflow

1. **Reproduce the issue** with verbose logging enabled
2. **Read the log** to find the failing request
   - Look for 4xx/5xx status codes
   - Check the request URL and body for correctness
   - Check the response body for error details
3. **Reproduce the call** with `Invoke-OrchApi` using the URL and
   method from the log
4. **Compare** the direct result with the cmdlet result
5. **Read the source code** if the cause is still unclear. Clone the
   repository following the
   [Building from Source](06-ContributingGuide.md#building-from-source)
   section and read the relevant C# code to understand how UiPathOrch
   processes the API response.
6. If the issue is confirmed as a UiPathOrch bug, or if you cannot
   resolve it through the steps above, file a GitHub issue following the
   [Contributing Guide](06-ContributingGuide.md). UiPathOrch is not
   covered by UiPath Technical Support — GitHub is the only channel
   for getting help from the maintainers.

## Last Resort: Extracting the Access Token

`Invoke-OrchApi` covers nearly all diagnostic cases. The bearer token
is still accessible through `Get-OrchPSDrive` for the rare situations
where you must call the API from outside the PowerShell session
(another machine, a different language runtime, a third-party tool):

```powershell
Get-OrchPSDrive Orch2: | Select-Object -ExpandProperty AccessToken
```

To inspect the token claims (e.g., scopes, expiry) without running
the API call:

```powershell
$token = Get-OrchPSDrive Orch2: | Select-Object -ExpandProperty AccessToken
$payload = $token.Split('.')[1]
$padded = $payload + ('=' * (4 - $payload.Length % 4) % 4)
[System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded)) | ConvertFrom-Json
```

The token is a JWT minted by the UiPath Identity Server. Treat it
as a credential:

- Do not echo it into shell history, screenshots, screen-share, or
  AI-agent context windows. The token grants the OAuth scopes of the
  drive for its lifetime (typically about an hour).
- Do not paste it into bug reports or chat. The drive can be
  re-authenticated; a leaked token cannot be revoked individually
  before expiry.
- Avoid this path entirely in CI / shared sessions; reach for
  `Invoke-OrchApi` first.
