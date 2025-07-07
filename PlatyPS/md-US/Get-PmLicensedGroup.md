---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmLicensedGroup

## SYNOPSIS
Gets licensed groups from Platform Management.

## SYNTAX

```
Get-PmLicensedGroup [[-GroupName] <String[]>] [[-UserName] <String[]>] [-ExpandAllocation] [-Path <String[]>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-PmLicensedGroup cmdlet retrieves licensed group information from UiPath Platform Management. This cmdlet operates at the organization level and manages license allocations for user groups across the organization.

This is an organization entity cmdlet that calls the Platform Management API. It operates at the organization level, where multiple tenants can belong to the same organization. Licensed groups define how licenses are distributed and allocated to different user groups within the organization.

Platform Management licensed groups provide centralized license management, enabling administrators to allocate, track, and manage licenses across all tenants within the organization. This ensures efficient license utilization and proper compliance management.

Primary Endpoint: GET /api/licensedgroups

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Administration.View

## EXAMPLES

### Example 1
```powershell
Get-PmLicensedGroup
```

Gets all licensed groups from the current organization.

### Example 2
```powershell
Get-PmLicensedGroup DeveloperGroup
```

Gets the licensed group named "DeveloperGroup".

### Example 3
```powershell
Get-PmLicensedGroup *Admin*
```

Gets all licensed groups whose names contain "Admin".

### Example 4
```powershell
Get-PmLicensedGroup -Path Orch1:, Orch2:
```

Gets licensed groups from the organization, accessed through multiple tenant drives.

### Example 5
```powershell
Get-PmLicensedGroup | Where-Object {$_.LicenseCount -gt 10}
```

Gets all licensed groups that have more than 10 licenses allocated.

### Example 6
```powershell
Get-PmLicensedGroup | Select-Object Name, LicenseType, LicenseCount, UsedLicenses | Format-Table
```

Gets all licensed groups and displays their license allocation information in a table.

### Example 7
```powershell
Get-PmLicensedGroup | Export-Csv "LicensedGroups.csv" -NoTypeInformation
```

Gets all licensed groups and exports the information to a CSV file for analysis.

## PARAMETERS

### -CsvEncoding
{{ Fill CsvEncoding Description }}

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpandAllocation
Displays the users within the group who have been allocated a license.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
{{ Fill ExportCsv Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupName
Specifies the group names to be retrieved.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the name of the target tenant drives. The licensed group data is organization-wide regardless of which tenant drive is used.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UserName
Specifies the user names to be retrieved. This is only effective when -ExpandAllocation is specified.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Licensed group names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.PmLicensedGroup
Licensed group objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.PmLicensedGroup

## NOTES

## RELATED LINKS
