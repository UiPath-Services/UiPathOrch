---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Start-OrchJob
---

# Start-OrchJob

## SYNOPSIS

Starts jobs for specified processes in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Start-OrchJob [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [[-RuntimeType] <string>] [[-JobsCount] <int>] [[-InputArguments] <string>] [-Priority <string>]
 [-SpecificPriorityValue <int>] [-StopStrategy <string>] [-StopProcessExpression <string>]
 [-KillProcessExpression <string>] [-AlertPendingExpression <string>]
 [-AlertRunningExpression <string>] [-Wait] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Start-OrchJob` cmdlet starts jobs for one or more processes in UiPath Orchestrator. You specify processes by name using the `-Name` parameter, which supports wildcard characters. The cmdlet iterates through the target folders, matches processes by the specified wildcard pattern, and starts jobs for each matching process.

You can optionally specify the runtime type, the number of jobs to start, and input arguments for the process. The options of the web Start Job dialog that a job carries are available as well: the priority (`-Priority`), when to end the job (`-StopStrategy`, `-StopProcessExpression`, `-KillProcessExpression`) and when to raise an alert for it (`-AlertPendingExpression`, `-AlertRunningExpression`). These parameters carry the StartProcessDto property names, the same names `New-OrchTrigger` uses for a trigger. Tab completion is available for the `-Name` parameter (suggesting process names), the `-RuntimeType` parameter (suggesting available runtimes with availability information), and the `-InputArguments` parameter (suggesting a JSON template based on the process input argument definitions).

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.StartJobs

OAuth required scopes: OR.Jobs or OR.Jobs.Write

Required permissions: Jobs.Create

## EXAMPLES

### Example 1: Start a job for a specific process

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19
```

Starts a job for the process named `BlankProcess19` in the current folder.

### Example 2: Start a job with a specific runtime type

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 -RuntimeType Unattended
```

Starts an Unattended job for the process named `BlankProcess19`.

### Example 3: Start multiple jobs

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 -RuntimeType Unattended -JobsCount 3
```

Starts 3 Unattended jobs for the process named `BlankProcess19`.

### Example 4: Start a job with input arguments

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 -InputArguments '{"FilePath":"C:\\Invoices","BatchSize":10}'
```

Starts a job for the process named `BlankProcess19` and passes the specified JSON input arguments.

### Example 5: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Start-OrchJob Report* -WhatIf
```

Shows what jobs would be started for all processes matching `Report*` without actually starting them.

### Example 6: Start jobs from a specific folder

```powershell
PS C:\> Start-OrchJob -Path Orch1:\Shared BlankProcess19
```

Starts a job for the process named `BlankProcess19` in the Shared folder.

### Example 7: Start a job that is stopped after one hour

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 -StopStrategy SoftStop -StopProcessExpression 3600 -KillProcessExpression 600
```

Starts a job that Orchestrator asks to stop one hour (3600 seconds) after it is created, and kills if it is still running 10 minutes (600 seconds) after that.

### Example 8: Start a job with a high priority

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 -Priority High
```

Starts a job with the priority High (SpecificPriorityValue 65) instead of the process default.

### Example 9: Start jobs and wait until they end

```powershell
PS Orch1:\Shared> Start-OrchJob Report* -Wait
```

Starts a job for every process whose name begins with Report, waits until all of them have ended, and returns each job, with its final State, as it ends. The same as `Start-OrchJob Report* | Wait-OrchJob`.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -AlertPendingExpression

Specifies, in seconds, how long a job may stay Pending or Resumed before Orchestrator raises an alert, the "Generate an alert if the job is stuck in Pending or Resumed status" option of the web Start Job dialog. Not sent to Orchestrator below API version 16; the cmdlet warns when it drops the value.

```yaml
Type: System.String
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

### -AlertRunningExpression

Specifies, in seconds, how long a job may run before Orchestrator raises an alert, the "Generate an alert if the job has started and has not completed" option of the web Start Job dialog. The job returned by Orchestrator carries the value as MaxExpectedRunningTimeSeconds. Not sent to Orchestrator below API version 16; the cmdlet warns when it drops the value.

```yaml
Type: System.String
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

### -InputArguments

Specifies the input arguments to pass to the process as a JSON string. Tab completion suggests a JSON template based on the process input argument definitions.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -JobsCount

Specifies the number of jobs to start for each matching process.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -KillProcessExpression

Specifies, in seconds, the grace period after a soft stop. A job that has not stopped when it ends is killed. Use it with `-StopStrategy SoftStop` and `-StopProcessExpression`.

```yaml
Type: System.String
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

### -Name

Specifies the name of the processes to start. Wildcard characters are permitted. Tab completion suggests process names from Orchestrator.

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

### -Priority

Specifies the job priority, overriding the process default. Valid values are Critical (95), Highest (85), VeryHigh (75), High (65), MediumHigh (55), Medium (45), MediumLow (35), Low (25), VeryLow (15) and Lowest (5); the number is the SpecificPriorityValue sent. Below API version 14 the value is sent as the Low / Normal / High JobPriority it falls in. When both are given, -Priority wins over -SpecificPriorityValue.

```yaml
Type: System.String
DefaultValue: None
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

### -RuntimeType

Specifies the runtime type for the job. Tab completion suggests available runtime types with availability information. Valid values are: NonProduction, Attended, Unattended, Development, Studio, RpaDeveloper, StudioX, CitizenDeveloper, Headless, RpaDeveloperPro, StudioPro, TestAutomation, AutomationCloud, Serverless, AutomationKit, ServerlessTestAutomation, AutomationCloudTestAutomation, AttendedStudioWeb.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -SpecificPriorityValue

Specifies the job priority as a number from 1 to 100. Hidden from tab completion; it is meant for values bound by property name from piped objects. Use -Priority on the command line.

```yaml
Type: System.Nullable`1[System.Int32]
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
DontShow: true
AcceptedValues: []
HelpMessage: ''
```

### -StopProcessExpression

Specifies, in seconds, how long after it is created the job is ended, the "Schedule ending of job execution" option of the web Start Job dialog. A job still Pending at that time is ended as well, with the Info "Job stopped due to trigger timeout". How it is ended is set by -StopStrategy; without it, Orchestrator kills the job.

```yaml
Type: System.String
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

### -StopStrategy

Specifies how a job is ended when -StopProcessExpression elapses: SoftStop asks the job to stop, Kill ends it at once. When omitted, Orchestrator uses Kill.

```yaml
Type: System.String
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
AcceptedValues:
- SoftStop
- Kill
HelpMessage: ''
```

### -Wait

Waits until every job the cmdlet started has ended (Successful, Faulted, Stopped or Suspended) and returns each job, read again, as it ends, instead of the jobs as created. All the jobs are started first and then waited for together, so -Wait does not run the processes one after another. The output is the same as piping to `Wait-OrchJob`; for a time limit, pipe to `Wait-OrchJob -Timeout` instead.

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

You can pipe process names to the **Name** parameter.

### System.String

You can pipe a folder path to the **Path** parameter, a runtime type to the **RuntimeType** parameter, or a JSON string to the **InputArguments** parameter.

### System.Int32

You can pipe a job count value to the **JobsCount** parameter.

## OUTPUTS

### UiPath.PowerShell.Entities.Job

This cmdlet returns the job objects created by Orchestrator.

## NOTES

The cmdlet iterates through the target folders, matches processes by the specified wildcard pattern, and calls the StartJobs API for each matching process.

## RELATED LINKS

[Get-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJob.md)

[Stop-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchJob.md)

[Wait-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Wait-OrchJob.md)

[Open-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Open-OrchJob.md)
