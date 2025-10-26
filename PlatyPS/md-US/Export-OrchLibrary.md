---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchLibrary

## SYNOPSIS
Exports libraries from tenants to local files.

## SYNTAX

```
Export-OrchLibrary [[-Id] <String[]>] [[-Version] <String[]>] [[-Destination] <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Export-OrchLibrary cmdlet exports reusable component libraries from UiPath Orchestrator tenants to local .nupkg files. This cmdlet enables backup, archival, or migration of activity libraries between different Orchestrator environments or for offline storage.

Libraries contain reusable activities, custom activities, and workflow components that can be shared across multiple automation projects. Exporting libraries creates .nupkg files that can be stored locally, transferred to other environments, or used for backup purposes. This is essential for component management, version control, and environment migration scenarios.

Use the -Id parameter to specify which libraries to export by their library ID. The -Version parameter allows targeting specific library versions, and -Destination specifies where the exported .nupkg files should be saved. The cmdlet supports wildcard patterns for exporting multiple libraries efficiently.

This is a tenant entity cmdlet. The -Path parameter specifies the source tenant drives (e.g., Orch1:, Orch2:) from which libraries should be exported.

Primary Endpoint: GET /odata/Libraries, GET /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions, GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Export-OrchLibrary CustomActivities
```

Exports the latest version of the CustomActivities library from the current tenant to the default destination.

### Example 2
```powershell
PS C:\> Export-OrchLibrary -Path Orch1: SharedComponents 2.1.0 "C:\Exports"
```

Exports version 2.1.0 of the SharedComponents library from Orch1 tenant to C:\Exports directory.

### Example 3
```powershell
PS Orch1:\> Export-OrchLibrary *Utilities*, *Helpers* "C:\Backup" -WhatIf
```

Shows what would happen when exporting multiple libraries with names containing Utilities or Helpers to C:\Backup directory.

### Example 4
```powershell
PS C:\> Export-OrchLibrary -Path Orch1: CommonLibrary 1.* "C:\Versions"
```

Exports all versions starting with 1. of the CommonLibrary from Orch1 tenant to C:\Versions directory.

### Example 5
```powershell
PS Orch1:\> Export-OrchLibrary *Critical* "C:\CriticalBackup" -Confirm
```

Exports all libraries containing Critical in their names from the current tenant to C:\CriticalBackup with confirmation prompts.

### Example 6
```powershell
PS Orch1:\> Get-OrchLibrary *Enterprise* | Export-OrchLibrary "C:\EnterpriseLibs"
```

Gets all libraries containing Enterprise in their names and exports them to C:\EnterpriseLibs using pipeline input.

## PARAMETERS

### -Destination
Specifies the destination directory where exported .nupkg files will be saved. If not specified, files will be saved to the current directory.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
Specifies the library IDs to be exported. If not specified, all libraries will be exported.

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
Specifies the source tenant drives. If not specified, the current tenant will be used as the source.

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

### -Version
Specifies the library versions to be exported. If not specified, the latest version will be exported.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

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

### System.Object
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies tenant drive names (e.g., Orch1:, Orch2:) for source tenants.

Exported libraries are saved as .nupkg files that can be imported into other Orchestrator environments using Import-OrchLibrary. Libraries contain reusable activities and components. Ensure sufficient disk space for large libraries. Use version wildcards for exporting multiple versions. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Import-OrchLibrary](Import-OrchLibrary.md)

[Get-OrchLibrary](Get-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

