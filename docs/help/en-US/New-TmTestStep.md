---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestStep.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: New-TmTestStep
---

# New-TmTestStep

## SYNOPSIS

Adds a manual step to test cases in Test Manager.

## SYNTAX

### __AllParameterSets

```
New-TmTestStep [-Path <string[]>] [-LiteralPath <string[]>] [-TestCase] <string[]>
 [-Description] <string> [[-ExpectedResult] <string>] [[-ClipboardData] <string>]
 [-Confirm] [-OrderNo <int?>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds one step to each test case identified by the `-TestCase` parameter, which supports wildcard matching.
The fields are those of a manual step in the web app: Description, Expected behaviour (`-ExpectedResult`) and Clipboard data.
By default the step is added after the last step. `-OrderNo` inserts it at a position instead, like Insert Before / Insert After in the web app; the steps from that position on move down by one.
To add several steps, pipe objects with Description, ExpectedResult and ClipboardData properties, such as rows of a CSV file or the steps of another test case. They are added in order.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/teststeps

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: Add a step at the end

```powershell
PS Orch1Tm:\MTP> New-TmTestStep Login "Enter the user name and password" "The dashboard is shown"
```

The positional arguments follow the web form: Description, Expected behaviour, Clipboard data.

### Example 2: Insert a step at the top

```powershell
PS Orch1Tm:\MTP> New-TmTestStep Login "Open the login page" -OrderNo 0
```

### Example 3: Add steps from a CSV file

```powershell
PS Orch1Tm:\MTP> Import-Csv steps.csv | New-TmTestStep -TestCase Login
```

The CSV file has the columns Description, ExpectedResult and (optionally) ClipboardData.

### Example 4: Copy the steps of a test case to another one

```powershell
PS Orch1Tm:\MTP> Get-TmTestStep Login | New-TmTestStep -TestCase LoginCopy
```

The position of each source step is bound to `-OrderNo`, so the steps keep their order in a test case that has no steps yet.

## PARAMETERS

### -TestCase

Specifies the names of the test cases to add the step to.
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

### -Description

Specifies the description of the step. This parameter is mandatory, but it may be an empty string.

```yaml
Type: System.String
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

### -ExpectedResult

Specifies the expected behaviour of the step.

```yaml
Type: System.String
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

### -ClipboardData

Specifies the clipboard data of the step.

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

### -OrderNo

Specifies where the step goes, counting from 0. Negative positions count from the end: -1 adds the step after the last one, -2 inserts it before the last one. If omitted, or bound from an empty CSV cell, the step is added after the last one. A position out of range is reported as an error.

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

### System.Management.Automation.PSObject

You can pipe objects with TestCase, Description, ExpectedResult, ClipboardData and OrderNo properties to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestStep

The created steps.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf.

## RELATED LINKS

[Get-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestStep.md)

[Update-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestStep.md)

[Move-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-TmTestStep.md)

[Remove-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestStep.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)