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
PS Orch1:\Shared> Get-OrchJobMedia -JobKey "12345678-1234-1234-1234-123456789012"
```

Retrieves all media files for a specific job identified by its JobKey.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | Where-Object {$_.MediaType -eq "Screenshot"}
```

Gets all screenshot media files from jobs in the current folder.

### Example 3
```powershell
PS C:\> Get-OrchJobMedia -Path Orch1:\Production -Recurse | Where-Object {$_.CreatedTime -gt (Get-Date).AddDays(-7)}
```

Retrieves job media from the Production folder and subfolders created in the last 7 days.

### Example 4
```powershell
PS Orch1:\> Get-OrchJobMedia -JobKey "*" | Group-Object MediaType
```

Groups all job media files by their media type (Screenshot, Video, etc.).

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | Select-Object JobKey, MediaType, FileName, FileSize, CreatedTime
```

Displays a summary view of job media with key properties.

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

### System.String[]
Job keys can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Job
Job objects from Get-OrchJob can be piped to this cmdlet. The Key property will be automatically mapped to the -JobKey parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.JobMedia
Returns JobMedia objects containing information about job media files. Key properties typically include:
- JobKey: Associated job identifier
- MediaType: Type of media (Screenshot, Video, Recording)
- FileName: Original file name
- FileSize: File size in bytes
- CreatedTime: Media creation timestamp
- DownloadUrl: URL for downloading the media file
- ContentType: MIME type of the media file

## NOTES
This cmdlet is a folder entity operation for accessing job media files including screenshots, videos, and recordings. Job media provides visual documentation of automation execution for debugging, auditing, and process verification. Media files are associated with specific job executions and may have retention policies affecting availability. Use JobKey parameter to target specific jobs or filter by time ranges for recent executions. This operation requires Jobs.View permissions in the target folders.

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchJobVideo](Get-OrchJobVideo.md)

[Download-OrchJobMedia](Download-OrchJobMedia.md)

[Remove-OrchJobMedia](Remove-OrchJobMedia.md)
