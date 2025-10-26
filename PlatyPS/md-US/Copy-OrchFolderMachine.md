---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchFolderMachine

## SYNOPSIS
Copies folder machine assignments to destination folders.

## SYNTAX

```
Copy-OrchFolderMachine [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchFolderMachine cmdlet copies machine assignments from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet replicates the machine-to-folder relationships, ensuring that the same machines are available in the destination folders.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). This is useful for maintaining consistent machine availability across different environments or for setting up parallel folder structures.

Use the -Name parameter to specify which machines to copy assignments for and the -Destination parameter to specify the target folder. The cmdlet copies the machine assignments rather than the machines themselves.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying machine assignments from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={key}), POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth required scopes: OR.Folders

Required permissions: Units.View, SubFolders.View, Units.Edit, SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchFolderMachine Robot01 \Production
```

Copies the Robot01 machine assignment from the current folder (Development) to the Production folder within the same tenant.

### Example 2
```powershell
PS C:\> Copy-OrchFolderMachine -Path Orch1:\Development SharedBot Orch2:\Production
```

Copies the SharedBot machine assignment from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant machine assignment copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchFolderMachine Robot*, Bot* \Production -WhatIf
```

Shows what would happen when copying multiple machine assignments with names starting with Robot or Bot from the current folder to the Production folder.

## PARAMETERS

### -Destination
Specifies the destination folder where machine assignments should be copied.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the machines whose assignments should be copied.

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

### -Path
Specifies the source folder. If not specified, the current folder will be used as the source.

```yaml
Type: String
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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

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

### -Recurse
Specifies that machine assignments should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### System.Object
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet copies machine assignments, not the machines themselves. The machines must already exist in the target tenant when copying across tenants.

## RELATED LINKS

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)

[Add-OrchFolderMachine](Add-OrchFolderMachine.md)

[Remove-OrchFolderMachine](Remove-OrchFolderMachine.md)
