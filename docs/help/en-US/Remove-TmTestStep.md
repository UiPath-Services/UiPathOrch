---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestStep.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: Remove-TmTestStep
---

# Remove-TmTestStep

## SYNOPSIS

Removes test steps from test cases in Test Manager.

## SYNTAX

### __AllParameterSets

```
Remove-TmTestStep [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-TestCase] <string[]> [-OrderNo] <int[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes the steps at the positions given by `-OrderNo` from the test cases identified by `-TestCase`, which supports wildcard matching.
A test step has no name: it is identified by its test case and its position, `orderNo`, which counts from 0. Negative positions count from the end: -1 is the last step.
Removing a step moves the steps after it up by one. The cmdlet therefore resolves every position, including those piped in, against the steps as they were before the first removal, and removes the steps only after all input has been read.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: DELETE /testmanager_/api/v2/{projectId}/teststeps/{testStepId}

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: Remove the last step

```powershell
PS Orch1Tm:\MTP> Remove-TmTestStep Login -1
```

### Example 2: Remove the steps whose description is empty

```powershell
PS Orch1Tm:\MTP> Get-TmTestStep Login | Where-Object description -eq "" | Remove-TmTestStep
```

## PARAMETERS

### -TestCase

Specifies the names of the test cases whose steps to work on.
Wildcard characters are permitted.
This parameter is mandatory.

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

### -OrderNo

Specifies the positions of the steps to remove. Positions count from 0; negative positions count from the end (-1 is the last step). A position out of range is reported as an error.
This parameter is mandatory; an empty CSV cell is rejected rather than read as 0.

```yaml
Type: System.Int32[]
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

Specifies the target project. If not specified, the current project is targeted.

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

Specifies the target project by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe projects directly.

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

Includes the target project and all its subfolders in the operation.

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

### UiPath.PowerShell.Entities.TmTestStep

You can pipe test steps from Get-TmTestStep to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which steps would be removed.

## RELATED LINKS

[Get-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestStep.md)

[New-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestStep.md)

[Update-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestStep.md)

[Move-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-TmTestStep.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)