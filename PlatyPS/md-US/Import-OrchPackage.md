---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchPackage

## SYNOPSIS
Uploads packages.

## SYNTAX

```
Import-OrchPackage [-Source] <String[]> [[-Path] <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

Primary Endpoint:

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Import-OrchPackage c:\pkg\*.nupkg
```
Uploads the specified package file to the process feed of the current folder. If the current folder does not have a process feed, it uploads to the tenant's package feed.

### Example 2
```powershell
PS Orch1:\> Import-OrchPackage c:\pkg
```
When a folder is specified for the -Source parameter, it uploads all *.nupkg files contained in this folder. Note that the -Source parameter can simultaneously specify multiple folder names and file names, separated by commas.

### Example 3
```powershell
PS Orch1:\> Import-OrchPackage c:\pkg\*.nupkg -Path Orch1:\,Orch2:\myfolder
```
Uploads the specified package files to both the tenant process feed of Orch1: and the process feed of myfolder in Orch2:. If the Orch2:\myfolder folder does not have a process feed, it uploads to the tenant process feed of Orch2:.

### Example 4
```powershell
PS Orch1:\> Import-OrchPackage c:\pkg -Recurse
```
Running with the -Recurse parameter in the root folder will upload in bulk to the tenant feed and all folders with a feed. Note that only folders directly under the root folder can have a feed, so specifying the -Recurse option only makes sense when the root folder is targeted. In other words, when this cmdlet is run at the root folder of the UiPathOrch drive or when the -Path parameter specifies the root folder of the UiPathOrch drive. It is useful for migrating packages downloaded with Export-OrchPackage -Recurse to another tenant in bulk.


## PARAMETERS

### -Path
Specifies the target folder. If not specified, the current folder will be targeted.

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
{{ Fill Source Description }}

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
{{ Fill Recurse Description }}

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

## RELATED LINKS
