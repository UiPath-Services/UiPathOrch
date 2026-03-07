---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchLocation
---

# Set-OrchLocation

## SYNOPSIS

Sets the current location to the UiPathOrch module's installation directory.

## SYNTAX

### __AllParameterSets

```
Set-OrchLocation [[-ModuleName] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The Set-OrchLocation cmdlet changes the current location (working directory) to the directory where the specified PowerShell module is installed. By default, it navigates to the UiPath.PowerShell.OrchProvider module directory.

This is useful for quickly navigating to the module's files, documentation, and configuration resources.

The -ModuleName parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available loaded PowerShell modules. Wildcard patterns are supported to match module names.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Navigate to the UiPathOrch module directory

```powershell
PS C:\> Set-OrchLocation
```

Sets the current location to the installation directory of the UiPathOrch module (the default module).

### Example 2: Navigate to a specific module directory

```powershell
PS C:\> Set-OrchLocation UiPath.PowerShell.OrchProvider
```

You can specify the module name explicitly. The cmdlet navigates to the directory where the specified module DLL is located.

## PARAMETERS

### -ModuleName

Specifies the name of the module whose installation directory to navigate to. Defaults to "UiPath.PowerShell.OrchProvider". Accepts wildcard patterns.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.Object

This cmdlet does not produce pipeline output. It changes the current working directory.

## NOTES

The cmdlet only considers assemblies that reference System.Management.Automation and have a .dll extension, which are indicators of PowerShell modules. If the module name matches zero or more than one loaded assembly, an error is thrown.

## RELATED LINKS

Get-OrchConfigPath

Get-OrchHelp
