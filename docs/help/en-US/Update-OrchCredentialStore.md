---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchCredentialStore.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/05/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchCredentialStore
---

# Update-OrchCredentialStore

## SYNOPSIS

Updates an existing credential store in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchCredentialStore [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-AdditionalConfiguration <string>] [-Confirm] [-HostName <string>] [-NewName <string>]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates an existing credential store. Only the parameters that are explicitly specified are modified; other server-known fields are preserved by deep-copying the current store entity before applying changes.

The primary post-migration use case is to re-supply the secret values inside `AdditionalConfiguration` (for example, the CyberArk vault URL/API key, Azure Key Vault credentials, or HashiCorp Vault token). The Web API returns `AdditionalConfiguration` with secret values masked, so any subsequent `PUT` would write the masked values back unless this cmdlet handles it specially:

  - The cmdlet drops the masked `AdditionalConfiguration` from the deep-copied entity before applying changes.
  - It only includes a value in the outgoing payload when the caller explicitly supplies one via `-AdditionalConfiguration`.
  - This mirrors the `UR_Password` handling in `Update-OrchUser`.

The `-Name` parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available credential store names from the target drive.

Primary Endpoint: PUT /odata/CredentialStores({id}), GET /odata/CredentialStores({id})

OAuth required scopes: OR.Administration

Required permissions: Settings.Edit, Settings.View

## EXAMPLES

### Example 1: Re-supply secrets after migration

```powershell
PS Destination:\> $config = @{
    Url     = 'https://vault.contoso.com/'
    ApiKey  = '<new vault API key>'
    AppId   = 'orchestrator-prod'
} | ConvertTo-Json -Compress
PS Destination:\> Update-OrchCredentialStore -Name 'CyberArkProd' -AdditionalConfiguration $config
```

Restores the CyberArk credentials on the destination tenant after `Copy-OrchCredentialStore`. Until this step runs, every credential asset that resolves through `CyberArkProd` will fail.

### Example 2: Rename a store

```powershell
PS Destination:\> Update-OrchCredentialStore -Name 'CyberArk' -NewName 'CyberArkProd'
```

Renames the store. Existing credential assets continue to resolve because they reference the store by Id, not by name.

### Example 3: Change the host

```powershell
PS Destination:\> Update-OrchCredentialStore -Name 'CyberArkProd' -HostName 'vault.new-region.contoso.com'
```

Updates `HostName` while preserving the existing `AdditionalConfiguration` (no `-AdditionalConfiguration` parameter, no overwrite).

### Example 4: Preview with WhatIf

```powershell
PS Destination:\> Update-OrchCredentialStore -Name 'CyberArkProd' -AdditionalConfiguration $config -WhatIf
```

Reports the intended update without performing any change. Useful for verifying that the JSON payload will be applied to the right store.

## PARAMETERS

### -Path

Specifies the target Orchestrator drive(s). If not specified, the current drive is targeted.

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

### -Name

Specifies the names of the credential stores to update. Supports wildcards and multiple comma-separated values. Tab completion suggests credential store names from the target drive. This parameter is mandatory and positional (position 0).

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

Specifies a new name for the credential store. Use this parameter to rename a single store (do not combine with wildcards in `-Name` when renaming).

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

### -HostName

Sets the host name (vault URL or hostname) used by the credential store provider.

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

### -AdditionalConfiguration

Sets the provider-specific JSON configuration that contains the store's secrets (vault API keys, OAuth tokens, etc.). The exact JSON shape depends on the credential store `Type`. Because the API returns this field with secrets masked, the cmdlet only sends a value when one is explicitly supplied — calling `Update-OrchCredentialStore` without this parameter will not overwrite the existing configuration with masked values.

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

You can pipe credential store objects to this cmdlet. Properties are bound by name (Name, HostName, AdditionalConfiguration, Path).

## OUTPUTS

### None

This cmdlet does not produce output. Credential stores are updated in place on the Orchestrator server.

## NOTES

Credential stores are tenant-level entities, not folder-scoped. Use `-Path` to target a specific drive when multiple drives are mounted.

The Web API returns `AdditionalConfiguration` with secret fields masked. To prevent the masked values from being written back, the cmdlet null-clears `AdditionalConfiguration` on the deep-copied entity before applying user-supplied overrides. This means that when `-AdditionalConfiguration` is not specified, the field is omitted from the PUT payload (so the server keeps its existing configuration intact).

The credential store `Type` field cannot be changed after creation; this cmdlet does not expose it.

## RELATED LINKS

[Get-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialStore.md)

[Copy-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCredentialStore.md)

[Remove-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCredentialStore.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)
