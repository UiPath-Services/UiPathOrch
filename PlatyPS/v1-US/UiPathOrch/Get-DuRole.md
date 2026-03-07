---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
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
Get-DuRole [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
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
If not specified, the current drive will be targeted.

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



## RELATED LINKS

Get-DuUser

Add-DuUser

Remove-DuRoleFromDuUser
