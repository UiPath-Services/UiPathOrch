---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Search-OrchDirectory.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Search-OrchDirectory
---

# Search-OrchDirectory

## SYNOPSIS

Searches for users in the UiPath Orchestrator directory.

## SYNTAX

### __AllParameterSets

```
Search-OrchDirectory [-Name] <string> [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Searches for users in the UiPath Orchestrator directory service by name. The directory search looks up users from the configured identity provider (such as Active Directory or Azure AD) and returns DirectoryObject results containing identity names, email addresses, and other user details.

This cmdlet is useful for verifying that a user exists in the directory before assigning them to folders or other Orchestrator resources.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] after typing at least one character to see matching directory users. If no users match the search term, a message indicating no results is shown.

Primary Endpoint: GET /odata/DirectoryService/UiPath.Server.Configuration.OData.SearchDirectory

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1: Search for a user by name

```powershell
PS Orch1:\> Search-OrchDirectory ytsuda
```

Searches the directory for users matching "ytsuda".

### Example 2: Search for a user on a specific drive

```powershell
PS C:\> Search-OrchDirectory -Path Orch1: ytsuda@gmail.com
```

Searches the directory on the Orch1: drive for the specified user.

### Example 3: Search across multiple drives

```powershell
PS C:\> Search-OrchDirectory -Path Orch1:,Orch2: admin
```

Searches the directory on both Orch1: and Orch2: drives. Queries are executed in parallel.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the search term for finding users in the directory. This parameter is mandatory.

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

You can pipe the search name to this cmdlet via the Name property.

### System.String[]

You can pipe drive names to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.DirectoryObject

Returns DirectoryObject instances containing user information from the directory service, including identityName, email, source, and other details.

## NOTES

When multiple drives are specified, searches are executed in parallel using the thread pool. The directory search is performed server-side, so the -Name value is sent to the Orchestrator API as the search term.

## RELATED LINKS

Test-OrchUserMappingCsv

New-OrchUserMappingCsv
