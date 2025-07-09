---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Stop-OrchJob

## SYNOPSIS
Stops running or pending UiPath robot jobs.

## SYNTAX

```
Stop-OrchJob [-Id] <Int64[]> [-Force] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stops running or pending UiPath robot jobs in Orchestrator folders. This cmdlet can gracefully stop jobs or force termination when jobs are unresponsive.

Jobs in Running state will be gracefully stopped, allowing them to complete their current activity and perform cleanup. Jobs in Pending state will be cancelled before execution begins.

Use the -Force parameter to forcefully terminate unresponsive jobs, though this may result in incomplete cleanup or data inconsistency.

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.StopJobs

OAuth required scopes: OR.Jobs

Required permissions: Jobs.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Stop-OrchJob 123456 -WhatIf
```

Shows what would happen when stopping a job without actually stopping it.

### Example 2
```powershell
PS Orch1:\Shared> Stop-OrchJob 123456
```

Gracefully stops the specified job.

### Example 3
```powershell
PS Orch1:\> Stop-OrchJob 123456, 789012 -Confirm
```

Stops multiple jobs with confirmation prompts.

### Example 4
```powershell
PS Orch1:\> Stop-OrchJob 123456 -Force
```

Forcefully terminates an unresponsive job.

### Example 5
```powershell
PS Orch1:\> Get-OrchJob -State Running | Stop-OrchJob -WhatIf
```

Shows which running jobs would be stopped using pipeline input.

### Example 6
```powershell
PS Orch1:\> Get-OrchJob -State Pending -ReleaseName TestProcess | Stop-OrchJob -Confirm
```

Cancels pending jobs for a specific process with confirmation.

## PARAMETERS

### -Depth
Specifies the depth of folder recursion. A depth of 0 targets only the current folder.

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

### -Force
Forces termination of jobs without graceful shutdown. Use with caution as this may result in incomplete cleanup.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
Specifies the IDs of jobs to stop. Supports multiple values.

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies target folders. Use comma-separated values for multiple folders. Supports wildcards. If not specified, targets the current folder.

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
Controls how progress information is displayed during cmdlet execution.

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
Includes the target folder and all its subfolders in the operation.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts for confirmation before stopping jobs. Recommended when stopping multiple jobs.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs without actually stopping jobs. Recommended for safety verification.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int64[]
### UiPath.PowerShell.Entities.Job
### System.String[]
## OUTPUTS

### System.Object
## NOTES
Job entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Jobs in Running state will be gracefully stopped unless -Force is specified. Jobs in Pending state will be cancelled immediately.

Use -WhatIf to preview job termination before actual execution, especially when using pipeline input from Get-OrchJob.

Use -Force only when jobs are unresponsive and cannot be stopped gracefully, as forced termination may result in incomplete cleanup or data inconsistency.

Use -Confirm when stopping multiple jobs to review each job termination individually.

## RELATED LINKS

[Start-OrchJob](Start-OrchJob.md)

[Get-OrchJob](Get-OrchJob.md)

[Open-OrchJob](Open-OrchJob.md)
