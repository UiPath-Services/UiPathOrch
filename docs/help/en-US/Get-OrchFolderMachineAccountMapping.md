---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachineAccountMapping.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchFolderMachineAccountMapping
---

# Get-OrchFolderMachineAccountMapping

## SYNOPSIS

Gets account-to-machine mappings in folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchFolderMachineAccountMapping [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets account-to-machine mapping information from UiPath Orchestrator. This cmdlet shows which user accounts (robots) are mapped to specific machines within folders. Only machines that are directly assigned to the folder (not inherited via PropagateToSubFolders) are included.

The -Name parameter filters by machine name and supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

Personal workspace folders are automatically excluded from the operation.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}) and GET /odata/Robots/UiPath.Server.Configuration.OData.GetFolderRobots(folderId={folderId},machineId={machineId})

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: SubFolders.View or Units.View or Jobs.Create

## EXAMPLES

### Example 1: Get all account-to-machine mappings in the current folder

```powershell
PS Orch1:\Shared> Get-OrchFolderMachineAccountMapping
```

Gets all account-to-machine mappings from the current folder.

### Example 2: Get mappings for a specific machine

```powershell
PS Orch1:\Shared> Get-OrchFolderMachineAccountMapping MyMachine
```

Gets account mappings for the machine named 'MyMachine' in the current folder.

### Example 3: Get mappings across all folders

```powershell
PS Orch1:\> Get-OrchFolderMachineAccountMapping -Recurse
```

Gets all account-to-machine mappings from the current folder and all its subfolders.

### Example 4: Get mappings from a specific folder

```powershell
PS C:\> Get-OrchFolderMachineAccountMapping -Path Orch1:\Production
```

Gets account-to-machine mappings from the Production folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -Name

Specifies the machine names to filter by. Supports wildcards. Tab completion dynamically suggests machine names that are directly assigned (not inherited) to the target folders.

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

You can pipe machine names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedRobot

Returns ExtendedRobot objects representing user accounts mapped to machines within folders, with properties including User information (UserName, FullName).

## NOTES

Only machines that are directly assigned to the folder (PropagateToSubFolders = false) are included. Inherited machine assignments are excluded.

Personal workspace folders are automatically excluded from the operation.

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders.

## RELATED LINKS

[Enable-OrchFolderMachineAccountMapping](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchFolderMachineAccountMapping.md)

[Disable-OrchFolderMachineAccountMapping](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchFolderMachineAccountMapping.md)

[Get-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md)
