---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchTrigger
---

# Copy-OrchTrigger

## SYNOPSIS

Copies triggers to another folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchTrigger [-Path <string>] [-LiteralPath <string>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies triggers (process schedules) from one UiPath Orchestrator folder to another. The cmdlet creates a new trigger in the destination folder using the same configuration as the source trigger, including executor robots and machine robots settings. Cross-drive copy is supported, allowing triggers to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual trigger names in the source folders. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which triggers would be copied, or -Confirm to be prompted before each copy operation.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/ProcessSchedules

OAuth required scopes: OR.Folders or OR.Folders.Read, OR.Jobs, and OR.Execution or OR.Execution.Read (both sides; OR.Jobs or OR.Jobs.Read is enough on the source). Depending on what the trigger references, also: OR.Queues or OR.Queues.Read for a queue trigger, OR.Settings or OR.Settings.Read for a non-working-days calendar, and OR.Robots or OR.Robots.Read plus OR.Machines or OR.Machines.Read for executor robots and machine–robot pairs. With -Recurse, OR.Folders or OR.Folders.Write on the destination if missing subfolders have to be created.

Required permissions: Schedules.View and Processes.View (source); Schedules.Create and Processes.View (destination). Depending on the trigger, also Queues.View, Robots.View, and Machines.View; with -Recurse, Units.Create or SubFolders.Create to create missing destination subfolders. These permissions are checked for user sign-ins (non-confidential application, PAT) and for a confidential application whose token includes OR.Default. A confidential application that requests individual OR.* scopes without OR.Default is authorized by those scopes alone, so it needs no folder assignment on either side.

## EXAMPLES

### Example 1: Copy a trigger to another folder

```powershell
PS Orch1:\Shared> Copy-OrchTrigger "high trigger" Dept#2
```

Copies the trigger named "high trigger" from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance.

### Example 2: Copy triggers using a wildcard

```powershell
PS Orch1:\Shared> Copy-OrchTrigger *trigger* Dept#2
```

Copies all triggers matching "*trigger*" from the current folder to the Dept#2 folder.

### Example 3: Copy a trigger across Orchestrator instances

```powershell
PS C:\> Copy-OrchTrigger -Path Orch1:\Shared "high trigger" Orch2:\Shared
```

Copies the trigger named "high trigger" from the Shared folder on Orch1 to the Shared folder on Orch2. Trigger settings including executor robots and machine robots are carried over.

### Example 4: Copy all triggers recursively

```powershell
PS Orch1:\> Copy-OrchTrigger -Recurse * Orch2:\Shared
```

Copies all triggers from all folders on Orch1 to the Shared folder on Orch2.

### Example 5: Preview copy with WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchTrigger "high trigger" Dept#2 -WhatIf
```

Shows what would happen without actually copying. Useful for verifying which triggers would be affected before execution.

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

### -Destination

Specifies the destination folder where triggers will be copied. Supports wildcards. Can reference a folder on a different Orchestrator drive (e.g., Orch2:\Production) for cross-instance copy.

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

Specifies the names of the triggers to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests trigger names from the source folders. This parameter is mandatory and positional (position 0).

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

### System.String

You can pipe a destination folder path to this cmdlet via the Destination property.

## OUTPUTS

### None

This cmdlet produces no pipeline output. Progress is reported through Write-Progress, and per-trigger problems through warnings and non-terminating errors — including the warning that each copied trigger is created disabled. Verify the result with `Get-OrchTrigger` on the destination, or compare both sides with `Compare-OrchTrigger`.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the source folder.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet copies trigger settings including executor robots and machine robots. The destination folder must have a process (Release) with the same name, and a queue trigger needs a queue with the same name there; a referenced calendar must exist in the destination tenant (calendars are tenant-level).

A reference that does not exist in the destination and a lookup that fails are handled differently. When a robot, machine, or calendar has no counterpart in the destination, a warning is written, the trigger is still created, and that reference is left out of the copy (a missing process, or a missing queue for a queue trigger, skips the trigger instead). When a lookup fails (for example, 403 because OR.Robots, OR.Machines, OR.Queues, OR.Settings, or OR.Execution is missing), an error naming the reference is written and that trigger is not created; the remaining triggers are still copied. This keeps a missing scope from producing a trigger that silently lost its executor robots, which would run on any robot in the folder once enabled. If source and destination resolve to the same folder, the cmdlet does nothing.

## RELATED LINKS

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[New-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md)

[Update-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTrigger.md)

[Remove-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTrigger.md)
