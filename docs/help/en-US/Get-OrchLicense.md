---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLicense
---

# Get-OrchLicense

## SYNOPSIS

Gets the license information.

## SYNTAX

### __AllParameterSets

```
Get-OrchLicense [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchLicense` cmdlet retrieves the license information from UiPath Orchestrator. It returns a License object that contains details such as the expiration date, allowed and used license counts by type, subscription plan, licensed features, and whether the license is registered or expired.

The `-Path` parameter supports tab completion for drive names.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetLicense

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get the license information from the current drive

```powershell
PS Orch1:\> Get-OrchLicense
```

Gets the license information from the current Orchestrator drive.

### Example 2: Get the license information from a specific drive

```powershell
PS C:\> Get-OrchLicense Orch1:
```

Gets the license information from the drive named `Orch1`.

### Example 3: Get the license information from multiple drives

```powershell
PS C:\> Get-OrchLicense Orch1:, Orch2:
```

Gets the license information from the drives named `Orch1` and `Orch2`.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe drive names to the **Path** parameter.

## OUTPUTS

### UiPath.PowerShell.Entities.License

This cmdlet returns a License object containing license details such as expiration date, allowed and used license counts, subscription plan, and licensed features.

## NOTES

This cmdlet is tenant-scoped and returns license summary information including expiration date, allowed/used counts, subscription plan, and licensed features.


## RELATED LINKS

[Get-OrchLicenseNamedUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicenseNamedUser.md)

[Get-OrchLicenseRuntime](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicenseRuntime.md)

[Get-OrchLicenseStats](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicenseStats.md)
