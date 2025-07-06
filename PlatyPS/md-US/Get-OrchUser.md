---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUser

## SYNOPSIS
Gets users from UiPath Orchestrator.

## SYNTAX

```
Get-OrchUser [[-UserName] <String[]>] [[-FullName] <String[]>] [-Type <String[]>] [-ExpandDetails]
 [-Path <String[]>] [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Gets user information from UiPath Orchestrator tenants. This includes individual users, groups, and robot accounts that can authenticate and interact with the Orchestrator environment.

By default, this cmdlet uses the list API endpoint which provides basic user information efficiently. When the -ExpandDetails parameter is specified, the cmdlet calls individual user detail APIs to retrieve comprehensive information including detailed notification preferences, session permissions, and robot provisioning settings.

Users are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name.

Primary Endpoint: GET /odata/Users

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchUser
```

Gets all users from the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchUser *admin*
```

Gets users containing "admin" in their username.

### Example 3
```powershell
PS Orch1:\> Get-OrchUser -Type DirectoryUser
```

Gets only directory users (excluding groups and robot accounts).

### Example 4
```powershell
PS Orch1:\> Get-OrchUser -ExpandDetails | Where-Object {$_.IsActive -eq $false}
```

Gets inactive users using expanded details.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Path Orch1:, Orch2:
```

Gets users from multiple tenants.

### Example 6
```powershell
PS Orch1:\> Get-OrchUser -FullName "John*" -Type DirectoryUser
```

Gets directory users whose full name starts with "John".

### Example 7
```powershell
PS Orch1:\> Get-OrchUser -ExportCsv C:\Reports\Users.csv
```

Exports all users to CSV with UTF-8 BOM encoding.

## PARAMETERS

### -FullName
Specifies the full names to filter by. Supports wildcards and multiple values.

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

### -UserName
Specifies the usernames to retrieve. Supports wildcards and multiple values.

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

### -ExpandDetails
Retrieves additional detailed user information including login providers history, tenant details, and account IDs. Without this parameter, properties like LoginProviders array, TenancyName, and AccountId return empty values.

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
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. The exported CSV can be used with Import-Csv and piped to Add-OrchUser for bulk operations.

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

### -Type
Specifies the user types to filter by. Valid values include: DirectoryUser, DirectoryGroup, User, Robot.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES
User entities are tenant-scoped and operate across the entire tenant.

Use -ExpandDetails when you need access to additional user information such as login providers history, tenant details (TenancyName), and account IDs. Basic user information including roles, notification preferences, and robot provisioning is available without -ExpandDetails, but some properties like LoginProviders array and AccountId require expansion.

The -ExportCsv parameter creates import-ready CSV files with human-readable names instead of internal IDs.

User types include DirectoryUser (individual users), DirectoryGroup (user groups), User (local users), and Robot (robot accounts).

## RELATED LINKS

[Add-OrchUser](Add-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)

[Remove-OrchUser](Remove-OrchUser.md)

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)

