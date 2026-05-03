---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchTestSet
---

# Copy-OrchTestSet

## SYNOPSIS

Copies test sets to another folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchTestSet [-Name] <string[]> [-Destination] <string> [-Path <string>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies test sets from one UiPath Orchestrator folder to another. The cmdlet creates a new test set in the destination folder with the same configuration as the source test set. Cross-drive copy is supported, allowing test sets to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

If the source and destination folders are the same, the operation is silently skipped.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which test sets would be copied, or -Confirm to be prompted before each copy operation.

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual test set names in the source folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TestSets

OAuth required scopes: OR.TestSets

Required permissions: TestSets.Create (destination), TestSets.View (source)

## EXAMPLES

### Example 1: Copy a test set to another folder

```powershell
PS Orch1:\Shared> Copy-OrchTestSet * Orch1:\Dept#2
```

Copies all test sets from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance.

### Example 2: Copy test sets using a wildcard

```powershell
PS Orch1:\Shared> Copy-OrchTestSet Test* Orch1:\Dept#2
```

Copies all test sets matching "Test*" from the current folder to the Dept#2 folder.

### Example 3: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchTestSet * Orch1:\Dept#2 -WhatIf
```

Shows what would happen without actually copying.

### Example 4: Copy a test set across Orchestrator instances

```powershell
PS C:\> Copy-OrchTestSet -Path Orch1:\Shared * Orch2:\Shared
```

Copies all test sets from the Shared folder on Orch1 to the Shared folder on Orch2.

### Example 5: Copy all test sets recursively to another instance

```powershell
PS Orch1:\> Copy-OrchTestSet -Recurse * Orch2:\Shared
```

Copies all test sets from all folders on Orch1 to the Shared folder on Orch2.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source. Supports wildcards.

```yaml
Type: System.String
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

Includes the source folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder where test sets will be copied. Supports wildcards. Can reference a folder on a different Orchestrator drive (e.g., Orch2:\Production) for cross-instance copy.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the test sets to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests test set names from the source folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test set names to this cmdlet via the Name property.

### System.String

You can pipe a destination folder path to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSet

Returns the newly created TestSet object in the destination folder. When the source and destination are the same folder, the test set is silently skipped and no output is returned.

## NOTES

Test sets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the source folder.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet recreates the test set definition on the destination Orchestrator. The destination Orchestrator must have the referenced test cases available for the test set to function correctly.

If a test set with the same name already exists in the destination folder, the behavior depends on the Orchestrator configuration.

## RELATED LINKS

Get-OrchTestSet

Remove-OrchTestSet

Start-OrchTestSet
