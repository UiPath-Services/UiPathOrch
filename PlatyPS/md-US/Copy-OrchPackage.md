---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchPackage

## SYNOPSIS
Copies packages to destination folders.

## SYNTAX

```
Copy-OrchPackage [-Id] <String[]> [[-Version] <String[]>] [-Destination] <String[]> [-Path <String>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchPackage cmdlet copies packages from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of packages, including their automation workflows, dependencies, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Packages contain the actual automation workflows published from UiPath Studio and are essential for process deployment and execution.

Use the -Id parameter to specify which packages to copy by their unique identifiers and the -Destination parameter to specify the target folders. The -Version parameter allows you to specify particular versions of packages to copy. The -Path parameter allows you to specify the source folder when working with different folder structures.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying packages from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions(processId='{processId}'), GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{key}'), POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution

Required permissions: Packages.View, Packages.Create, FolderPackages.View, FolderPackages.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchPackage InvoiceProcessor * \Production
```

Copies the InvoiceProcessor package from the current folder (Development) to the Production folder within the same tenant, using * for all versions. Orch1:\Production should have its own feed, otherwise it tries to copy to the tenant feed and it may fail.

### Example 2
```powershell
PS C:\> Copy-OrchPackage -Path Orch1:\ EmailAutomation * Orch2:\
```

Copies the EmailAutomation package from Orch1:\ to Orch2:\, demonstrating inter-tenant package copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchPackage ReportGenerator 1.2.3 Orch1:\ -WhatIf
```

Shows what would happen when copying a specific version (1.2.3) of ReportGenerator package from the current folder feed to the tenant feed.

### Example 4
```powershell
PS C:\> Copy-OrchPackage -Path Orch1:\ *Automation* * Orch2:\
```

Copies all packages containing Automation in their ID from Orch1:\ to Orch2:\ using wildcards.

### Example 5
```powershell
PS Orch1:\> Copy-OrchPackage -Recurse *Daily* * Orch2:\ -WhatIf
```

Shows what would happen when copying all packages containing Daily from all subfolders recursively to Orch2:\.

### Example 6
```powershell
PS Orch1:\> Get-OrchPackage *Scheduled* | Copy-OrchPackage -Destination Orch2:\Production
```

Gets all packages containing Scheduled in their IDs and copies them to Orch2:\Production using pipeline input.

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
Specifies the destination folders where packages should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Id
Specifies the Id of the packages to be copied.

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
Specifies the source folder. If not specified, the current folder will be used as the source.

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

### -Version
Specifies the version(s) of the packages to be copied. Use * to copy all versions or when you want to omit this positional parameter to specify -Destination directly.

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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Recurse
Specifies that packages should be copied from all subfolders recursively, maintaining the folder structure in the destination.

```yaml
Type: SwitchParameter
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

### System.String[]
Package IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Package
Package objects from Get-OrchPackage can be piped to this cmdlet. The Id property will be automatically mapped to the -Id parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Packages are identified by their Id rather than Name. Both -Id and -Version are positional parameters. When omitting parameter names, use * for -Version if you want to specify -Destination directly (e.g., Copy-OrchPackage MyPackage * Orch1:\Production).

Packages can exist in tenant feeds (Orch1:\) or folder feeds (Orch1:\FolderName). Ensure the destination folder has its own feed configured, otherwise copying to folder will attempt to copy to the tenant feed. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Import-OrchPackage](Import-OrchPackage.md)

[Export-OrchPackage](Export-OrchPackage.md)
