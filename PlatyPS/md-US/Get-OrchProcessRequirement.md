---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchProcessRequirement

## SYNOPSIS
Gets the dependency resources (requirements) of processes.

## SYNTAX

```
Get-OrchProcessRequirement [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

Retrieves information about external resources (processes, assets, queues, triggers, task catalogs, connections, etc.) that processes in the specified folder depend on.
You can also check the validation status (Success, NotFound, Unknown) of each dependency resource.

Primary Endpoint: GET /odata/Releases({release.Id})/UiPath.Server.Configuration.OData.GetResources(processKey='{release.ProcessKey}:{release.ProcessVersion}')

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1: Get all dependency resources
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse
```

Retrieves all dependency resources for all processes in the current folder and its subfolders.

### Example 2: Filter by process name and format output
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Path Shared *Queue*
```

Retrieves dependency resources for processes in the 'Shared' folder matching the wildcard pattern '*Queue*'.

### Example 3: Find missing dependencies
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse | Where-Object ValidationResult -eq 'NotFound'
```

Identifies all missing dependency resources across all processes, useful for troubleshooting deployment issues.

## PARAMETERS

### -Depth
Specifies the depth of hierarchy to search. 0 searches only the current folder, 1 searches one level down.

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
Specifies the process name(s). Wildcard characters (*, ?) are supported. You can also specify multiple names as an array.

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
Specifies the folder path(s) to search. Wildcard characters are supported. Specify UiPathOrch provider paths (e.g., Orch1:\FolderName).

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
Recursively searches the specified folder and all its subfolders. If this parameter is not specified, only the current folder will be searched.

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
Controls the behavior of PowerShell progress display.

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

### UiPath.PowerShell.Entities.SubtypedPackageResource

## NOTES

This cmdlet can retrieve the following types of resources that processes depend on:
- Process: Other processes
- Asset: Assets
- Queue: Queues
- TaskCatalog: Task catalogs
- TimeTrigger: Time triggers
- Connection: Connections (integrations with external services)

Each resource has a ValidationResult property with one of the following values:
- Success: The resource is successfully validated and exists in the folder
- NotFound: The resource was not found
- Unknown: The validation status of the resource is unknown

You must specify one of -Path, -Recurse, or -Depth parameters.
If none are specified, navigate to the target folder first using the Set-Location cmdlet.

## RELATED LINKS
