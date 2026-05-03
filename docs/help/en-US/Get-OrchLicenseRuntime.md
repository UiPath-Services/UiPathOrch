---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicenseRuntime.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLicenseRuntime
---

# Get-OrchLicenseRuntime

## SYNOPSIS

Gets runtime licenses.

## SYNTAX

### __AllParameterSets

```
Get-OrchLicenseRuntime [[-RobotType] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchLicenseRuntime` cmdlet retrieves runtime license information from UiPath Orchestrator. It returns LicenseRuntime objects containing details such as the machine key, machine name, host machine name, service user name, machine type, runtime count, robot count, executing count, online status, licensing status, and enabled status.

You can filter results by robot type using the `-RobotType` parameter. If `-RobotType` is not specified, results for all robot types are returned. The `-RobotType` parameter supports tab completion for available robot type values.

The cmdlet supports multi-threaded processing when querying multiple drives or robot types.

Primary Endpoint: /odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{robotType}')

OAuth required scopes: OR.License or OR.License.Read

Required permissions: License.View

## EXAMPLES

### Example 1: Get all runtime licenses

```powershell
PS Orch1:\> Get-OrchLicenseRuntime
```

Gets all runtime licenses for all robot types from the current drive.

### Example 2: Get runtime licenses for a specific robot type

```powershell
PS Orch1:\> Get-OrchLicenseRuntime Unattended
```

Gets runtime licenses for the `Unattended` robot type.

### Example 3: Get runtime licenses for multiple robot types

```powershell
PS Orch1:\> Get-OrchLicenseRuntime Unattended, NonProduction
```

Gets runtime licenses for the `Unattended` and `NonProduction` robot types.

### Example 4: Get runtime licenses from a specific drive

```powershell
PS C:\> Get-OrchLicenseRuntime -Path Orch1: -RobotType Unattended
```

Gets runtime licenses for the `Unattended` robot type from the drive named `Orch1`.

### Example 5: Filter enabled runtime licenses

```powershell
PS Orch1:\> Get-OrchLicenseRuntime Unattended | Where-Object { $_.Enabled -eq $true }
```

Gets runtime licenses for the `Unattended` robot type and filters to show only the enabled ones.

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

### UiPath.PowerShell.Entities.LicenseRuntime

This cmdlet returns LicenseRuntime objects containing details such as the machine key, machine name, host machine name, service user name, machine type, runtime count, online status, and enabled status.

## NOTES

This cmdlet is tenant-scoped and supports multi-threaded processing when querying multiple drives or robot types.


## RELATED LINKS

Get-OrchLicense

Get-OrchLicenseNamedUser

Enable-OrchLicenseRuntime

Disable-OrchLicenseRuntime

Get-OrchLicenseStats
