---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmRobotAccount.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Set-PmRobotAccount
---

# Set-PmRobotAccount

## SYNOPSIS

Creates or updates a robot account in a UiPath organization.

## SYNTAX

### ConsoleInput (Default)

```
Set-PmRobotAccount [-Path <string[]>] [-UserName] <string[]> [[-GroupName] <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

### CsvInput

```
Set-PmRobotAccount [-Path <string[]>] -UserName <string[]> [-GroupName <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates or updates robot accounts at the organization (platform management) level. If the specified robot account does not exist, it is created. If it already exists, its group memberships are updated to match the specified groups (a full replace). This is the standard create-or-update Set semantic; use [New-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmRobotAccount.md) instead for a strict create that errors on an existing account.

To add or remove only some of an existing account's group memberships (rather than replacing the whole set), prefer `Add-PmGroupMember` / `Remove-PmGroupMember` with `-Type DirectoryRobotUser`, which are additive/subtractive and merge multiple CSV rows for the same robot.

The cmdlet supports two parameter sets: ConsoleInput for interactive use and CsvInput for bulk operations via CSV import. `Get-PmRobotAccount -ExportCsv` writes a single comma-separated `GroupName` column, read back by `-GroupName`. (The fixed `GroupName0`..`GroupName9` columns written by older versions are still accepted on import but are **deprecated** — re-export to migrate; supplying them emits a one-time deprecation warning.)

The -UserName and -GroupName parameters support wildcards. The -Path parameter supports tab completion.

Primary Endpoint: POST /api/RobotAccount, PUT /api/RobotAccount/{robotId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Create a robot account

```powershell
PS Orch1:\> Set-PmRobotAccount MyRobot1
```

Creates a robot account named "MyRobot1" in the current organization with no group membership. Because -UserName is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Create a robot account with group membership

```powershell
PS Orch1:\> Set-PmRobotAccount MyRobot1 "Automation Developers"
```

Creates a robot account named "MyRobot1" and assigns it to the "Automation Developers" group. Because -GroupName is a positional parameter (position 1), the parameter name can be omitted.

### Example 3: Update group membership for an existing robot account

```powershell
PS Orch1:\> Set-PmRobotAccount MyRobot1 "Automation Developers","Automation Users"
```

Updates "MyRobot1" to be a member of exactly the "Automation Developers" and "Automation Users" groups (any other group membership is removed). If the account does not exist, it is created.

### Example 4: Bulk import robot accounts from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\robots.csv | Set-PmRobotAccount
```

Imports robot accounts from a CSV previously exported by `Get-PmRobotAccount -ExportCsv` (columns Path, UserName, GroupName). Each account is created if new or updated if it already exists. Use `New-PmRobotAccount` instead for a strict create that errors on accounts that already exist.

### Example 5: Preview robot account creation

```powershell
PS Orch1:\> Set-PmRobotAccount MyRobot1 "Automation Developers" -WhatIf
```

Shows what would happen without actually creating or updating the robot account.

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

Specifies the name(s) of the robot account(s) to create or update. Supports wildcards for pattern matching. In the ConsoleInput parameter set, this is a positional parameter at position 0.

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

Specifies the group name(s) to assign the robot account to (a full replace of its group set). Supports wildcards for pattern matching. In the ConsoleInput parameter set, this is a positional parameter at position 1. In the CsvInput parameter set, the value is bound from the pipeline by property name (the single comma-separated `GroupName` column written by `Get-PmRobotAccount -ExportCsv`).

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

Returns the created or updated PmRobotAccount object.

## NOTES

If a robot account with the specified name already exists, its group memberships are updated to match the specified groups (a full replace). If it does not exist, a new robot account is created. For a strict create that errors on an existing account, use New-PmRobotAccount; to change just some memberships of an existing account, use Add-PmGroupMember / Remove-PmGroupMember with -Type DirectoryRobotUser.

The CsvInput parameter set reads the single comma-separated GroupName column written by Get-PmRobotAccount -ExportCsv. The legacy GroupName0..GroupName9 columns from CSVs exported by older versions are still accepted but deprecated.

## RELATED LINKS

[New-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmRobotAccount.md)

[Get-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmRobotAccount.md)

[Copy-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmRobotAccount.md)

[Remove-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmRobotAccount.md)
