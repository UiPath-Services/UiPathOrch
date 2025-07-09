---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestDataQueue

## SYNOPSIS
Copies test data queues to destination folders.

## SYNTAX

```
Copy-OrchTestDataQueue [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchTestDataQueue cmdlet copies test data queues from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of test data queues, including their configurations, schema definitions, and structure metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Test data queues contain structured test data used for automated testing scenarios, making this cmdlet essential for deploying test environments and maintaining consistent test data across different development stages.

Use the -Name parameter to specify which test data queues to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple test data queues efficiently. Note that test data items are not copied with the queue structure - only the queue definition and schema.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying test data queues from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/TestDataQueues, POST /odata/TestDataQueues

OAuth required scopes: OR.TestDataQueues

Required permissions: TestDataQueues.View, TestDataQueues.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTestDataQueue CustomerTestData Orch1:\Production
```

Copies the CustomerTestData queue from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchTestDataQueue -Path Orch1:\Development UserAccountData Orch2:\Production
```

Copies the UserAccountData test data queue from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant test data queue copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestDataQueue *Test*, *Sample* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple test data queues with names containing Test or Sample from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchTestDataQueue -Path Orch1:\Development *Data* Orch2:\Production
```

Copies all test data queues containing Data in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestDataQueue -Recurse *TestData* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all test data queues containing TestData from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestDataQueue *QA* | Copy-OrchTestDataQueue -Destination Orch2:\Production
```

Gets all test data queues containing QA in their names and copies them to Orch2:\Production using pipeline input.

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
Specifies the destination folder where test data queues should be copied.

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
Specifies the Name of the test data queues to be copied.

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
Specifies that test data queues should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Test data queues contain structured test data definitions and schemas. When copying across environments, the queue structure and schema are copied but not the test data items themselves. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchTestDataQueue](Get-OrchTestDataQueue.md)

[Remove-OrchTestDataQueue](Remove-OrchTestDataQueue.md)

[Set-OrchTestDataQueue](Set-OrchTestDataQueue.md)

[New-OrchTestDataQueue](New-OrchTestDataQueue.md)
