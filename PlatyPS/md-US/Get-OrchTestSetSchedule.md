---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSetSchedule

## SYNOPSIS
Gets the test schedules.

## SYNTAX

```
Get-OrchTestSetSchedule [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchTestSetSchedule cmdlet retrieves test set schedules from UiPath Orchestrator. Test set schedules define automated execution plans for test sets, including timing, frequency, and execution parameters.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

Test set schedules enable automated testing scenarios by executing test sets at predetermined times or intervals, supporting continuous integration and quality assurance workflows.

Primary Endpoint: Get /odata/TestSetSchedules

OAuth required scopes: OR.TestSetExecutions or OR.TestSetExecutions.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule
```

Gets all test set schedules in the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule NightlyRegression
```

Gets the test set schedule named "NightlyRegression" from the current folder using positional parameter.

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule *Daily*
```

Gets all test set schedules whose names contain "Daily" using wildcard pattern matching.

### Example 4
```powershell
PS Orch1:\> Get-OrchTestSetSchedule -Recurse
```

Gets all test set schedules from the current folder and all its subfolders recursively.

### Example 5
```powershell
PS C:\> Get-OrchTestSetSchedule -Path Orch1:\Production
```

Gets test set schedules from the Production folder, demonstrating execution from any location.

### Example 6
```powershell
PS Orch1:\> Get-OrchTestSetSchedule -Path \Production, \Shared -Recurse
```

Gets test set schedules from multiple specific folders recursively, demonstrating -Path parameter priority with -Recurse.

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule | ConvertTo-Json -Depth 2
```

Gets all test set schedules and displays their structure in JSON format for detailed analysis of schedule properties.

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

### -Name
Specifies the Name of the test schedules to be retrieved.

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

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule
## NOTES

Primary Endpoint: GET /odata/TestSetSchedules
OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Read
Required permissions: TestSetSchedules.View

## RELATED LINKS
