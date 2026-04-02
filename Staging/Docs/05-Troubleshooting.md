# UiPathOrch Module - Troubleshooting Guide

- [Overview](#overview)
- [Connection Issues](#connection-issues)
- [Enable Verbose Logging](#enable-verbose-logging)
- [Reading Logs](#reading-logs)
- [Getting Access Tokens](#getting-access-tokens)
- [API Reference](#api-reference)
- [Calling Endpoints Directly](#calling-endpoints-directly)
- [Common Investigation Workflow](#common-investigation-workflow)

## Overview

This document is primarily intended for AI agents investigating issues
with UiPathOrch. It covers how to enable logging, read HTTP logs, extract
access tokens, and call Orchestrator API endpoints directly to isolate
whether a problem is in UiPathOrch or in the Orchestrator API itself.

## Connection Issues

When a drive fails to connect, verify the configuration values:

```powershell
Get-OrchPSDrive Orch2: | Select-Object Root, AppId, Scope, RedirectUrl, IdentityUrl
```

| Property | Notes |
|---|---|
| `Root` | Orchestrator URL (include organization and tenant for Cloud) |
| `AppId` | Must match the external application registered in Orchestrator |
| `Scope` | Must not exceed scopes granted to the external application |
| `RedirectUrl` | Auto-configured for Cloud and on-premises. Verify it matches the Redirect URL registered in the external application |
| `IdentityUrl` | Auto-configured for Cloud and on-premises. Must be manually set in the config file for Automation Suite (e.g., `https://host/identity`) |

If `IdentityUrl` is wrong or missing for Automation Suite, authentication
will fail. Open the config file to fix it:

```powershell
Get-OrchConfigPath  # Get the file path, then read and edit directly
```

After editing, reload with `Import-OrchConfig`.

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

## Reading Logs

Get the log folder path for the current drive:

```powershell
Get-OrchLogLocation
```

Logs are stored as daily files named `YYYY-MM-DD_DriveName.log`. Each
entry contains:

- Timestamp and sequence number
- HTTP method and URL
- Request headers (including Authorization token)
- Request body (for POST/PUT/PATCH)
- Response status code
- Response headers
- Response body

Example log entry:

```
23:23:24.171 #0001 GET https://cloud.uipath.com/org/tenant/odata/Folders
  REQ HEAD {"User-Agent":"UiPathOrch/0.9.16.2","Authorization":"Bearer eyJ..."}
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

## Getting Access Tokens

Extract the current access token from a PSDrive:

```powershell
Get-OrchPSDrive Orch2: | Select-Object -ExpandProperty AccessToken
```

The token is a JWT issued by the UiPath Identity Server. It can be used
with `Invoke-RestMethod` to call Orchestrator endpoints directly.

To inspect the token claims (e.g., scopes, expiry):

```powershell
$token = Get-OrchPSDrive Orch2: | Select-Object -ExpandProperty AccessToken
$payload = $token.Split('.')[1]
# Pad Base64 if needed
$padded = $payload + ('=' * (4 - $payload.Length % 4) % 4)
[System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded)) | ConvertFrom-Json
```

## API Reference

To look up available endpoints, parameters, and response schemas,
fetch the Swagger JSON directly. First, set up authentication variables
(these are reused in subsequent sections):

```powershell
$token = Get-OrchPSDrive Orch2: | Select-Object -ExpandProperty AccessToken
$root = (Get-OrchPSDrive Orch2:).Root
$headers = @{ Authorization = "Bearer $token" }
```

Orchestrator API (used by `Orch` cmdlets):

```powershell
$swagger = Invoke-RestMethod -Uri "$root/swagger/v20.0/swagger.json" -Headers $headers
$swagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*Asset*' }
$swagger.paths.'/odata/Assets'.get  # Inspect a specific endpoint
```

Identity Server API (used by `Pm` cmdlets):

```powershell
$idSwagger = Invoke-RestMethod -Uri 'https://cloud.uipath.com/identity_/swagger/internal/swagger.json' -Headers $headers
$idSwagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*User*' }
```

Test Manager API (used by `Tm` cmdlets):

```powershell
$tmSwagger = Invoke-RestMethod -Uri "$root/testmanager_/swagger/v2/swagger.json" -Headers $headers
$tmSwagger.paths.PSObject.Properties.Name | Where-Object { $_ -like '*TestSet*' }
```

Document Understanding API (used by `Du` cmdlets):

```powershell
$duSwagger = Invoke-RestMethod -Uri "$root/du_/api/documentmanager/swagger_dm.json" -Headers $headers
$duSwagger.paths.PSObject.Properties.Name
```

For on-premises or Automation Suite, replace the host with the
`IdentityUrl` from the drive configuration.

To open the Swagger UI in the user's browser:

```powershell
Start-Process "$root/swagger"
```

Official documentation:
[UiPath Orchestrator API Reference](https://docs.uipath.com/orchestrator/reference)

## Calling Endpoints Directly

When a cmdlet produces an unexpected result, reproduce the call using
`Invoke-RestMethod` to determine whether the issue is in UiPathOrch or
in the Orchestrator API.

### Step 1: Get the endpoint URL from the log

Find the relevant request in the log. Note the HTTP method, URL, and
any request body.

### Step 2: Call the endpoint

Using `$root` and `$headers` from the [API Reference](#api-reference)
section above:

Use the URL from the log, or construct it from the root URL and the
OData endpoint path.

**List folders** (equivalent to `dir`):

```powershell
(Invoke-RestMethod -Uri "$root/odata/Folders?`$select=DisplayName,FolderType" `
    -Headers $headers).value
```

**Get assets in a folder** (equivalent to `Get-OrchAsset`):

```powershell
# X-UIPATH-OrganizationUnitId header specifies the folder
$folderId = 1273640  # Get from dir output or log
$h = $headers + @{ 'X-UIPATH-OrganizationUnitId' = $folderId }
(Invoke-RestMethod -Uri "$root/odata/Assets" -Headers $h).value
```

**Get users** (equivalent to `Get-OrchUser`):

```powershell
(Invoke-RestMethod -Uri "$root/odata/Users?`$select=UserName,Type" `
    -Headers $headers).value
```

**Get machines** (equivalent to `Get-OrchMachine`):

```powershell
(Invoke-RestMethod -Uri "$root/odata/Machines?`$select=Name,Type" `
    -Headers $headers).value
```

**Create an asset** (equivalent to `New-OrchAsset`):

```powershell
$body = @{
    Name = 'TestAsset'
    ValueScope = 'Global'
    ValueType = 'Text'
    StringValue = 'hello'
} | ConvertTo-Json
Invoke-RestMethod -Uri "$root/odata/Assets" -Method Post `
    -Headers $headers -Body $body -ContentType 'application/json'
```

### Step 3: Compare results

Compare the direct API response with what UiPathOrch returned. If the
API response is the same, the issue is on the Orchestrator side. If it
differs, the issue may be in UiPathOrch's processing of the response.

## Common Investigation Workflow

1. **Reproduce the issue** with verbose logging enabled
2. **Read the log** to find the failing request
   - Look for 4xx/5xx status codes
   - Check the request URL and body for correctness
   - Check the response body for error details
3. **Extract the token** and call the endpoint directly
4. **Compare** the direct result with the cmdlet result
5. **Read the source code** if the cause is still unclear. Clone the
   repository following the
   [Building from Source](06-ContributingGuide.md#building-from-source)
   section and read the relevant C# code to understand how UiPathOrch
   processes the API response.
6. If the issue is confirmed as a UiPathOrch bug, or if you cannot
   resolve it through the steps above, file a GitHub issue following the
   [Contributing Guide](06-ContributingGuide.md). UiPathOrch is not
   covered by UiPath Technical Support -- GitHub is the only channel
   for getting help from the maintainers.
