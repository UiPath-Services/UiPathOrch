---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/08/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchConfig
---

# Import-OrchConfig

## SYNOPSIS

Imports the UiPathOrch configuration file and creates PSDrives.

## SYNTAX

### __AllParameterSets

```
Import-OrchConfig [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Import-OrchConfig` cmdlet reads the UiPathOrchConfig.json configuration file and creates PSDrives for all enabled Orchestrator tenants. All existing UiPathOrch drives (including Document Understanding and Test Manager drives) are removed and re-created. Cached data in the existing drives is discarded.

If the configuration file has not changed since the last import, the cmdlet skips re-creation. Use `-Force` to override this check.

Use this cmdlet to apply configuration changes after editing UiPathOrchConfig.json with `Edit-OrchConfig`, without restarting the PowerShell session.

This cmdlet can also be used for initial setup: after the configuration file is created with `Edit-OrchConfig`, run `Import-OrchConfig` to create the drives.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Import configuration and create drives

```powershell
PS C:\> Import-OrchConfig
```

Reads UiPathOrchConfig.json and creates PSDrives for all enabled tenants.

### Example 2: Apply configuration changes

```powershell
PS C:\> Edit-OrchConfig
PS C:\> Import-OrchConfig
```

Opens the configuration file for editing, then reloads it to apply the changes.

### Example 3: Force re-creation

```powershell
PS C:\> Import-OrchConfig -Force
```

Forces re-creation of all drives even if the configuration file has not changed since the last import.

### Example 4: Preview with -WhatIf

```powershell
PS C:\> Import-OrchConfig -WhatIf
```

Shows the configuration file path that would be loaded without actually removing or creating any drives.

## PARAMETERS

### -Force

Forces re-creation of all drives even if the configuration file has not changed since the last import. Without this switch, the cmdlet skips when the file's last write time matches the previous import.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
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

This cmdlet does not produce output. Use `-Verbose` to see the number of drives created.

## NOTES

This cmdlet removes all existing UiPathOrch, UiPathOrchDu, and UiPathOrchTm drives before creating new ones. Any cached data is discarded. Run `Get-OrchPSDrive` after importing to verify the drive configuration.

## RELATED LINKS

[Edit-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Edit-OrchConfig.md)

[Get-OrchConfigPath](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchConfigPath.md)

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)

[New-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchPSDrive.md)
