---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachine

## SYNOPSIS
Gets machines from UiPath Orchestrator.

## SYNTAX

```
Get-OrchMachine [[-Name] <String[]>] [-Path <String[]>] [-ExpandRobotUser] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets machine information from UiPath Orchestrator tenants. Machines represent the compute resources where robots execute automation processes, including both physical machines and machine templates used for robot provisioning.

This cmdlet returns comprehensive machine information including machine types (Standard, Template), scopes (Default, PersonalWorkspace, Serverless, AgentService), license slot allocations, robot user assignments, and configuration details.

Machines are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name.

Primary Endpoint: GET /odata/Machines?$expand=UpdateInfo

OAuth required scopes: OR.Machines or OR.Machines.Read

Required permissions: Machines.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchMachine
```

Gets all machines from the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchMachine *Template*
```

Gets machines containing "Template" in their name.

### Example 3
```powershell
PS Orch1:\> Get-OrchMachine -Path Orch1:, Orch2:
```

Gets machines from multiple tenants.

### Example 4
```powershell
PS Orch1:\> Get-OrchMachine | Where-Object {$_.Type -eq "Standard"}
```

Gets standard (non-template) machines.

### Example 5
```powershell
PS Orch1:\> Get-OrchMachine | Where-Object {$_.UnattendedSlots -gt 0}
```

Gets machines with unattended robot slots allocated.

### Example 6
```powershell
PS Orch1:\> Get-OrchMachine -ExpandRobotUser
```

Gets machines with expanded robot user details.

### Example 7
```powershell
PS Orch1:\> Get-OrchMachine -ExportCsv C:\Reports\Machines.csv
```

Exports all machines to CSV with UTF-8 BOM encoding.

## PARAMETERS

### -ExpandRobotUser
Expands robot user details for each machine, showing which users are assigned to run robots on the machines.

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

### -Name
Specifies the names of machines to retrieve. Supports wildcards and multiple values.

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
Specifies target tenants by drive name. Use comma-separated values for multiple tenants. If not specified, targets the current tenant.

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

### -ProgressAction
Controls how progress information is displayed during cmdlet execution.

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

### -CsvEncoding
Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. The exported CSV can be used with Import-Csv and piped to New-OrchMachine for bulk operations.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedMachine
### UiPath.PowerShell.Entities.RobotUser
## NOTES
Machine entities are tenant-scoped and operate across the entire tenant.

Machine types include Standard (physical machines) and Template (machine templates for provisioning). Scopes include Default, PersonalWorkspace, Serverless, and AgentService.

Use -ExpandRobotUser when you need detailed information about which users are assigned to run robots on specific machines.

The -ExportCsv parameter creates import-ready CSV files with human-readable names instead of internal IDs.

## RELATED LINKS

[New-OrchMachine](New-OrchMachine.md)

[Update-OrchMachine](Update-OrchMachine.md)

[Remove-OrchMachine](Remove-OrchMachine.md)

[Copy-OrchMachine](Copy-OrchMachine.md)
