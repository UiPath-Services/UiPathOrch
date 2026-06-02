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

Reads a user's organization-level portal preferences (theme, language, ...).

## SYNTAX

### Default (Default)

```
Get-PmUserPreference [-UserName] <string[]> [-Path <string[]>] [-ExportCsv <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads the per-user portal preferences (the "Preferences" shown in the Orchestrator UI) for the named user(s), via the identity Setting API. Emits one object per (user, setting) with `Path`, `UserName`, `Key` and `Value`.

The column names line up with the `Set-PmUserPreference` parameters, so `Get-PmUserPreference -ExportCsv` round-trips through `Import-Csv | Set-PmUserPreference`. `Copy-PmUserPreference` copies a user's preferences to another organization.

`-UserName` supports wildcards. Reading another user's preferences may require host-admin rights.

## EXAMPLES

### Example 1: Read a user's preferences

```powershell
PS Orch1:\> Get-PmUserPreference jsmith
```

Lists the portal preferences (e.g. `UserLanguage.Language` = `ja`, `UserTheme.Theme` = `dark`) for the user "jsmith".

### Example 2: Export to CSV for round-tripping

```powershell
PS Orch1:\> Get-PmUserPreference * -ExportCsv C:\temp\prefs.csv
```

Exports every user's preferences with the columns `Path,UserName,Key,Value`, which import straight back through `Import-Csv C:\temp\prefs.csv | Set-PmUserPreference`.

## PARAMETERS

### -UserName

The name(s) of the user(s) whose preferences to read. Supports wildcards. Positional (position 0).

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

### -ExportCsv

Writes the results to the given CSV file (columns `Path,UserName,Key,Value`) instead of to the pipeline, in a form `Import-Csv | Set-PmUserPreference` accepts.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe user names to this cmdlet via the UserName property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUserPreference

Returns one object per (user, preference) with Path, UserName, Key and Value.

## NOTES

The cmdlets use the name "PmUserPreference" to match the Orchestrator UI; the underlying API is the generic identity Setting endpoint, so any key/value is supported, not only the well-known theme/language keys.

## RELATED LINKS

[Set-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmUserPreference.md)

[Copy-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUserPreference.md)
