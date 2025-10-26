---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchProcess

## SYNOPSIS
Gets the processes.

## SYNTAX

```
Get-OrchProcess [[-Name] <String[]>] [-ExpandDetails] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets process information from Orchestrator folders. This cmdlet retrieves basic process metadata by default, with option to expand detailed information including process settings, arguments, and runtime configurations.

Processes are folder entities that operate within specific folder scopes. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Multiple values for the -Path parameter can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Releases/?$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint


OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchProcess
```

Gets processes from the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchProcess -Recurse
```

Gets processes from the current folder and all subfolders.

### Example 3
```powershell
PS Orch1:\> Get-OrchProcess *Invoice*
```

Gets processes containing Invoice in their name from all folders.

### Example 4
```powershell
PS Orch1:\> Get-OrchProcess -Path Orch1:\Shared, Orch1:\Finance -Recurse
```

Gets processes from specific folders and their subfolders.

### Example 5
```powershell
PS Orch1:\> Get-OrchProcess MainProcess, *Test*
```

Gets specific processes by name pattern from all folders.

### Example 6
```powershell
PS Orch1:\> Get-OrchProcess -ExpandDetails | Where-Object {$_.ProcessSettings.AutopilotForRobots.Enabled}
```

Gets processes with Autopilot for Robots enabled using expanded details.

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess | Where-Object {$_.JobPriority -eq "High"}
```

Gets high-priority processes from all folders.

### Example 8
```powershell
PS Orch1:\> Get-OrchProcess -ExportCsv C:\Reports\Processes.csv
```

Exports all processes to CSV with UTF-8 BOM encoding. The exported CSV can be imported using Import-Csv | New-OrchProcess or Import-Csv | Update-OrchProcess.

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

### -Name
Specifies the names of processes to retrieve. Supports wildcards and multiple values.

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

### -CsvEncoding
Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. Can be used with corresponding Import cmdlets.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpandDetails
Retrieves detailed process information including ProcessSettings, Arguments, and VideoRecordingSettings. Without this parameter, these properties return null values.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Release
## NOTES
Process entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Use -ExpandDetails when you need access to ProcessSettings properties such as AutopilotForRobots, VideoRecordingSettings, or detailed Arguments metadata.

The -ExportCsv parameter creates import-ready CSV files with human-readable names instead of internal IDs.



Primary Endpoint: GET /odata/Releases
OAuth required scopes: OR.Execution or OR.Execution.Read
Required permissions: Processes.View

## RELATED LINKS

[New-OrchProcess](New-OrchProcess.md)

[Update-OrchProcess](Update-OrchProcess.md)

[Remove-OrchProcess](Remove-OrchProcess.md)

[Copy-OrchProcess](Copy-OrchProcess.md)



