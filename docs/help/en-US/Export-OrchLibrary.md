---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchLibrary.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Export-OrchLibrary
---

# Export-OrchLibrary

## SYNOPSIS

Exports library packages from UiPath Orchestrator to the local filesystem.

## SYNTAX

### __AllParameterSets

```
Export-OrchLibrary [-Path <string[]>] [[-Id] <string[]>] [[-Version] <string[]>]
 [[-Destination] <string>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Exports library packages (NuGet packages containing reusable workflows) from UiPath Orchestrator to the local filesystem as .nupkg files. The cmdlet downloads matching library versions from the tenant feed and saves them to the specified destination directory.

Both -Id and -Version parameters support wildcards, allowing flexible exports such as downloading all versions of a specific library or all libraries matching a pattern. If -Destination is not specified, files are saved to the current filesystem location.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the target tenant. The -Version completer filters based on the currently specified -Id value.

Primary Endpoint: GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{libraryId}:{libraryVersion}')

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Libraries.View

## EXAMPLES

### Example 1: Export all libraries to the current directory

```powershell
PS C:\Export> Export-OrchLibrary -Path Orch1:\
```

Exports all library packages from the Orch1 tenant feed to the current directory (C:\Export). Each version is saved as a separate .nupkg file.

### Example 2: Export a specific library

```powershell
PS Orch1:\> Export-OrchLibrary MyShared.Utilities c:\Export
```

Exports all versions of the library "MyShared.Utilities" to the C:\Export directory. The -Id (position 0) and -Destination (position 2) parameters are positional. Since -Version (position 1) is omitted, all versions are exported.

### Example 3: Export a specific version of a library

```powershell
PS Orch1:\> Export-OrchLibrary MyShared.Utilities 1.0.0 C:\Export
```

Exports version 1.0.0 of the library "MyShared.Utilities" to the C:\Export directory. All three positional parameters (-Id, -Version, -Destination) are specified by position.

### Example 4: Export libraries using wildcards

```powershell
PS Orch1:\> Export-OrchLibrary MyShared.* * C:\Backup
```

Exports all versions of all libraries whose Id starts with "MyShared." to the C:\Backup directory.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. Use tab completion to see available drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Destination

Specifies the destination directory on the local filesystem where .nupkg files will be saved. If not specified, the current filesystem location is used. The directory must already exist.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Id

Specifies the Id of the library packages to export. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the target tenant. If not specified, all libraries are exported.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Version

Specifies the version of the library packages to export. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests available versions based on the specified -Id. If not specified, all versions are exported.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe library IDs and versions to this cmdlet via the Id and Version properties.

### System.String

You can pipe a destination path to this cmdlet via the Destination property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. Library packages are saved as .nupkg files to the specified destination directory.

## NOTES

The destination directory must already exist; the cmdlet does not create it. Each exported library version is saved as a separate .nupkg file with the naming convention {Id}.{Version}.nupkg.

## RELATED LINKS

[Get-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibrary.md)

[Get-OrchLibraryVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibraryVersion.md)

[Import-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchLibrary.md)

[Copy-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchLibrary.md)
