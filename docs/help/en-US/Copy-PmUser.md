---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmUser
---

# Copy-PmUser

## SYNOPSIS

Copies platform management users from one UiPath Automation Cloud organization to another.

## SYNTAX

### __AllParameterSets

```
Copy-PmUser [-Path <string>] [-LiteralPath <string>] [-Email] <string[]> [-Destination] <string[]> [-Confirm]
 [-UserMappingCsv <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies users at the organization (platform management) level from a source organization to one or more destination organizations. The cmdlet replicates user properties including email, name, surname, displayName, bypassBasicAuthRestriction, and invitationAccepted.

Users are grouped by their source group membership, and the corresponding groups are automatically created or matched in the destination organization. If a group with the same name already exists in the destination, it is reused. Group matching is case-insensitive.

If a user with the same email already exists in the destination organization, the copy is skipped with an error. The cmdlet also prevents copying between drives that belong to the same organization (same partitionGlobalId).

The -UserMappingCsv parameter allows remapping usernames during the copy. When specified, source user emails are looked up in the mapping CSV and replaced with the mapped values in the destination. This is useful when migrating users between organizations with different identity providers.

The -Email parameter supports wildcards and tab completion. The -Destination and -Path parameters support tab completion for available drives.

Primary Endpoint: POST /api/User/BulkCreate (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Copy all users to another organization

```powershell
PS Orch1:\> Copy-PmUser * -Destination Orch2:
```

Copies all users from the Orch1 organization to the Orch2 organization, preserving group memberships.

### Example 2: Copy specific users across drives

```powershell
PS C:\> Copy-PmUser -Path Orch1: -Email ytsuda@gmail.com -Destination Orch2:
```

Copies the specified user from the Orch1 organization to the Orch2 organization.

### Example 3: Copy users matching a pattern

```powershell
PS Orch1:\> Copy-PmUser *@gmail.com -Destination Orch2:
```

Copies all users whose email ends with "@gmail.com" to the Orch2 organization.

### Example 4: Copy users with username remapping

```powershell
PS Orch1:\> Copy-PmUser * -Destination Orch2: -UserMappingCsv C:\temp\mapping.csv
```

Copies all users to the Orch2 organization, remapping usernames according to the mapping CSV file. The CSV maps source email addresses to destination usernames.

### Example 5: Preview a cross-organization copy

```powershell
PS Orch1:\> Copy-PmUser * -Destination Orch2: -WhatIf
```

Shows which users would be copied without performing the operation.

## PARAMETERS

### -Path

Specifies the source Pm: drive (organization) to copy users from. If not specified, the current drive is used. Tab completion suggests available drive names.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
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

### -Destination

Specifies the destination Pm: drive(s) (organizations) to copy users to. Multiple destinations can be specified. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Email

Specifies the email address(es) of the user(s) to copy. Supports wildcards for pattern matching. Tab completion dynamically suggests user email addresses from the source organization. This parameter has the alias "UserName".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- UserName
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

### -UserMappingCsv

Specifies the path to a user mapping CSV file for remapping usernames during the copy. The CSV maps source email addresses to destination usernames. This is useful when migrating between organizations with different identity providers. Only supported when a single destination drive is specified.

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
  ValueFromPipelineByPropertyName: false
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

You can pipe email addresses to this cmdlet via the Email property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUser

Returns the created PmUser objects in the destination organization, ordered by email.

## NOTES

The source and destination drives must belong to different organizations (different partitionGlobalId). If they belong to the same organization, the operation is skipped with a warning.

Users that already exist in the destination organization (matched by email, case-insensitive) are skipped with an error.

Group memberships are preserved during the copy. Groups are matched by name (case-insensitive) in the destination, and created automatically if they do not exist.

The -UserMappingCsv parameter is only supported when exactly one destination drive is specified.

## RELATED LINKS

[Get-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md)

[New-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmUser.md)

[Remove-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUser.md)

[Copy-PmGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmGroup.md)
