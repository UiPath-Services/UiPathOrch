---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchTestSetSchedule

## SYNOPSIS
Enables test set schedules in specified folders.

## SYNTAX

```
Enable-OrchTestSetSchedule [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchTestSetSchedule cmdlet enables previously disabled test set schedules in specified folders within UiPath Orchestrator. This cmdlet reactivates scheduled test executions, allowing automated test sets to run according to their configured schedules.

Test set schedules automate the execution of test sets at specified intervals or times. Enabling them restores scheduled test execution functionality after being disabled for maintenance or troubleshooting purposes.

Use the -Name parameter to specify which test set schedules to enable. The cmdlet supports wildcard patterns for enabling multiple schedules efficiently. The -Path parameter allows targeting specific folders, and -Recurse enables processing all subfolders.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables enabling test set schedules from all subfolders.

Primary Endpoint: POST /odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Write

Required permissions: TestSetSchedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Enable-OrchTestSetSchedule RegressionTestSchedule
```

Enables the RegressionTestSchedule in the current folder (Development) using positional parameters.

### Example 2
```powershell
PS C:\> Enable-OrchTestSetSchedule -Path Orch1:\Development SmokeTestSchedule
```

Enables the SmokeTestSchedule in the Orch1:\Development folder.

### Example 3
```powershell
PS Orch1:\Development> Enable-OrchTestSetSchedule *Daily*, *Weekly* -WhatIf
```

Shows what would happen when enabling multiple test set schedules with names containing Daily or Weekly in the current folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Enable-OrchTestSetSchedule -Path Orch1:\Development *Automated* -Confirm
```

Enables all test set schedules containing Automated in their name in the Development folder with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Enable-OrchTestSetSchedule -Recurse *Nightly*
```

Enables all test set schedules containing Nightly in their names from all subfolders recursively.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSetSchedule -Enabled $false | Enable-OrchTestSetSchedule -WhatIf
```

Gets all disabled test set schedules and shows what would happen when enabling them using pipeline input.

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

### -Name
Specifies the Name of the test set schedules to be enabled.

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
Specifies the target folders. If not specified, the current folder will be targeted.

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
Specifies that test set schedules should be enabled from all subfolders recursively.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Test set schedules automate test execution at specified intervals. Enabling them restores scheduled test execution functionality after being disabled. Use Disable-OrchTestSetSchedule to temporarily suspend schedule operations. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Disable-OrchTestSetSchedule](Disable-OrchTestSetSchedule.md)

[Get-OrchTestSetSchedule](Get-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](Remove-OrchTestSetSchedule.md)

[Set-OrchTestSetSchedule](Set-OrchTestSetSchedule.md)
