---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetDetail.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestSetDetail
---

# Get-OrchTestSetDetail

## SYNOPSIS

Gets TestSets with their Packages and TestCases arrays populated (per-item GetForEdit).

## SYNTAX

### __AllParameterSets

```
Get-OrchTestSetDetail [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Outputs full-payload TestSet entities for every TestSet whose name matches `-Name` in the target folders. Each row is fetched via the per-item `GetForEdit` endpoint so the `Packages[]` and `TestCases[]` arrays come back populated.

`Get-OrchTestSet` (without `Detail`) uses the cheaper LIST endpoint, which returns `TestCaseCount` populated but the arrays empty — that's fine for inventory, but does not give a downstream cmdlet (`New-OrchTestSet` etc.) the data it needs to recreate the TestSet elsewhere.

`-Name` is mandatory because the detail path makes one extra API call per matched TestSet — accidental fan-out from a default "all TestSets" would be expensive on large folders. Wildcards (including `*`) still work; the user just has to type the selector explicitly.

Documented clone path:

```powershell
Get-OrchTestSetDetail SrcSet | New-OrchTestSet -Path DstFolder -Name DstSet
```

works because `New-OrchTestSet` accepts `Packages` and `TestCases` via `ValueFromPipelineByPropertyName`.

Primary Endpoint: GET /odata/TestSets({id})/UiPath.Server.Configuration.OData.GetForEdit()

OAuth required scopes: OR.TestSets or OR.TestSets.Read

Required permissions: TestSets.View

## EXAMPLES

### Example 1: Get a single TestSet with arrays populated

```powershell
PS Orch1:\Shared> Get-OrchTestSetDetail MyTestSet
```

Returns the TestSet with `Packages` and `TestCases` arrays filled in.

### Example 2: Clone a TestSet across folders

```powershell
PS Orch1:\Shared> Get-OrchTestSetDetail MyTestSet |
                    New-OrchTestSet -Path Orch1:\OtherFolder -Name MyTestSet-copy
```

Equivalent to `Copy-OrchTestSet` for the single-TestSet case. The pipeline carries the Packages and TestCases arrays into `New-OrchTestSet`, which posts them back through `POST /odata/TestSets` so the server stores the full payload.

### Example 3: Walk every TestSet under a folder tree

```powershell
PS Orch1:\> Get-OrchTestSetDetail * -Recurse
```

Wildcard required because `-Name` is mandatory; `*` here selects everything. One extra GET per TestSet — be mindful on large tenants.

## PARAMETERS

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the Name(s) of the TestSets to retrieve. Wildcards supported. Mandatory because the detail path fans out one API call per matched TestSet — accidental defaults would be expensive on large folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe a single TestSet name to this cmdlet.

### System.String[]

You can pipe TestSet names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSet

Returns full-payload TestSet entities with Packages and TestCases arrays populated.

## NOTES

Main endpoint called: GET /odata/TestSets({id})/UiPath.Server.Configuration.OData.GetForEdit()

One GET per matched TestSet, so wildcards that expand wide ("*") cost proportionally. `Get-OrchTestSet` (without `Detail`) is the cheap inventory query — use `Detail` only when you need the arrays.

## RELATED LINKS

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)

[New-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSet.md)

[Copy-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSet.md)

[Remove-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSet.md)

[Start-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchTestSet.md)

