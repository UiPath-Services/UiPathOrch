---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLibrary

## SYNOPSIS
Gets library packages from Orchestrator or external host feeds.

## SYNTAX

```
Get-OrchLibrary [[-Id] <String[]>] [-HostFeed] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Gets library packages from UiPath Orchestrator tenants or external host feeds. Libraries are reusable components that contain activities, workflows, and other automation resources that can be shared across multiple processes.

By default, this cmdlet returns libraries from the connected Orchestrator instance. When the -HostFeed parameter is specified, it retrieves packages from external host feeds (such as the official NuGet gallery and UiPath feeds) that can be imported into Orchestrator.

Libraries are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name.

Primary Endpoint: GET /odata/Libraries

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Libraries.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchLibrary
```

Gets all libraries from the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchLibrary *Excel*
```

Gets libraries containing "Excel" in their ID.

### Example 3
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed
```

Gets available libraries from external host feeds.

### Example 4
```powershell
PS Orch1:\> Get-OrchLibrary *Activities* -HostFeed
```

Gets libraries containing "Activities" in their ID from host feeds.

### Example 5
```powershell
PS Orch1:\> Get-OrchLibrary -Path Orch1:, Orch2:
```

Gets libraries from multiple tenants.

### Example 6
```powershell
PS Orch1:\> Get-OrchLibrary | Where-Object {$_.Published -gt (Get-Date).AddDays(-30)}
```

Gets libraries published in the last 30 days.

### Example 7
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed | Where-Object {$_.Id -like "*UiPath*"} | Select-Object Id, Version, Description
```

Gets UiPath libraries from host feeds with selected properties.

## PARAMETERS

### -Id
Specifies the IDs of libraries to retrieve. Supports wildcards and multiple values.

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
Specifies target tenants by drive name. Use comma-separated values for multiple tenants. If not specified, targets the current tenant.

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

### -HostFeed
Retrieves libraries from external host feeds instead of the local Orchestrator instance. This shows libraries available for import from NuGet gallery and UiPath feeds.

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

### UiPath.PowerShell.Entities.Library
## NOTES
Library entities are tenant-scoped and operate across the entire tenant.

Use -HostFeed to browse libraries available for import from external sources before using Import-OrchLibrary to add them to your Orchestrator.

Libraries from host feeds show available packages that can be imported, while local libraries show packages already available in the Orchestrator.

The Published property shows when the library was last published, useful for identifying recent updates.



Primary Endpoint: GET /odata/Libraries
OAuth required scopes: OR.Execution or OR.Execution.Read
Required permissions: Libraries.View

## RELATED LINKS

[Import-OrchLibrary](Import-OrchLibrary.md)

[Export-OrchLibrary](Export-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

[Copy-OrchLibrary](Copy-OrchLibrary.md)
