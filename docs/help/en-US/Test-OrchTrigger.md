---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Test-OrchTrigger
---

# Test-OrchTrigger

## SYNOPSIS

Validates a process schedule (trigger) against the server.

## SYNTAX

### __AllParameterSets

```
Test-OrchTrigger [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Submits an existing trigger to Orchestrator's pre-flight validator and returns the ValidationResult. The validator performs the same checks the runtime would do (robot/template/license/maintenance/runtime version) without firing the trigger, so you can preview whether a schedule would actually run.

The result includes IsValid, plus Errors (human-readable messages) and ErrorCodes (machine-readable like RobotNotFound, RobotConcurrencyLimit, TemplateNoLicense, TemplateMaintenanceMode, DynamicJobAccountCredentialsInvalid, etc.). Each output object also carries TriggerName and TriggerPSPath as PSNoteProperty so multi-trigger pipelines stay correlatable.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see triggers in the current folder.

Common idiom — health-check every trigger:

```powershell
Get-OrchTrigger -Recurse | Test-OrchTrigger | Where-Object IsValid -eq $false
```

Primary Endpoint: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.ValidateProcessSchedule

OAuth required scopes: OR.Jobs or OR.Jobs.Write

Required permissions: Schedules.Create

## EXAMPLES

### Example 1: Validate a single trigger

```powershell
PS Orch1:\Shared> Test-OrchTrigger 'Daily report'
```

Validates the trigger named "Daily report" and returns its ValidationResult.

### Example 2: Find all unhealthy triggers tenant-wide

```powershell
PS Orch1:\> Test-OrchTrigger -Recurse | Where-Object IsValid -eq $false |
              Format-Table TriggerPSPath, ErrorCodes
```

Validates every trigger in the tenant and tabulates the failing ones with their error codes.

### Example 3: Validate via pipeline from Get-OrchTrigger

```powershell
PS Orch1:\Shared> Get-OrchTrigger | Test-OrchTrigger
```

Pipes triggers from Get-OrchTrigger directly into the validator.

## PARAMETERS

### -Name

Specifies the names of the triggers to validate. Supports wildcards and multiple comma-separated values. Tab completion suggests trigger names from the target folder(s).

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

### -Path

Specifies the target folder path(s). If not specified, the current folder is targeted.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -Recurse

Recursively traverse subfolders under the specified -Path.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -Depth

When -Recurse is used, limits recursion depth.

```yaml
Type: System.UInt32
DefaultValue: ''
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe trigger names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ValidationResult

Returns ValidationResult objects with IsValid, Errors, and ErrorCodes properties. Each result is decorated with TriggerName and TriggerPSPath PSNoteProperty for pipeline correlation.

## NOTES

This cmdlet does not modify state or fire the trigger; it only invokes the validator. Safe to run on production triggers.

## RELATED LINKS

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[New-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md)

[Update-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTrigger.md)

[Enable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTrigger.md)

[Disable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTrigger.md)
