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

Classic environments are folder entities that exist within specific folders. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

Classic environments provide backward compatibility for existing robot deployments and are primarily used for migrating from older Orchestrator versions to the modern folder structure.

Primary Endpoint: GET /odata/Environments?$expand=Robots

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Environments.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Legacy> Get-OrchClassicEnvironment
```

Gets all classic environments in the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Recurse
```

Gets all classic environments from all folders recursively.

### Example 3
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Path Orch1:\Legacy *Test*
```

Gets classic environments whose names contain "Test" from the Legacy folder.

### Example 4
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Recurse | ConvertTo-Json -Depth 2
```

Gets all classic environments and displays the complete structure including nested Robots array.

### Example 5
```powershell
PS C:\> Get-OrchClassicEnvironment -Path Orch1:\Legacy,Orch1:\Migration -Recurse -Depth 2
```

Gets classic environments from Legacy and Migration folders with maximum depth of 2 levels.

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

## OUTPUTS

### UiPath.PowerShell.Entities.Environment

## NOTES

## RELATED LINKS
