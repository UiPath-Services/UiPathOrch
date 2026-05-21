---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: New-OrchTestSet
---

# New-OrchTestSet

## SYNOPSIS

Creates a new TestSet in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchTestSet [-Name] <string[]> [-Description <string>] [-Enabled <string>] [-Path <string[]>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new TestSet in the target folder.

KNOWN LIMITATION: the wrapped POST /odata/TestSets endpoint rejects barebones calls with a 400 `Test Set is empty. It should have at least one package and one test case.` (errorCode 3204). Standalone creation needs Packages[] and TestCases[] which aren't yet exposed as parameters here — pipe a complete TestSet from Copy-OrchTestSet (which carries the full payload from the source folder) until standalone-creation parameter expansion lands.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/TestSets

OAuth required scopes: OR.TestSets or OR.TestSets.Write

Required permissions: TestSets.Create

## EXAMPLES

### Example 1: Duplicate a TestSet via pipeline

```powershell
PS Orch1:\Shared> Get-OrchTestSet MyTestSet | New-OrchTestSet -Path Orch1:\OtherFolder
```

Copies the TestSet definition (including its Packages and TestCases) into a different folder. The pipeline carries every required field; barebones creation does not work without those fields.

## PARAMETERS

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

### -Name

Specifies the Name(s) of the TestSet to create.

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

You can pipe a TestSet object (e.g. from Get-OrchTestSet) to this cmdlet.

### System.String[]

You can pipe a TestSet object (e.g. from Get-OrchTestSet) to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSet

Returns the created TestSet entity on success.

## NOTES

Main endpoint called: POST /odata/TestSets

The server requires both Packages[] and TestCases[] to be populated; barebones creation fails.

## RELATED LINKS

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)

[Copy-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSet.md)

[Remove-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSet.md)

[Start-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchTestSet.md)

[New-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSetSchedule.md)

