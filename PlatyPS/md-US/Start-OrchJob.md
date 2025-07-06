---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Start-OrchJob

## SYNOPSIS
Starts UiPath robot jobs for specified processes.

## SYNTAX

```
Start-OrchJob [-Name] <String[]> [[-RuntimeType] <String>] [[-JobsCount] <Int32>] [-InputArguments <String>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Initiates job execution for specified UiPath processes in Orchestrator folders. This cmdlet creates job instances that will be executed by available robots according to the folder's robot assignments and process configurations.

Jobs are created with Pending status and will be picked up by available robots that have access to the target folder and meet the process requirements.

Multiple values for the -Name and -Path parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.StartJobs

OAuth required scopes: OR.Jobs.Write OR.Execution.Read

Required permissions: Jobs.Create Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Start-OrchJob MyProcess -WhatIf
```

Shows what would happen when starting a job without actually starting it.

### Example 2
```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19
```

Starts a job for the specified process in the current folder.

### Example 3
```powershell
PS Orch1:\> Start-OrchJob -Recurse InvoiceProcess -JobsCount 3
```

Starts 3 jobs for InvoiceProcess found recursively in all folders.

### Example 4
```powershell
PS Orch1:\> Start-OrchJob MyProcess -InputArguments '{"FilePath": "C:\\Data\\input.xlsx", "ProcessCount": 100}'
```

Starts a job with input arguments passed as JSON string.

### Example 5
```powershell
PS Orch1:\> Start-OrchJob -Path Orch1:\Shared, Orch1:\Finance ProcessName
```

Starts jobs for ProcessName in specific folders.

### Example 6
```powershell
PS Orch1:\> Start-OrchJob TestProcess -RuntimeType Unattended
```

Starts a job with specific runtime type.

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess *Critical* | Start-OrchJob -Confirm
```

Starts jobs for critical processes with confirmation prompts.

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

### -JobsCount
Specifies the number of job instances to create for each process. Default is 1.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: 1
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the names of processes to start jobs for. Supports wildcards and multiple values.

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

### -RuntimeType
Specifies the runtime type for job execution. Valid values include: Unattended, NonProduction.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts for confirmation before starting jobs. Recommended when starting multiple jobs.

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
Shows what would happen if the cmdlet runs without actually starting jobs. Recommended for safety verification.

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

### -InputArguments
Specifies input arguments for the process as JSON string. Arguments must match the process's input parameter definitions.

```yaml
Type: String
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

### System.Object
## NOTES
Process entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Jobs are created with Pending status and will be executed by robots that have access to the target folder and meet the process requirements.

Use -WhatIf to preview job creation before actual execution, especially when using wildcards or recursive operations.

Input arguments must be provided as valid JSON string and match the process's defined input parameters.

Use -Confirm when starting multiple jobs to review each job creation individually.

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Stop-OrchJob](Stop-OrchJob.md)

[Get-OrchProcess](Get-OrchProcess.md)

[Open-OrchJob](Open-OrchJob.md)
