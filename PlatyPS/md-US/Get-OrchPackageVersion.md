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
The Get-OrchPackageVersion cmdlet retrieves package versions from UiPath Orchestrator. Packages represent automation processes, libraries, and other executable components with version control and deployment management. Each package version contains metadata about the automation project including version information, activity status, and execution requirements.

Package versions include comprehensive information such as Version number, Published date, IsActive status, Description, TargetFramework (Windows, Cross-platform), MainEntryPointPath, and project metadata. The cmdlet provides visibility into available automation packages and their deployment status.

This cmdlet operates as a tenant-level entity operation, retrieving package versions from the specified Orchestrator environment. Package information includes project type (Process, Library), target framework, compilation status, and execution requirements.

Primary Endpoint: GET /odata/Packages

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Packages.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchPackageVersion
```

Retrieves all package versions, displaying Id, Version, Published date, IsActive status, and Description.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchPackageVersion | ConvertTo-Json -Depth 3
```

Displays detailed package version properties in JSON format, including TargetFramework, ProjectType, MainEntryPointPath, and all metadata.

### Example 3
```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1: -Id "*Process*"
```

Gets all package versions with IDs containing "Process" in the Orch1 tenant.

### Example 4
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.IsActive -eq $true}
```

Retrieves only active package versions.

### Example 5
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.ProjectType -eq "Library"} | Select-Object Id, Version, Description
```

Displays library packages with their versions and descriptions.

### Example 6
```powershell
PS Orch1:\> Get-OrchPackageVersion | Group-Object ProjectType | Select-Object Name, Count
```

Groups package versions by project type and shows counts for each type.

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

### System.String[]
Package IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Package
Package objects can be piped to this cmdlet. The Id property will be automatically mapped to the -Id parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.PackageVersion
Returns PackageVersion objects containing comprehensive package information. Key properties include:
- Path: Current tenant context path
- Id, Title: Package identification and display name
- Version, Key: Version information and composite key
- IsActive, IsLatestVersion: Activation and version status
- Published: Publication timestamp
- Description, ReleaseNotes: Documentation and release information
- Authors: Package creator information
- ProjectType: Type classification (Process, Library)
- TargetFramework: Execution platform (Windows, Cross-platform)
- MainEntryPointPath: Primary execution file
- IsCompiled: Compilation status
- SupportsMultipleEntryPoints: Multi-entry point capability
- RequiresUserInteraction, IsAttended: Execution requirements
- Tags: Associated metadata tags

## NOTES
This cmdlet is a tenant-level entity operation for accessing package version information. Packages represent automation processes and libraries with version control and deployment management. Use IsActive property to identify currently deployed versions. ProjectType distinguishes between executable processes and reusable libraries. TargetFramework indicates platform compatibility. This operation requires Packages.View permissions.

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)

[Add-OrchPackage](Add-OrchPackage.md)

[Set-OrchPackage](Set-OrchPackage.md)

[Remove-OrchPackageVersion](Remove-OrchPackageVersion.md)
