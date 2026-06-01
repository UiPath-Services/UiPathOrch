---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmUserLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/28/2026
PlatyPS schema version: 2024-05-01
title: Add-PmUserLicense
---

# Add-PmUserLicense

## SYNOPSIS

Adds one or more user bundle licenses to a Platform Management user.

## SYNTAX

### __AllParameterSets

```
Add-PmUserLicense [-Path <string[]>] [-Email] <string[]>
 [-License] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Allocates one or more user bundle licenses to a Platform Management user. The user analog of `Add-PmGroupLicense`: the cmdlet collects target bundle codes per (drive, user) across pipeline rows and `-License` wildcards, merges them with the user's existing `userBundleLicenses`, and submits one PUT per user in `EndProcessing` so multiple license additions for the same user fold into a single request.

The underlying endpoint is atomic-replace (it sets the user's full bundle list to the value submitted), so the cmdlet always includes the user's existing bundles in addition to the new codes. If nothing actually changes (every matched code is already held), the API call is skipped.

When the target user is not yet a licensed user, the same PUT covers add-user + bundle-assignment in a single round trip; the cmdlet resolves the directory identifier via `PmBulkResolveByName` before submitting.

The `-License` parameter supports tab completion against the bundle catalog (`AvailableUserBundlesItems`), matching by friendly name (e.g. "Attended - Named User") or raw code (e.g. "ATTUNU"). A pattern that doesn't match any catalog bundle is a silent no-op (matches the group cmdlet's contract).

Primary Endpoint: PUT /portal_/api/license/accountant/UserLicense (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Add a license to a user

```powershell
PS Orch1:\> Add-PmUserLicense alice@contoso.com "Citizen Developer - Named User"
```

Adds the "Citizen Developer - Named User" bundle to alice@contoso.com. Because `-Email` and `-License` are positional (positions 0 and 1), parameter names can be omitted.

### Example 2: Add multiple licenses by wildcard

```powershell
PS Orch1:\> Add-PmUserLicense alice@contoso.com Attended*
```

Adds every bundle whose friendly name matches `Attended*` (e.g. "Attended - Named User", "Attended - Multiuser").

### Example 3: Use raw bundle codes

```powershell
PS Orch1:\> Add-PmUserLicense alice@contoso.com ATTUNU,CTZDEVNU
```

Adds the bundles by their raw codes. Both code and friendly name match the same catalog.

### Example 4: Preview without applying

```powershell
PS Orch1:\> Add-PmUserLicense alice@contoso.com Citizen* -WhatIf
```

Shows what would happen without sending the PUT.

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

### -Email

Specifies the user(s) to allocate licenses to, by email (preferred) or name. Tab completion is rooted in `Search-OrchDirectory` and requires entering at least one character. This parameter has the alias "UserName".

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

### -License

Specifies the user bundle license(s) to add. Supports wildcards. Tab completion shows friendly bundle names from the static catalog (`AvailableUserBundlesItems`); raw codes are also accepted directly.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

You can pipe objects with Email, License, and Path properties to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedUser

Returns the refreshed licensed-user record after the PUT, with `userBundleLicenses` (raw codes) and `userBundleLicenseNames` (friendly names) populated. Emitted only if the user is still present in the licensed-users set after the call.

## NOTES

The PUT endpoint is atomic-replace, so the cmdlet always merges with the user's existing `userBundleLicenses` before submitting. If the merge yields no new codes, the API call is skipped (the cmdlet is idempotent).

If the target user is not in the licensed-users set, the cmdlet resolves the identifier via directory search (`PmBulkResolveByName`) and submits a PUT that both adds them to the licensed-users set and allocates the bundles in a single round trip.

When `-Email` matches a principal that is neither already licensed nor resolvable from the directory, a non-terminating `UserNotFoundError` is written.

`useExternalLicense` is sent as `false` (matches the Portal "Licenses > Users" UI's default).

## RELATED LINKS

[Remove-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUserLicense.md)

[Remove-PmLicensedUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedUser.md)

[Get-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md)

[Add-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmGroupLicense.md)
