---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchLicenseRuntime

## SYNOPSIS
Disables the runtime licenses.

## SYNTAX

```
Disable-OrchLicenseRuntime [-RobotType] <String[]> [-Key] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchLicenseRuntime cmdlet disables runtime licenses for specific robot types and machine keys. Runtime licenses control the ability for robots to execute automation processes.

This is a tenant entity cmdlet. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:). If not specified, the current tenant will be targeted.

Disabling runtime licenses will prevent robots from executing new jobs, but running jobs will continue until completion. This is useful for maintenance scenarios or license management.

Primary Endpoint: POST /odata/LicensesRuntime('{machineName}')/UiPath.Server.Configuration.OData.ToggleEnabled

OAuth required scopes: OR.License

Required permissions: Machines.Edit

## EXAMPLES

### Example 1
```powershell
Disable-OrchLicenseRuntime Unattended Machine01 -WhatIf
```

Shows what would happen when disabling the Unattended runtime license for Machine01.

### Example 2
```powershell
Disable-OrchLicenseRuntime Unattended Machine01
```

Disables the Unattended runtime license for Machine01 in the current tenant.

### Example 3
```powershell
Disable-OrchLicenseRuntime Studio, StudioX *DevMachine*
```

Disables Studio and StudioX runtime licenses for all machines whose keys contain "DevMachine".

### Example 4
```powershell
Disable-OrchLicenseRuntime -Path Orch1:, Orch2: Unattended TestMachine01, TestMachine02 -Confirm
```

Disables Unattended runtime licenses for TestMachine01 and TestMachine02 across multiple tenants with confirmation.

### Example 5
```powershell
Disable-OrchLicenseRuntime NonProduction *Test*
```

Disables NonProduction runtime licenses for all machines with keys containing "Test".

### Example 6
```powershell
Get-OrchLicenseRuntime -RobotType Unattended | Where-Object {$_.IsEnabled -eq $true} | Disable-OrchLicenseRuntime
```

Disables all currently enabled Unattended runtime licenses. License information is passed via pipeline using ByPropertyName binding.

## PARAMETERS

### -Key
Specifies the Key of the runtime licenses to be disabled. This typically corresponds to machine names or identifiers.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the name of the target tenants using drive names. If not specified, the current tenant will be targeted.

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

### -RobotType
Specifies the RobotType of the runtime licenses to be disabled. Common values include: Unattended, Attended, Studio, StudioX, NonProduction.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

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
Robot types and machine keys can be piped to this cmdlet.

### UiPath.PowerShell.Entities.LicenseRuntime
License runtime objects can be piped to this cmdlet. The RobotType and Key properties will be automatically mapped to the respective parameters via ByPropertyName binding.

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS
