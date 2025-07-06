---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Find-OrchFolderNoUserAssigned

## SYNOPSIS
Finds folders that have no direct user assignments in UiPath Orchestrator.

## SYNTAX

```
Find-OrchFolderNoUserAssigned [[-Path] <String>] [-IncludeInherited] [<CommonParameters>]
```

## DESCRIPTION
The `Find-OrchFolderNoUserAssigned` cmdlet searches for folders within UiPath Orchestrator that have no users directly assigned to them. This is useful for identifying orphaned folders, conducting security audits, and ensuring proper folder access management across the organization.

The cmdlet analyzes folder structures and user assignments to identify folders that may be inaccessible to users or require attention from administrators. By default, it only considers direct user assignments, but can optionally include inherited permissions from parent folders.

This cmdlet operates on tenant entities and can be run from any location within an Orchestrator drive. It provides comprehensive folder information including folder type, feed type, and provision type to help administrators understand the nature of unassigned folders.

Primary Endpoint: GET /odata/Folders with custom filtering logic

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Folders.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned
```

Finds all folders in the current tenant that have no direct user assignments.

### Example 2
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned -IncludeInherited
```

Finds folders with no user assignments, including those that might inherit permissions from parent folders.

### Example 3
```powershell
PS C:\> Find-OrchFolderNoUserAssigned Orch1:
```

Finds folders with no user assignments in the Orch1 tenant, specifying the path explicitly.

### Example 4
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.FolderType -eq "Standard"} | Select-Object Path, DisplayName, FolderType, ProvisionType
```

Finds unassigned folders, filters for Standard folders only, and displays key properties. Note that Path is selected first to identify the location of each folder.

### Example 5
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Group-Object FolderType | Select-Object Name, Count
```

Groups unassigned folders by type and shows the count for each type.

### Example 6
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.ProvisionType -eq "Manual"} | Format-Table DisplayName, FolderType, Description
```

Finds manually provisioned folders with no user assignments and displays them in a formatted table.

### Example 7
```powershell
PS Orch1:\> $unassignedFolders = Find-OrchFolderNoUserAssigned
PS Orch1:\> $unassignedFolders | ConvertTo-Json -Depth 3
```

Gets all unassigned folders and displays the complete object structure for detailed analysis.

### Example 8
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.FeedType -eq "Processes"} | Select-Object Path, DisplayName, Description, Id
```

Finds process-type folders with no user assignments and displays their key identification information.

## PARAMETERS

### -Path
Specifies the tenant path to search. If not specified, the current tenant is used. Use tenant drive names (e.g., Orch1:, Orch2:).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Current tenant
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeInherited
When specified, includes folders that might inherit user permissions from parent folders in the analysis. By default, only direct user assignments are considered.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.Folder
## NOTES

**Important Administrative Use Cases:**
- **Security Audits**: Identify folders that may be inaccessible to users
- **Cleanup Operations**: Find orphaned folders that can be safely removed
- **Access Management**: Ensure all folders have appropriate user assignments
- **Compliance**: Verify that organizational access policies are properly implemented

**Folder Information Returned:**
- Path: Full path to the folder within the tenant
- DisplayName: Human-readable name of the folder
- Id: Unique identifier for the folder
- FolderType: Type of folder (Standard, Personal, Solution, etc.)
- FeedType: Feed type (Processes, FolderHierarchy, PersonalWorkspace, etc.)
- ProvisionType: How the folder was created (Manual, Automatic)
- PermissionModel: Permission model applied to the folder

**Common Folder Types:**
- Standard: Regular organizational folders
- Personal: User-specific personal workspace folders
- Solution: Folders associated with UiPath Solutions
- Department: Departmental folders

**Common Feed Types:**
- Processes: Folders designed for storing automation processes
- FolderHierarchy: Structural folders for organization
- PersonalWorkspace: Individual user workspace folders

**Interpretation Guidelines:**
- Personal workspace folders with no assignments may be normal (user-specific)
- Standard process folders with no assignments typically require attention
- Solution folders may have specialized permission models
- Automatically provisioned folders might be system-generated

**Best Practices:**
- Run this cmdlet periodically as part of access management reviews
- Investigate Standard folders with no assignments for potential security issues
- Consider using -IncludeInherited to understand the complete permission picture
- Document any intentionally unassigned folders for future reference

**Important Note about Path Selection:**
When using Select-Object with the results, always include Path as the first property to identify which tenant and location each folder belongs to. This is essential for managing folders across multiple tenants.

Use ConvertTo-Json to explore the complete folder object structure including detailed permission and configuration information.

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Add-OrchFolderUser](Add-OrchFolderUser.md)

[Get-OrchFolderUsage](Get-OrchFolderUsage.md)
