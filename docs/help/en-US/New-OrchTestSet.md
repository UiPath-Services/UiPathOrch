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

The server rejects creation with errorCode 3204 (`Test Set is empty. It should have at least one package and one test case.`) unless both `-Packages` and `-TestCases` are supplied. Pass the typed `TestSetPackage[]` and `TestCase[]` arrays directly; live-verified end-to-end against a yotsuda tenant 2026-05-21.

Pipeline-from-Get does NOT carry arrays: `Get-OrchTestSet` uses the LIST endpoint which returns `TestCaseCount` populated but the `Packages` and `TestCases` arrays empty. To clone a TestSet between folders use `Copy-OrchTestSet` instead — it calls the per-item GetForEdit endpoint server-side and carries the full payload without round-tripping through the cmdlet pipeline.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/TestSets

OAuth required scopes: OR.TestSets or OR.TestSets.Write

Required permissions: TestSets.Create

## EXAMPLES

### Example 1: Create a TestSet with explicit Packages and TestCases

```powershell
PS Orch1:\Shared> $packages = ,([UiPath.PowerShell.Entities.TestSetPackage]@{
                       PackageIdentifier = 'MyTestProject'
                       VersionMask       = '1.0.0'
                   })
PS Orch1:\Shared> $cases = @(
                       [UiPath.PowerShell.Entities.TestCase]@{ DefinitionId = 743; Enabled = $true; VersionNumber = '1.0.0'; ReleaseId = 426481 }
                       [UiPath.PowerShell.Entities.TestCase]@{ DefinitionId = 744; Enabled = $true; VersionNumber = '1.0.0'; ReleaseId = 426481 }
                   )
PS Orch1:\Shared> New-OrchTestSet -Name MyTestSet -Packages $packages -TestCases $cases
```

Constructs the TestSet payload from the live test-case-definition IDs (discoverable via `Get-OrchTestCase`) and the deployed release ID.

### Example 2: Clone a TestSet into a different folder

```powershell
PS Orch1:\Shared> Copy-OrchTestSet -Name MyTestSet -Destination Orch1:\OtherFolder
```

Use `Copy-OrchTestSet`, not pipeline-from-Get, to duplicate a TestSet across folders — Copy carries the full payload server-side via GetForEdit. `Get-OrchTestSet | New-OrchTestSet` would lose the Packages and TestCases arrays because the LIST endpoint returns metadata only.

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

