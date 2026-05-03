---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchProcess
---

# Update-OrchProcess

## SYNOPSIS

Updates process settings in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchProcess [-Name] <string[]> [-NewName <string>] [-Description <string>]
 [-Version <string>] [-EntryPoint <string>] [-InputArguments <string>]
 [-SpecificPriorityValue <int>] [-Priority <string>] [-HiddenForAttendedUser <string>]
 [-RemoteControlAccess <string>] [-RetentionAction <string>] [-RetentionPeriod <int>]
 [-RetentionBucket <string>] [-StaleRetentionAction <string>] [-StaleRetentionPeriod <int>]
 [-StaleRetentionBucket <string>] [-ErrorRecordingEnabled <string>] [-Quality <int>]
 [-Frequency <int>] [-Duration <int>] [-AutoStartProcess <string>] [-AlwaysRunning <string>]
 [-A4R_Enabled <string>] [-A4R_HealingEnabled <string>] [-VideoRecordingType <string>]
 [-QueueItemVideoRecordingType <string>] [-MaxDurationSeconds <int>] [-Tags <string[]>]
 [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates process settings in UiPath Orchestrator. This cmdlet modifies process configuration properties such as description, priority, video recording, retention, and automation settings. To update the package version of a process, use Update-OrchProcessVersion instead.

Most parameters accept values by property name from the pipeline, enabling bulk updates via CSV import. The importable CSV format can be obtained using `Get-OrchProcess -Recurse -ExportCsv c:\temp\processes.csv`.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual process names in the target folders. Several other parameters also support tab completion: -Version suggests available package versions, -EntryPoint suggests entry points in the selected package, -Priority suggests job priority values, -VideoRecordingType suggests None/Failed/All, -QueueItemVideoRecordingType suggests None/Failed, -RetentionAction suggests Delete/Archive, -AlwaysRunning suggests True/False, and -RetentionBucket/-StaleRetentionBucket suggest available bucket names.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases/UiPath.Server.Configuration.OData.EditRelease

OAuth required scopes: OR.Execution

Required permissions: Processes.Edit

## EXAMPLES

### Example 1: Update the description of a process

```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -Description "Updated blank process workflow"
```

Updates the description of the process named "BlankProcess19" in the current folder.

### Example 2: Update multiple processes using wildcards

```powershell
PS Orch1:\Shared> Update-OrchProcess Blank* -VideoRecordingType Failed -ErrorRecordingEnabled True
```

Configures all processes whose name starts with "Blank" to record video only for failed executions and enables error recording. The wildcard pattern matches all matching processes in the folder.

### Example 3: Update multiple processes by comma-separated names

```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19, BlankProcess27 -RetentionAction Archive -RetentionPeriod 90 -RetentionBucket TestBucket2
```

Updates the retention settings for two specific processes at once. Completed job records will be archived to the specified bucket after 90 days.

### Example 4: Update processes recursively across all folders

```powershell
PS Orch1:\> Update-OrchProcess -Recurse -Name Blank* -HiddenForAttendedUser True -WhatIf
```

```output
What if: Performing the operation "Update Process" on target "BlankProcess19 [Shared]".
What if: Performing the operation "Update Process" on target "BlankProcess19-2 [Shared]".
What if: Performing the operation "Update Process" on target "BlankProcess27 [Shared]".
```

Previews hiding all processes starting with "Blank" from attended users across all folders. Remove -WhatIf to execute.

### Example 5: Update a process from a specific folder

```powershell
PS C:\> Update-OrchProcess -Path Orch1:\Production BlankProcess19 -AlwaysRunning True -Priority High
```

Sets the BlankProcess19 in the Production folder to always-running mode with high priority. When -Path uses an absolute path, the command can be run from any location.

### Example 6: Bulk update from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\processes.csv | Update-OrchProcess
```

Imports process settings from a CSV file and applies them. The CSV must contain a Name column (and optionally Path) plus any combination of updatable property columns. Use `Get-OrchProcess -Recurse -ExportCsv c:\temp\processes.csv` to generate the CSV template.

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

### -A4R_Enabled

Specifies whether Automation for Robots (A4R) is enabled for the process. Valid values are True and False.

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

Specifies whether A4R self-healing is enabled for the process. Valid values are True and False.

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

Specifies whether the process should always be running. When set to True, Orchestrator ensures the process is continuously running on available robots. Tab completion suggests True and False.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AutoStartProcess

Specifies whether the process should auto-start when a trigger fires. Valid values are True and False.

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

### -Description

Specifies the description for the process.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Duration

Specifies the expected duration (in minutes) for the process execution. Used for scheduling and monitoring purposes.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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

Specifies the entry point within the package. Tab completion dynamically suggests available entry points for the selected package version.

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

### -ErrorRecordingEnabled

Specifies whether error recording is enabled for the process. Valid values are True and False. When enabled, screenshots are captured on execution errors.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Frequency

Specifies the frequency value for the process. Used in conjunction with scheduling settings.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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

Specifies whether the process is hidden from attended users in the UiPath Assistant. Tab completion suggests True and False.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -InputArguments

Specifies the input arguments for the process as a JSON string. These are the default input values used when the process is started.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaxDurationSeconds

Specifies the maximum allowed execution duration in seconds. If the process runs longer than this, Orchestrator can take action (e.g., kill the job).

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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

Specifies the names of the processes to update. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders.

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

### -NewName

Specifies a new display name for the process. Use this parameter to rename a process.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Priority

Specifies the job execution priority. Tab completion suggests available priority values (e.g., Low, Normal, High).

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

### -Quality

Specifies the video recording quality as a percentage (0-100).

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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

Specifies the video recording type for queue item processing. Tab completion suggests None and Failed.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RemoteControlAccess

Specifies the remote control access level for attended robot sessions. Tab completion suggests available access levels.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RetentionAction

Specifies the action to take on completed job records after the retention period. Tab completion suggests Delete and Archive.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RetentionBucket

Specifies the storage bucket name for archived job records. Tab completion dynamically suggests available bucket names.

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

### -RetentionPeriod

Specifies the retention period (in days) for completed job records before the retention action is applied.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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

Specifies a specific numeric priority value. This parameter is hidden from tab completion and is typically used internally or for advanced scenarios.

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

Specifies the action to take on stale (pending/running) job records after the stale retention period. Valid values are Delete and Archive.

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

Specifies the storage bucket name for archived stale job records. Tab completion dynamically suggests available bucket names.

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

Specifies the retention period (in days) for stale (pending/running) job records before the stale retention action is applied.

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

Specifies the tags to assign to the process. Tags are string labels used for organizing and filtering processes.

```yaml
Type: System.String[]
DefaultValue: None
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

Specifies the package version to set for the process. Tab completion dynamically suggests available versions. Note: to update only the version without changing other settings, use Update-OrchProcessVersion instead.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -VideoRecordingType

Specifies the video recording type for process executions. Tab completion suggests None, Failed, and All.

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

### System.String

You can pipe string values to this cmdlet via parameter property names for single-value parameters such as Description, NewName, and Version.

### System.Int32

You can pipe integer values to this cmdlet via parameter property names for numeric parameters such as RetentionPeriod, Quality, and MaxDurationSeconds.

### System.String[]

You can pipe string arrays to this cmdlet via the Name and Tags properties.

## OUTPUTS

### UiPath.PowerShell.Entities.Release

Returns the updated Release object representing the process with its new settings.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

This cmdlet updates process settings only, not the package version. To update to a specific package version or the latest version, use Update-OrchProcessVersion. To roll back to the previous version, use Reset-OrchProcessVersion.

Most parameters support ValueFromPipelineByPropertyName, enabling bulk updates by piping objects from Import-Csv. The CSV format compatible with this cmdlet can be generated using `Get-OrchProcess -ExportCsv`.

## RELATED LINKS

Get-OrchProcess

Update-OrchProcessVersion

Remove-OrchProcess

Edit-OrchProcess
