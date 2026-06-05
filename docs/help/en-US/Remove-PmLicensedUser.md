---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/28/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmLicensedUser
---

# Remove-PmLicensedUser

## SYNOPSIS

Drops one or more users from the licensed-users set of a UiPath Platform Management organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmLicensedUser [-Path <string[]>] [-LiteralPath <string[]>] [[-Email] <string[]>] [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more users from the licensed-users set entirely (the "License Allocations to Users" table in the Portal UI), including the empty-bundle "No license" rows left behind by `Remove-PmUserLicense <user> *`.

This is the user-side counterpart of `Remove-PmLicensedGroup`. Unlike `Remove-PmUserLicense` (which strips bundles but leaves the row), this cmdlet drops the row itself.

The endpoint accepts a batched `userIds` array, so the cmdlet collects matching users across pipeline rows and `-Email` wildcards (OR-match on email/name, dedup by id) and submits ONE DELETE per drive in `EndProcessing` — N users = 1 round trip, not N. `ShouldProcess` is still invoked per user so `-WhatIf` and `-Confirm` prompt granularly; only approved users are included in the batch.

Omitting `-Email` matches every licensed user on the drive (parallels `Remove-PmLicensedGroup` without `-GroupName`).

Primary Endpoint: DELETE /portal_/api/license/accountant/UserLicense (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific user

```powershell
PS Orch1:\> Remove-PmLicensedUser alice@contoso.com
```

Removes alice@contoso.com from the licensed-users set.

### Example 2: Remove all "No license" rows left by prior bundle strips

```powershell
PS Orch1:\> Get-PmUserLicense | Where-Object { -not $_.userBundleLicenses } | Remove-PmLicensedUser
```

Drops every licensed-user row whose bundle list is empty (typical cleanup after `Remove-PmUserLicense <user> *`).

### Example 3: Preview removal of multiple users by wildcard

```powershell
PS Orch1:\> Remove-PmLicensedUser bob*@contoso.com -WhatIf
```

Shows which licensed users matching `bob*@contoso.com` would be removed, without sending the DELETE.

### Example 4: Bulk removal

```powershell
PS Orch1:\> Remove-PmLicensedUser alice@contoso.com,bob@contoso.com,carol@contoso.com
```

Removes three users in a single batched DELETE per drive.

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

### -Email

Specifies the user(s) to remove, by email or name. Supports wildcards. Tab completion is scoped to currently-licensed users; tooltips show each user's current bundle list (or "No license") so the consequence of the removal is visible before confirming. Omitting this parameter matches every licensed user on the drive. This parameter has the alias "UserName".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- UserName
ParameterSets:
- Name: (All)
  Position: 0
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

You can pipe objects with Email and Path properties (e.g. `NuLicensedUser` records from `Get-PmUserLicense`) to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output. The deletion is performed as a side effect; the licensed-users cache is cleared so the next `Get-PmUserLicense` reflects the new state.

## NOTES

The DELETE is batched per drive — `N` matched users on the same drive issue a single DELETE with all userIds, not N separate calls. `ShouldProcess` is still per-user so `-WhatIf` / `-Confirm` give per-user granularity; only approved users go into the batch.

Removing a user from the licensed-users set is independent of removing the underlying directory user. The user remains in the directory (still discoverable via `Search-OrchDirectory`); only the license allocation row is dropped.

The licensed-users and available-user-bundles caches are cleared after the DELETE so subsequent `Get-PmUserLicense` / `Get-PmLicense` reflect the new state.

## RELATED LINKS

[Remove-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUserLicense.md)

[Add-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmUserLicense.md)

[Get-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md)

[Remove-PmLicensedGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedGroup.md)
