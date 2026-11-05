---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTaskCatalog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchTaskCatalog
---

# Copy-OrchTaskCatalog

## SYNOPSIS

Copies task catalogs to a destination folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchTaskCatalog [-Path <string>] [-LiteralPath <string>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies task catalogs from a source folder to a destination folder in UiPath Orchestrator. When used with -Recurse, the folder hierarchy is preserved at the destination.

Formerly Copy-OrchActionCatalog. The former name still works once the module is loaded, but tab completion does not offer it.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available task catalog names. Multiple values can be specified using comma-separated text that includes wildcards.

If the source and destination resolve to the same folder, the operation is skipped.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TaskCatalogs

OAuth required scopes: OR.Tasks

Required permissions: TaskCatalogs.View (source) and TaskCatalogs.Create (destination)

## EXAMPLES

### Example 1: Copy task catalogs to another folder

```powershell
PS Orch1:\Shared> Copy-OrchTaskCatalog * Orch1:\Dept#2
```

Copies all task catalogs from the Shared folder to the Dept#2 folder.

### Example 2: Copy task catalogs across drives

```powershell
PS C:\> Copy-OrchTaskCatalog -Path Orch1:\Shared * Orch2:\Shared
```

Copies all task catalogs from the Shared folder on Orch1 to the Shared folder on Orch2.

### Example 3: Copy task catalogs recursively

```powershell
PS Orch1:\> Copy-OrchTaskCatalog -Recurse * Orch2:\
```

Copies all task catalogs from all folders on Orch1 to the corresponding folders on Orch2, preserving the folder hierarchy.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchTaskCatalog * Orch1:\Dept#2 -WhatIf
```

Shows what task catalogs would be copied without actually performing the operation.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source.

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

### -Recurse

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy is preserved at the destination.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the source folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

Specifies the destination folder where task catalogs will be copied to.

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

Specifies the names of the task catalogs to copy. Supports wildcards. Tab completion dynamically suggests task catalog names from the source folders.

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

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

You can pipe task catalog names to this cmdlet via the Name property.

### System.String

You can pipe the Destination path to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

If the source and destination resolve to the same folder, the operation is silently skipped.

When using -Recurse, the relative folder hierarchy from the source is replicated at the destination. Destination subfolders that do not exist are skipped.

## RELATED LINKS

[Get-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskCatalog.md)

[Remove-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTaskCatalog.md)
