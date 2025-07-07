---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSetExecution

## SYNOPSIS
Gets the test set execution.

## SYNTAX

```
Get-OrchTestSetExecution [[-Name] <String[]>] [-Status <String[]>] [-Last <String>]
 [-StartTimeAfter <DateTime>] [-StartTimeBefore <DateTime>] [-TriggerType <String[]>] [-Skip <UInt64>]
 [-First <UInt64>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchTestSetExecution cmdlet retrieves test set execution records from UiPath Orchestrator. Test set executions represent the historical records of test set runs, including their status, execution time, and results.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

You can filter executions by status, time range, trigger type, and other criteria. The cmdlet supports pagination through -Skip and -First parameters for large result sets.

Primary Endpoint: GET /odata/TestSetExecutions?$expand=TestSet

OAuth required scopes: OR.TestSetExecutions or OR.TestSetExecutions.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1
```powershell
Get-OrchTestSetExecution
```

Gets all test set executions in the current folder.

### Example 2
```powershell
Get-OrchTestSetExecution RegressionTests
```

Gets all executions for the test set named "RegressionTests".

### Example 3
```powershell
Get-OrchTestSetExecution -Status Failed, Stopped
```

Gets all test set executions with Failed or Stopped status.

### Example 4
```powershell
Get-OrchTestSetExecution -Last "7d"
```

Gets test set executions from the last 7 days.

### Example 5
```powershell
Get-OrchTestSetExecution -StartTimeAfter (Get-Date).AddDays(-1) -Status Successful
```

Gets successful test set executions that started within the last day.

### Example 6
```powershell
Get-OrchTestSetExecution -Recurse -TriggerType Schedule -First 50
```

Gets the first 50 scheduled test set executions from the current folder and all subfolders.

### Example 7
```powershell
Get-OrchTestSetExecution -Path Orch1:\Production *Smoke* | Where-Object {$_.Duration.TotalMinutes -gt 30}
```

Gets all smoke test executions from the Production folder that took more than 30 minutes to complete.

## PARAMETERS

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

### -First
Gets only the specified number of objects.
Enter the number of objects to get.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Last
Specifies the most recent period for retrieving test set executions. Valid values include time spans like "1h", "24h", "7d", "30d", etc.

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

### -Name
Specifies the Name of the test set executions to be retrieved.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folder. If not specified, the current folder will be targeted.

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

### -Skip
Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeAfter
Specifies the start date and time for the StartTime of the test set executions to be retrieved.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeBefore
Specifies the end date and time for the StartTime of the test set executions to be retrieved.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Status
Specifies the Status of the test set executions to be retrieved. Common values include: Running, Successful, Failed, Stopped, Pending.

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

### -TriggerType
Specifies the TriggerType of the test set executions to be retrieved. Common values include: Manual, Schedule, API.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Test set names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.TestSet
Test set objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSetExecution

## NOTES

## RELATED LINKS
