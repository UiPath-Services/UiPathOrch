---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchJobMedia

## SYNOPSIS
Exports job media files from specified folders to local files.

## SYNTAX

```
Export-OrchJobMedia [[-JobId] <Int64[]>] [[-Destination] <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Export-OrchJobMedia cmdlet exports media files (screenshots, recordings, logs) associated with job executions from UiPath Orchestrator folders to local storage. This cmdlet enables backup, archival, or detailed analysis of job execution artifacts for troubleshooting, compliance, or audit purposes.

Job media includes execution screenshots, video recordings, execution logs, and other diagnostic files generated during automation job runs. Exporting these files allows for offline analysis, long-term archival, or sharing with support teams for troubleshooting purposes.

Use the -JobId parameter to specify which jobs' media files to export. The -Destination parameter specifies where the exported media files should be saved. The cmdlet supports exporting media from multiple jobs and organizing files by job ID in the destination directory.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables exporting job media from all subfolders.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Export-OrchJobMedia 12345
```

Exports all media files for job ID 12345 from the current folder to the default destination.

### Example 2
```powershell
PS C:\> Export-OrchJobMedia -Path Orch1:\Production -JobId 67890 -Destination "C:\JobMedia"
```

Exports all media files for job ID 67890 from the Production folder to C:\JobMedia directory.

### Example 3
```powershell
PS Orch1:\Development> Export-OrchJobMedia 11111, 22222, 33333 -Destination "C:\Exports" -WhatIf
```

Shows what would happen when exporting media files for multiple job IDs from the current folder to C:\Exports directory.

### Example 4
```powershell
PS C:\> Export-OrchJobMedia -Path Orch1:\Development -Recurse -Destination "C:\AllJobMedia"
```

Exports all job media files from the Development folder and all its subfolders to C:\AllJobMedia directory.

### Example 5
```powershell
PS Orch1:\Production> Export-OrchJobMedia -Depth 2 -Destination "C:\BackupMedia" -Confirm
```

Exports all job media files from the current folder and up to 2 levels of subfolders to C:\BackupMedia with confirmation prompts.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchJob -State Faulted | Export-OrchJobMedia -Destination "C:\FailedJobs"
```

Gets all failed jobs and exports their media files to C:\FailedJobs using pipeline input for troubleshooting purposes.

## PARAMETERS

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

### -Destination
Specifies the destination directory where exported media files will be saved. If not specified, files will be saved to the current directory.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -JobId
Specifies the job IDs whose media files should be exported. If not specified, media files for all jobs will be exported.

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the source folders. If not specified, the current folder will be used as the source.

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
Specifies that job media files should be exported from all subfolders recursively.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int64[]
Job IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Job
Job objects from Get-OrchJob can be piped to this cmdlet. The Id property will be automatically mapped to the -JobId parameter via ByPropertyName binding.

## OUTPUTS

### System.IO.FileInfo
Returns information about the exported media files.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Job media files include screenshots, video recordings, and execution logs. Export operations may take time for large media files or multiple jobs. Ensure sufficient disk space for media exports. Media files are organized by job ID in the destination directory. Use -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchJobMedia](Get-OrchJobMedia.md)

[Remove-OrchJobMedia](Remove-OrchJobMedia.md)

[Start-OrchJob](Start-OrchJob.md)
