---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchApiTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: New-OrchApiTrigger
---

# New-OrchApiTrigger

## SYNOPSIS

Creates a new API trigger in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchApiTrigger [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-AlertPendingJobAfterSeconds <int>] [-AlertRunningJobAfterSeconds <int>]
 [-CallingMode <string>] [-Confirm] [-ConsecutiveJobFailuresThreshold <int>]
 [-Description <string>] [-Enabled <string>] [-InputArguments <string>]
 [-KillJobAfterSeconds <int>] [-MachineRobots <string[]>] [-Method <string>]
 -Release <string> [-RemoteControlAccess <string>] [-ResumeOnSameContext <string>]
 [-RunAsCaller <string>] [-RuntimeType <string>] [-Slug <string>]
 [-StopJobAfterSeconds <int>] [-StopStrategy <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new API trigger (HttpTrigger) bound to a release in the target folder. An API trigger exposes a callable HTTP endpoint that starts a job each time it is invoked. Each call returns immediately or waits for the run to complete depending on -CallingMode.

The cmdlet supplies defaults the server requires but the Swagger schema marks as optional: when omitted, -Slug defaults to the trigger Name, MachineRobots defaults to a single placeholder entry, and Tags defaults to an empty array. Calling the cmdlet without these still produces a POST body the server accepts.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which triggers would be created, or -Confirm to be prompted before each creation.

Primary Endpoint: POST /odata/HttpTriggers

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: HttpTriggers.Create

## EXAMPLES

### Example 1: Create a barebones API trigger

```powershell
PS Orch1:\Shared> New-OrchApiTrigger -Name MyTrigger -Release MyProcess
```

Creates an API trigger named `MyTrigger` bound to release `MyProcess` in the current folder. Method defaults to `Get`, CallingMode to `AsyncRequestReply`, Enabled to true, and Slug to the trigger Name.

### Example 2: Create a trigger with explicit fields

```powershell
PS Orch1:\Shared> New-OrchApiTrigger MyTrigger -Release MyProcess -Method Post -Slug my-endpoint -Description 'Webhook for orders' -RunAsCaller true
```

Creates a Post-method trigger with a custom slug, description, and RunAsCaller enabled. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Bind the trigger to a specific robot

```powershell
PS Orch1:\Shared> New-OrchApiTrigger MyTrigger -Release MyProcess -MachineRobots '[{"UserName":"domain\\user","MachineName":"prod-vm"}]'
```

Binds the trigger to a specific user-machine combination. -MachineRobots accepts a JSON array of MachineRobotSession objects (UserName, MachineName, SessionName); any can be omitted to let the server bind by elimination.

### Example 4: Bulk create from CSV

```powershell
PS C:\> Import-Csv api-triggers.csv | New-OrchApiTrigger
```

Pipes rows exported by `Get-OrchApiTrigger -ExportCsv` back into creation. Every column emitted on export maps to a parameter, so the same CSV round-trips into another tenant without rewriting.

## PARAMETERS

### -AlertPendingJobAfterSeconds

Raise a pending-job alert after this many seconds.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -AlertRunningJobAfterSeconds

Raise a running-job alert after this many seconds.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -CallingMode

How the caller waits for the job: LongPolling, FireAndForget (caller returns immediately), or AsyncRequestReply (caller waits for the job result).

```yaml
Type: System.String
DefaultValue: ''
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

### -ConsecutiveJobFailuresThreshold

Trigger consecutive-failure handling after this many failed runs in a row.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -Description

A free-form description stored on the entity.

```yaml
Type: System.String
DefaultValue: ''
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

### -Enabled

Whether the entity is enabled at creation. Accepts "true" or "false". The server defaults to true when omitted.

```yaml
Type: System.String
DefaultValue: ''
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

### -InputArguments

JSON object string supplying the job's input arguments.

```yaml
Type: System.String
DefaultValue: ''
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

### -KillJobAfterSeconds

Kill the job if it has not stopped within this many seconds.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -MachineRobots

JSON array of MachineRobotSession objects (UserName, MachineName, SessionName). Each entry binds the trigger to a specific robot, machine, or robot-machine combination. Re-importable from `Get-Orch*Trigger -ExportCsv`.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Method

HTTP method the trigger accepts. One of Get, Post, Put, Delete, Patch. Server default is Post when omitted.

```yaml
Type: System.String
DefaultValue: ''
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

### -Name

Specifies the Name(s) of the API trigger to create.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Path

Specifies the target folder(s). If not specified, the current folder is targeted. Supports wildcards.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Release

Name of the release (deployed process) to bind the trigger to. Supports wildcards. Tab-completable.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RemoteControlAccess

Remote control access level. One of None, ReadOnly, Full.

```yaml
Type: System.String
DefaultValue: ''
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

### -ResumeOnSameContext

Whether the job resumes on the same robot context after suspension. Accepts "true" or "false".

```yaml
Type: System.String
DefaultValue: ''
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

### -RunAsCaller

Whether the job runs under the API caller identity (true) or the trigger owner's identity (false).

```yaml
Type: System.String
DefaultValue: ''
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

### -RuntimeType

Runtime to use for the job. Defaults to "Unattended" when omitted.

```yaml
Type: System.String
DefaultValue: ''
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

### -Slug

URL slug suffix for the trigger endpoint. When omitted, defaults to the trigger Name.

```yaml
Type: System.String
DefaultValue: ''
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

### -StopJobAfterSeconds

Stop the job if it has not completed within this many seconds.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -StopStrategy

How to stop the job: SoftStop or Kill.

```yaml
Type: System.String
DefaultValue: ''
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

### System.String

You can pipe trigger names and rows exported by Get-OrchApiTrigger -ExportCsv to this cmdlet.

### System.Int32

You can pipe trigger names and rows exported by Get-OrchApiTrigger -ExportCsv to this cmdlet.

### System.String[]

You can pipe trigger names and rows exported by Get-OrchApiTrigger -ExportCsv to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.HttpTrigger

Returns the created HttpTrigger entity on success.

## NOTES

Main endpoint called: POST /odata/HttpTriggers

The cmdlet supplies Tags=[], MachineRobots=[{}] (placeholder), and Slug=Name when those are omitted. The server returns a generic 500 `An error has occurred.` if any of the three is absent from the POST body — this default-completion keeps barebones invocations working.

## RELATED LINKS

[Get-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchApiTrigger.md)

[Update-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchApiTrigger.md)

[Copy-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchApiTrigger.md)

[Enable-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchApiTrigger.md)

[Disable-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchApiTrigger.md)

[Remove-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchApiTrigger.md)

