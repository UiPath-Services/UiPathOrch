---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchLibrary.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchLibrary
---

# Copy-OrchLibrary

## SYNOPSIS

Copies library packages from one UiPath Orchestrator tenant to another.

## SYNTAX

### __AllParameterSets

```
Copy-OrchLibrary [-Id] <string[]> [[-Version] <string[]>] [-Destination] <string[]> [-Path <string>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies library packages (NuGet packages containing reusable workflows) from one UiPath Orchestrator tenant feed to another. The cmdlet downloads matching library versions from the source tenant and uploads them to the destination tenant(s). If a library with the same Id and version already exists in the destination, the copy is skipped and an error is written.

The destination tenant must have its library feed configured as "Only tenant feed" or "Both host and tenant feeds". If the destination tenant is set to "Only host feed", the copy operation fails with a descriptive error message.

Both -Id and -Version parameters support wildcards, allowing bulk copy of multiple libraries in a single operation. The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the source tenant.

Primary Endpoint: GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage (source), POST /odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage (destination)

OAuth required scopes: OR.Execution or OR.Execution.Read (source), OR.Execution (destination)

Required permissions: Libraries.View (source), Libraries.Create (destination)

## EXAMPLES

### Example 1: Copy all libraries to another tenant

```powershell
PS Orch1:\> Copy-OrchLibrary * Orch2:\
```

Copies all versions of all libraries from Orch1 to Orch2. The -Id (position 0) and -Destination (position 2) parameters are positional. Since -Version (position 1) is omitted, all versions are copied. Libraries that already exist in the destination are skipped.

### Example 2: Copy a specific version to another tenant

```powershell
PS Orch1:\> Copy-OrchLibrary UiPath.* 1.0.* Orch2:\
```

Copies all versions matching "1.0.*" of libraries whose Id starts with "UiPath." from Orch1 to Orch2. All three positional parameters (-Id, -Version, -Destination) are specified by position.

### Example 3: Copy libraries from any location using -Path

```powershell
PS C:\> Copy-OrchLibrary -Path Orch1:\ UiPath.* -Destination Orch2:\
```

Copies all libraries whose Id starts with "UiPath." from Orch1 to Orch2. When -Path specifies the source drive with an absolute path, the command can be run from any location.

### Example 4: Copy libraries to multiple destinations

```powershell
PS Orch1:\> Copy-OrchLibrary * -Destination Orch2:\,Orch3:\
```

Copies all versions of all libraries from Orch1 to both Orch2 and Orch3. The -Destination parameter accepts multiple comma-separated drive names.

## PARAMETERS

### -Path

Specifies the source Orchestrator drive. If not specified, the current drive is used as the source.

```yaml
Type: System.String
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

Specifies the destination Orchestrator drive names. Multiple destinations can be specified using comma-separated values. Tab completion dynamically suggests available drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Id

Specifies the Id of the library packages to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the source tenant. This parameter is mandatory.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Version

Specifies the version of the library packages to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests available versions based on the specified -Id. If not specified, all versions of the matching libraries are copied.

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

You can pipe library IDs, versions, and destination drive names to this cmdlet via the Id, Version, and Destination properties.

### System.String

You can pipe a source drive path to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString

Returns upload result objects for each successfully copied library package.

## NOTES

If a library with the same Id and version already exists in the destination tenant, the copy is skipped and an error is written. The source and destination cannot be the same drive.

The destination tenant must have its library feed setting configured to accept tenant-level uploads. If the feed is set to "Only host feed", the copy operation fails with a message instructing you to change the tenant settings to "Only tenant feed" or "Both host and tenant feeds".

## RELATED LINKS

Get-OrchLibrary

Get-OrchLibraryVersion

Import-OrchLibrary

Export-OrchLibrary

Remove-OrchLibrary
