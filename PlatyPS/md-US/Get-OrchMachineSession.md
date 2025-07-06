---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachineSession

## SYNOPSIS
Gets machine runtime sessions from UiPath Orchestrator.

## SYNTAX

```
Get-OrchMachineSession [-Status <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Retrieves machine runtime sessions from UiPath Orchestrator, showing connection status and runtime information for machines across folders. This cmdlet provides information about machine connectivity, runtime availability, and session details including robot execution capacity.

Primary Endpoint: /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: (Machines.View or Jobs.Create)

This cmdlet operates on folder entities and requires either navigation to the target folder or specification of target folders using -Path, -Recurse, or -Depth parameters.

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchMachineSession
```

Gets all machine sessions in the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse
```

Gets machine sessions from all folders in the current drive.

### Example 3
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse -Status Connected
```

Gets only connected machine sessions from all folders.

### Example 4
```powershell
PS Orch1:\> Get-OrchMachineSession -Path Orch1:\Shared -Status Disconnected
```

Gets disconnected machine sessions from the Shared folder.

### Example 5
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse | Where-Object {$_.Runtimes -gt 0} | Select-Object Path, MachineName, Runtimes, UsedRuntimes
```

Gets all machine sessions with available runtimes and displays capacity information. Note that Path is selected first to identify which folder each entity belongs to.

### Example 6
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse | ConvertTo-Json -Depth 3
```

Gets machine sessions and displays the complete object structure including detailed session information.

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

### -Path
Specifies the folder paths where to search for machine sessions. Supports wildcard characters (* and ?) for pattern matching. Use this parameter when you want to target specific folders without changing the current location.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
Specifies the status of the machine sessions to be retrieved. Common values include Connected, Disconnected, and others. Supports wildcard characters (* and ?) for pattern matching.

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
Specifies how progress information is displayed during the operation.

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime
## NOTES

This cmdlet operates on folder entities and requires either:
- Navigation to the target folder using Set-Location (cd), OR  
- Specification of target folders using the -Path, -Recurse, or -Depth parameters

**Important:** For optimal PowerShell IntelliSense support, specify -Path, -Recurse, or -Depth before other parameters when using multiple parameters.

**Machine Session Information:**
- SessionId: Unique identifier for the machine session
- Status: Connection status (Connected, Disconnected, etc.)
- RuntimeType: Type of runtime (Headless, AutomationCloud, etc.)
- Runtimes: Total available runtime slots
- UsedRuntimes: Currently used runtime slots
- MaintenanceMode: Current maintenance mode setting

**Common Status Values:**
- Connected: Machine is actively connected
- Disconnected: Machine is not currently connected
- Busy: Machine is actively executing jobs

**Use Cases:**
- Monitor machine connectivity across environments
- Check runtime availability and utilization
- Troubleshoot machine connection issues
- Capacity planning for robot deployment

**Important Note about Path Selection:**
When using Select-Object with folder entities, always include Path as the first property to identify which folder each entity belongs to. This is essential for managing entities across multiple folders.

Use ConvertTo-Json to explore the complete machine session object structure including detailed connection information and runtime statistics.

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
