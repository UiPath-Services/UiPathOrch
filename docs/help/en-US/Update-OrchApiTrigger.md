---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchApiTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchApiTrigger
---

# Update-OrchApiTrigger

## SYNOPSIS

Updates one or more existing API triggers in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchApiTrigger [-Name] <string[]> [-NewName <string>] [-Release <string>]
 [-Description <string>] [-Enabled <string>] [-Method <string>] [-Slug <string>]
 [-CallingMode <string>] [-RunAsCaller <string>] [-RuntimeType <string>]
 [-ResumeOnSameContext <string>] [-StopStrategy <string>]
 [-StopJobAfterSeconds <int>] [-KillJobAfterSeconds <int>] [-AlertPendingJobAfterSeconds <int>]
 [-AlertRunningJobAfterSeconds <int>] [-RemoteControlAccess <string>]
 [-ConsecutiveJobFailuresThreshold <int>]
 [-InputArguments <string>] [-MachineRobots <string[]>] [-Path <string[]>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates fields on existing API triggers in the target folder. Triggers are matched by -Name (wildcards supported) — any number of triggers can be updated in one call.

Each non-null parameter overrides the corresponding field on the trigger; unspecified parameters leave their existing values intact. Update-OrchApiTrigger uses dirty-detection: if every passed-in value already matches the server-side state, the PUT is skipped.

Use -NewName to rename a trigger. The lookup is by -Name; -NewName is the post-update name.

The cmdlet pairs with `Get-OrchApiTrigger -ExportCsv` for CSV round-trip workflows: every CSV column name maps to a parameter on this cmdlet, so `Import-Csv exported.csv | Update-OrchApiTrigger` reapplies a snapshot.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: PUT /odata/HttpTriggers({id})

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: HttpTriggers.Edit

## EXAMPLES

### Example 1: Disable an API trigger

```powershell
PS Orch1:\Shared> Update-OrchApiTrigger MyTrigger -Enabled false
```

Disables the trigger named "MyTrigger" in the current folder.

### Example 2: Change the bound release

```powershell
PS Orch1:\Shared> Update-OrchApiTrigger MyTrigger -Release NewProcess
```

Rebinds "MyTrigger" to a different release. The cmdlet resolves the release name to a ReleaseKey before issuing the PUT.

### Example 3: Rename and edit description in one call

```powershell
PS Orch1:\Shared> Update-OrchApiTrigger OldName -NewName NewName -Description 'Renamed by deployment script'
```

### Example 4: Re-import a previously exported CSV

```powershell
PS C:\> Import-Csv api-triggers.csv | Update-OrchApiTrigger
```

Reapplies the state captured by `Get-OrchApiTrigger -ExportCsv`. Triggers absent from the CSV are left alone; only listed names are updated.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only.

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

Specifies the Name(s) of the API trigger to update. Wildcards are matched against the existing trigger list in the target folder.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -NewName

New name to rename the entity to. The lookup is still done by -Name; -NewName is the post-update name.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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
  IsRequired: false
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

Returns the updated HttpTrigger entity on success.

## NOTES

Main endpoint called: PUT /odata/HttpTriggers({id}) where id is the GUID HttpTrigger.Id.

The update body is built from the existing trigger (deep-copy) plus any parameter overrides. Server-derived fields (audit IDs, timestamps) are preserved unmodified.

## RELATED LINKS

[New-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchApiTrigger.md)

[Get-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchApiTrigger.md)

[Copy-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchApiTrigger.md)

[Enable-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchApiTrigger.md)

[Disable-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchApiTrigger.md)

[Remove-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchApiTrigger.md)

