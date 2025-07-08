---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestSet

## SYNOPSIS
Copies test sets to destination folders.

## SYNTAX

```
Copy-OrchTestSet [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchTestSet cmdlet copies test sets from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of test sets, including their test case configurations, execution parameters, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Test sets contain collections of test cases for automated testing workflows, making this cmdlet essential for deploying test automation across different environments.

Use the -Name parameter to specify which test sets to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple test sets efficiently. Note that copied test sets may need adjustment if the associated test cases have different names in the destination folder.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying test sets from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/TestSets, POST /odata/TestSets

OAuth required scopes: OR.TestSets

Required permissions: TestSets.View, TestSets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTestSet RegressionTests Orch1:\Production
```

Copies the RegressionTests test set from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchTestSet -Path Orch1:\Development SmokeTests Orch2:\Production
```

Copies the SmokeTests test set from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant test set copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestSet *API*, *UI* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple test sets with names containing API or UI from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchTestSet -Path Orch1:\Development *Integration* Orch2:\Production
```

Copies all test sets containing Integration in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestSet -Recurse *Automated* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all test sets containing Automated from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSet *Regression* | Copy-OrchTestSet -Destination Orch2:\Production
```

Gets all test sets containing Regression in their names and copies them to Orch2:\Production using pipeline input.

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
Specifies the destination folder where test sets should be copied.

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
Specifies the Name of the test sets to be copied.

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
Specifies that test sets should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### System.String[]
Test set names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.TestSet
Test set objects from Get-OrchTestSet can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Test sets contain collections of test cases for automated testing. When copying across environments, verify that associated test cases exist in the destination folder and adjust test set configurations if necessary. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchTestSet](Get-OrchTestSet.md)

[Remove-OrchTestSet](Remove-OrchTestSet.md)

[Set-OrchTestSet](Set-OrchTestSet.md)

[Start-OrchTestSet](Start-OrchTestSet.md)
