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

Copies a user's portal preferences to the same-named user in another organization.

## SYNTAX

### Default (Default)

```
Copy-PmUserPreference [-UserName] <string[]> [-Destination] <string[]> [-Path <string>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads the portal preferences (theme, language, ...) of the matching user(s) in the source organization and writes them to the user with the same name in each destination organization. The destination user must already exist (use `Copy-PmUser` first if needed).

`-UserName` supports wildcards. Copying within the same organization is a no-op.

## EXAMPLES

### Example 1: Copy one user's preferences to another org

```powershell
PS C:\> Copy-PmUserPreference jsmith -Path Source: -Destination Dest:
```

Copies "jsmith"'s preferences from the `Source:` organization to "jsmith" in `Dest:`.

### Example 2: Copy several users to multiple orgs

```powershell
PS C:\> Copy-PmUserPreference * -Path Source: -Destination Dest1:,Dest2:
```

Copies every source user's preferences to the same-named users in both destinations.

## PARAMETERS

### -UserName

The name(s) of the user(s) whose preferences to copy. Supports wildcards. Positional (position 0).

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

### -Destination

The destination Pm: drive(s) (organizations) to copy the preferences into. Positional (position 1).

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

You can pipe user names to this cmdlet via the UserName property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUserPreference

Returns the preferences written to each destination user.

## NOTES

The destination user must already exist in the destination organization. The cmdlets use the name "PmUserPreference" to match the Orchestrator UI; the underlying API is the generic identity Setting endpoint.

## RELATED LINKS

[Get-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserPreference.md)

[Set-PmUserPreference](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmUserPreference.md)
