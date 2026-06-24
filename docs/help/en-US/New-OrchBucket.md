---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchBucket.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchBucket
---

# New-OrchBucket

## SYNOPSIS

Creates a new storage bucket in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchBucket [-Path <string[]>] [-LiteralPath <string[]>] [[-Name] <string[]>] [-Confirm]
 [-CredentialStore <string>] [-Description <string>] [-ExternalName <string>]
 [-Options <string[]>] [-Password <string>] [-StorageContainer <string>]
 [-StorageParameters <string>] [-StorageProvider <string>] [-Tags <string[]>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new storage bucket in a UiPath Orchestrator folder. A storage bucket is a container for storing files (blob objects). You can configure the storage provider, credential store, and other settings during creation. A unique identifier is automatically generated for the new bucket.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see a suggested name for the new bucket based on existing bucket names in the target folder. The -StorageProvider and -Options parameters also support tab completion with predefined values. The -CredentialStore parameter provides tab completion with available credential store names.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which buckets would be created, or -Confirm to be prompted before each creation.

Primary Endpoint: POST /odata/Buckets

OAuth required scopes: OR.Administration or OR.Administration.Write

Required permissions: Buckets.Create

## EXAMPLES

### Example 1: Create a bucket with default settings

```powershell
PS Orch1:\Shared> New-OrchBucket NewBucket1
```

Creates a new storage bucket named "NewBucket1" in the current folder with default settings. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Create a bucket with a description

```powershell
PS Orch1:\Shared> New-OrchBucket NewBucket1 -Description "Stores monthly reports"
```

Creates a new storage bucket named "NewBucket1" with a description in the current folder.

### Example 3: Create a bucket in a specific folder

```powershell
PS C:\> New-OrchBucket -Path Orch1:\Shared -Name NewBucket1 -Description "Data files"
```

Creates a new storage bucket named "NewBucket1" in the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 4: Create a bucket with tags and options

```powershell
PS Orch1:\Shared> New-OrchBucket NewBucket1 -Description "Archive storage" -Tags "archive","readonly" -Options ReadOnly
```

Creates a new storage bucket with tags and options. The -Options parameter supports tab completion with available option values.

### Example 5: Preview creation with -WhatIf

```powershell
PS Orch1:\Shared> New-OrchBucket TestBucket -WhatIf
```

```output
What if: Performing the operation "New Bucket" on target "Orch1:\Shared\TestBucket".
```

Shows what would happen without actually creating the bucket. Use this to verify the operation before performing it.

## PARAMETERS

### -Path

Specifies the target folder where the bucket will be created. If not specified, the current folder is used. Supports wildcards.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -CredentialStore

Specifies the name of the credential store to associate with the bucket. Supports wildcards for matching. Tab completion dynamically suggests available credential store names from the target folder.

```yaml
Type: System.String
DefaultValue: ''
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

### -Description

Specifies a description for the storage bucket.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExternalName

Specifies an external name for the storage bucket, used to reference external storage resources.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the name of the storage bucket to create. Tab completion suggests a new name based on existing bucket names in the target folder. Multiple names can be specified to create multiple buckets.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

### -Options

Specifies bucket options. Tab completion suggests available option values. Multiple options can be specified as an array.

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

### -Password

Specifies the password for the storage bucket, if required by the storage provider.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StorageContainer

Specifies the storage container name.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StorageParameters

Specifies additional storage parameters as a string, used to configure the storage provider connection.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StorageProvider

Specifies the storage provider type for the bucket (e.g., Orchestrator, Azure, Amazon, Minio). Tab completion suggests available storage provider types.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Tags

Specifies tags to associate with the storage bucket. Multiple tags can be specified as an array.

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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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
DefaultValue: ''
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

### System.String

You can pipe a description, storage provider, or other string properties to this cmdlet.

### System.String[]

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket

Returns the newly created Bucket object with properties including Name, Description, StorageProvider, StorageContainer, CredentialStoreId, ExternalName, Options, Tags, and Path.

## NOTES

Buckets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

A unique Identifier (GUID) is automatically generated for each new bucket. The -CredentialStore parameter is resolved to a CredentialStoreId by looking up the credential store name in the target folder. If the specified CredentialStore is not found, the operation will fail. Multiple -Options values are joined with a comma separator.

## RELATED LINKS

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[Remove-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucket.md)

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)
