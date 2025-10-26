---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchProcess

## SYNOPSIS
Updates processes.

## SYNTAX

```
Update-OrchProcess [-Name] <String[]> [-NewName <String>] [-Description <String>] [-Version <String>]
 [-EntryPoint <String>] [-InputArguments <String>] [-Priority <String>] [-HiddenForAttendedUser <String>]
 [-RemoteControlAccess <String>] [-RetentionAction <String>] [-RetentionPeriod <Int32>]
 [-RetentionBucket <String>] [-StaleRetentionAction <String>] [-StaleRetentionPeriod <Int32>]
 [-StaleRetentionBucket <String>] [-ErrorRecordingEnabled <String>] [-Quality <Int32>] [-Frequency <Int32>]
 [-Duration <Int32>] [-AutoStartProcess <String>] [-AlwaysRunning <String>] [-A4R_Enabled <String>]
 [-A4R_HealingEnabled <String>] [-VideoRecordingType <String>] [-QueueItemVideoRecordingType <String>]
 [-MaxDurationSeconds <Int32>] [-Tags <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Update-OrchProcess cmdlet modifies the properties and configurations of existing automation processes in UiPath Orchestrator. This cmdlet allows you to update various process settings including descriptions, priorities, recording options, retention policies, and advanced features like Auto-Restart for Robots (A4R).

You can update process metadata such as names and descriptions, execution settings like priority levels and duration limits, monitoring configurations including video recording and error recording, and lifecycle management policies such as retention rules for job artifacts.

The cmdlet supports updating advanced automation features including always-running processes, auto-start configurations, and A4R (Auto-Restart for Robots) settings with self-healing capabilities. Video recording options can be configured for different scenarios including failure analysis and quality assurance.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables updating processes from all subfolders.

The cmdlet supports CSV import functionality. Use Get-OrchProcess -Recurse -ExportCsv to obtain the format for bulk process updates, enabling efficient management of multiple processes across different folders.

Use the -NewName parameter to rename processes while maintaining their configuration and historical data. The -Tags parameter allows updating process categorization for better organization and filtering.

Primary Endpoint: POST /odata/Releases/UiPath.Server.Configuration.OData.EditRelease

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Execution.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -Description "Updated process description" -WhatIf
```

Tests updating a process description using -WhatIf to preview the operation before execution.

### Example 2
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -Priority High -MaxDurationSeconds 3600
```

Updates a process with high priority and sets maximum execution duration to 1 hour.

### Example 3
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -VideoRecordingType OnFailure -ErrorRecordingEnabled True -Quality 75
```

Configures process monitoring with video recording on failure, error recording enabled, and medium quality recording.

### Example 4
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -NewName ProductionProcess -Tags Production,Critical
```

Renames a process and updates its tags for better organization and filtering.

### Example 5
```powershell
PS Orch1:\> Update-OrchProcess "Orch1:\Development","Orch1:\Testing" -Name TestProcess -Priority Normal -Recurse
```

Updates TestProcess across Development and Testing folders and their subfolders with normal priority.

### Example 6
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -AlwaysRunning True -AutoStartProcess True -A4R_Enabled True -A4R_HealingEnabled True
```

Configures a process for continuous operation with always-running mode, auto-start, and A4R with self-healing enabled.

### Example 7
```powershell
PS Orch1:\Shared> Update-OrchProcess BlankProcess19 -RetentionAction DeleteItems -RetentionPeriod 30 -StaleRetentionAction MoveToBucket -StaleRetentionBucket Archive
```

Sets up comprehensive retention policies for process job artifacts including automatic deletion and archiving.

### Example 8
```powershell
PS Orch1:\Shared> Update-OrchProcess ProductionProcess -InputArguments '{Environment:Production,Debug:False}' -HiddenForAttendedUser False
```

Updates process with specific input arguments in JSON format and makes it visible to attended users.

### Example 9
```powershell
PS C:\> Import-Csv processes.csv | Update-OrchProcess -WhatIf
```

Tests bulk process updates from a CSV file. The CSV should contain columns like Name, Description, Priority, MaxDurationSeconds.

### Example 10
```powershell
PS Orch1:\Shared> Update-OrchProcess Msg* -RemoteControlAccess Enabled -Depth 2 -WhatIf
```

Tests updating all processes starting with Msg within 2 folder levels deep to enable remote control access.

## PARAMETERS

### -AlwaysRunning
Specifies whether the process should be always running. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AutoStartProcess
Specifies whether the process should automatically start. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Specifies the maximum number of subdirectory levels to include in the operation. Use with -Recurse to limit the depth of folder traversal.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Specifies the new description for the process.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Duration
Specifies the duration for video recording in seconds.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EntryPoint
Specifies the entry point for the process execution.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ErrorRecordingEnabled
Specifies whether error recording is enabled for debugging purposes. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Frequency
Specifies the frequency for video recording.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -HiddenForAttendedUser
Specifies whether the process should be hidden from attended users. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -InputArguments
Specifies the input arguments for the process in JSON format.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MaxDurationSeconds
Specifies the maximum duration in seconds for process execution.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the names of the processes to update. Supports wildcard patterns for bulk operations.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the paths to the folders containing the processes to update. If not specified, the current location will be used.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Priority
Specifies the priority level for the process. Valid values: Low, Normal, High.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Quality
Specifies the quality level for video recording (0-100).

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -QueueItemVideoRecordingType
Specifies the video recording type for queue item processing. Valid values: Never, Always, OnFailure.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Recurse
Includes processes in all subfolders of the specified paths. Use with -Depth to limit the recursion depth.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RemoteControlAccess
Specifies the remote control access level. Valid values: Enabled, Disabled.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetentionAction
Specifies the retention action for job artifacts. Valid values: KeepItems, DeleteItems.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetentionBucket
Specifies the bucket for retention policies.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RetentionPeriod
Specifies the retention period in days for job artifacts.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Tags
Specifies the tags for the process for organization and filtering.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Version
Specifies the version for the process.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -VideoRecordingType
Specifies when video recording should occur. Valid values: Never, Always, OnFailure.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NewName
Specifies the new name for the process.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StaleRetentionAction
Specifies the retention action for stale items. Valid values: KeepItems, DeleteItems, MoveToBucket.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StaleRetentionBucket
Specifies the bucket for stale item retention.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -StaleRetentionPeriod
Specifies the retention period in days for stale items.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -A4R_Enabled
Specifies whether Auto-Restart for Robots (A4R) is enabled for the process. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -A4R_HealingEnabled
Specifies whether A4R self-healing is enabled to automatically recover from robot failures. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.Release
## NOTES
- Use -WhatIf to preview operations before execution, especially for bulk updates.
- The cmdlet supports CSV import for bulk process updates.
- Always-running and A4R configurations require careful planning for resource usage.
- Video recording settings can significantly impact storage requirements.
- Retention policies help manage storage and maintain system performance.

## RELATED LINKS

[Get-OrchProcess](Get-OrchProcess.md)
[New-OrchProcess](New-OrchProcess.md)
[Remove-OrchProcess](Remove-OrchProcess.md)
[Start-OrchJob](Start-OrchJob.md)
