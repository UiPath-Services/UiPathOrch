---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/22/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicense
---

# Get-PmLicense

## SYNOPSIS

Gets user license bundles (Named User licenses) assigned to a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Get-PmLicense [[-License] <string[]>] [[-Code] <string[]>] [-Path <string[]>] [-HasCapacity]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets user license bundle information from a UiPath Automation Cloud organization at the platform management level. Each returned row represents one user bundle SKU (for example Attended - Named User, Citizen Developer - Named User) with its allocation, in-use, and total counts.

This cmdlet covers the "User Licenses" tab of the Admin / Licenses page in Automation Cloud. For the "Robots & Services" tab, see `Get-PmLicenseAllocation` and `Get-PmLicenseInventory`. For the full contract view, see `Get-PmLicenseContract`.

The -License parameter matches user-friendly bundle names (for example "Attended - Named User") and supports wildcards and tab completion. The -Code parameter matches the internal SKU code (for example ATTUNU) and also supports wildcards and tab completion. Use -HasCapacity to filter to bundles that still have free seats (allocated < total).

Results are cached per organization. Multiple drives that belong to the same organization share a single API call.

Primary Endpoint: GET /api/license/accountant/UserLicense (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

This cmdlet works only on UiPath Automation Cloud deployments; standalone on-premises Orchestrator does not expose the Portal Management API.

## EXAMPLES

### Example 1: Get all user license bundles

```powershell
PS Orch1:\> Get-PmLicense
```

Gets all user license bundles in the current organization, sorted by bundle name, with a usage bar view showing allocated / in-use / total counts.

### Example 2: Filter by bundle name

```powershell
PS Orch1:\> Get-PmLicense 'Attended*'
```

Gets user license bundles whose bundle name starts with "Attended". Because -License is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Filter by SKU code

```powershell
PS Orch1:\> Get-PmLicense -Code ATTUNU,CTZDEVNU
```

Gets the Attended Named User and Citizen Developer Named User bundles by SKU code.

### Example 4: Show only bundles with free capacity

```powershell
PS Orch1:\> Get-PmLicense -HasCapacity
```

Gets only bundles where the allocated count is less than the total (at least one free seat remaining).

### Example 5: Query multiple organizations in parallel

```powershell
PS C:\> Get-OrchPSDrive |
            Group-Object { ([Uri]$_.Root).Segments[1].TrimEnd('/') } |
            ForEach-Object { Get-PmLicense -Path "$($_.Group[0].Name):" }
```

Because `Get-PmLicense` is organization-scoped, this snippet groups drives by the organization slug in their URL and issues exactly one API call per organization.

## PARAMETERS

### -Path

Specifies the target drives. If not specified, the current drive is targeted. Tab completion suggests available drive names (for example `Orch1:`).

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

### -Code

Specifies the internal SKU codes to filter by (for example `ATTUNU`, `CTZDEVNU`, `RPADEVNU`). Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests SKU codes in the target organization.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -HasCapacity

Restricts output to bundles where allocated < total (that is, bundles with at least one free seat).

```yaml
Type: System.Management.Automation.SwitchParameter
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

### -License

Specifies the user-friendly bundle names to filter by (for example `Attended - Named User`). Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bundle names in the target organization.

```yaml
Type: System.String[]
DefaultValue: ''
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

You can pipe bundle names to the -License parameter and SKU codes to the -Code parameter via property name.

## OUTPUTS

### UiPath.PowerShell.Entities.AvailableUserBundle

Returns one object per bundle with `code`, `name`, `allocated`, `inUse`, and `total` properties. The default view renders a usage bar.

## NOTES

This cmdlet is organization-scoped. Because the underlying Portal API returns the same data for every tenant that shares an organization, querying multiple drives within the same organization is redundant. Group by the organization slug in the drive URL to avoid duplicate API calls.

Use `Get-PmLicensedGroup` and `Get-PmLicensedUser` to enumerate the groups and users that consume these bundles.

## RELATED LINKS

Get-PmLicenseAllocation

Get-PmLicenseInventory

Get-PmLicenseContract

Get-PmLicensedGroup

Get-PmLicensedUser
