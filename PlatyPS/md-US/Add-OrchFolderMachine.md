---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchFolderMachine

## SYNOPSIS
Assigns machines to folders.

## SYNTAX

```
Add-OrchFolderMachine [-Name] <String[]> [-PropagateToSubFolders <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchFolderMachine cmdlet assigns machines to specific folders within UiPath Orchestrator tenants. This cmdlet enables you to organize machines within the folder structure and control which folders have access to specific machines for process execution.

The cmdlet supports assigning machines to multiple folders simultaneously and can propagate machine assignments to subfolders when specified. Use this cmdlet to manage machine allocation across different teams, projects, or environments organized in the folder hierarchy.

Use the -Name parameter to specify which machines to assign and the -Path parameter to specify target folders. The -PropagateToSubFolders parameter controls whether the machine assignment should be inherited by all subfolders.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables assigning machines to all subfolders, maintaining consistent machine availability across the folder structure.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Update machines to any folder associations or only if user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Add-OrchFolderMachine Robot01
```

Assigns the Robot01 machine to the current folder (Production).

### Example 2
```powershell
PS C:\> Add-OrchFolderMachine -Path Orch1:\Development Robot01, Robot02
```

Assigns Robot01 and Robot02 machines to the Development folder.

### Example 3
```powershell
PS Orch1:\Finance> Add-OrchFolderMachine Bot* -PropagateToSubFolders True
```

Assigns all machines with names starting with Bot to the Finance folder and propagates the assignment to all subfolders.

### Example 4
```powershell
PS C:\> Add-OrchFolderMachine -Path Orch1:\Development, Orch1:\Testing SharedBot -WhatIf
```

Shows what would happen when assigning SharedBot machine to both Development and Testing folders.

### Example 5
```powershell
PS Orch1:\> Add-OrchFolderMachine -Recurse ProductionBot01 -PropagateToSubFolders True
```

Assigns ProductionBot01 to all folders recursively from the current location and propagates to their subfolders.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchMachine Template* | Add-OrchFolderMachine -PropagateToSubFolders False
```

Gets all machines with names starting with Template and assigns them to the current folder without propagating to subfolders.

## PARAMETERS

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

### -Name
Specifies the Name of the machines to be assigned to folders.

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
Specifies the target folders where machines should be assigned.

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
Specifies that machines should be assigned to all subfolders recursively from the specified path.

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

### -PropagateToSubFolders
Specifies whether the machine assignment should be propagated to all subfolders. Valid values are True and False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Machine names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Machine
Machine objects from Get-OrchMachine can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

The -PropagateToSubFolders parameter controls inheritance behavior. Use -WhatIf to preview changes before executing machine assignments.

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Remove-OrchFolderMachine](Remove-OrchFolderMachine.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)
