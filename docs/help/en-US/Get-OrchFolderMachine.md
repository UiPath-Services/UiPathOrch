---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchFolderMachine
---

# Get-OrchFolderMachine

## SYNOPSIS

Gets machines assigned to folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchFolderMachine [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets machine-to-folder assignment information from UiPath Orchestrator. This cmdlet retrieves machines that are directly assigned to the target folders (inherited machines are not included).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports CSV export with columns: Path, Name, PropagateToSubFolders.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={folderId})

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Units.View or SubFolders.View

## EXAMPLES

### Example 1: Get all machines assigned to the current folder

```powershell
PS Orch1:\reproNew> Get-OrchFolderMachine
```

Gets all machines assigned to the current folder.

### Example 2: Get machines by name

```powershell
PS Orch1:\reproNew> Get-OrchFolderMachine machine*
```

Gets machines whose names match `machine*` from the current folder.

### Example 3: Get machines assigned across all folders

```powershell
PS Orch1:\> Get-OrchFolderMachine -Recurse
```

Gets all machine assignments from the current folder and all its subfolders. When run from the root folder, this shows all machine-to-folder assignments across the entire tenant.

### Example 4: Find which folders a machine is assigned to

```powershell
PS Orch1:\> Get-OrchFolderMachine -Recurse machine1
```

Finds all folders where the machine named "machine1" is assigned.

### Example 5: Export machine assignments to CSV

```powershell
PS Orch1:\> Get-OrchFolderMachine -Recurse -ExportCsv C:\temp\folder-machines.csv
```

Exports all machine-to-folder assignments to a CSV file with columns: Path, Name, PropagateToSubFolders.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExportCsv

Exports machine-to-folder assignments to the specified CSV file path. The CSV includes columns: Path, Name, PropagateToSubFolders. Requires a filesystem path (not an Orch: drive path). If only a filename is specified, the default filename 'ExportedFolderMachines.csv' is used.

```yaml
Type: System.String
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

Specifies the names of the machines to retrieve. Supports wildcards. Tab completion dynamically suggests machine names assigned to the target folders.

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

### UiPath.PowerShell.Entities.MachineFolder

Returns MachineFolder objects representing machines assigned to folders, with properties including Name, PropagateToSubFolders, and IsAssignedToFolder.

## NOTES

This cmdlet only returns machines that are directly assigned to the folder. Inherited machines (propagated from parent folders) are not included.

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders.

## RELATED LINKS

[Add-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderMachine.md)

[Remove-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderMachine.md)

[Copy-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderMachine.md)

[Enable-OrchFolderMachineInherit](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchFolderMachineInherit.md)

[Disable-OrchFolderMachineInherit](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchFolderMachineInherit.md)
