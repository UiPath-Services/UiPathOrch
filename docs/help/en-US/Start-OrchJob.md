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
Start-OrchJob [-Name] <string[]> [[-RuntimeType] <string>] [[-JobsCount] <int>]
 [[-InputArguments] <string>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Start-OrchJob` cmdlet starts jobs for one or more processes in UiPath Orchestrator. You specify processes by name using the `-Name` parameter, which supports wildcard characters. The cmdlet iterates through the target folders, matches processes by the specified wildcard pattern, and starts jobs for each matching process.

You can optionally specify the runtime type, the number of jobs to start, and input arguments for the process. Tab completion is available for the `-Name` parameter (suggesting process names), the `-RuntimeType` parameter (suggesting available runtimes with availability information), and the `-InputArguments` parameter (suggesting a JSON template based on the process input argument definitions).

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.StartJobs

OAuth required scopes: OR.Jobs

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

Get-OrchJob

Stop-OrchJob

Open-OrchJob
