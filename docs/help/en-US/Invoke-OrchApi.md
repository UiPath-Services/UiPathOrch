---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Invoke-OrchApi.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/13/2026
PlatyPS schema version: 2024-05-01
title: Invoke-OrchApi
---

# Invoke-OrchApi

## SYNOPSIS

Invokes an arbitrary Orchestrator, Identity Server, or Portal API endpoint using the authentication and folder context bound to a UiPathOrch PSDrive.

## SYNTAX

### __AllParameterSets

```
Invoke-OrchApi [-ApiPath] <string> [-Body <Object>] [-Confirm] [-ContentType <string>]
 [-Headers <IDictionary>] [-Identity] [-InFile <string>] [-LiteralPath <string>]
 [-Method <string>] [-OutFile <string>] [-Path <string>] [-Portal] [-Raw]
 [-ResponseHeadersVariable <string>] [-SkipFolderContext] [-SkipHttpErrorCheck]
 [-StatusCodeVariable <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Invokes an arbitrary Orchestrator API endpoint through the same authenticated HTTP session that the typed cmdlets use. Designed as a superset of `Invoke-RestMethod` for the parameters that make sense in this context — auth, proxy, certificate, and session parameters are intentionally omitted because the drive owns those settings.

The drive supplied via `-Path` selects the tenant and (when a folder is included) the folder context that is sent as the `X-UIPATH-OrganizationUnitId` header. The `-Uri` parameter accepts either an absolute URL or a relative path; relative paths are resolved against the drive's base URL. The `-Identity` and `-Portal` switches retarget the request at the Identity Server or Portal base URLs respectively.

JSON responses are converted to `PSCustomObject` so they integrate cleanly with `Format-Table`, `Select-Object`, and `Sort-Object`. For OData collection responses (`{ "value": [...] }`), the wrapper is unwrapped and each item is emitted with an injected `Path` property indicating the originating drive context. The default formatter groups output by `Path`. Use `-Raw` to keep the unparsed response, including the OData envelope.

For non-GET HTTP methods, the cmdlet calls `ShouldProcess`, so `-WhatIf` previews the call without executing it and `-Confirm` prompts before executing. Pass `-Confirm:$false` to bypass the prompt in scripts.

This cmdlet is the recommended path for diagnostic API calls that are not yet covered by a typed cmdlet, and for reproducing a cmdlet's request directly to isolate whether a bug is in UiPathOrch or in the Orchestrator API. Because the bearer token never leaves the module, the cmdlet is also safe to use in CI workflows where the token must not appear in pipeline logs.

Primary Endpoint: caller-supplied via -Uri

OAuth required scopes: depends on -Uri (must be granted to the drive's external application)

Required permissions: depends on -Uri

## EXAMPLES

### Example 1: List folders in a tenant

```powershell
PS C:\> Invoke-OrchApi -Path Orch1: -Uri '/odata/Folders?$select=Id,DisplayName,FolderType'

   Path: Orch1:\

DisplayName : Dept#2
FolderType  : Standard
Id          : 1005605

DisplayName : fuga
FolderType  : Standard
Id          : 1005606
```

The OData `value` array is unwrapped automatically. Each item is a `PSCustomObject` with the response fields plus an injected `Path` property. The default view groups by `Path`.

### Example 2: List assets in a specific folder

```powershell
PS C:\> Invoke-OrchApi -Path Orch1:\Shared -Uri '/odata/Assets?$select=Id,Name,ValueType' |
            Format-Table Id, Name, ValueType, Path -AutoSize
```

The folder portion of `-Path` (`Shared`) is resolved to a folder Id and supplied to the request as the `X-UIPATH-OrganizationUnitId` header — the same header the typed cmdlets send. Format-Table works because the output is `PSCustomObject`.

### Example 3: Create an asset (POST with body)

```powershell
PS C:\> Invoke-OrchApi -Path Orch1:\Shared -Uri '/odata/Assets' -Method Post -Body @{
    Name        = 'TestAsset'
    ValueScope  = 'Global'
    ValueType   = 'Text'
    StringValue = 'hello'
}
```

For non-GET methods the cmdlet calls `ShouldProcess`. Add `-WhatIf` to preview the request without sending it, or `-Confirm:$false` to bypass the prompt in non-interactive scripts. The `-Body` value is a hashtable that is serialized to JSON automatically; pass a string to send a pre-formatted body.

### Example 4: Inspect a 4xx response without an exception

```powershell
PS C:\> $body = Invoke-OrchApi -Path Orch1: -Uri '/odata/Folders(99999999)' `
                    -SkipHttpErrorCheck `
                    -StatusCodeVariable sc `
                    -ResponseHeadersVariable rh
PS C:\> "$sc"
404
PS C:\> $body
message   : An error has occurred.
errorCode : 0
traceId   : 00-...
```

`-SkipHttpErrorCheck` returns the parsed body for any status code instead of throwing on 4xx/5xx. Pair with `-StatusCodeVariable` and `-ResponseHeadersVariable` to capture the status code and response headers in PowerShell variables.

### Example 5: Call the Identity Server API

```powershell
PS C:\> Invoke-OrchApi -Path Orch1: -Identity -Uri '/swagger/internal/swagger.json' -Raw |
            ConvertFrom-Json |
            Select-Object -ExpandProperty paths |
            Get-Member -MemberType NoteProperty |
            Where-Object Name -like '*User*'
```

The `-Identity` switch retargets the request at the Identity Server base URL (`https://cloud.uipath.com/{org}/identity_/...` for cloud, or the on-premises Identity URL). `-Portal` does the same for the Portal API. The folder context header is suppressed for these endpoints because they are tenant-or-organization-scoped.

### Example 6: Download a binary response to a file

```powershell
PS C:\> Invoke-OrchApi -Path Orch1: -Uri "/odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='MyPackage:1.0.0')" `
            -OutFile './MyPackage.1.0.0.nupkg'
```

`-OutFile` streams the response body directly to a file without parsing it as JSON. No object output is emitted on success.

### Example 7: Compare a cmdlet output with the raw API

```powershell
PS C:\> Get-OrchAsset -Path Orch1:\Shared -Name TestAsset1 | Format-List
PS C:\> Invoke-OrchApi -Path Orch1:\Shared -Uri "/odata/Assets?`$filter=Name eq 'TestAsset1'"
```

When a cmdlet returns unexpected results, reproducing the call with `Invoke-OrchApi` shows the raw API response. If the cmdlet output differs from the API response, the issue is in UiPathOrch's response processing; if the API response itself is wrong, the issue is on the Orchestrator side.

## PARAMETERS

### -ApiPath

The API endpoint to invoke. Accepts either an absolute URL (`https://...`) or a relative path. Relative paths are resolved against the drive's base URL — `_base_url_orchestrator` (the Orchestrator service base) by default, or `_base_url_identity` / `_base_url_portal` when `-Identity` or `-Portal` is set. Use `-Uri` as the alias when porting `Invoke-RestMethod` snippets.

For Automation Suite drives, `_base_url_orchestrator` includes the `/orchestrator_/` service prefix automatically, so `-ApiPath /odata/Folders` resolves to `https://{host}/{org}/{tenant}/orchestrator_/odata/Folders` without you having to spell the prefix out. For Cloud and on-premises drives the prefix isn't required and isn't added.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Uri
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Body

Request body. A hashtable or any non-string object is serialized to JSON. A string is sent as-is with the configured ContentType. A byte[] is sent unchanged with the configured ContentType (defaulting to application/octet-stream when ContentType is empty). Mutually exclusive with -InFile.

```yaml
Type: System.Object
DefaultValue: ''
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

### -Confirm

Prompts you for confirmation before running the cmdlet. Confirmation is requested when the HTTP method is not GET or HEAD.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
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

### -ContentType

Content-Type header value sent with the request body. Defaults to `application/json`.

```yaml
Type: System.String
DefaultValue: 'application/json'
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

### -Headers

Additional request headers as a hashtable or any IDictionary. Authorization is silently dropped — the drive owns authentication and lets it never leak through user input.

```yaml
Type: System.Collections.IDictionary
DefaultValue: ''
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

### -Identity

Retargets the request at the Identity Server base URL (`_base_url_identity`). Used by Pm cmdlets to call Identity Server endpoints. The folder context header is suppressed because Identity endpoints are organization-scoped, not folder-scoped. Mutually exclusive with -Portal.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -InFile

Sends the contents of a local file as the request body. The Content-Type defaults to `application/octet-stream` when -ContentType is not specified. Mutually exclusive with -Body.

```yaml
Type: System.String
DefaultValue: ''
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

### -Method

HTTP method. Defaults to GET. For methods other than GET and HEAD, the cmdlet calls ShouldProcess so -WhatIf and -Confirm apply.

```yaml
Type: System.String
DefaultValue: GET
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

### -OutFile

Writes the response body directly to the named file as bytes, without parsing it. No object output is emitted on success. Pair with `-SkipHttpErrorCheck` to retain the file even on a non-success status code.

```yaml
Type: System.String
DefaultValue: ''
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

### -Path

The drive (and optional folder) that supplies the authenticated session, base URL, and folder context for the request. Examples: `Orch1:` (tenant root), `Orch1:\Shared` (Shared folder context). When omitted, the current PowerShell location is used and must be on a UiPathOrch drive. Tab completion lists known drives and folders.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Portal

Retargets the request at the Portal base URL (`_base_url_portal`). Used internally by some cmdlets to call undocumented Portal APIs. The folder context header is suppressed. Mutually exclusive with -Identity.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -Raw

Returns the unparsed response body as a string instead of converting it to a PSObject. Suppresses the OData `value` unwrap, the `Path` property injection, and the default Format view. Use this to retain the OData envelope (`@odata.context`, `@odata.count`, `@odata.nextLink`).

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -ResponseHeadersVariable

Name of a PowerShell variable to populate with the response headers as a case-insensitive Hashtable. Both response and content headers are merged into the same hashtable.

```yaml
Type: System.String
DefaultValue: ''
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

### -SkipFolderContext

Suppresses the automatic injection of the `X-UIPATH-OrganizationUnitId` header even when -Path includes a folder. Use this for tenant-scope endpoints that must not receive a folder context.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -SkipHttpErrorCheck

Returns the parsed response body even when the status code indicates failure (4xx/5xx) instead of writing an error record. Pair with -StatusCodeVariable and -ResponseHeadersVariable to capture the full diagnostic context for error responses.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -StatusCodeVariable

Name of a PowerShell variable to populate with the HTTP status code as an Int32.

```yaml
Type: System.String
DefaultValue: ''
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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions. Applies when the HTTP method is not GET or HEAD.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
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

### System.String

The drive path can be received from the pipeline by property name (`Path`).

## OUTPUTS

### System.Management.Automation.PSObject

JSON responses are converted to `PSCustomObject`. Each emitted record carries an injected `Path` property, and the `UiPathOrch.ApiResponseItem` PSTypeName which the default Format view uses to group output by `Path`.

### System.String

Returned when -Raw is specified, when the response is not parseable as JSON, or when no JSON content is present.

## NOTES

The bearer token is never written into user space. This cmdlet is the recommended way to call arbitrary endpoints from CI / automation contexts where extracting the access token would be a leakage hazard.

The `Authorization` header in any custom `-Headers` value is silently dropped — the drive's authentication takes precedence and cannot be overridden from user input.

The Authorization header is masked in HTTP log files when logging is enabled, so verbose logs are safe to share.

## RELATED LINKS

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)

[New-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchPSDrive.md)

[Get-OrchHelp](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchHelp.md)
