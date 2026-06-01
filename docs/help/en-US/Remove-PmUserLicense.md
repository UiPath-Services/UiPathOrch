---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUserLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/28/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmUserLicense
---

# Remove-PmUserLicense

## SYNOPSIS

Removes one or more user bundle licenses from a Platform Management user.

## SYNTAX

### __AllParameterSets

```
Remove-PmUserLicense [-Path <string[]>] [-Email] <string[]>
 [-License] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Mirror of `Add-PmUserLicense` for the subtractive case: re-uses the same atomic-replace PUT endpoint, submitting the user's current `userBundleLicenses` MINUS the matched codes.

Unlike `Add`'s resolver (which matches the `-License` pattern against the full catalog), `Remove` resolves `-License` wildcards against the user's CURRENT bundles only:

- `-License *` strips exactly the user's currently-held set (one PUT with empty `licenseCodes`).
- A pattern that doesn't match any held bundle is a silent no-op.

Stripping every bundle (via `*`) leaves the user record present in `Get-PmUserLicense` with an empty `userBundleLicenses` list. To drop the record itself, use `Remove-PmLicensedUser`.

Primary Endpoint: PUT /portal_/api/license/accountant/UserLicense (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific license from a user

```powershell
PS Orch1:\> Remove-PmUserLicense alice@contoso.com "Citizen Developer - Named User"
```

Removes the "Citizen Developer - Named User" bundle from alice@contoso.com, keeping any other bundles.

### Example 2: Strip every license the user currently holds

```powershell
PS Orch1:\> Remove-PmUserLicense alice@contoso.com *
```

Removes every bundle alice currently holds. The user record stays in the licensed-users set with an empty bundle list (use `Remove-PmLicensedUser` to drop the record entirely).

### Example 3: Remove all Attended-family bundles

```powershell
PS Orch1:\> Remove-PmUserLicense alice@contoso.com Attended*
```

Removes only Attended* bundles from the user's current set. Other bundles (e.g. RPA Developer) are kept.

### Example 4: Preview without applying

```powershell
PS Orch1:\> Remove-PmUserLicense alice@contoso.com Citizen* -WhatIf
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

Specifies the user(s) to remove licenses from, by email or name. Tab completion is scoped to currently-licensed users (those with bundles to remove). This parameter has the alias "UserName".

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

Specifies the bundle(s) to remove. Supports wildcards. Tab completion shows ONLY the bundles the specified user currently holds (provide `-Email` first to populate the candidates).

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

Returns the refreshed licensed-user record after the PUT, with the remaining `userBundleLicenses` (raw codes) and `userBundleLicenseNames` (friendly names). Emitted only if the user is still present in the licensed-users set after the call (after a full strip via `-License *` the record stays, with empty bundle arrays).

## NOTES

`-License *` strips every bundle the user holds — but **does not remove the user from the licensed-users set**. The user remains as a "No license" row in the Portal UI / `Get-PmUserLicense` output. To drop the row entirely, follow up with `Remove-PmLicensedUser`.

If the target user is not in the licensed-users set, a non-terminating `UserNotLicensedError` is written (there's nothing to remove from a non-licensed user).

`useExternalLicense` is sent as `false` (matches Add's default and the Portal UI capture).

## RELATED LINKS

[Add-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmUserLicense.md)

[Remove-PmLicensedUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedUser.md)

[Get-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md)

[Remove-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroupLicense.md)
