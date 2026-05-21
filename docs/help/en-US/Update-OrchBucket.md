---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchBucket.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/05/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchBucket
---

# Update-OrchBucket

## SYNOPSIS

Updates an existing storage bucket in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchBucket [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-CredentialStore <string>] [-Description <string>] [-ExternalName <string>]
 [-NewName <string>] [-Options <string[]>] [-Password <string>]
 [-StorageContainer <string>] [-StorageParameters <string>] [-StorageProvider <string>]
 [-Tags <string[]>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates an existing storage bucket. Only the parameters that are explicitly specified are modified; all other properties are preserved by deep-copying the current bucket entity before applying the changes.

The primary post-migration use case is to re-supply the bucket `Password` (S3 secret access key, Azure Storage key, etc.). The Web API never returns the password from `GET /odata/Buckets`, so it is dropped during `Copy-OrchBucket`. After copy, run `Update-OrchBucket -Name <name> -Password <secret>` on the destination tenant to restore access.

The `-Name` parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names dynamically populated from the target folders.

When specifying the `-Path`, `-Recurse`, and `-Depth` parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: PUT /odata/Buckets({id}), GET /odata/CredentialStores

OAuth required scopes: OR.Administration

Required permissions: Buckets.Edit, Buckets.View

## EXAMPLES

### Example 1: Re-set the bucket password after migration

```powershell
PS Destination:\Shared> Update-OrchBucket -Name 'invoice-store' -Password '<S3 secret access key>'
```

Restores the secret on a bucket that was copied from another tenant. The `Copy-OrchBucket` cmdlet cannot carry the password, so this is the post-migration step.

### Example 2: Update the description

```powershell
PS Destination:\Shared> Update-OrchBucket -Name 'invoice-store' -Description 'Migrated from Source tenant'
```

Sets a new description without touching any other field.

### Example 3: Re-bind to a different credential store

```powershell
PS Destination:\Shared> Update-OrchBucket -Name 'vault-store' -CredentialStore 'CyberArkProd'
```

Resolves the named credential store on the destination drive and updates the bucket to use it. The credential store must already exist on the destination.

### Example 4: Bulk re-tag

```powershell
PS Destination:\Shared> Update-OrchBucket -Name '*' -Tags 'tier:archive','migrated:2026'
```

Adds the same tags to every bucket in the current folder. Tag normalization follows the same rules as `New-OrchBucket`.

### Example 5: Preview with WhatIf

```powershell
PS Destination:\Shared> Update-OrchBucket -Name 'invoice-*' -Password 'rotated' -WhatIf
```

Reports which buckets would have their password rotated without making any change.

### Example 6: Update across all subfolders

```powershell
PS Destination:\> Update-OrchBucket -Recurse -Name 'shared-cache' -Description 'Standard cache bucket'
```

Walks the folder tree from the drive root and updates every bucket named `shared-cache`.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

```yaml
Type: System.Management.Automation.SwitchParameter
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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder. When `-Depth` is specified, `-Recurse` is implied.

```yaml
Type: System.UInt32
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

### -Name

Specifies the names of the buckets to update. Supports wildcards and multiple comma-separated values. Tab completion suggests bucket names from the target folders. This parameter is mandatory and positional (position 0).

```yaml
Type: System.String[]
DefaultValue: ''
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

### -NewName

Specifies a new name for the bucket. Use this parameter to rename a single bucket (do not combine with wildcards in `-Name` when renaming).

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

### -Description

Sets a new description for the bucket. The description appears in the Orchestrator UI.

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

Sets the storage provider for the bucket (for example, `Amazon`, `Azure`, `Minio`, `FileSystem`). Tab completion suggests valid values.

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

Sets the provider-specific connection parameters (e.g., region/endpoint URLs encoded as a query-string). The exact format depends on `StorageProvider`.

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

Sets the storage container (S3 bucket name, Azure container name, etc.).

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

### -CredentialStore

Sets the named credential store that holds the bucket's credentials. The store is resolved by name on the same drive; it must already exist. Supports wildcards (the first match wins).

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

### -Password

Sets the bucket's secret (S3 secret access key, Azure storage key, etc.). The Web API never returns this value from GET, so it must be re-supplied after `Copy-OrchBucket` migrations.

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

### -Options

Sets the bucket option flags (e.g., `ReadOnly`, `BypassRule`). Tab completion suggests valid values. Multiple options are joined with commas.

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

### -ExternalName

Sets the external display name shown in the Orchestrator UI when the bucket integrates with an external system.

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

Specifies one or more tags to assign to the bucket. Tags help categorize and filter buckets in the Orchestrator UI. Tag handling matches `New-OrchBucket`.

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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### System.String[]

You can pipe bucket objects (such as CSV import rows) to this cmdlet. Properties are bound by name (Name, Description, StorageProvider, StorageContainer, StorageParameters, CredentialStore, Password, Options, ExternalName, Tags, Path).

## OUTPUTS

### None

This cmdlet does not produce output. Buckets are updated in place on the Orchestrator server.

## NOTES

Buckets are folder-scoped entities. Use `-Path` to target a specific folder, or run from a folder location on the Orch: drive.

Only explicitly specified parameters are modified. The cmdlet deep-copies the current bucket before applying changes, so unspecified properties retain their existing values. The `Password` field is write-only on the server (`GET` never returns it), so `Update-OrchBucket` only sends `Password` in the payload when the user explicitly supplies one.

Tag updates are additive in spirit but the underlying API replaces the entire `Tags` collection. Pass the full desired tag list when updating tags.

## RELATED LINKS

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[New-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchBucket.md)

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)

[Remove-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucket.md)
