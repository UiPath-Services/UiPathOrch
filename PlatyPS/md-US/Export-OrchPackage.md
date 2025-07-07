---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchPackage

## SYNOPSIS
Exports packages from specified folders to local files.

## SYNTAX

```
Export-OrchPackage [-Id] <String[]> [[-Version] <String[]>] [[-Destination] <String>] [-Path <String[]>]
 [-Recurse] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Export-OrchPackage cmdlet exports automation packages from UiPath Orchestrator folders to local .nupkg files. This cmdlet enables backup, archival, or migration of automation packages between different Orchestrator environments or for offline storage.

Packages contain the compiled automation workflows and their dependencies. Exporting packages creates .nupkg files that can be stored locally, transferred to other environments, or used for backup purposes. This is essential for version control, disaster recovery, and environment migration scenarios.

Use the -Id parameter to specify which packages to export by their package ID. The -Version parameter allows targeting specific package versions, and -Destination specifies where the exported .nupkg files should be saved. The cmdlet supports wildcard patterns for exporting multiple packages efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables exporting packages from all subfolders.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Export-OrchPackage ProcessAutomation
```

Exports the latest version of the ProcessAutomation package from the current folder to the default destination.

### Example 2
```powershell
PS C:\> Export-OrchPackage -Path Orch1:\Development DataProcessor 1.0.5 "C:\Exports"
```

Exports version 1.0.5 of the DataProcessor package from the Development folder to C:\Exports directory.

### Example 3
```powershell
PS Orch1:\Development> Export-OrchPackage *Automation*, *Workflow* -Destination "C:\Backup" -WhatIf
```

Shows what would happen when exporting multiple packages with names containing Automation or Workflow to C:\Backup directory.

### Example 4
```powershell
PS C:\> Export-OrchPackage -Path Orch1:\Development CustomerProcess 1.0.* "C:\Versions"
```

Exports all versions starting with 1.0 of the CustomerProcess package from the Development folder to C:\Versions directory.

### Example 5
```powershell
PS Orch1:\> Export-OrchPackage -Recurse *Critical* "C:\CriticalBackup" -Confirm
```

Exports all packages containing Critical in their names from all subfolders recursively to C:\CriticalBackup with confirmation prompts.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchPackage *Production* | Export-OrchPackage -Destination "C:\ProductionBackup"
```

Gets all packages containing Production in their names and exports them to C:\ProductionBackup using pipeline input.

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
Specifies the package IDs to be exported.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the source folders. If not specified, the current folder will be used as the source.

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
Specifies that packages should be exported from all subfolders recursively.

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

### -Version
Specifies the package versions to be exported. If not specified, the latest version will be exported.

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

### System.String[]
Package IDs and versions can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Package
Package objects from Get-OrchPackage can be piped to this cmdlet. The Id and Version properties will be automatically mapped to corresponding parameters via ByPropertyName binding.

## OUTPUTS

### System.IO.FileInfo
Returns information about the exported .nupkg files.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Exported packages are saved as .nupkg files that can be imported into other Orchestrator environments using Import-OrchPackage. Ensure sufficient disk space for large packages. Use version wildcards for exporting multiple versions. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Import-OrchPackage](Import-OrchPackage.md)

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Update-OrchPackage](Update-OrchPackage.md)
