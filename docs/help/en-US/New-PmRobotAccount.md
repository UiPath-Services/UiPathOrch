---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmRobotAccount.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: New-PmRobotAccount
---

# New-PmRobotAccount

## SYNOPSIS

Creates a new robot account in a UiPath organization, erroring if one with the name already exists.

## SYNTAX

### ConsoleInput (Default)

```
New-PmRobotAccount [-Path <string[]>] [-UserName] <string[]> [[-GroupName] <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

### CsvInput

```
New-PmRobotAccount [-Path <string[]>] -UserName <string[]> [-GroupName <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a robot account at the organization (platform management) level. This is a strict create: if an account with the specified name already exists, an error is written and the existing account is left unchanged (the same semantics as `New-Item`).

For create-or-update (update the account if it exists, create it if it doesn't), use [Set-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmRobotAccount.md). To add or remove individual group memberships of an existing robot account, use `Add-PmGroupMember` / `Remove-PmGroupMember` with `-Type DirectoryRobotUser` — those are additive/subtractive and also merge multiple CSV rows for the same robot.

`New-PmRobotAccount` shares all parameters and the create path with `Set-PmRobotAccount`; only the already-exists case differs. The optional `-GroupName` sets the new account's initial group memberships (a new account starts empty, so this is unambiguous). `Get-PmRobotAccount -ExportCsv` writes a single comma-separated `GroupName` column that imports straight back through `-GroupName`.

The -UserName and -GroupName parameters support wildcards. The -Path parameter supports tab completion.

Primary Endpoint: POST /api/RobotAccount (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Create a robot account

```powershell
PS Orch1:\> New-PmRobotAccount MyRobot1
```

Creates a robot account named "MyRobot1" with no group membership. Because -UserName is positional (position 0), the parameter name can be omitted.

### Example 2: Create a robot account with initial group membership

```powershell
PS Orch1:\> New-PmRobotAccount MyRobot1 "Automation Developers"
```

Creates "MyRobot1" and assigns it to the "Automation Developers" group. Because -GroupName is positional (position 1), the parameter name can be omitted.

### Example 3: Erroring when the account already exists

```powershell
PS Orch1:\> New-PmRobotAccount MyRobot1
New-PmRobotAccount: "Orch1:\MyRobot1": PmRobotAccount 'MyRobot1' already exists. Use Set-PmRobotAccount to update it, or Add-/Remove-PmGroupMember to change its group memberships.
```

Because the account already exists, an error is written and the existing account is left unchanged. Use `Set-PmRobotAccount` to update it instead.

### Example 4: Bulk-create robot accounts from CSV

```powershell
PS C:\> Import-Csv C:\temp\robots.csv | New-PmRobotAccount
```

Creates each robot account from a CSV exported by `Get-PmRobotAccount -ExportCsv` (columns `Path`, `UserName`, `GroupName`). Rows whose account already exists are reported as errors and skipped; use `Set-PmRobotAccount` for an idempotent create-or-update import.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

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

### -UserName

Specifies the name(s) of the robot account(s) to create. Supports wildcards. In the ConsoleInput parameter set, this is a positional parameter at position 0.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: ConsoleInput
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: CsvInput
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GroupName

Specifies the group name(s) the new robot account is assigned to. Supports wildcards. In the ConsoleInput parameter set, this is a positional parameter at position 1. In the CsvInput parameter set, the value is bound from the pipeline by property name (the single comma-separated `GroupName` column written by `Get-PmRobotAccount -ExportCsv`).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: ConsoleInput
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: CsvInput
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe robot account data to this cmdlet via the UserName and GroupName properties (CsvInput parameter set).

## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount

Returns the created PmRobotAccount object.

## NOTES

New-PmRobotAccount is the strict-create counterpart of Set-PmRobotAccount: New errors if the account exists, Set creates-or-updates. Both replace the account's full group set; to change just some memberships of an existing account, prefer Add-PmGroupMember / Remove-PmGroupMember with -Type DirectoryRobotUser.

## RELATED LINKS

[Set-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmRobotAccount.md)

[Get-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmRobotAccount.md)

[Remove-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmRobotAccount.md)

[Add-PmGroupMember](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmGroupMember.md)
