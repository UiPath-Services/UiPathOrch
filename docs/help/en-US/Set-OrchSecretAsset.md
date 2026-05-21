---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchSecretAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchSecretAsset
---

# Set-OrchSecretAsset

## SYNOPSIS

Creates and updates Secret-type assets.

## SYNTAX

### DefaultParameterSet (Default)

```
Set-OrchSecretAsset [-Path <string[]>] [-Name] <string[]> [[-UserName] <string[]>]
 [[-MachineName] <string[]>] [-Confirm] [-CredentialStore <string>]
 [-Description <string>] -Secret <securestring> [-WhatIf] [<CommonParameters>]
```

### SpecifyPlainSecretParameterSet

```
Set-OrchSecretAsset [-Path <string[]>] [-Name] <string[]> [[-UserName] <string[]>]
 [[-MachineName] <string[]>] [[-SecretValue] <string>] [-Confirm]
 [-CredentialStore <string>] [-Description <string>] [-ExternalName <string>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates and updates Secret-type assets in UiPath Orchestrator folders. Secret assets store opaque values (as opposed to Credential's username+password pair). This cmdlet handles only Secret-type assets. For Text/Bool/Integer, use Set-OrchAsset; for Credential, use Set-OrchCredentialAsset.

The cmdlet has two parameter sets:

- **DefaultParameterSet**: Uses a SecureString via -Secret. Hidden (DontShow), primarily for programmatic use.
- **SpecifyPlainSecretParameterSet**: Uses -SecretValue as a plain string. Primary set for interactive and pipeline use including CSV import.

The cmdlet determines the operation automatically:

- **Create**: When -Name does not exist and a non-empty -SecretValue/-Secret/-ExternalName is supplied, a new Secret asset is created.
- **Update**: When -Name matches an existing Secret asset, it is updated. Description, CredentialStore, ExternalName, and SecretValue fields may be updated.

**Empty -SecretValue semantics (round-trip safe)**: Unlike Set-OrchAsset and Set-OrchCredentialAsset, an empty -SecretValue on Set-OrchSecretAsset is **silently skipped**. The existing secret is NOT cleared and no UserValue is deleted. This protects configured secrets during CSV round-trip, because Get-OrchSecretAsset always exports SecretValue as empty (the API masks it).

**Removing UserValues**: Because empty -SecretValue is a no-op, use `Remove-OrchAssetUserValue` to explicitly delete PerRobot UserValues from a Secret asset. There is no cmdlet-level way to clear a Global secret via Set-OrchSecretAsset — use `Remove-OrchAsset` to remove the entire asset.

**External credential stores**: When -ExternalName is specified, the secret is retrieved from an external credential store (e.g., CyberArk, Azure Key Vault) at runtime, and -SecretValue is not used.

The -Name, -UserName, -MachineName, -CredentialStore, and -Path parameters support tab completion. The -Name completion lists only Secret-type assets.

Primary Endpoint: POST /odata/Assets, PUT /odata/Assets({asset.Id})

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Create, Assets.Edit

## EXAMPLES

### Example 1: Create a Secret asset

```powershell
PS Orch1:\Shared> Set-OrchSecretAsset -Name ApiKey -SecretValue 'abc-123-xyz'
```

Creates a new Secret asset at the current folder with the given value.

### Example 2: Update Description only (no secret change)

```powershell
PS Orch1:\Shared> Set-OrchSecretAsset -Name ApiKey -Description 'Used by prod workflow' -SecretValue ''
```

Updates only the description. Passing `-SecretValue ''` selects the plain parameter set without changing the stored secret. If `-SecretValue` is omitted entirely, the Default parameter set is selected and `-Secret` becomes a mandatory prompt.

### Example 3: Use a SecureString

```powershell
PS Orch1:\Shared> $s = Read-Host 'Secret' -AsSecureString
PS Orch1:\Shared> Set-OrchSecretAsset -Name ApiKey -Secret $s
```

Supplies the secret as a SecureString via the Default parameter set.

### Example 4: Set a per-robot secret

```powershell
PS Orch1:\Shared> Set-OrchSecretAsset -Name ApiKey -UserName 'alice@example.com' -SecretValue 'alice-specific'
```

Creates a PerRobot UserValue for the specified user. MachineName may also be supplied to scope further.

### Example 5: Use an external credential store

```powershell
PS Orch1:\Shared> Set-OrchSecretAsset -Name VaultSecret -ExternalName 'vault/secret/mykey' -CredentialStore MyVault
```

Creates or updates a Secret asset that references an external credential store. -SecretValue is not used with -ExternalName.

### Example 6: CSV round-trip preserves existing secrets

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -ExportCsv C:\temp\secrets.csv
# Edit descriptions in the CSV; leave SecretValue column empty
PS Orch1:\Shared> Import-Csv C:\temp\secrets.csv | Set-OrchSecretAsset
```

Exports, edits, and re-imports Secret assets. Empty SecretValue cells are preserved silently — the server-side secret is not overwritten or removed.

### Example 7: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Set-OrchSecretAsset -Name NewSecret -SecretValue 'v' -WhatIf
```

```output
What if: Performing the operation "Add SecretAsset" on target "Orch1:\Shared\NewSecret".
```

Shows what would happen without executing.

## PARAMETERS

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

### -CredentialStore

Name of the credential store to associate with the Secret asset. When omitted, the default credential store (usually Orchestrator Database) is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
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

Free-form description of the Secret asset.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
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

Reference key in an external credential store (e.g., CyberArk, Azure Key Vault). When specified, the secret is retrieved from the external store at runtime and -SecretValue is not used. Requires -CredentialStore to point to the external store.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: SpecifyPlainSecretParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MachineName

Machine name(s) for scoping a PerRobot UserValue. Combined with -UserName, forms the (user, machine) key of the UserValue. When omitted, the UserValue is user-scoped without machine binding.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Name of the Secret asset to create or update. Wildcards are supported for update operations. Multiple values may be specified.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Target folder path(s) on an Orch drive. When omitted, the current folder is used.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Secret

Secret value as a SecureString. Mandatory in the Default parameter set. Hidden (DontShow) because the plain-string -SecretValue is the more common choice for interactive and pipeline use.

```yaml
Type: System.Security.SecureString
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -SecretValue

Secret value as a plain string. Selecting this parameter chooses the SpecifyPlainSecretParameterSet, which is the primary set for CSV import and interactive use.

An empty string (`''`) is silently skipped — the existing secret is NOT cleared and UserValues are NOT deleted. This is intentional because CSV round-trip exports always have SecretValue empty. To remove a PerRobot UserValue use Remove-OrchAssetUserValue; to remove the entire asset use Remove-OrchAsset.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: SpecifyPlainSecretParameterSet
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Target user name(s) for creating or updating a PerRobot UserValue. When omitted, the Global scope is targeted. Multiple users may be specified comma-separated.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainSecretParameterSet
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

Name of the Secret asset.

### System.String[]

Array of Secret asset names.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns the created Asset object on create. On update operations no output is produced.

## NOTES

Secret assets require UiPath Orchestrator v20+. Attempting to create a Secret on older servers fails.

Set-OrchAsset silently skips -ValueType Secret as a no-op; this cmdlet is the correct one for Secret management.

The CSV produced by Get-OrchSecretAsset -ExportCsv always has an empty SecretValue column because the server masks secrets. An empty SecretValue in an import row is deliberately a no-op to preserve the existing secret.

To bulk-change Description on many Secret assets while leaving secrets intact: `Get-OrchSecretAsset -ExportCsv out.csv`, edit Description in the CSV, then `Import-Csv out.csv | Set-OrchSecretAsset`.

## RELATED LINKS

[Get-OrchSecretAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSecretAsset.md)

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)

[Remove-OrchAssetUserValue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetUserValue.md)

[Remove-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md)
