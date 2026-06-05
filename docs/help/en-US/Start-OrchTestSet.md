---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Start-OrchTestSet
---

# Start-OrchTestSet

## SYNOPSIS

Starts the execution of test sets in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Start-OrchTestSet [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Starts the execution of one or more test sets in UiPath Orchestrator. When a test set is started, all test cases within the set are queued for execution. The cmdlet returns the test set execution ID (Int64) for each started test set, which can be used with `Get-OrchTestSetExecution` to monitor progress.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which test sets would be started, or -Confirm to be prompted before each start operation.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual test set names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /api/TestSetExecutions/StartTestSetExecution

OAuth required scopes: OR.TestSetExecutions

Required permissions: TestSetExecutions.Create

## EXAMPLES

### Example 1: Start a test set by name

```powershell
PS Orch1:\Shared> Start-OrchTestSet SmokeTests
```

Starts the execution of the test set named "SmokeTests" in the current folder and returns the test set execution ID.

### Example 2: Start multiple test sets using a wildcard

```powershell
PS Orch1:\Shared> Start-OrchTestSet Regression*
```

Starts all test sets whose name matches "Regression*" in the current folder.

### Example 3: Start a test set and monitor the execution

```powershell
PS Orch1:\Shared> $execId = Start-OrchTestSet SmokeTests
PS Orch1:\Shared> Get-OrchTestSetExecution -Last Hour | Where-Object { $_.Id -eq $execId }
```

Starts the test set named "SmokeTests" and captures the execution ID. Then retrieves the test set execution to check its status.

### Example 4: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Start-OrchTestSet * -WhatIf
```

Shows which test sets would be started without actually starting them.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the names of the test sets to start. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests test set names from the target folders.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test set names to this cmdlet via the Name property.

## OUTPUTS

### System.Int64

Returns the test set execution ID for each started test set. This ID can be used with `Get-OrchTestSetExecution` to monitor execution progress or with `Stop-OrchTestSetExecution` to cancel the execution.

## NOTES

Test sets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Starting a test set creates a new test set execution. The execution runs asynchronously; the cmdlet returns immediately after queueing the execution.

## RELATED LINKS

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)

[Get-OrchTestSetExecution](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetExecution.md)

[Stop-OrchTestSetExecution](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchTestSetExecution.md)
