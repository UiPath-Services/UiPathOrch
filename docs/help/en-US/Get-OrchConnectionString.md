---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchConnectionString.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchConnectionString
---

# Get-OrchConnectionString

## SYNOPSIS

Gets the connection string from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchConnectionString [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the connection string from UiPath Orchestrator. The connection string contains the database connection information used by the Orchestrator instance.

The cmdlet uses multi-threaded processing to retrieve connection strings from multiple drives in parallel.

Connection strings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetConnectionString

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get the connection string

```powershell
PS Orch1:\> Get-OrchConnectionString
```

Gets the connection string from the current Orchestrator tenant.

### Example 2: Get the connection string from a specific drive

```powershell
PS C:\> Get-OrchConnectionString Orch1:\
```

Gets the connection string from the Orch1 tenant.

### Example 3: Get connection strings from multiple drives

```powershell
PS C:\> Get-OrchConnectionString Orch1:\,Orch2:\
```

Gets the connection strings from both Orch1 and Orch2 tenants.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

You can pipe drive paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.ODataValueOfString

Returns an ODataValueOfString object containing the connection string value.

## NOTES

Connection strings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve connection strings from multiple drives in parallel.

## RELATED LINKS

Get-OrchSetting
