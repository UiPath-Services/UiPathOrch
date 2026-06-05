---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchTrigger
---

# Remove-OrchTrigger

## SYNOPSIS

Removes triggers from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchTrigger [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes triggers (process schedules) from UiPath Orchestrator folders. The triggers matching the specified names in the target folders are permanently deleted.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available trigger names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which triggers would be removed, or -Confirm to be prompted before each removal.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE /odata/ProcessSchedules({processScheduleId})

OAuth required scopes: OR.Execution

Required permissions: Schedules.Delete

## EXAMPLES

### Example 1: Remove a specific trigger

```powershell
PS Orch1:\Shared> Remove-OrchTrigger DailyInvoiceTrigger
```

Removes the trigger named "DailyInvoiceTrigger" from the current folder (Shared).

### Example 2: Preview removal with WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchTrigger *Test* -WhatIf
```

Displays what would happen if all triggers matching "*Test*" were removed, without actually deleting them. Useful for verifying wildcard matches before execution.

### Example 3: Remove triggers recursively

```powershell
PS Orch1:\> Remove-OrchTrigger -Recurse OldTrigger*
```

Removes all triggers whose name starts with "OldTrigger" from the current folder and all its subfolders.

### Example 4: Remove a trigger from a specific folder

```powershell
PS C:\> Remove-OrchTrigger -Path Orch1:\Shared ObsoleteTrigger
```

Removes the trigger named "ObsoleteTrigger" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Name

Specifies the names of the triggers to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests trigger names from the target folders. This parameter is mandatory and positional (position 0).

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe trigger names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output. Triggers are deleted from the Orchestrator server.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Removing a trigger is a permanent operation. Use -WhatIf to preview which triggers would be affected before executing the command.

## RELATED LINKS

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[New-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md)

[Disable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTrigger.md)
