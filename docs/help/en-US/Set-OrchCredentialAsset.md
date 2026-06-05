---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchCredentialAsset
---

# Set-OrchCredentialAsset

## SYNOPSIS

Creates, updates, and removes credential assets.

## SYNTAX

### DefaultParameterSet (Default)

```
Set-OrchCredentialAsset [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [[-UserName] <string[]>]
 [[-MachineName] <string[]>] [-Confirm] -Credential <pscredential>
 [-CredentialStore <string>] [-Description <string>] [-WhatIf] [<CommonParameters>]
```

### SpecifyPlainPasswordParameterSet

```
Set-OrchCredentialAsset [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [[-UserName] <string[]>]
 [[-MachineName] <string[]>] [[-CredentialUsername] <string>]
 [[-CredentialPassword] <string>] [-Confirm] [-CredentialStore <string>]
 [-Description <string>] [-ExternalName <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates, updates, and removes credential assets in UiPath Orchestrator folders. This cmdlet handles only Credential-type assets. For Text, Bool, and Integer assets, use Set-OrchAsset instead.

The cmdlet has two parameter sets:

- **DefaultParameterSet**: Uses a PSCredential object. This parameter set is hidden (DontShow) and is primarily for programmatic use with `Get-Credential` or `New-Object PSCredential`.
- **SpecifyPlainPasswordParameterSet**: Uses separate -CredentialUsername and -CredentialPassword string parameters. This is the primary parameter set for interactive and pipeline use, including CSV import.

The cmdlet determines the operation automatically:

- **Create**: When -Name specifies a name that does not exist, a new credential asset is created.
- **Update**: When -Name matches an existing credential asset, the credential is updated.
- **Remove**: When -CredentialPassword is set to an empty string (`''`) without -CredentialUsername, the credential asset is removed. With -UserName specified, only the per-robot value is removed.

Credential assets can reference an external credential store (e.g., CyberArk, Azure Key Vault) via -CredentialStore and -ExternalName. When -ExternalName is specified, -CredentialUsername and -CredentialPassword are not used; the credential is retrieved from the external store at runtime.

The -Name, -UserName, -MachineName, -CredentialStore, and -Path parameters support tab completion. The -Name completion lists only credential-type assets and suggests 'New asset name here'. The -CredentialStore completion lists credential stores configured in the tenant (e.g., 'Orchestrator Database'). The -CredentialUsername and -CredentialPassword completions suggest placeholder text.

Primary Endpoint: POST /odata/Assets, PUT /odata/Assets({asset.Id}), DELETE /odata/Assets({assetId})

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Create, Assets.Edit, Assets.Delete

## EXAMPLES

### Example 1: Create a credential asset

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name ApiCredential -CredentialUsername apiuser -CredentialPassword 'secret123'
```

Creates a new credential asset with the specified username and password. The asset is stored in the default credential store (Orchestrator Database).

### Example 2: Preview credential operations with -WhatIf

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name TestCredential1 -CredentialUsername newuser -CredentialPassword 'newpass' -WhatIf
```

```output
What if: Performing the operation "Update CredentialAsset" on target "Orch1:\Shared\TestCredential1".
```

Shows what would happen without executing. The -WhatIf output shows "Add CredentialAsset", "Update CredentialAsset", or "Remove CredentialAsset".

### Example 3: Update credential username and password

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name TestCredential1 -CredentialUsername updateduser -CredentialPassword 'updatedpass'
```

Updates both the username and password of an existing credential asset. No output is produced on update; output is only returned when creating a new asset.

### Example 4: Update only the password

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name TestCredential1 -CredentialPassword 'newpassword'
```

Updates only the password while preserving the existing username.

### Example 5: Use an external credential store

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name VaultSecret -ExternalName 'vault/secret/mykey' -CredentialStore aa
```

Creates or updates a credential asset that references an external credential store. When -ExternalName is specified, the credential is retrieved from the external store at runtime, and -CredentialUsername/-CredentialPassword are not used.

### Example 6: Set a per-robot credential

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name AppCredential -UserName testrobot01 -CredentialUsername robotuser -CredentialPassword 'robotpass'
```

Sets a per-robot credential value for a specific user. The asset's ValueScope changes to PerRobot. Multiple -UserName and -MachineName values can be specified with wildcards.

### Example 7: Remove a credential asset

```powershell
PS Orch1:\Shared> Set-OrchCredentialAsset -Name ObsoleteCredential -CredentialPassword ''
```

Removes the credential asset by specifying an empty string as the password without a -CredentialUsername. For per-robot values, specify -UserName to remove only that user's value.

### Example 8: Create a credential in a specific folder

```powershell
PS C:\> Set-OrchCredentialAsset -Path Orch1:\Production -Name DbCredential -CredentialUsername dbadmin -CredentialPassword 'dbpass123'
```

Creates a credential asset in the Production folder using -Path. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 9: Import credentials from CSV

```powershell
PS Orch1:\Shared> Import-Csv c:credentials.csv | Set-OrchCredentialAsset
```

Imports credential assets from a CSV file exported by Get-OrchAsset -ExportCredentialCsv. The CSV columns (Path, Name, CredentialStore, UserName, MachineName, CredentialUsername, CredentialPassword, ExternalName) are bound to parameters via ValueFromPipelineByPropertyName. This uses the SpecifyPlainPasswordParameterSet.

### Example 10: Use PSCredential object

```powershell
PS Orch1:\Shared> $cred = New-Object PSCredential('admin', (ConvertTo-SecureString 'password' -AsPlainText -Force))
PS Orch1:\Shared> Set-OrchCredentialAsset -Name AdminCred -Credential $cred
```

Creates a credential asset using a PSCredential object. This parameter set is primarily for scripts that already have PSCredential objects.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
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
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

### -Credential

Specifies a PSCredential object containing the credential username and password. This parameter is hidden (DontShow) and is mandatory in the DefaultParameterSet. Use `Get-Credential` or `New-Object PSCredential` to create the object. For interactive and pipeline use, prefer the SpecifyPlainPasswordParameterSet with -CredentialUsername and -CredentialPassword instead.

```yaml
Type: System.Management.Automation.PSCredential
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

### -CredentialPassword

Specifies the password for the credential asset as a plain text string. An empty string (`''`) removes the credential asset (or per-robot value when -UserName is specified). When not specified, the existing password is not changed. Tab completion suggests 'CredentialPassword here'.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: SpecifyPlainPasswordParameterSet
  Position: 4
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CredentialStore

Specifies the credential store to use for the credential asset. Tab completion lists credential stores configured in the tenant. Supports wildcards; the pattern must resolve to exactly one credential store. When updating an existing asset, this changes the credential store for the asset and all its per-robot values.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CredentialUsername

Specifies the username for the credential asset as a plain text string. Tab completion suggests 'CredentialUsername here'. When updating an existing asset, omitting this parameter preserves the current username.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: SpecifyPlainPasswordParameterSet
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Description

Specifies a description for the credential asset. Tab completion suggests the current description of the asset specified by -Name, or 'Description here' if no description exists.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
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

Specifies the external name (key path) for credentials stored in an external credential store such as CyberArk or Azure Key Vault. When -ExternalName is specified, -CredentialUsername and -CredentialPassword are not used; the credential is retrieved from the external store at runtime. Typically used with -CredentialStore to specify which external store to use. An empty string (`''`) with -UserName removes the per-robot value.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: SpecifyPlainPasswordParameterSet
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

Specifies machine names for per-robot credential values. Used together with -UserName to assign credentials to specific user/machine combinations. If specified without -UserName, the machine names are ignored with a warning. Supports wildcards and multiple comma-separated values. Tab completion lists machines assigned to the target folder.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
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

Specifies the names of credential assets to create or update. This is a mandatory parameter. If the name matches an existing credential asset, the asset is updated. If the name does not exist, a new credential asset is created. Supports wildcards for bulk updates. Tab completion lists existing credential-type assets in the target folder and suggests 'New asset name here'.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies usernames for per-robot credential values. When specified, the asset's ValueScope is set to PerRobot. Supports wildcards and multiple comma-separated values. Tab completion lists users assigned to the target folder, excluding directory groups.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: SpecifyPlainPasswordParameterSet
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

Shows what would happen if the cmdlet runs. The cmdlet is not run. The output indicates the operation type: "Add CredentialAsset" for creation, "Update CredentialAsset" for modification, and "Remove CredentialAsset" for deletion.

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

### System.String

You can pipe credential asset property values to this cmdlet via property names. The SpecifyPlainPasswordParameterSet supports ValueFromPipelineByPropertyName for all parameters, enabling CSV import with Import-Csv.

### System.String[]

You can pipe multiple asset names via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns an Asset object only when creating a new credential asset. Update and remove operations produce no output. The returned object includes properties such as Name, ValueType (Credential), ValueScope, CredentialUsername, CredentialStoreId, and Id.

## NOTES

This cmdlet has two parameter sets. The DefaultParameterSet uses -Credential (PSCredential) and is hidden from tab completion. The SpecifyPlainPasswordParameterSet uses -CredentialUsername and -CredentialPassword as plain text strings and is the recommended parameter set for scripts and CSV import.

The DefaultParameterSet does not support ValueFromPipelineByPropertyName on most parameters. Use the SpecifyPlainPasswordParameterSet for pipeline input scenarios such as CSV import.

Credential assets reference a CredentialStore via CredentialStoreId. Use Get-OrchCredentialStore to list available credential stores and their names. When -CredentialStore is not specified, the asset uses the existing credential store (for updates) or the default store (for new assets).

## RELATED LINKS

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)

[Remove-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md)

[Get-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialStore.md)
