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

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Import-OrchPackage "C:\Packages\ProcessAutomation.1.0.0.nupkg"
```

Imports the ProcessAutomation package from the specified .nupkg file to the current folder (Development).

### Example 2
```powershell
PS C:\> Import-OrchPackage -Source "C:\Exports\*.nupkg" -Path Orch1:\Development
```

Imports all .nupkg files from C:\Exports directory to the Orch1:\Development folder.

### Example 3
```powershell
PS Orch1:\Development> Import-OrchPackage "C:\Backup\DataProcessor.*.nupkg" -WhatIf
```

Shows what would happen when importing all versions of DataProcessor package from C:\Backup directory to the current folder.

### Example 4
```powershell
PS C:\> Import-OrchPackage -Source "C:\Packages" -Path Orch1:\Production -Recurse
```

Imports all .nupkg files from C:\Packages directory and its subdirectories to the Orch1:\Production folder.

### Example 5
```powershell
PS Orch1:\Development> Import-OrchPackage "C:\Critical\*.nupkg", "C:\Backup\*.nupkg" -Confirm
```

Imports all .nupkg files from both C:\Critical and C:\Backup directories to the current folder with confirmation prompts.

### Example 6
```powershell
PS C:\> Get-ChildItem "C:\Exports\*.nupkg" | Import-OrchPackage -Path Orch1:\Development
```

Gets all .nupkg files from C:\Exports directory and imports them to Orch1:\Development using pipeline input.

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
