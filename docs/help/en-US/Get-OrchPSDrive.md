---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPSDrive
---

# Get-OrchPSDrive

## SYNOPSIS

Gets information about UiPathOrch PSDrives.

## SYNTAX

### __AllParameterSets

```
Get-OrchPSDrive [[-Path] <string[]>] [-LiteralPath <string[]>] [-Force] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets information about UiPathOrch PSDrives that are currently available in the session. This cmdlet enumerates all drive types managed by the UiPathOrch module, including Orchestrator drives (Orch), Document Understanding drives (DU), and Test Manager drives (TM).

When the -Path parameter is specified, only the matching drives are returned. Without -Path, all registered drives of all types are enumerated.

When -Force is specified, the cmdlet actively verifies the connection to each drive by ensuring authentication and retrieving the partition global ID.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available drives.

This cmdlet is typically the first command to run after connecting to an Orchestrator instance, to verify the drive configuration and connection status.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Get all UiPathOrch drives

```powershell
PS C:\> Get-OrchPSDrive
```

Gets information about all UiPathOrch PSDrives registered in the current session.

### Example 2: Get a specific drive

```powershell
PS C:\> Get-OrchPSDrive Orch1:
```

Gets information about the Orch1: drive.

### Example 3: Verify drive connections

```powershell
PS C:\> Get-OrchPSDrive -Force
```

Gets all UiPathOrch drives and actively verifies the connection to each by authenticating and retrieving partition information.

### Example 4: Get multiple drives

```powershell
PS C:\> Get-OrchPSDrive Orch1:,Orch2:
```

Gets information about the Orch1: and Orch2: drives by specifying multiple drive names.

### Example 5: Access JWT claims

```powershell
PS C:\> $drive = Get-OrchPSDrive Orch1: -Force
PS C:\> $drive.Claims.prt_id
baa40998-d374-4814-b765-77d7b0551ae7
PS C:\> $drive.Claims.email
ytsuda@gmail.com
PS C:\> $drive.Claims.exp
2026/03/20 18:13:21
```

When -Force is used and the drive is authenticated, the Claims property contains the decoded JWT access token claims as a PSObject. Each JWT claim is available as a property. Timestamp claims (exp, iat, nbf, auth_time) are automatically converted to local DateTime.

### Example 6: Select specific JWT claims

```powershell
PS C:\> Get-OrchPSDrive Orch1: -Force | Select-Object -ExpandProperty Claims | Select-Object prt_id, email, exp, ext_idp_disp_name
```

Use Select-Object -ExpandProperty to extract claims, then select specific properties.

### Example 7: Verify the resolved deployment edition

```powershell
PS C:\> Get-OrchPSDrive | Select-Object Name, Root, Edition

Name           Root                                                                  Edition
----           ----                                                                  -------
Orch1          https://cloud.uipath.com/myorg/mytenant                               Cloud
AS_Default_INT https://rpa-emea-int.eu-central-1.aws.cloud.bmw/Default/Default       AutomationSuite
local          https://orchestrator.local/default                                    OnPremises
```

The Edition column shows whether each drive is Cloud, AutomationSuite, or OnPremises. The value is taken from the explicit `Edition` field in the configuration when set, otherwise inferred from the `Root` URL. Use this to confirm the auto-detection picked the right deployment kind for each drive.

## PARAMETERS

### -Path

Specifies the name of the target drives to retrieve.
If not specified, all UiPathOrch drives are returned.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Force

When specified, actively verifies the connection to each drive by performing authentication and retrieving the partition global ID.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.OrchPSDrive

Returns OrchPSDrive objects containing drive information such as drive name, root, description, and connection details.

Key properties include:

- **Edition** — Resolved deployment kind: `Cloud`, `AutomationSuite`, or `OnPremises`. Taken from the explicit `Edition` field in `UiPathOrchConfig.json` when set, otherwise inferred from the `Root` URL (uipath.com host → Cloud, two-segment `/{org}/{tenant}` path → AutomationSuite, anything else → OnPremises). Determines which URL pattern the module uses for Orchestrator API calls (Cloud and AS keep the tenant in the URL path; on-premises sends it in the `X-UIPATH-TenantName` header). The classification is based on URL structure, not on the underlying infrastructure — single-tenant Automation Suite deployments and Orchestrator on Azure App Service both use the on-prem URL shape (no `/{org}/{tenant}` in the path) and so resolve to `OnPremises` even though the infrastructure is AS or cloud-hosted. If the cosmetic label matters, set `"Edition"` explicitly in the config.
- **IdentityUrl** — The Identity Server URL, automatically derived from Root at drive mount time. Cloud and Automation Suite drives use `/{org}/identity_`, on-premises drives use `/identity`. Can be explicitly overridden in the configuration file.
- **Claims** — A PSObject containing decoded JWT access token claims. Available when the drive has an active access token (use -Force to ensure authentication). Timestamp claims (exp, iat, nbf, auth_time) are converted to local DateTime. Array claims (scope, aud, amr) are stored as string arrays. Access individual claims via `$drive.Claims.prt_id`, `$drive.Claims.email`, etc.

## NOTES

This cmdlet enumerates Orchestrator, Document Understanding, and Test Manager drives in sequence. It is recommended to run this cmdlet first when starting a new session to verify your drive configuration.

## RELATED LINKS

[Import-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md)

[New-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchPSDrive.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)

[Get-OrchHelp](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchHelp.md)
