---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUserPreference.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmUserPreference
---

# Copy-PmUserPreference

## SYNOPSIS

Migrates your own portal preferences to yourself in another organization.

## SYNTAX

### Default (Default)

```
Copy-PmUserPreference [-Path <string>] [-LiteralPath <string>] [-Destination] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads your own portal preferences (theme, language, ...) in the source organization and writes them to you in each destination organization. "You" is resolved per organization from that drive's connection — the identity user id differs across organizations — so every drive involved must be connected with a non-confidential application or a personal access token. A drive connected with a confidential application has no user and is reported with an error and skipped.

Copying within the same organization is a no-op.

Primary Endpoint: GET /api/Setting, PUT /api/Setting (Identity Server)

OAuth required scopes: PM.Setting or PM.Setting.Read (source); PM.Setting or PM.Setting.Write (destination)

## EXAMPLES

### Example 1: Migrate your preferences to another org

```powershell
PS C:\> Copy-PmUserPreference -Path Source: -Destination Dest:
```

Copies your preferences from the `Source:` organization to yourself in `Dest:`.

### Example 2: Migrate to multiple orgs

```powershell
PS C:\> Copy-PmUserPreference -Path Source: -Destination Dest1:,Dest2:
```

Copies your source preferences to yourself in both destinations.

## PARAMETERS

### -Destination

The destination Pm: drive(s) (organizations) to copy the preferences into. Positional (position 0).

```yaml
Type: System.String[]
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

### -Path

The source Pm: drive (organization). Defaults to the current drive.

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

You can pipe destination drive names to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUserPreference

Returns the preferences written to each destination, with Path, Key and Value.

## NOTES

The cmdlets use the name "PmUserPreference" to match the Orchestrator UI; the underlying API is the generic identity Setting endpoint. They operate on the connected user's own preferences only.

Copied preferences are stored exactly as the Orchestrator web UI writes them and take effect on the destination user's next fresh sign-in. The web UI renders the active language/theme from a client-side cache, so a destination browser session that is already signed in may keep showing the previous value until re-login or after clearing site data. See `Set-PmUserPreference` for details.

## RELATED LINKS

[Get-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserPreference.md)

[Set-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmUserPreference.md)
