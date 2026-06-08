---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchTrigger
---

# Disable-OrchTrigger

## SYNOPSIS

Disables triggers in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Disable-OrchTrigger [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables enabled triggers (process schedules) in UiPath Orchestrator folders. Only triggers that are currently enabled are processed; triggers that are already disabled are skipped.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available trigger names dynamically populated from enabled triggers in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which triggers would be disabled, or -Confirm to be prompted before each operation.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Jobs or OR.Jobs.Write

Required permissions: Schedules.Edit

## EXAMPLES

### Example 1: Disable a specific trigger

```powershell
PS Orch1:\root> Disable-OrchTrigger "high trigger"
```

Disables the trigger named "high trigger" in the current folder (root). If the trigger is already disabled, it is skipped.

### Example 2: Disable triggers using a wildcard

```powershell
PS Orch1:\root> Disable-OrchTrigger *trigger*
```

Disables all enabled triggers whose name contains "trigger" in the current folder.

### Example 3: Disable triggers recursively

```powershell
PS Orch1:\> Disable-OrchTrigger -Recurse *
```

Disables all enabled triggers across all folders recursively.

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

Specifies the names of the triggers to disable. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests names of enabled triggers from the target folders. This parameter is mandatory and positional (position 0).

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

This cmdlet does not produce output. Triggers are disabled on the Orchestrator server.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Only enabled triggers are processed. Triggers that are already disabled are silently skipped.

## RELATED LINKS

[Enable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTrigger.md)

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[Update-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTrigger.md)

[CSV Export & Import Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/05-CsvExportImport.md)
