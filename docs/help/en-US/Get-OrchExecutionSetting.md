---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchExecutionSetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchExecutionSetting
---

# Get-OrchExecutionSetting

## SYNOPSIS

Gets the execution settings configuration from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchExecutionSetting [[-Scope] <string[]>] [[-DisplayName] <string[]>] [-Path <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the execution settings configuration (display name, value type, etc.) from UiPath Orchestrator. Execution settings define resolution, timeout, and other robot execution parameters.

If scope is Global, the default values will be the initial ones. If scope is Robot, then the default values will be the actual values set globally. For example, if "Resolution width" was set globally to 720, then within the configuration returned by this cmdlet, the default value for this setting will be 0 if the scope is Global and 720 if the scope is Robot.

The available scopes are "Global" and "Robot".

The -Scope parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available scope names. Multiple values can be specified using comma-separated text that includes wildcards.

The -DisplayName parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available display names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve execution settings from multiple drives in parallel.

Execution settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration(scope={scope})

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.Edit or Robots.Create or Robots.Edit

## EXAMPLES

### Example 1: Get all execution settings for all scopes

```powershell
PS Orch1:\> Get-OrchExecutionSetting
```

Gets all execution settings for both Global and Robot scopes from the current Orchestrator tenant.

### Example 2: Get execution settings for the Global scope

```powershell
PS Orch1:\> Get-OrchExecutionSetting Global
```

Gets the execution settings configuration for the Global scope.

### Example 3: Get a specific execution setting by display name

```powershell
PS Orch1:\> Get-OrchExecutionSetting Robot Resolution*
```

Gets execution settings from the Robot scope whose display names match 'Resolution*'.

### Example 4: Get execution settings from multiple drives

```powershell
PS C:\> Get-OrchExecutionSetting -Path Orch1:\,Orch2:\
```

Gets all execution settings for all scopes from both Orch1 and Orch2 tenants.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

### -DisplayName

Specifies the display names of the execution settings to retrieve. Supports wildcards. Tab completion dynamically suggests display names from the target drives, filtered by the specified scope.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Scope

Specifies the scope of execution settings to retrieve. Valid values are "Global" and "Robot". Supports wildcards. Tab completion dynamically suggests available scopes.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

You can pipe scope or display name values to this cmdlet via the Scope or DisplayName properties.

## OUTPUTS

### UiPath.PowerShell.Entities.ExecutionSettingDefinition

Returns ExecutionSettingDefinition objects representing the execution settings configuration, including properties such as DisplayName, Key, ValueType, DefaultValue, and PossibleValues.

## NOTES

Execution settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve execution settings from multiple drives in parallel.

When no -Scope is specified, settings for both Global and Robot scopes are returned.

## RELATED LINKS

Get-OrchSetting

Get-OrchActivitySetting
