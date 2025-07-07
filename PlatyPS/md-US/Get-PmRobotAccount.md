---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmRobotAccount

## SYNOPSIS
Gets robot accounts from Platform Management.

## SYNTAX

### Default (Default)
```
Get-PmRobotAccount [[-Name] <String[]>] [-Path <String[]>] [-ExpandGroup] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### ExportCsv
```
Get-PmRobotAccount [-Path <String[]>] [-ExportCsv <String>] [[-CsvEncoding] <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-PmRobotAccount cmdlet retrieves robot account information from UiPath Platform Management. This cmdlet operates at the organization level and manages robot service accounts across the organization.

This is an organization entity cmdlet that calls the Platform Management API. It operates at the organization level, where multiple tenants can belong to the same organization. Robot accounts are service accounts used for automation processes and robot authentication across all tenants within the organization.

Platform Management robot accounts provide centralized identity management for automation scenarios, enabling secure and consistent robot authentication across the entire organization. These accounts are shared across tenants within the same organization to improve performance and reduce memory usage.

Primary Endpoint: GET /api/robotaccounts

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Administration.View

## EXAMPLES

### Example 1
```powershell
Get-PmRobotAccount
```

Gets all robot accounts from the current organization.

### Example 2
```powershell
Get-PmRobotAccount ServiceAccount01
```

Gets the robot account named "ServiceAccount01".

### Example 3
```powershell
Get-PmRobotAccount *Automation*
```

Gets all robot accounts whose names contain "Automation".

### Example 4
```powershell
Get-PmRobotAccount -Path Orch1:, Orch2:
```

Gets robot accounts from the organization, accessed through multiple tenant drives.

### Example 5
```powershell
Get-PmRobotAccount | Where-Object {$_.IsActive -eq $true}
```

Gets all active robot accounts.

### Example 6
```powershell
Get-PmRobotAccount | Select-Object Name, EmailAddress, IsActive, CreationTime | Format-Table
```

Gets all robot accounts and displays their key properties in a table.

### Example 7
```powershell
Get-PmRobotAccount | Where-Object {$_.LastLoginDate -lt (Get-Date).AddDays(-30)} | Select-Object Name, LastLoginDate
```

Gets robot accounts that have not logged in within the last 30 days, useful for identifying unused accounts.

## PARAMETERS

### -CsvEncoding
{{ Fill CsvEncoding Description }}

```yaml
Type: Encoding
Parameter Sets: ExportCsv
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpandGroup
{{ Fill ExpandGroup Description }}

```yaml
Type: SwitchParameter
Parameter Sets: Default
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
Parameter Sets: ExportCsv
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the names of the robot accounts to be retrieved.

```yaml
Type: String[]
Parameter Sets: Default
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the name of the target tenant drives. The robot account data is organization-wide regardless of which tenant drive is used.

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
Robot account names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.PmRobotAccount
Robot account objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount

## NOTES

## RELATED LINKS
