---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchPackage

## SYNOPSIS
Imports packages from local files to specified folders.

## SYNTAX

```
Import-OrchPackage [-Source] <String[]> [[-Path] <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Import-OrchPackage cmdlet imports automation packages from local .nupkg files into UiPath Orchestrator folders. This cmdlet enables deployment of automation packages from backup files, migration between environments, or installation of packages created offline.

Packages contain compiled automation workflows and their dependencies. Importing packages uploads .nupkg files to Orchestrator, making them available for process deployment and execution. This is essential for environment setup, disaster recovery, and automation deployment scenarios.

Use the -Source parameter to specify the local .nupkg files or directories containing packages to import. The -Path parameter specifies the target Orchestrator folders where packages should be uploaded. The cmdlet supports wildcard patterns for importing multiple packages efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables processing all subdirectories when importing from local directories.

Primary Endpoint: POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution

Required permissions: Packages.Create and FolderPackages.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Import-OrchPackage C:\LocalFeed\MyProcess.1.0.1.nupkg
```

Imports a package from the local file system to the Orchestrator tenant feed.

### Example 2
```powershell
PS Orch1:\Development> Import-OrchPackage C:\LocalFeed\MyProcess.1.0.1.nupkg
```

Imports a package from the local file system to the Orchestrator folder feed. If there is no feed in the Orch1:\Development folder, it will be imported to the tenant feed.

### Example 3
```powershell
PS C:\LocalFeed> Import-OrchPackage MyProcess.1.0.1.nupkg -Path Orch1:, Orch2:
```

Imports the same package to the tenant feeds of multiple Orchestrator instances.

### Example 4
```powershell
PS LocalFeed:\> Import-OrchPackage . -Path Orch1: -Confirm
```

Bulk imports all package files in the folder. The import destination will be the current folder of the Orch1: drive. If you want to always import to the tenant feed regardless of the current folder location, specify Orch1:\ in the -Path parameter.

## PARAMETERS

### -Path
Specifies the target folders where packages should be imported. If not specified, the current folder will be used as the target.

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

### -Source
Specifies the source .nupkg files or directories containing packages to be imported.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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

### -Recurse
Specifies that all subdirectories should be processed when importing from local directories.

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

### UiPath.PowerShell.Entities.BulkItemDtoOfString
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Imported packages must be valid .nupkg files created by UiPath Studio or exported using Export-OrchPackage. Ensure package dependencies are available in the target environment. Package names and versions must be unique within folders. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Export-OrchPackage](Export-OrchPackage.md)

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Update-OrchPackage](Update-OrchPackage.md)

