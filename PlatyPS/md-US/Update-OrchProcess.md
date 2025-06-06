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
Supports import from CSV. The format of the importable CSV can be obtained using Get-OrchProcess -Recurse -ExportCsv c:.

Primary Endpoint: POST /odata/Releases/UiPath.Server.Configuration.OData.EditRelease

OAuth required scopes: 

Required permissions: 

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Import-Csv "C:\tmp\Get-OrchProcess.csv" | Update-OrchProcess
```

  

Update process information using import from CSV.

### Example 2
```powershell
PS Orch1:\Shared> Update-OrchProcess -Name "InvoiceProcess" -Description "Updated invoice processing workflow" -Priority "High"
```

  

Update a specific process with new description and priority in the current folder.

### Example 3
```powershell
PS Orch1:\> Update-OrchProcess -Name "PaymentProcess" -Path "Shared\Finance" -Tags "Finance", "Production", "Critical"
```

  

Update a process in a specific folder with new tags.

### Example 4
```powershell
PS Orch1:\> Update-OrchProcess -Name "*Invoice*" -Recurse -MaxDurationSeconds 3600 -WhatIf
```

  

Preview what would happen when updating all processes containing "Invoice" in their name across all folders with a new maximum duration.

### Example 5
```powershell
PS Orch1:\> Update-OrchProcess -Name "DataEntry" -NewName "DataEntryV2" -Version "2.0.0" -Description "Updated data entry process"
```

  

Rename a process and update its version and description.

### Example 6
```powershell
PS Orch1:\> Update-OrchProcess -Name "ReportGenerator" -InputArguments '{"OutputPath": "C:\\Reports", "Format": "PDF"}' -VideoRecordingType "OnError"
```

  

Update a process with new input arguments and video recording settings.

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess -Name "LegacyProcess*" | Update-OrchProcess -RetentionPeriod 90 -RetentionAction "Delete"
```

  

Update retention settings for all processes starting with "LegacyProcess" using pipeline input.

### Example 8
```powershell
PS Orch1:\> Update-OrchProcess -Name "AttendedProcess" -HiddenForAttendedUser "False" -RemoteControlAccess "Enabled" -Confirm
```

  

Update attended process settings with confirmation prompt.

### Example 9
```powershell
PS Orch1:\> Import-Csv "C:\tmp\BulkProcessUpdate.csv" | Where-Object {$_.Priority -eq "Low"} | Update-OrchProcess -Priority "Normal"
```

  

Import processes from CSV, filter by current priority, and update to a new priority level.

### Example 10
```powershell
PS Orch1:\> Update-OrchProcess -Name "CriticalProcess" -Path "Production" -A4R_Enabled "True" -A4R_HealingEnabled "True" -ErrorRecordingEnabled "True"
```

  

Enable Action for Recovery (A4R) and error recording features for a critical production process.

### Example 11
```powershell
PS Orch1:\> Update-OrchProcess -Name "ScheduledProcess" -Recurse -Depth 2 -AutoStartProcess "True" -AlwaysRunning "False"
```

  

Update scheduled process settings across folders up to 2 levels deep.

### Example 12
```powershell
$processUpdates = @{
    Name = "22.4"
    Description = "Complex business process with multiple steps"
    Quality = 95
    Duration = 1800
    Frequency = 24
}
Update-OrchProcess @processUpdates
```

  

Update multiple process properties using parameter splatting for better readability.

## PARAMETERS

### -AlwaysRunning
Specifies the AlwaysRunning of the processes to be updated.

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
Specifies the AutoStartProcess of the processes to be updated.

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
Specifies the Description of the processes to be updated.

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
Specifies the Duration of the processes to be updated.

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
Specifies the EntryPoint of the processes to be updated.

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
Specifies the ErrorRecordingEnabled of the processes to be updated.

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
Specifies the Frequency of the processes to be updated.

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
Specifies the HiddenForAttendedUser of the processes to be updated.

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
Specifies the InputArguments of the processes to be updated.

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
Specifies the MaxDurationSeconds of the processes to be updated.

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
Specifies the Name of the processes to be updated.

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
{{ Fill Path Description }}

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
Specifies the Priority of the processes to be updated.

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
Specifies the Quality of the processes to be updated.

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
Specifies the QueueItemVideoRecordingType of the processes to be updated.

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
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RemoteControlAccess
Specifies the RemoteControlAccess of the processes to be updated.

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
Specifies the RetentionAction of the processes to be updated.

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
Specifies the RetentionBucket of the processes to be updated.

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
Specifies the RetentionPeriod of the processes to be updated.

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
Specifies the Tags of the processes to be updated.

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
Specifies the Version of the processes to be updated.

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
Specifies the VideoRecordingType of the processes to be updated.

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
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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
{{ Fill ProgressAction Description }}

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
{{ Fill NewName Description }}

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
{{ Fill StaleRetentionAction Description }}

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
{{ Fill StaleRetentionBucket Description }}

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
{{ Fill StaleRetentionPeriod Description }}

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
{{ Fill A4R_Enabled Description }}

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
{{ Fill A4R_HealingEnabled Description }}

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
