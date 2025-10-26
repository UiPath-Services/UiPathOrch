---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueue

## SYNOPSIS
Retrieves test data queues from UiPath Orchestrator.

## SYNTAX

```
Get-OrchTestDataQueue [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchTestDataQueue cmdlet retrieves test data queues from UiPath Orchestrator. Test data queues provide structured test data management for automation testing, enabling organized storage and retrieval of test datasets used in automation validation and quality assurance processes.

Test data queues contain structured data sets that can be consumed by test automation workflows, supporting data-driven testing scenarios. These queues enable separation of test data from test logic, facilitating maintainable and scalable test automation practices.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include test data queues from subfolders, and -Depth to control recursion levels.

Primary Endpoint: GET /odata/TestDataQueues

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue
```

Retrieves all test data queues from the current folder.

### Example 2
```powershell
PS C:\> Get-OrchTestDataQueue -Path Orch1:\Production -Name *UserData*
```

Gets test data queues with names containing "UserData" from the Production folder.

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue -Recurse
```

Retrieves all test data queues from the current folder and all subfolders recursively.

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue TestUserQueue, LoginTestData
```

Retrieves specific test data queues by name.

## PARAMETERS

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

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
Specifies the names of test data queues to be retrieved. Supports wildcard patterns for flexible queue selection.

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
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

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
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive test data queue discovery.

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

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES
This cmdlet is a folder entity operation for accessing test data queue configurations. Test data queues support data-driven testing by providing structured datasets for automation validation. Queues organize test data separately from test logic, enabling maintainable test automation practices. Use in conjunction with Get-OrchTestDataQueueItem to access individual test data entries. This operation requires TestDataQueues.View permissions in the target folders.



Primary Endpoint: GET /odata/TestDataQueueDefinitions
OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Read
Required permissions: TestDataQueues.View

## RELATED LINKS

[Get-OrchTestDataQueueItem](Get-OrchTestDataQueueItem.md)



[Remove-OrchTestDataQueue](Remove-OrchTestDataQueue.md)

