---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTrigger

## SYNOPSIS
Get the triggers.

## SYNTAX

```
Get-OrchTrigger [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExpandDetails]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Outputs information about triggers with specified names in the target folders. The target folders can be specified using the -Path, -Recurse, and -Depth parameters. If these are not specified, the current location is used as the target folder. If no trigger names are specified, it outputs all triggers in the target folders.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/ProcessSchedules, GET /odata/ProcessSchedules({processScheduleId})

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Schedules.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTrigger
```

Displays all triggers in the 'Shared' folder, which is the current location.

### Example 2
```powershell
PS Orch1:\> Get-OrchTrigger -Recurse
```

Displays all triggers in the current folder and all its subfolders. When run in the root folder, it shows all triggers across all folders in that tenant.

### Example 3
```powershell
PS C:\> Get-OrchTrigger -Path Orch1:\ -Recurse -Name *Schedule*
```

Gets triggers containing 'Schedule' in their name from all folders recursively.

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchTrigger -ExportCsv triggers.csv
```

Exports all triggers to a CSV file. The exported CSV can be imported and modified using Import-Csv, then updated using Update-OrchTrigger or other related cmdlets.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchTrigger | ConvertTo-Json
```

Displays the complete trigger structure including schedule configuration and execution parameters in JSON format.

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
Specifies the Name of the triggers to be retrieved.

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
Specifies the character encoding for CSV export. Common values include 'UTF8', 'ASCII', 'Unicode', and 'UTF32'. Default is UTF8.

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
Exports the trigger information to a CSV file. Specify the file path where the CSV should be saved. Use with -CsvEncoding to control file encoding.

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
Instructs the cmdlet to call GET /odata/ProcessSchedules({processScheduleId}).

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

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
Main endpoint called: GET /odata/ProcessSchedules

Required Scope: OR.Jobs.Read



Primary Endpoint: GET /odata/ProcessSchedules
OAuth required scopes: OR.Jobs or OR.Jobs.Read
Required permissions: Schedules.View

## RELATED LINKS
