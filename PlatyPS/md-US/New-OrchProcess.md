---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchProcess

## SYNOPSIS
Creates automation processes from published packages.

## SYNTAX

```
New-OrchProcess [-Id] <String[]> [[-Version] <String>] [-Name <String>] [-Description <String>]
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
The New-OrchProcess cmdlet creates automation processes from published packages in UiPath Orchestrator. A process is a deployable automation workflow that can be executed by robots. This cmdlet allows you to configure process settings including entry points, input arguments, priority levels, recording options, and retention policies.

**This is a folder entity cmdlet.** To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using the -Path, -Recurse, or -Depth parameters. If you attempt to run this cmdlet without being in a folder context, you will receive the error: "Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters."

The process is created from an existing package identified by its Id. You can specify a particular version or use the latest version by default. Process configuration includes execution settings, monitoring options, and lifecycle management policies such as retention rules for job artifacts.

The cmdlet supports CSV import functionality. Use Get-OrchProcess -Recurse -ExportCsv to obtain the format for bulk process creation.

Primary Endpoint: POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
OAuth required scopes: OR.Execution or OR.Execution.Write
Required permissions: Execution.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess4 -WhatIf
```

Tests creating a new process from the BlankProcess4 package using the latest version and default settings.

### Example 2
```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess4 -Name ProductionProcess -Description "Production automation process"
```

Creates a process from the BlankProcess4 package with a custom name and description.

### Example 3
```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess4 1.0.2 -Priority High -MaxDurationSeconds 3600
```

Creates a process from a specific package version with high priority and 1-hour maximum duration limit.

### Example 4
```powershell
PS Orch1:\Shared> New-OrchProcess BackgroundProcess -VideoRecordingType OnFailure -ErrorRecordingEnabled True -Quality 75
```

Creates a process with video recording enabled on failure, error recording enabled, and medium quality recording.

### Example 5
```powershell
PS Orch1:\> New-OrchProcess -Path Orch1:\Development,Orch1:\Testing -Recurse MultiEnvironment
```

Creates a process named MultiEnvironment across multiple folders using the -Path parameter.

### Example 6
```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess4 -InputArguments '{arg1:value1,arg2:value2}' -EntryPoint Main.xaml
```

Creates a process with specific input arguments in JSON format and a custom entry point.

### Example 7
```powershell
PS Orch1:\Shared> New-OrchProcess BackgroundProcess -AutoStartProcess True -AlwaysRunning True -A4R_Enabled True -A4R_HealingEnabled True
```

Creates an always-running process with Auto-Restart for Robots (A4R) enabled including self-healing capabilities.

### Example 8
```powershell
PS Orch1:\Shared> New-OrchProcess BlankProcess4 -RetentionAction DeleteItems -RetentionPeriod 30 -StaleRetentionAction MoveToBucket -StaleRetentionBucket Archive
```

Creates a process with comprehensive retention policies for job artifacts and stale item management.

### Example 9
```powershell
PS C:\> Import-Csv processes.csv | New-OrchProcess -WhatIf
```

Tests bulk process creation from a CSV file. The CSV should contain columns like Id, Name, Description, Priority, MaxDurationSeconds.

### Example 10
```powershell
PS Orch1:\Shared> New-OrchProcess ReportGenerator -Tags Financial,Critical -HiddenForAttendedUser False -RemoteControlAccess Enabled
```

Creates a process with tags for organization, visible to attended users, and remote control access enabled.

## PARAMETERS

### -AlwaysRunning
Specifies the AlwaysRunning of the processes to be created.

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
Specifies the AutoStartProcess of the processes to be created.

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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.

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
Specifies the Description of the processes to be created.

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
Specifies the Duration of the processes to be created.

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
Specifies the EntryPoint of the processes to be created.

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
Specifies the ErrorRecordingEnabled of the processes to be created.

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
Specifies the Frequency of the processes to be created.

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
Specifies the HiddenForAttendedUser of the processes to be created.

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

### -Id
[PLACEHOLDER - requires verification of Id parameter description]

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

### -InputArguments
Specifies the InputArguments of the processes to be created.

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
Specifies the MaxDurationSeconds of the processes to be created.

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
Specifies the Name of the processes to be created.

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

### -Path
Specifies the source folders. If not specified, the current folder will be used as the source.

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
Specifies the Priority of the processes to be created.

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
Specifies the Quality of the processes to be created.

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
Specifies the QueueItemVideoRecordingType of the processes to be created.

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
Specifies that the operation should include the target folder and all its subfolders.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -RemoteControlAccess
Specifies the RemoteControlAccess of the processes to be created.

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
Specifies the RetentionAction of the processes to be created.

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
Specifies the RetentionBucket of the processes to be created.

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
Specifies the RetentionPeriod of the processes to be created.

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
Specifies the Tags of the processes to be created.

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
Specifies the Version of the processes to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -VideoRecordingType
Specifies the VideoRecordingType of the processes to be created.

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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Determines how PowerShell responds to progress updates generated by this cmdlet. The default value is Continue.

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

### -StaleRetentionAction
[PLACEHOLDER - requires verification of StaleRetentionAction parameter description]

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
[PLACEHOLDER - requires verification of StaleRetentionBucket parameter description]

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
[PLACEHOLDER - requires verification of StaleRetentionPeriod parameter description]

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

## RELATED LINKS
