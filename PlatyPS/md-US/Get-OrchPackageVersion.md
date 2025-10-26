---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPackageVersion

## SYNOPSIS
Retrieves package versions from UiPath Orchestrator.

## SYNTAX

```
Get-OrchPackageVersion [[-Id] <String[]>] [[-Version] <String[]>] [-Path <String[]>] [-Recurse]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchPackageVersion cmdlet retrieves package versions from UiPath Orchestrator. This cmdlet is unique as it operates on both tenant entities and folder entities, allowing access to packages from both folder feeds and tenant feeds. Packages represent automation processes, libraries, and other executable components with version control and deployment management.

Packages can be deployed to two types of feeds:
- Folder feeds: Packages deployed to specific folders with package feeds enabled
- Tenant feeds: Packages available at the tenant level that can be deployed to any folder

When executed from a folder with no feed, the cmdlet automatically retrieves packages from the tenant feed. When executed from a folder with a feed, it retrieves packages from that folder's feed. Use -Path to explicitly specify tenant feeds (Orch1:) or folder feeds (Orch1:\FolderName).

Package versions include comprehensive information such as Version number, Published date, IsActive status, Description, TargetFramework (Windows, Cross-platform), MainEntryPointPath, and project metadata. The cmdlet provides visibility into available automation packages and their deployment status across both folder and tenant levels.

Primary Endpoint: GET /odata/Packages

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Packages.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchPackageVersion
```

Retrieves all package versions from the current folder feed, displaying Id, Version, Published date, IsActive status, and Description.

### Example 2
```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1:\ -Recurse
```

Retrieves all package versions from the tenant feed and all folder feeds using -Recurse. This demonstrates accessing the tenant feed (packages available tenant-wide) as opposed to folder feeds (packages specific to individual folders). The command can be executed from any location by explicitly specifying the tenant path.

### Example 3
```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1:\ *Process*
```

Gets all package versions with IDs containing "Process" in the Orch1 tenant feed.

### Example 4
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.IsActive -eq $true}
```

Retrieves only active package versions.

### Example 5
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.ProjectType Library} | Select-Object Id, Version, Description
```

Displays library packages with their versions and descriptions.

## PARAMETERS

### -Id
Specifies the package IDs to be retrieved. Supports wildcard patterns for flexible package selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level operations.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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

### -Recurse
Specifies that the operation should include the target folder and all its subfolders.

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

### -Version
Specifies the Version of the process packages to be retrieved.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Package
## NOTES
This cmdlet is a tenant-level entity operation for accessing package version information. Packages represent automation processes and libraries with version control and deployment management. Use IsActive property to identify currently deployed versions. ProjectType distinguishes between executable processes and reusable libraries. TargetFramework indicates platform compatibility. This operation requires Packages.View permissions.



Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions
OAuth required scopes: OR.Execution or OR.Execution.Read
Required permissions: Packages.View

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)

[Add-OrchPackage](Add-OrchPackage.md)

[Set-OrchPackage](Set-OrchPackage.md)

[Remove-OrchPackageVersion](Remove-OrchPackageVersion.md)


