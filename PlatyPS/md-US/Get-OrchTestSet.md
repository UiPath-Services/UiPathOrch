---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSet

## SYNOPSIS
Gets the test sets.

## SYNTAX

```
Get-OrchTestSet [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchTestSet cmdlet retrieves test sets from UiPath Orchestrator. Test sets are collections of test cases that can be executed together to validate automation processes.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

If no parameters are specified, all test sets in the current folder are returned.

Primary Endpoint: GET /odata/TestSets?$filter=(SourceType eq 'User')&$expand=Environment

OAuth required scopes: OR.TestSets or OR.TestSets.Read

Required permissions: TestSets.View

## EXAMPLES

### Example 1
```powershell
Get-OrchTestSet
```

Gets all test sets in the current folder.

### Example 2
```powershell
Get-OrchTestSet RegressionTests
```

Gets the test set named "RegressionTests" from the current folder.

### Example 3
```powershell
Get-OrchTestSet *Smoke*
```

Gets all test sets whose names contain "Smoke".

### Example 4
```powershell
Get-OrchTestSet -Recurse
```

Gets all test sets from the current folder and all its subfolders.

### Example 5
```powershell
Get-OrchTestSet -Path Orch1:\Development, Orch1:\Testing UITests
```

Gets the "UITests" test set from both Development and Testing folders.

### Example 6
```powershell
Get-OrchTestSet | Where-Object {$_.TestCases.Count -gt 5}
```

Gets all test sets that contain more than 5 test cases.

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
Specifies the Name of the test sets to be retrieved.

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

### UiPath.PowerShell.Entities.TestSet

## NOTES

## RELATED LINKS
