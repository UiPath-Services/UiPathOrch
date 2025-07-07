---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchClassicEnvironment

## SYNOPSIS
Gets environments from classic folders.

## SYNTAX

```
Get-OrchClassicEnvironment [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchClassicEnvironment cmdlet retrieves classic environment configurations from UiPath Orchestrator. Classic environments are the legacy robot grouping mechanism used before the modern folder-based approach, containing robot collections and their associated processes.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

Classic environments provide backward compatibility for existing robot deployments and are primarily used for migrating from older Orchestrator versions to the modern folder structure.

Primary Endpoint: GET /odata/Environments?$expand=Robots

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Environments.View

## EXAMPLES

### Example 1
```powershell
Get-OrchClassicEnvironment
```

Gets all classic environments in the current folder.

### Example 2
```powershell
Get-OrchClassicEnvironment Production
```

Gets the classic environment named "Production" from the current folder.

### Example 3
```powershell
Get-OrchClassicEnvironment *Test*
```

Gets all classic environments whose names contain "Test".

### Example 4
```powershell
Get-OrchClassicEnvironment -Recurse
```

Gets all classic environments from the current folder and all its subfolders.

### Example 5
```powershell
Get-OrchClassicEnvironment -Path Orch1:\Legacy, Orch1:\Migration
```

Gets classic environments from the Legacy and Migration folders.

### Example 6
```powershell
Get-OrchClassicEnvironment | Where-Object {$_.Robots.Count -gt 0}
```

Gets all classic environments that contain at least one robot.

### Example 7
```powershell
Get-OrchClassicEnvironment -Depth 1 | Select-Object Name, Description, @{Name="RobotCount"; Expression={$_.Robots.Count}}
```

Gets classic environments from the current folder only (no subfolders) and displays their names, descriptions, and robot counts.

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
Specifies the Name of the environments to be retrieved.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Environment names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Environment
Environment objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.Environment

## NOTES

## RELATED LINKS
