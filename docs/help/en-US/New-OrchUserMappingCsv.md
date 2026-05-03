---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchUserMappingCsv.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchUserMappingCsv
---

# New-OrchUserMappingCsv

## SYNOPSIS

Generates a user mapping CSV file for cross-organization tenant migration.

## SYNTAX

### __AllParameterSets

```
New-OrchUserMappingCsv [-SourceTenant] <string> [-DestinationTenant] <string> [-ExportCsv] <string>
 [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Generates a user mapping CSV file that maps directory users from a source tenant to a destination tenant for cross-organization migration scenarios. This CSV is used by copy cmdlets (such as Copy-OrchAsset and Copy-PmUser) to correctly map user references when migrating between Orchestrator tenants belonging to different organizations.

The cmdlet performs the following steps:

1. Enumerates directory users from the source tenant by scanning PmGroup members, tenant users, and folder user assignments.
2. Resolves source user details (email, source/provider) from the source directory.
3. Searches the destination directory to find matching users, first by user name, then by email address.
4. Exports the results to a CSV file with columns: SourceUserName, SourceEmail, SourceDisplayName, SourceSource, DestinationUserName, Name, SurName, DisplayName.

If all destination users are automatically resolved, the CSV is ready to use. Otherwise, you must manually fill in the DestinationUserName column for unresolved entries and validate the file using Test-OrchUserMappingCsv.

The -SourceTenant and -DestinationTenant parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available drives.

Primary Endpoint: GET /odata/Users, GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder, GET /api/Group/{partitionGlobalId} (Identity Server), POST /api/Directory/BulkResolveByName/{partitionGlobalId} (Identity Server)

OAuth required scopes: OR.Users or OR.Users.Read, OR.Folders or OR.Folders.Read

Required permissions: Users.View, SubFolders.View

## EXAMPLES

### Example 1: Generate a user mapping CSV

```powershell
PS C:\> New-OrchUserMappingCsv Orch1: Orch2: C:\Migration\UserMapping.csv
```

Generates a user mapping CSV by scanning all directory users in the Orch1: source tenant and searching for matching users in the Orch2: destination tenant. The CSV is saved to C:\Migration\UserMapping.csv.

### Example 2: Generate with explicit parameters and UTF-8 encoding

```powershell
PS C:\> New-OrchUserMappingCsv -SourceTenant Orch1: -DestinationTenant Orch2: -ExportCsv UserMapping.csv -CsvEncoding utf8
```

Generates the user mapping CSV with UTF-8 encoding.

## PARAMETERS

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DestinationTenant

Specifies the destination tenant drive name. The destination directory is searched to find matching users for each source user.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Specifies the path for the output CSV file. If a directory path is specified, the default file name "UserMapping.csv" is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -SourceTenant

Specifies the source tenant drive name. All directory users from this tenant are enumerated by scanning PmGroup members, tenant users, and folder user assignments.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the source tenant or destination tenant drive name to this cmdlet via the SourceTenant or DestinationTenant properties.

## OUTPUTS

### System.String

Outputs status messages indicating whether the user mapping is complete or requires manual editing.

## NOTES

This cmdlet only enumerates directory users (Type = "DirectoryUser"). Local users are not included in the mapping because they can be recreated directly using New-PmUser.

If the source and destination tenants are the same drive, a warning is issued. If they belong to the same organization (same partition global ID), a warning indicates that user mapping is not needed.

The cmdlet shows progress as it enumerates PmGroup members, tenant users, and folder users.

## RELATED LINKS

Test-OrchUserMappingCsv

Search-OrchDirectory

Copy-OrchAsset
