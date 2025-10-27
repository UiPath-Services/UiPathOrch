---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchClassicRobot

## SYNOPSIS
Gets robots from classic environments.

## SYNTAX

```
Get-OrchClassicRobot [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchClassicRobot cmdlet retrieves classic robot configurations from UiPath Orchestrator. Classic robots are the legacy robot entities used before the modern folder-based approach, associated with classic environments rather than modern folder structures.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

Classic robots provide backward compatibility for existing robot deployments and are primarily used for migrating from older Orchestrator versions to the modern folder structure. These robots maintain their environment associations and process assignments from the legacy system.

Primary Endpoint: GET /odata/Robots?$expand=Machine,Environment

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Robots.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchClassicRobot
```

Gets all classic robots in the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchClassicRobot *Prod*
```

Gets all classic robots whose names contain "Prod".

### Example 3
```powershell
PS Orch1:\> Get-OrchClassicRobot -Recurse
```

Gets all classic robots from the root folder recursively.

### Example 4
```powershell
PS C:\> Get-OrchClassicRobot -Path Orch1:\Legacy,Orch1:\Migration
```

Gets classic robots from the Legacy and Migration folders.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchClassicRobot | Where-Object {$_.Type -eq "Unattended"}
```

Gets all classic robots of type "Unattended".

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchClassicRobot -ExportCsv C:\Reports\ClassicRobots.csv
```

Exports all classic robots from the current folder to a CSV file for reporting or backup purposes.

## PARAMETERS

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.



``yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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
Specifies the Name of the robots to be retrieved.



``yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True

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



``yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True

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



``yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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



``yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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

### -CsvEncoding
Specifies the encoding for the exported CSV file when using -ExportCsv. The default is UTF-8 with BOM for Excel compatibility.



``yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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
Exports the retrieved classic robots to a CSV file with the specified path. The CSV includes human-readable names instead of internal IDs and uses UTF-8 encoding with BOM for Excel compatibility.

The exported CSV can be imported using Import-Csv for further processing or documentation purposes.

``yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Session
## NOTES

## RELATED LINKS
