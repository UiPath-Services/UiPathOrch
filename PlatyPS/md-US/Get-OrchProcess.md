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

  

Show the processes assigned to the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchProcess -Recurse
```

  

Show the processes assigned to the current folder and all its subfolders. When run in the root folder, it displays all processes from folders included in that tenant.

### Example 3
```powershell
PS Orch1:\> Get-OrchProcess -Recurse *proc*
```

  

Search all folders for processes that contain proc in the process name.

### Example 4
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv "C:\tmp\Get-OrchProcess.csv"
```

  

Export process details of all folders to CSV with UTF-8 BOM using module's function.

### Example 5
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv c:
```

  

Export process details of all folders to C drive current directory as CSV with UTF-8 BOM using module's function.

### Example 6
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv "C:\tmp\Get-OrchProcess.csv" -CsvEncoding Shift-JIS
```

  

Export process details of all folders to CSV with Shift-JIS using module's function.

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess -Recurse | Export-Csv "C:\tmp\Get-OrchProcess.csv"
```

  

Export process details of all folders to CSV with UTF-8 using PowerShell cmdlet.

### Example 8
```powershell
PS Orch1:\> Get-OrchProcess -Path "Shared\Finance", "Shared\HR" -Recurse
```

  

Get processes from specific multiple folders and their subfolders.

### Example 9
```powershell
PS Orch1:\> Get-OrchProcess -Depth 2
```

  

Get processes from the current folder and subfolders up to 2 levels deep.

### Example 10
```powershell
PS Orch1:\> Get-OrchProcess -Name "*Invoice*", "*Payment*" -Recurse
```

  

Search for processes with names containing "Invoice" or "Payment" in all folders.

### Example 11
```powershell
PS Orch1:\> Get-OrchProcess -ExpandDetails -Recurse
```

  

Get processes with expanded details from all folders.

### Example 12
```powershell
PS Orch1:\> Get-OrchProcess -Path "Shared\*" -Depth 1
```

  

Get processes from all immediate subfolders under Shared folder (1 level deep only).

### Example 13
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv "C:\tmp\Get-OrchProcesses_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
```

  

Export processes to a timestamped CSV using module's function.

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
Specifies the Name of the processes to be retrieved.

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

### -CsvEncoding
{{ Fill CsvEncoding Description }}

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
{{ Fill ExportCsv Description }}

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
{{ Fill ExpandDetails Description }}

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

### UiPath.PowerShell.Entities.Release
## NOTES

## RELATED LINKS
