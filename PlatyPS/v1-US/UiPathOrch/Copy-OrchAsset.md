---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchAsset
---

# Copy-OrchAsset

## SYNOPSIS

Copies assets to another folder or Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchAsset [-Name] <string[]> [-Destination] <string> [-Path <string>] [-Recurse]
 [-Depth <uint>] [-UserMappingCsv <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies assets from a source folder to a destination folder in UiPath Orchestrator. The destination can be a different folder on the same Orchestrator instance or a folder on a different Orchestrator instance (cross-drive copy). All asset types are supported: Text, Bool, Integer, and Credential.

If the source and destination resolve to the same folder, the operation is silently skipped. If an asset with the same name already exists in the destination, it is updated with the source asset's values.

For per-robot assets, user assignments are copied based on matching usernames between source and destination. When copying across different Orchestrator instances, use -UserMappingCsv to specify how source usernames map to destination usernames. The user mapping CSV can be generated using New-OrchUserMappingCsv.

The -Name parameter supports wildcards to copy multiple assets at once. With -Recurse, the cmdlet preserves the folder hierarchy relative to the source root. Tab completion for -Name lists assets from the source folder.

Note that -Path is a single string (not string[]), unlike most other cmdlets.

Primary Endpoint: GET /odata/Assets, POST /odata/Assets, PUT /odata/Assets({asset.Id})

OAuth required scopes: OR.Assets or OR.Assets.Read (source), OR.Assets (destination)

Required permissions: Assets.View (source), Assets.Create, Assets.Edit (destination)

## EXAMPLES

### Example 1: Copy a specific asset to another folder

```powershell
PS Orch1:\Shared> Copy-OrchAsset -Name TestAsset1 -Destination Orch1:\Dept#2
```

Copies the asset "TestAsset1" from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance.

### Example 2: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchAsset -Name 'Test*' -Destination Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Copy Asset" on target "Item: 'Orch1:\Shared\TestAsset1' Destination: 'Orch1:\Dept#2'".
What if: Performing the operation "Copy Asset" on target "Item: 'Orch1:\Shared\TestAsset2024' Destination: 'Orch1:\Dept#2'".
What if: Performing the operation "Copy Asset" on target "Item: 'Orch1:\Shared\TestCredential1' Destination: 'Orch1:\Dept#2'".
```

Shows which assets would be copied without executing the command. Wildcard patterns match multiple assets including credential assets.

### Example 3: Copy all assets to another Orchestrator instance

```powershell
PS Orch1:\Shared> Copy-OrchAsset -Name '*' -Destination Orch2:\Shared
```

Copies all assets from the Shared folder on Orch1 to the Shared folder on Orch2. Cross-drive copy enables migration between Orchestrator instances.

### Example 4: Copy assets recursively with folder hierarchy

```powershell
PS C:\> Copy-OrchAsset -Path Orch1:\Shared -Recurse -Name '*' -Destination Orch2:\Shared
```

Copies all assets from Shared and all its subfolders on Orch1, preserving the folder hierarchy relative to the source root. Subfolders are matched by relative path on the destination.

### Example 5: Copy with user mapping for cross-instance migration

```powershell
PS C:\> Copy-OrchAsset -Path Orch1:\Shared -Name '*' -Destination Orch2:\Shared -UserMappingCsv c:user-mapping.csv
```

Copies assets with a user mapping CSV file for per-robot assets. The CSV maps source usernames to destination usernames, enabling cross-instance migration where user accounts differ. Use New-OrchUserMappingCsv to generate the mapping file.

### Example 6: Copy from a specific folder using -Path

```powershell
PS C:\> Copy-OrchAsset -Path Orch1:\Shared -Name TestAsset1 -Destination Orch1:\Dept#2
```

Copies the asset from the Shared folder to the Dept#2 folder. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used. Unlike most other cmdlets, this parameter accepts a single string (not an array).

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

### -Recurse

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy relative to the source root is preserved at the destination.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Depth

Specifies the depth for recursion into the source folders. A depth of 0 targets only the source folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -Destination

Specifies the destination folder path. This is a mandatory parameter. Can be a folder on the same Orchestrator instance (e.g., Orch1:\Production) or on a different instance (e.g., Orch2:\Shared) for cross-instance migration.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
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

### -Name

Specifies the names of assets to copy. This is a mandatory parameter. Supports wildcards to copy multiple assets. Tab completion lists assets from the source folder.

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

### -UserMappingCsv

Specifies the path to a user mapping CSV file for per-robot asset migration. The CSV maps source usernames to destination usernames, which is required when copying per-robot assets across Orchestrator instances where user accounts have different names. Use New-OrchUserMappingCsv to generate the mapping file. Requires a filesystem path (not an Orch: drive path).

```yaml
Type: System.String
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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run. The output shows the source asset path and the destination folder.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe asset names to this cmdlet via the Name property.

### System.String

You can pipe source path and destination via the Path and Destination properties.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns Asset objects for newly created assets at the destination. When an existing asset is updated, no output is produced.

## NOTES

The -Path parameter is a single string, not a string array. This differs from most other cmdlets in the module that accept string arrays for -Path.

When the source and destination resolve to the same folder, the operation is silently skipped without error.

For cross-instance migration of per-robot assets, a user mapping CSV is required because user IDs differ between instances. Without -UserMappingCsv, per-robot values may not be copied correctly if usernames do not match.

Credential asset passwords cannot be read from the source; only the credential username and structure are copied. Passwords must be re-entered at the destination.

## RELATED LINKS

Get-OrchAsset

Set-OrchAsset

Set-OrchCredentialAsset

Remove-OrchAsset

New-OrchUserMappingCsv
