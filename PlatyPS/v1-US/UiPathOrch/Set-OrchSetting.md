---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchSetting
---

# Set-OrchSetting

## SYNOPSIS

Sets a general setting value on Orchestrator.

## SYNTAX

### __AllParameterSets

```
Set-OrchSetting [-Name] <string> [-Value] <string> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates a general setting on UiPath Orchestrator. Settings control the behavior and configuration of the Orchestrator instance (e.g., deployment, security, and other system-wide settings).

The -Name parameter supports tab completion against the existing Settings keys; the tooltip shows the current value. -Value also tab-completes to the current value, which is convenient when you want to inspect/echo it before changing.

Pipeline-friendly: pass `Settings`-shaped objects (with `Name` and `Value` properties) and they are accumulated and sent in a single bulk API call at end of pipeline.

```powershell
'Abp.Net.Mail.DefaultFromAddress','Abp.Net.Mail.DefaultFromDisplayName' |
    ForEach-Object { [pscustomobject]@{ Name = $_; Value = "..." } } |
    Set-OrchSetting
```

Primary Endpoint: POST /odata/Settings/UiPath.Server.Configuration.OData.UpdateBulk

OAuth required scopes: OR.Settings or OR.Settings.Write

Required permissions: Settings.Edit

## EXAMPLES

### Example 1: Update a single setting

```powershell
PS Orch1:\> Set-OrchSetting -Name 'Abp.Net.Mail.DefaultFromAddress' -Value 'noreply@example.com'
```

Sets the SMTP from-address setting on the current drive's tenant.

### Example 2: Update multiple settings via pipeline

```powershell
PS Orch1:\> @(
    [pscustomobject]@{ Name = 'Abp.Net.Mail.DefaultFromAddress';     Value = 'noreply@example.com' },
    [pscustomobject]@{ Name = 'Abp.Net.Mail.DefaultFromDisplayName'; Value = 'Orchestrator' }
) | Set-OrchSetting
```

All piped settings accumulate and ship in a single bulk update at the end of the pipeline.

### Example 3: Apply the same setting across multiple tenants

```powershell
PS C:\> Set-OrchSetting -Path Orch1, Orch2 -Name 'Abp.Net.Mail.DefaultFromAddress' -Value 'noreply@example.com'
```

Issues the same update against each named drive's tenant.

### Example 4: Preview without applying

```powershell
PS Orch1:\> Set-OrchSetting -Name 'Abp.Net.Mail.DefaultFromAddress' -Value 'noreply@example.com' -WhatIf
```

Shows what would be set without sending the API call.

## PARAMETERS

### -Name

Setting key to update. Supports tab completion against existing Setting names.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Value

New value for the setting. Tab completion offers the current value as a starting point.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the target drive name(s). If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### UiPath.PowerShell.Entities.Settings

Accepts Settings-shaped objects via property-name pipeline binding (Name + Value). All inputs are accumulated and sent as a single bulk update at the end of the pipeline.

## OUTPUTS

### System.Object

This cmdlet does not emit objects on success. Errors surface as ErrorRecord via the standard error stream.

## NOTES

Updates are batched per drive: regardless of how many settings flow through the pipeline, only one POST /odata/Settings/.../UpdateBulk call is made per drive at the end of the pipeline. The Settings cache on the drive is cleared after a successful update so subsequent Get-OrchSetting reflects the new value.

## RELATED LINKS

Get-OrchSetting
