---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLicenseNamedUser
---

# Get-OrchLicenseNamedUser

## SYNOPSIS

Gets named user licenses.

## SYNTAX

### __AllParameterSets

```
Get-OrchLicenseNamedUser [[-RobotType] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchLicenseNamedUser` cmdlet retrieves named user license information from UiPath Orchestrator. It returns LicenseNamedUser objects containing details such as the user name, last login date, machine count, licensing status, and associated machine names.

You can filter results by robot type using the `-RobotType` parameter. If `-RobotType` is not specified, results for all robot types are returned. The `-RobotType` parameter supports tab completion for available robot type values.

The cmdlet supports multi-threaded processing when querying multiple drives or robot types.

Primary Endpoint: /odata/LicensesNamedUser/UiPath.Server.Configuration.OData.GetLicensesNamedUser(robotType='{robotType}')

OAuth required scopes: OR.License or OR.License.Read

Required permissions: License.View

## EXAMPLES

### Example 1: Get all named user licenses

```powershell
PS Orch1:\> Get-OrchLicenseNamedUser
```

Gets all named user licenses for all robot types from the current drive.

### Example 2: Get named user licenses for a specific robot type

```powershell
PS Orch1:\> Get-OrchLicenseNamedUser Attended
```

Gets named user licenses for the `Attended` robot type.

### Example 3: Get named user licenses for multiple robot types

```powershell
PS Orch1:\> Get-OrchLicenseNamedUser Attended, Development
```

Gets named user licenses for the `Attended` and `Development` robot types.

### Example 4: Get named user licenses from a specific drive

```powershell
PS C:\> Get-OrchLicenseNamedUser -RobotType Unattended -Path Orch1:
```

Gets named user licenses for the `Unattended` robot type from the drive named `Orch1`.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
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

### -RobotType

Specifies the RobotType of the licenses to be retrieved. If not specified, results for all robot types are returned. Tab completion suggests available robot type values.

```yaml
Type: System.String[]
DefaultValue: None
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe robot type names to the **RobotType** parameter, or drive names to the **Path** parameter.

## OUTPUTS

### UiPath.PowerShell.Entities.LicenseNamedUser

This cmdlet returns LicenseNamedUser objects containing details such as the user name, last login date, machine count, licensing status, and associated machine names.

## NOTES

This cmdlet is tenant-scoped and supports multi-threaded processing when querying multiple drives or robot types.


## RELATED LINKS

Get-OrchLicense

Get-OrchLicenseRuntime

Get-OrchLicenseStats
