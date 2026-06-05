---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmRobotAccount.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmRobotAccount
---

# Get-PmRobotAccount

## SYNOPSIS

Gets robot accounts from UiPath Automation Cloud organizations.

## SYNTAX

### Default (Default)

```
Get-PmRobotAccount [-Path <string[]>] [-LiteralPath <string[]>] [[-Name] <string[]>] [-ExpandGroup]
 [<CommonParameters>]
```

### ExportCsv

```
Get-PmRobotAccount [-Path <string[]>] [-LiteralPath <string[]>] [[-CsvEncoding] <Encoding>] [-ExportCsv <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets robot account information from UiPath Automation Cloud at the organization (platform management) level. Robot accounts are service identities used for unattended automation and API access.

This cmdlet retrieves robot accounts from the identity service API and returns PmRobotAccount objects. When the -ExpandGroup switch is specified, the cmdlet returns PmRobotAccountExpanded objects that include resolved group membership details.

When -ExportCsv is specified, the output is written to a CSV file instead of the pipeline. The CSV includes columns: Path, UserName, GroupName0 through GroupName9. This CSV can be used as input for Set-PmRobotAccount for bulk import.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/RobotAccount/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all robot accounts

```powershell
PS Orch1:\> Get-PmRobotAccount
```

Gets all robot accounts from the current organization.

### Example 2: Get a specific robot account by name

```powershell
PS Orch1:\> Get-PmRobotAccount MyRobot1
```

Gets the robot account with the specified name. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get robot accounts with expanded group membership

```powershell
PS Orch1:\> Get-PmRobotAccount -ExpandGroup
```

Gets all robot accounts with their group memberships expanded into human-readable group names.

### Example 4: Export robot accounts to CSV

```powershell
PS Orch1:\> Get-PmRobotAccount -ExportCsv C:\temp\robots.csv
```

Exports all robot accounts to a CSV file. The exported CSV can be used with Set-PmRobotAccount for bulk import to another organization.

### Example 5: Export robot accounts with Shift_JIS encoding

```powershell
PS Orch1:\> Get-PmRobotAccount -ExportCsv C:\temp\robots.csv -CsvEncoding shift_jis
```

Exports all robot accounts to a CSV file using Shift_JIS encoding for compatibility with Japanese editions of Excel.

### Example 6: Get robot accounts from a specific organization

```powershell
PS C:\> Get-PmRobotAccount -Path Orch1: MyRobot*
```

Gets robot accounts whose name starts with "MyRobot" from the Orch1 organization drive.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ExportCsv
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExpandGroup

Expands group membership information for each robot account. When specified, the cmdlet returns PmRobotAccountExpanded objects that include resolved group names instead of group IDs.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Default
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Exports robot accounts to the specified CSV file path. The CSV includes Path, UserName, and GroupName0 through GroupName9 columns. When this parameter is specified, no objects are written to the pipeline.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ExportCsv
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

Specifies the names of robot accounts to retrieve. Multiple values can be specified. Tab completion dynamically suggests robot account names from the target organizations.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Default
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

You can pipe robot account names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount

Returns PmRobotAccount objects when the Default parameter set is used without -ExpandGroup.

### UiPath.PowerShell.Entities.PmRobotAccountExpanded

Returns PmRobotAccountExpanded objects when the -ExpandGroup switch is specified, including resolved group membership details.

## NOTES

Robot accounts are organization-level service identities used for unattended automation. They are distinct from Orchestrator-level robot definitions.

The -ExportCsv parameter generates a CSV file that can be used as input for Set-PmRobotAccount to replicate robot accounts to another organization.

## RELATED LINKS

[Set-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmRobotAccount.md)

[Copy-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmRobotAccount.md)

[Remove-PmRobotAccount](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmRobotAccount.md)
