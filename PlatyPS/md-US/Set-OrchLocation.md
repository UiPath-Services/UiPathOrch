---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchLocation

## SYNOPSIS
Sets the current location to the UiPathOrch module's installation directory.

## SYNTAX

```
Set-OrchLocation [[-ModuleName] <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Set-OrchLocation cmdlet changes the current location (working directory) to the directory where the UiPathOrch module is installed. This allows for quick navigation to the module's files and resources.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-OrchLocation
```

This command sets the current location to the installation directory of the UiPathOrch module.

### Example 2
```powershell
PS C:\> Set-OrchLocation UiPath.PowerShell.OrchProvider
```

You can specify the module name explicitly.

## PARAMETERS

### -ModuleName
Specifies the name of the module. When specified, moves to the directory where that module is installed.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
