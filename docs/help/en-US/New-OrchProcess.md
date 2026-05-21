---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchProcess.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchProcess
---

# New-OrchProcess

## SYNOPSIS

Creates a new process in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchProcess [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Id] <string[]>
 [[-Version] <string>] [-A4R_Enabled <string>] [-A4R_HealingEnabled <string>]
 [-AlwaysRunning <string>] [-AutoStartProcess <string>] [-Confirm] [-Description <string>]
 [-Duration <int>] [-EntryPoint <string>] [-ErrorRecordingEnabled <string>]
 [-Frequency <int>] [-HiddenForAttendedUser <string>] [-InputArguments <string>]
 [-MaxDurationSeconds <int>] [-Name <string>] [-Priority <string>] [-Quality <int>]
 [-QueueItemVideoRecordingType <string>] [-RemoteControlAccess <string>]
 [-RetentionAction <string>] [-RetentionBucket <string>] [-RetentionPeriod <int>]
 [-SpecificPriorityValue <int>] [-StaleRetentionAction <string>]
 [-StaleRetentionBucket <string>] [-StaleRetentionPeriod <int>] [-Tags <string[]>]
 [-VideoRecordingType <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new process (Release) in a UiPath Orchestrator folder by linking a package from the NuGet feed. A process associates a published package with a folder, making it available for job execution by robots in that folder.

The -Id parameter specifies the package ID from the NuGet feed, not the process display name. Use tab completion to browse available package IDs. When -Version is omitted, the latest available version of the package is used. When -Name is omitted, the process display name defaults to the package ID.

This cmdlet provides extensive configuration options for job retention, video and screenshot recording, priority, and Automation for Robots (A4R) settings. Most optional parameters have sensible defaults and do not need to be specified for basic process creation.

The -Id, -Version, -EntryPoint, -Priority, -RetentionBucket, and -StaleRetentionBucket parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id and -Version completions are dynamically populated from the package feed.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Processes.Create

## EXAMPLES

### Example 1: Create a process with default settings

```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess19
```

Creates a process from the package "BlankProcess19" using the latest available version. The process display name defaults to "BlankProcess19" and all other settings use their defaults.

### Example 2: Create a process with a specific version

```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess19 1.0.3
```

Creates a process from the package "BlankProcess19" pinned to version 1.0.3. When a version is specified, the process will not automatically update when newer package versions are published.

### Example 3: Create a process with a custom display name

```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess19 -Name "Invoice Processing" -Description "Processes vendor invoices from the shared mailbox"
```

Creates a process from the package "BlankProcess19" with the custom display name "Invoice Processing" and a description. The custom name appears in the Orchestrator UI instead of the package ID.

### Example 4: Create a process with priority and recording settings

```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess19 -Priority High -VideoRecordingType Failed -ErrorRecordingEnabled True
```

Creates a process with high priority, video recording enabled for failed jobs, and screenshot recording on error enabled. These settings apply to all jobs started from this process.

### Example 5: Preview the process creation with WhatIf

```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess19 1.0.3 -WhatIf
```

```output
What if: Performing the operation "New Process" on target "Orch1:\Shared\BlankProcess19:1.0.3".
```

Displays what would happen without actually creating the process. This is useful for verifying the package ID, version, and target folder before committing the change.

### Example 6: Create a process in a specific folder

```powershell
PS C:\> New-OrchProcess -Path Orch1:\Production -Id BlankProcess19 -Version 1.0.3 -Name "Prod Invoice Processing"
```

Creates a process in the Production folder using an absolute path. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 7: Create processes from CSV import

```powershell
PS Orch1:\Shared> Import-Csv C:\temp\processes.csv | New-OrchProcess
```

Creates processes from a CSV file. The CSV columns are bound to parameters via ValueFromPipelineByPropertyName. The CSV can include columns such as Id, Version, Name, Description, Priority, and SpecificPriorityValue to configure each process.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: ''
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
DefaultValue: ''
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
DefaultValue: ''
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

### -A4R_Enabled

Enables or disables Automation for Robots (A4R) for this process. A4R allows attended robots to run automations triggered by user actions. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -A4R_HealingEnabled

Enables or disables A4R self-healing for this process. When enabled, A4R attempts to automatically recover from failures. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -AlwaysRunning

Enables or disables the always-running mode for this process. When enabled, Orchestrator ensures that a job for this process is always running. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -AutoStartProcess

Enables or disables auto-start for this process. When enabled, a job is automatically started when a robot becomes available. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -Description

Specifies a custom description for the process. If omitted, the description is inherited from the package description defined in the NuGet feed.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Duration

Specifies the maximum duration in seconds for screenshot recording per job execution. Default is 40. This setting only applies when -ErrorRecordingEnabled is True.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -EntryPoint

Specifies the .xaml entry point file for the process. If omitted, the main entry point defined in the package is used. Tab completion dynamically suggests available .xaml files from the package.

```yaml
Type: System.String
DefaultValue: ''
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

### -ErrorRecordingEnabled

Enables or disables screenshot recording on error for this process. When enabled, screenshots are captured during job execution for debugging purposes. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Frequency

Specifies the screenshot capture frequency in milliseconds. Default is 500. This setting only applies when -ErrorRecordingEnabled is True.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -HiddenForAttendedUser

Specifies whether the process is hidden from attended users in the UiPath Assistant. Valid values are True and False. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Id

Specifies the package ID from the NuGet feed. This is the package identifier, not the process display name. Tab completion dynamically suggests available package IDs from the feed. This parameter is mandatory and accepts positional input at position 0.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -InputArguments

Specifies input arguments for the process as a JSON string. The arguments must match the input parameters defined in the package workflow. Tab completion dynamically suggests available input arguments from the package.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -MaxDurationSeconds

Specifies the maximum allowed duration in seconds for a job started from this process. Jobs exceeding this duration are terminated. Default is approximately 180 seconds.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

Specifies a custom display name for the process. If omitted, the process name defaults to the package ID. The display name appears in the Orchestrator UI and can differ from the underlying package ID.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Priority

Specifies the job priority for this process. Valid values are Critical, Highest, VeryHigh, High, MediumHigh, Medium, MediumLow, Low, VeryLow, and Lowest. If not specified, the default maps to a numeric priority of 45. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
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

### -Quality

Specifies the screenshot quality as a percentage (0-100). Default is 100. This setting only applies when -ErrorRecordingEnabled is True.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -QueueItemVideoRecordingType

Specifies the video recording type for queue item processing. Valid values are None and Failed. When set to Failed, video is recorded only for queue items that fail during processing.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -RemoteControlAccess

Specifies the remote control access level for attended robots running this process. Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -RetentionAction

Specifies the retention action for completed jobs. Valid values are Delete and Archive. Determines whether completed jobs are deleted or archived after the retention period.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -RetentionBucket

Specifies the storage bucket name for archiving completed jobs. Only applicable when -RetentionAction is Archive. Tab completion dynamically suggests available storage bucket names.

```yaml
Type: System.String
DefaultValue: ''
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

### -RetentionPeriod

Specifies the retention period in days for completed jobs before the retention action is applied. Default is 30.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -SpecificPriorityValue

Specifies the numeric priority value (0-90) for the process. This parameter is used internally for CSV import operations and is not shown in standard parameter lists. Use -Priority for human-readable priority values.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -StaleRetentionAction

Specifies the retention action for stale (pending/running) jobs. Valid values are Delete and Archive. Determines whether stale jobs are deleted or archived after the stale retention period.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -StaleRetentionBucket

Specifies the storage bucket name for archiving stale jobs. Only applicable when -StaleRetentionAction is Archive. Tab completion dynamically suggests available storage bucket names.

```yaml
Type: System.String
DefaultValue: ''
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

### -StaleRetentionPeriod

Specifies the retention period in days for stale (pending/running) jobs before the stale retention action is applied.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
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

### -Tags

Specifies one or more tags to assign to the process. Tags help organize and categorize processes in the Orchestrator UI.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

### -Version

Specifies the package version to use. If omitted, the latest available version is used. Tab completion dynamically suggests available versions for the specified package ID. This parameter accepts positional input at position 1.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -VideoRecordingType

Specifies the video recording type for jobs started from this process. Valid values are None, Failed, and All. When set to Failed, video is recorded only for failed jobs. When set to All, video is recorded for every job execution.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### System.String

You can pipe package IDs to this cmdlet via the Id property.

### System.Int32

You can pipe numeric values to this cmdlet via properties such as RetentionPeriod, Quality, Frequency, and Duration.

### System.String[]

You can pipe string arrays to this cmdlet via properties such as Id, Tags, and Path.

## OUTPUTS

### UiPath.PowerShell.Entities.Release

Returns a Release object representing the newly created process. Properties include Id, Name, ProcessKey, ProcessVersion, Description, EntryPointPath, InputArguments, and FolderPath.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The -Id parameter refers to the package ID in the NuGet feed, not the process display name. A package must be published to the feed before it can be linked as a process. Use Get-OrchPackage to list available packages.

The -SpecificPriorityValue parameter is hidden from standard usage and is intended for CSV import scenarios where the numeric priority value (0-90) is used directly instead of the human-readable -Priority parameter.

Screenshot recording parameters (-Quality, -Frequency, -Duration) only take effect when -ErrorRecordingEnabled is set to True.

## RELATED LINKS

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)

[Remove-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchProcess.md)

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)
