---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchFolderMachineAccountMapping

## SYNOPSIS
Enables machine account mapping for folder-scoped machines.

## SYNTAX

```
Enable-OrchFolderMachineAccountMapping [-Name] <String[]> [[-UserName] <String[]>] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchFolderMachineAccountMapping cmdlet enables the mapping between machines and user accounts within specific folders. This creates an association that allows specific users to connect to machines in the folder context.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

When account mapping is enabled, the specified users can establish dedicated connections to the specified machines through the folder context, providing controlled access to automation resources.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}), POST /odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots

OAuth required scopes: OR.Robots

Required permissions: Robots.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineAccountMapping Machine01 john.doe -WhatIf
```

Shows what would happen when enabling account mapping between Machine01 and user john.doe in the current folder.

### Example 2
```powershell
PS Orch1:\> Enable-OrchFolderMachineAccountMapping Machine01 john.doe
```

Enables the account mapping between Machine01 and user john.doe in the current folder.

### Example 3
```powershell
PS Orch1:\> Enable-OrchFolderMachineAccountMapping *Prod* automation.user
```

Enables account mappings for all machines containing "Prod" and the automation.user account.

### Example 4
```powershell
PS Orch1:\> Enable-OrchFolderMachineAccountMapping -Path Orch1:\Development Machine01, Machine02 developer1, developer2
```

Enables account mappings between multiple machines and users in the Development folder.

### Example 5
```powershell
PS Orch1:\> Enable-OrchFolderMachineAccountMapping -Recurse *Robot* service.account -Confirm
```

Enables account mappings for all machines containing "Robot" and the service.account in the current folder and all subfolders with confirmation.

### Example 6
```powershell
PS Orch1:\> Get-OrchMachine -Status Available | Enable-OrchFolderMachineAccountMapping -UserName qa.tester
```

Enables account mappings between all available machines and the qa.tester account. Machine names are passed via pipeline using ByPropertyName binding.

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
Specifies the name of the machines for which account mapping should be enabled.

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
Specifies the user names for which account mapping should be enabled. Multiple users can be specified to create mappings with multiple machines.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
