---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJobMedia

## SYNOPSIS
Retrieves job media files (screenshots, videos, and recordings) from UiPath Orchestrator.

## SYNTAX

```
Get-OrchJobMedia [-Skip <UInt64>] [-First <UInt64>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchJobMedia cmdlet retrieves job media files including screenshots, videos, and recordings from UiPath Orchestrator. Job media provides visual documentation of automation execution, enabling debugging, audit trails, and process verification through captured images and videos.

Media files are associated with specific job executions and contain information such as file type, creation time, file size, and download links. This cmdlet helps access visual evidence of automation execution for troubleshooting, compliance, and process optimization purposes.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include media from jobs in subfolders, and -Depth to control recursion levels.

Primary Endpoint: GET /odata/Jobs/{jobKey}/Media

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJobMedia
```

Retrieves all job media files from the current Shared folder, displaying basic properties like MediaType, FileName, and CreatedTime.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | ConvertTo-Json -Depth 2
```

Displays detailed job media properties in JSON format, including complete file information and metadata structure.

### Example 3
```powershell
PS C:\> Get-OrchJobMedia -Path Orch1:\Production -Recurse | Where-Object {$_.CreatedTime -gt (Get-Date).AddDays(-7)}
```

Retrieves job media from the Production folder and subfolders created in the last 7 days.

### Example 4
```powershell
PS Orch1:\> Get-OrchJobMedia -Recurse -First 10
```

Retrieves the first 10 job media files across all folders using the -First parameter for performance optimization.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | Where-Object {$_.MediaType Screenshot} | Select-Object FileName, FileSize, CreatedTime
```

Filters for screenshot media files and displays key properties. Uses Where-Object since no dedicated MediaType parameter exists.

### Example 6
```powershell
PS Orch1:\> Get-OrchJobMedia -Recurse | Measure-Object FileSize -Sum
```

Calculates the total file size of all job media across folders.

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

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive job media discovery.

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
Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

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

### -First
Gets only the specified number of objects. Enter the number of objects to get.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExecutionMedia
## NOTES
This cmdlet is a folder entity operation for accessing job media files including screenshots, videos, and recordings. Job media provides visual documentation of automation execution for debugging, auditing, and process verification. Media files are associated with specific job executions and may have retention policies affecting availability. Use JobKey parameter to target specific jobs or filter by time ranges for recent executions. This operation requires Jobs.View permissions in the target folders.



Primary Endpoint: [PLACEHOLDER]
OAuth required scopes: [PLACEHOLDER]
Required permissions: [PLACEHOLDER]

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchJobVideo](Get-OrchJobVideo.md)

[Download-OrchJobMedia](Download-OrchJobMedia.md)

[Remove-OrchJobMedia](Remove-OrchJobMedia.md)
