---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmExternalApplication

## SYNOPSIS
Copies external applications between organizations.

## SYNTAX

```
Copy-PmExternalApplication [[-Name] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-PmExternalApplication cmdlet copies external applications from source organizations to destination organizations within UiPath Process Mining. This cmdlet creates copies of external application configurations, including their authentication settings, permissions, and group associations, enabling external application management across multiple organization environments.

The cmdlet supports copying external applications to multiple destination organizations simultaneously. External applications can be identified by their Name parameter, and the cmdlet supports wildcard patterns for copying multiple applications efficiently.

If the groups to which the applications belong do not exist in the destination organization, they are automatically created during the copy operation, ensuring complete application configuration transfer.

Use the -Name parameter to specify which external applications to copy and the -Destination parameter to specify the target organizations. The -Path parameter enables working with multiple source organizations when not operating from within a specific organization context.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target organization drives where external applications should be copied.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmExternalApplication DataConnector Orch2:
```

Copies the DataConnector external application from the current organization (Orch1) to Orch2 organization.

### Example 2
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: APIIntegration Orch2:, Orch3:
```

Copies the APIIntegration external application from Orch1 to both Orch2 and Orch3 organizations.

### Example 3
```powershell
PS Orch1:\> Copy-PmExternalApplication ReportingApp, DashboardApp Orch2: -WhatIf
```

Shows what would happen when copying ReportingApp and DashboardApp from the current organization to Orch2.

### Example 4
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: *Integration* Orch2:
```

Copies all external applications with names containing Integration from Orch1 to Orch2 using wildcard patterns.

### Example 5
```powershell
PS Orch1:\> Get-PmExternalApplication *Analytics* | Copy-PmExternalApplication -Destination Orch2:, Orch3:
```

Gets all external applications with names containing Analytics and copies them to both Orch2 and Orch3 organizations using pipeline input.

### Example 6
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: Orch2: -Confirm
```

Copies all external applications from Orch1 to Orch2 with confirmation prompts (when -Name is not specified, all applications are copied).

## PARAMETERS

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

### -Destination
Specifies the destination organization drives where external applications should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the Name of the external applications to be copied. If not specified, all external applications will be copied.

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
Specifies the source organization drive. If not specified, the current organization will be used as the source.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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
External application names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.ExternalApplication
ExternalApplication objects from Get-PmExternalApplication can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClientCreated
Returns information about the created external application copies.

## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination organizations.

External applications contain authentication settings, permissions, and group associations. If associated groups do not exist in the destination organization, they are automatically created. When copying across environments, verify that the external application configurations are appropriate for the destination environment. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmExternalApplication](Get-PmExternalApplication.md)

[New-PmExternalApplication](New-PmExternalApplication.md)

[Remove-PmExternalApplication](Remove-PmExternalApplication.md)

[Set-PmExternalApplication](Set-PmExternalApplication.md)
