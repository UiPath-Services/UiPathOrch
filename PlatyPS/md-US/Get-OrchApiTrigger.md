---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchApiTrigger

## SYNOPSIS
Gets the API triggers.

## SYNTAX

```
Get-OrchApiTrigger [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
API triggers are folder entities that exist within specific folders. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Outputs information about API triggers with specified names in the target folders. The target folders can be specified using the -Path, -Recurse, and -Depth parameters. If these are not specified, the current location is used as the target folder. If no API trigger names are specified, it outputs all API triggers in the target folders.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/HttpTriggers

OAuth required scopes: (undocumented)

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchApiTrigger
```

Displays all API triggers in the 'Shared' folder, which is the current location.

### Example 2
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse
```

Displays all API triggers in the current folder and all its subfolders. When run in the root folder, it shows all API triggers across all folders in that tenant.

### Example 3
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse *api*
```

Gets API triggers whose names contain 'api' from the current folder and all its subfolders.

### Example 4
```powershell
PS Orch1:\> Get-OrchApiTrigger -Path Orch1:\Production TestTrigger
```

Gets the API trigger named 'TestTrigger' from the Production folder.

### Example 5
```powershell
PS C:\> Get-OrchApiTrigger -Path Orch1:\,Orch2:\ -Recurse
```

Displays all API triggers in Orch1: and Orch2:.

### Example 6
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse | ConvertTo-Json -Depth 2
```

Gets all API triggers and displays the complete structure including nested properties like Release and MachineRobots.

### Example 7
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | Export-Csv c:apiTriggers.csv
```

Exports the output to a CSV file. The CSV file will be located at the current location of the C: drive. You can customize the CSV format by combining with `select` to specify which columns to include. Try `ii c:' to open the current location of the C: drive.

### Example 8
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | ConvertTo-Json
```

Converts the output to JSON format, providing a raw view of the data from Orchestrator.

### Example 9
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse -Name *test*,*prod*
```

Gets API triggers whose names contain 'test' or 'prod' from all folders recursively.

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
Specifies the Name of the API triggers to be retrieved.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.HttpTrigger
## NOTES
Main endpoint called: GET /odata/HttpTriggers

Required Scope: OR.Folders.Read



Primary Endpoint: GET /odata/HttpTriggers
OAuth required scopes: [PLACEHOLDER]
Required permissions: [PLACEHOLDER]

## RELATED LINKS
