---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueueItem

## SYNOPSIS
Gets the items in the test data queues.

## SYNTAX

```
Get-OrchTestDataQueueItem [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchTestDataQueueItem cmdlet retrieves items from test data queues. Test data queue items contain structured data used for test automation scenarios.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

If no parameters are specified, all test data queue items in the current folder are returned.

Primary Endpoint: GET /odata/TestDataQueueItems?$filter=(TestDataQueueId eq {testDataQueueId})

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Read

Required permissions: TestDataQueueItems.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem
```

Gets all test data queue items in the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem CustomerData
```

Gets all items from the test data queue named "CustomerData" in the current folder.

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem *Data*
```

Gets all items from test data queues whose names contain "Data".

### Example 4
```powershell
PS Orch1:\> Get-OrchTestDataQueueItem -Recurse
```

Gets all test data queue items from the current folder and all its subfolders.

### Example 5
```powershell
PS C:\> Get-OrchTestDataQueueItem -Path Orch1:\Development, Orch1:\Production UserTestData
```

Gets all items from the "UserTestData" queue in both Development and Production folders.

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem | ConvertTo-Json -Depth 2
```

Displays test data queue items in JSON format for detailed analysis of data structure and content.

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
Specifies the Name of the test data queues containing the items to be retrieved.

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

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES

Primary Endpoint: GET /odata/TestDataQueues
OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Read
Required permissions: TestDataQueues.View

## RELATED LINKS
