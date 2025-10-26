---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestSetSchedule

## SYNOPSIS
Copies test set schedules to destination folders.

## SYNTAX

```
Copy-OrchTestSetSchedule [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchTestSetSchedule cmdlet copies test set schedules from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of test set schedules, including their timing configurations, execution parameters, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Test set schedules define automated execution timing for test sets, making this cmdlet essential for deploying test automation schedules across different environments.

Use the -Name parameter to specify which test set schedules to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple schedules efficiently. Note that copied schedules may need adjustment if the associated test sets have different names in the destination folder.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying test set schedules from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/TestSetSchedules, POST /odata/TestSetSchedules

OAuth required scopes: OR.TestSetSchedules

Required permissions: TestSetSchedules.View, TestSetSchedules.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchTestSetSchedule NightlyRegressionSchedule Orch1:\Production
```

Copies the NightlyRegressionSchedule from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchTestSetSchedule -Path Orch1:\Development WeeklyTestSchedule Orch2:\Production
```

Copies the WeeklyTestSchedule from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant test set schedule copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestSetSchedule *Daily*, *Weekly* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple test set schedules with names containing Daily or Weekly from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchTestSetSchedule -Path Orch1:\Development *Automated* Orch2:\Production
```

Copies all test set schedules containing Automated in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestSetSchedule -Recurse *Nightly* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all test set schedules containing Nightly from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSetSchedule *Regression* | Copy-OrchTestSetSchedule -Destination Orch2:\Production
```

Gets all test set schedules containing Regression in their names and copies them to Orch2:\Production using pipeline input.

## PARAMETERS

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

### -Destination
Specifies the destination folder where test set schedules should be copied.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the test set schedules to be copied.

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
Specifies the source folder. If not specified, the current folder will be used as the source.

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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

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

### -Recurse
Specifies that test set schedules should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Test set schedules define automated execution timing for test sets. When copying across environments, verify that associated test sets exist in the destination folder and adjust schedule configurations if necessary. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchTestSetSchedule](Get-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](Remove-OrchTestSetSchedule.md)


[Copy-OrchTestSet](Copy-OrchTestSet.md)
