---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchLibrary

## SYNOPSIS
Copies libraries between tenants.

## SYNTAX

```
Copy-OrchLibrary [-Id] <String[]> [[-Version] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchLibrary cmdlet copies libraries from source tenants to destination tenants within UiPath Orchestrator. This cmdlet creates complete copies of libraries.

The cmdlet supports copying libraries to multiple destination tenants simultaneously. Libraries contain reusable automation components, custom activities, and shared code that can be used across multiple processes.

Use the -Id parameter to specify which libraries to copy by their unique identifiers and the -Destination parameter to specify the target tenants. The -Version parameter allows you to specify particular versions of libraries to copy. The -Path parameter allows you to specify the source tenant when working with multiple Orchestrator instances.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target tenant drives where libraries should be copied.

Primary Endpoint: GET /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='{packageId}'), GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{key}'), POST /odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution

Required permissions: Libraries.View, Libraries.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchLibrary UiPath.Excel.Activities * Orch2:
```

Copies the UiPath.Excel.Activities library from the current tenant (Orch1) to Orch2 tenant.

### Example 2
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: UiPath.Mail.Activities * Orch2:, Orch3:
```

Copies the UiPath.Mail.Activities library from Orch1 to both Orch2 and Orch3 tenants.

### Example 3
```powershell
PS Orch1:\> Copy-OrchLibrary UiPath.Excel.Activities 2.20.1 Orch2: -WhatIf
```

Shows what would happen when copying a specific version (2.20.1) of UiPath.Excel.Activities library from the current tenant to Orch2.

### Example 4
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: *Custom* * Orch2:
```

Copies all libraries containing Custom in their ID from Orch1 to Orch2 using wildcards.

### Example 5
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed *Excel* | Copy-OrchLibrary -Destination Orch2:, Orch3:
```

Gets all host feed libraries containing Excel in their IDs and copies them to both Orch2 and Orch3 tenants using pipeline input.

### Example 6
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: UiPath.WebAPI.Activities 1.18.0, 1.19.0 Orch2:
```

Copies specific versions (1.18.0 and 1.19.0) of UiPath.WebAPI.Activities library from Orch1 to Orch2.

## PARAMETERS

### -Destination
Specifies the destination tenant drives where libraries should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
Specifies the Id of the libraries to be copied.

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

### -Path
Specifies the source tenant drive. If not specified, the current tenant will be used as the source.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -Version
Specifies the version(s) of the libraries to be copied. Use * to copy all versions or when you want to omit this positional parameter to specify -Destination directly.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
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
Library IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Library
Library objects from Get-OrchLibrary can be piped to this cmdlet. The Id property will be automatically mapped to the -Id parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination tenants.

Libraries are identified by their Id (package name) rather than Name. Both -Id and -Version are positional parameters. When omitting parameter names, use * for -Version if you want to specify -Destination directly (e.g., Copy-OrchLibrary MyLibrary * Orch2:). Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchLibrary](Get-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

[Import-OrchLibrary](Import-OrchLibrary.md)
