---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmUserPreference.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Set-PmUserPreference
---

# Set-PmUserPreference

## SYNOPSIS

Sets your own organization-level portal preferences (theme, language, ...).

## SYNTAX

### Default (Default)

```
Set-PmUserPreference [-Path <string[]>] [-LiteralPath <string[]>] [-Key] <string>
 [-Value] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Sets your own per-user portal preferences (the "Preferences" shown in the Orchestrator UI) as generic `-Key` / `-Value` pairs, via the identity Setting API.

The cmdlet always acts on the connected user's *own* preferences — there is no `-UserName` parameter — so the drive must be connected with a non-confidential application or a personal access token. A drive connected with a confidential application has no user and is reported with an error and skipped.

Each pipeline row is one key/value. Rows for the same drive are accumulated and sent as a single request, so an `Import-Csv` of several keys (e.g. `UserLanguage.Language` and `UserLanguage.Date`) is one call. The parameter names line up with the columns written by `Get-PmUserPreference -ExportCsv`, so the export round-trips back in.

`-Key` tab-completes the known portal keys; `-Value` adapts to the chosen `-Key` (for example `-Key UserLanguage.Language` lists `ja`, `en`, `de`, ...; `-Key UserTheme.Theme` lists `light`, `dark`, `dark-hc`) and shows a friendly label. The completion inserts the raw value, so the friendly labels never reach the command line or CSV. Any key/value is accepted, not only the suggested ones.

## EXAMPLES

### Example 1: Set the UI language

```powershell
PS Orch1:\> Set-PmUserPreference UserLanguage.Language ja
```

Sets your portal language to Japanese. `-Key` and `-Value` are positional.

### Example 2: Set the theme

```powershell
PS Orch1:\> Set-PmUserPreference UserTheme.Theme dark
```

### Example 3: Import preferences from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\prefs.csv | Set-PmUserPreference
```

Imports a CSV exported by `Get-PmUserPreference -ExportCsv` (columns `Path,Key,Value`). Multiple rows for the same drive are coalesced into one request.

## PARAMETERS

### -Key

The preference key, e.g. `UserLanguage.Language`, `UserTheme.Theme`, `UserAccessibility.Accessibility`. Tab completion lists the known portal keys; any key is accepted. Positional (position 0).

```yaml
Type: System.String
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

### -Value

The value for the key. Tab completion adapts to the bound `-Key` (e.g. language codes for `UserLanguage.Language`) and shows a friendly label, but inserts the raw value. Positional (position 1).

```yaml
Type: System.String
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

### -Path

The target Pm: drives (organizations). Defaults to the current drive.

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

You can pipe rows with Key and Value properties (e.g. from Import-Csv).

## OUTPUTS

### UiPath.PowerShell.Entities.PmUserPreference

Returns the preferences that were set, as objects with Path, Key and Value.

## NOTES

The cmdlets use the name "PmUserPreference" to match the Orchestrator UI; the underlying API is the generic identity Setting endpoint. They operate on the connected user's own preferences only.

This cmdlet writes the preference to the same identity Setting store, in the same way, that the Orchestrator web UI does. However, the web UI renders the *active* language/theme from a client-side cache that is updated only when you change the setting through the web UI itself. A value set with this cmdlet is therefore stored correctly and is applied on a fresh sign-in (or after clearing the browser's site data / using a new browser profile), but a browser session that is already signed in may keep showing the previous language/theme until then. In other words, this cmdlet is well suited to reading/auditing stored preferences and to provisioning or migrating them (which take effect on the next sign-in) — not to live-switching the UI of a browser session that is already open.

## RELATED LINKS

[Get-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserPreference.md)

[Copy-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUserPreference.md)
