---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserPreference.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Get-PmUserPreference
---

# Get-PmUserPreference

## SYNOPSIS

Reads your own organization-level portal preferences (theme, language, ...).

## SYNTAX

### Default (Default)

```
Get-PmUserPreference [-Path <string[]>] [-LiteralPath <string[]>] [[-Key] <string[]>]
 [-CsvEncoding <Encoding>] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads the per-user portal preferences (the "Preferences" shown in the Orchestrator UI) of the user connected to each drive, via the identity Setting API. Emits one object per setting with `Path`, `Key` and `Value`.

The cmdlet always acts on the connected user's *own* preferences — there is no `-UserName` parameter — so the drive must be connected with a non-confidential application or a personal access token. A drive connected with a confidential application authenticates as an application, not a user, and has no preferences; such a drive is reported with an error and skipped.

The column names line up with the `Set-PmUserPreference` parameters, so `Get-PmUserPreference -ExportCsv` round-trips through `Import-Csv | Set-PmUserPreference`. `Copy-PmUserPreference` migrates your preferences to another organization.

## EXAMPLES

### Example 1: Read your preferences

```powershell
PS Orch1:\> Get-PmUserPreference
```

Lists your portal preferences (e.g. `UserLanguage.Language` = `ja`, `UserTheme.Theme` = `dark`).

### Example 2: Read a single key

```powershell
PS Orch1:\> Get-PmUserPreference UserTheme.Theme
```

Returns just the theme. `-Key` tab-completes the known portal keys; you can pass several.

### Example 3: Export to CSV for round-tripping

```powershell
PS Orch1:\> Get-PmUserPreference -ExportCsv C:\temp\prefs.csv
```

Exports your preferences with the columns `Path,Key,Value`, which import straight back through `Import-Csv C:\temp\prefs.csv | Set-PmUserPreference`.

## PARAMETERS

### -Key

Optional preference key(s) to read, e.g. `UserLanguage.Language`, `UserTheme.Theme`. When omitted, the well-known portal preference keys are read. Tab completion lists the known keys; any key is accepted. Positional (position 0).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### -Path

The target Pm: drives (organizations). Defaults to the current drive. Tab completion suggests available drive names.

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

### -ExportCsv

Writes the results to the given CSV file (columns `Path,Key,Value`) instead of to the pipeline, in a form `Import-Csv | Set-PmUserPreference` accepts.

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

### -CsvEncoding

The text encoding for the `-ExportCsv` file (e.g. `utf8`, `utf8BOM`, `unicode`). Defaults to the module's standard CSV encoding.

```yaml
Type: System.Text.Encoding
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

### System.String[]

You can pipe preference keys to this cmdlet via the Key property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUserPreference

Returns one object per preference with Path, Key and Value.

## NOTES

The cmdlets use the name "PmUserPreference" to match the Orchestrator UI; the underlying API is the generic identity Setting endpoint, so any key/value is supported, not only the well-known theme/language keys. They operate on the connected user's own preferences only.

## RELATED LINKS

[Set-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmUserPreference.md)

[Copy-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUserPreference.md)
