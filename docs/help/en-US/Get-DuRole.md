---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuRole.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-DuRole
---

# Get-DuRole

## SYNOPSIS

Gets the roles available in Document Understanding.

## SYNTAX

### __AllParameterSets

```
Get-DuRole [-Path <string[]>] [-LiteralPath <string[]>] [[-Name] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the roles defined for Document Understanding projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned in alphabetical order.
This cmdlet operates on the PSDrive of the UiPathOrchDu provider.
If the scope in the configuration file includes "Du.", the PSDrive of the UiPathOrchDu provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding

OAuth required scopes: (Document Understanding PAP API)

## EXAMPLES

### Example 1: Get all Document Understanding roles

```powershell
PS Orch1Du:\> Get-DuRole
```

Gets all roles available in Document Understanding for the current drive.

### Example 2: Get roles by name with wildcards

```powershell
PS Orch1Du:\> Get-DuRole "DU Project*"
```

Gets roles whose name starts with "DU Project".

### Example 3: Get roles from a specific drive

```powershell
PS C:\> Get-DuRole -Path Orch1Du:
```

Gets all Document Understanding roles from the specified drive.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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
Type: System.String[]
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

### -Name

Specifies the name of the roles to be retrieved.
Wildcard characters are permitted.

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
  ValueFromPipelineByPropertyName: true
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

### System.String[]

You can pipe role names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.DuRole

This cmdlet returns DuRole objects representing Document Understanding roles.

## NOTES

This cmdlet operates on the UiPathOrchDu provider PSDrive. The returned roles can be used with the -Roles parameter of Add-DuUser.


## RELATED LINKS

[Get-DuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuUser.md)

[Add-DuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-DuUser.md)

[Remove-DuRoleFromDuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-DuRoleFromDuUser.md)
