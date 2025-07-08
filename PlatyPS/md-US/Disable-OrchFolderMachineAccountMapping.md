---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchFolderMachineAccountMapping

## SYNOPSIS
Disables machine account mapping for folder-scoped machines.

## SYNTAX

```
Disable-OrchFolderMachineAccountMapping [-Name] <String[]> [[-UserName] <String[]>] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchFolderMachineAccountMapping cmdlet disables the mapping between machines and user accounts within specific folders. This removes the association that allows specific users to connect to machines in the folder context.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

When account mapping is disabled, users will no longer be able to establish dedicated connections to the specified machines through the folder context.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}), POST /odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots

OAuth required scopes: OR.Robots

Required permissions: Robots.Edit

## EXAMPLES

### Example 1
```powershell
Disable-OrchFolderMachineAccountMapping Machine01 -WhatIf
```

Shows what would happen when disabling all account mappings for Machine01 in the current folder.

### Example 2
```powershell
Disable-OrchFolderMachineAccountMapping Machine01 john.doe
```

Disables the account mapping between Machine01 and user john.doe in the current folder.

### Example 3
```powershell
Disable-OrchFolderMachineAccountMapping *Dev* *test*
```

Disables account mappings for all machines containing "Dev" and all users containing "test".

### Example 4
```powershell
Disable-OrchFolderMachineAccountMapping -Path Orch1:\Development Machine01, Machine02 -Confirm
```

Disables all account mappings for Machine01 and Machine02 in the Development folder with confirmation.

### Example 5
```powershell
Disable-OrchFolderMachineAccountMapping -Recurse *Template*
```

Disables account mappings for all machines containing "Template" in the current folder and all subfolders.

### Example 6
```powershell
Get-OrchMachine *Robot* | Disable-OrchFolderMachineAccountMapping -UserName admin.user
```

Disables account mappings between all machines containing "Robot" and the admin.user account. Machine names are passed via pipeline using ByPropertyName binding.

## PARAMETERS

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
Specifies the name of the machines for which account mapping should be disabled.

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

### -UserName
Specifies the user names for which account mapping should be disabled. If not specified, all account mappings for the specified machines will be disabled.

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
Machine names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Machine
Machine objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### System.Object

## NOTES



## RELATED LINKS
