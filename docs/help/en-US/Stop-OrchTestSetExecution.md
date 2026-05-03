---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchTestSetExecution.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Stop-OrchTestSetExecution
---

# Stop-OrchTestSetExecution

## SYNOPSIS

Stops running test set executions in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Stop-OrchTestSetExecution [-Id] <long[]> [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Cancels one or more running or pending test set executions in UiPath Orchestrator. The cmdlet sends a cancel request for each specified execution ID. Only test set executions in Pending or Running status can be stopped.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which test set executions would be stopped, or -Confirm to be prompted before each cancel operation.

The -Id parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available IDs from currently stoppable (Pending or Running) test set executions. The tooltip displays the execution name, description, start time, and status.

Primary Endpoint: POST /api/TestSetExecutions/CancelTestSetExecution

OAuth required scopes: OR.TestSetExecutions

Required permissions: TestSetExecutions.Edit

## EXAMPLES

### Example 1: Stop a specific test set execution

```powershell
PS Orch1:\Shared> Stop-OrchTestSetExecution 12345
```

Sends a cancel request for the test set execution with ID 12345.

### Example 2: Stop multiple test set executions

```powershell
PS Orch1:\Shared> Stop-OrchTestSetExecution 12345, 12346, 12347
```

Sends cancel requests for the test set executions with IDs 12345, 12346, and 12347.

### Example 3: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Stop-OrchTestSetExecution 12345 -WhatIf
```

```output
What if: Performing the operation "Stop TestSetExecution" on target "Shared\12345".
```

Shows what would happen without actually stopping the execution.

### Example 4: Stop a test set execution from a specific folder

```powershell
PS C:\> Stop-OrchTestSetExecution -Path Orch1:\Production -Id 12345
```

Cancels the test set execution with ID 12345 in the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Id

Specifies the test set execution ID or IDs to stop. Tab completion suggests IDs from Pending or Running test set executions.

```yaml
Type: System.Int64[]
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

### System.Int64[]

You can pipe test set execution IDs to this cmdlet via the Id property.

### System.String[]

You can pipe folder paths to the Path parameter.

## OUTPUTS

### None

This cmdlet does not generate output by default.

## NOTES

Only test set executions in Pending or Running status can be stopped. Executions in other states (Passed, Failed, Cancelled, Cancelling) are not affected.

## RELATED LINKS

Get-OrchTestSetExecution

Start-OrchTestSet

Get-OrchTestSet
