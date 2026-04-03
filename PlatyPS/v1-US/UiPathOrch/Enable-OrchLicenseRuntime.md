---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchLicenseRuntime
---

# Enable-OrchLicenseRuntime

## SYNOPSIS

Enables the runtime licenses.

## SYNTAX

### __AllParameterSets

```
Enable-OrchLicenseRuntime [-RobotType] <string[]> [-Key] <string[]> [-Path <string[]>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Enable-OrchLicenseRuntime` cmdlet enables runtime licenses in UiPath Orchestrator. It targets only runtime licenses that are currently disabled and toggles them to the enabled state.

Both the `-RobotType` and `-Key` parameters are mandatory. The `-RobotType` parameter supports tab completion for available robot type values. The `-Key` parameter supports tab completion, which suggests only the keys of currently disabled runtimes for the specified robot type. The tab completion tooltip shows the host machine name, service user name, and machine name for each key.

Both `-RobotType` and `-Key` support wildcard characters, allowing you to enable multiple runtime licenses at once.

This cmdlet supports `ShouldProcess`, so you can use the `-WhatIf` parameter to preview the changes and the `-Confirm` parameter to prompt for confirmation before each enable operation.

Primary Endpoint: POST /odata/LicensesRuntime('{machineName}')/UiPath.Server.Configuration.OData.ToggleEnabled

OAuth required scopes: OR.License

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Enable a specific runtime license

```powershell
PS Orch1:\> Enable-OrchLicenseRuntime Unattended m2
```

Enables the runtime license with the key `m2` for the `Unattended` robot type.

### Example 2: Enable all disabled runtime licenses for a robot type

```powershell
PS Orch1:\> Enable-OrchLicenseRuntime Unattended *
```

Enables all currently disabled runtime licenses for the `Unattended` robot type.

### Example 3: Preview enabling with -WhatIf

```powershell
PS Orch1:\> Enable-OrchLicenseRuntime Unattended m2 -WhatIf
```

Shows what would happen if the cmdlet enables the specified runtime license without actually performing the operation.

### Example 4: Enable runtime licenses from a specific drive

```powershell
PS C:\> Enable-OrchLicenseRuntime -Path Orch1: -RobotType Unattended -Key m2
```

Enables the specified runtime license from the drive named `Orch1`.

### Example 5: Enable runtime licenses matching a wildcard pattern

```powershell
PS Orch1:\> Enable-OrchLicenseRuntime Unattended new*
```

Enables all currently disabled runtime licenses for the `Unattended` robot type whose key matches the pattern `new*`.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -Key

Specifies the Key of the runtime licenses to be enabled. Wildcard characters are permitted. Tab completion suggests keys of currently disabled runtime licenses for the specified robot type.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -RobotType

Specifies the RobotType of the runtime licenses to be enabled. Wildcard characters are permitted. Tab completion suggests available robot type values.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### System.String[]

You can pipe robot type names to the **RobotType** parameter, key values to the **Key** parameter, or drive names to the **Path** parameter.

## OUTPUTS

### None

This cmdlet does not produce output. It enables runtime licenses as a side effect.

## NOTES

The cmdlet only targets runtime licenses that are currently disabled. Runtime licenses that are already enabled are skipped.

## RELATED LINKS

Disable-OrchLicenseRuntime

Get-OrchLicenseRuntime

Get-OrchLicense
