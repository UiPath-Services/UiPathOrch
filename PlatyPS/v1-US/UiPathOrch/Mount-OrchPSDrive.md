---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/08/2026
PlatyPS schema version: 2024-05-01
title: Mount-OrchPSDrive
---

# Mount-OrchPSDrive

## SYNOPSIS

Mounts UiPathOrch PSDrives from the configuration file.

## SYNTAX

### __AllParameterSets

```
Mount-OrchPSDrive [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Mount-OrchPSDrive` cmdlet reads the UiPathOrchConfig.json configuration file and mounts all enabled Orchestrator tenants as PSDrives. All existing UiPathOrch drives (including Document Understanding and Test Manager drives) are removed and re-created from the configuration file. Cached data in the existing drives is discarded.

Use this cmdlet to reload the configuration after editing UiPathOrchConfig.json with `Edit-OrchConfig`, without restarting the PowerShell session.

This cmdlet can also be used for initial setup: after the module is imported for the first time and the configuration file is created with `Edit-OrchConfig`, run `Mount-OrchPSDrive` to mount the drives.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Mount drives from the configuration file

```powershell
PS C:\> Mount-OrchPSDrive
```

Reads UiPathOrchConfig.json and mounts all enabled drives.

### Example 2: Reload configuration after editing

```powershell
PS C:\> Edit-OrchConfig
PS C:\> Mount-OrchPSDrive
```

Opens the configuration file for editing, then reloads it to apply the changes.

### Example 3: Preview with -WhatIf

```powershell
PS C:\> Mount-OrchPSDrive -WhatIf
```

Shows the configuration file path that would be loaded without actually removing or creating any drives.

## PARAMETERS

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### None

This cmdlet does not produce output. Use `-Verbose` to see the number of drives mounted.

## NOTES

This cmdlet removes all existing UiPathOrch, UiPathOrchDu, and UiPathOrchTm drives before creating new ones. Any cached data is discarded. Run `Get-OrchPSDrive` after mounting to verify the drive configuration.

## RELATED LINKS

Edit-OrchConfig

Get-OrchConfigPath

Get-OrchPSDrive

New-OrchPSDrive
