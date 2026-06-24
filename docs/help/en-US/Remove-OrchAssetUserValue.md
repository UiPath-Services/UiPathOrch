---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetUserValue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchAssetUserValue
---

# Remove-OrchAssetUserValue

## SYNOPSIS

Removes per-robot (UserValue) entries from assets, regardless of asset type.

## SYNTAX

### __AllParameterSets

```
Remove-OrchAssetUserValue [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-UserName] <string[]> [[-MachineName] <string[]>] [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes per-robot (UserValue) entries from one or more assets. Type-agnostic: works on Text, Bool, Integer, Credential, and Secret assets.

The primary motivation is Secret-typed assets, where the value is masked by the API so the empty-delete convention used by Set-OrchAsset / Set-OrchCredentialAsset is not round-trip safe. This cmdlet provides the explicit, type-agnostic path for any asset.

**Filtering logic**:
- -Name (mandatory) selects the target asset(s); wildcards supported.
- -UserName (mandatory) selects which UserValues to remove; wildcards supported. Use `*` to target all UserValues on the asset.
- -MachineName (optional) further scopes the match to user+machine combinations; wildcards supported. When omitted, all UserValues matching -UserName are removed regardless of machine.

**Scope recovery**: When all UserValues are removed from an asset that had a Global value, the asset's ValueScope reverts to 'Global' and the Global value is preserved (HasDefaultValue stays true). If the asset has no Global value either, the asset is left with ValueScope='Global' and HasDefaultValue=false — use Remove-OrchAsset to delete the asset shell.

**ShouldProcess**: Supports -WhatIf and -Confirm. Each UserValue removal is confirmed individually.

The -Name, -UserName, -MachineName, and -Path parameters support tab completion. The -Name completion lists assets that have at least one UserValue. The -UserName completion lists distinct UserNames appearing in UserValues for the selected asset(s). The -MachineName completion lists distinct MachineNames scoped by the selected -UserName.

Primary Endpoint: PUT /odata/Assets({asset.Id})

OAuth required scopes: OR.Assets

Required permissions: Assets.Edit

## EXAMPLES

### Example 1: Remove a UserValue from a Secret asset

```powershell
PS Orch1:\Shared> Remove-OrchAssetUserValue -Name ApiKey -UserName alice@example.com
```

Removes the PerRobot entry for alice on the Secret asset ApiKey. Other UserValues and the Global value are preserved.

### Example 2: Remove UserValues scoped by user and machine

```powershell
PS Orch1:\Shared> Remove-OrchAssetUserValue -Name ApiKey -UserName alice@example.com -MachineName Robot-01
```

Removes only the UserValue matching both user alice and machine Robot-01.

### Example 3: Clear all UserValues on an asset

```powershell
PS Orch1:\Shared> Remove-OrchAssetUserValue -Name ApiKey -UserName * -Confirm:$false
```

Removes all UserValues on ApiKey. If the asset has a Global value, it is retained and ValueScope reverts to 'Global'.

### Example 4: Wildcard name and user

```powershell
PS Orch1:\Shared> Remove-OrchAssetUserValue -Name 'Api*' -UserName '*example.com'
```

Removes matching UserValues across all assets whose names start with 'Api' for users whose names end with 'example.com'.

### Example 5: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchAssetUserValue -Name ApiKey -UserName alice@example.com -WhatIf
```

```output
What if: Performing the operation "Remove UserValue" on target "Orch1:\Shared\ApiKey [alice@example.com\]".
```

Shows which UserValue removals would occur without executing them.

### Example 6: Recursive removal across folders

```powershell
PS Orch1:\> Remove-OrchAssetUserValue -Recurse -Name * -UserName retired-user@example.com -Confirm:$false
```

Removes all UserValues for a retired user across every folder on the drive.

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

### -Depth

Depth limit for recursion. A depth of 0 targets only the current folder, with no subfolders included.

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

### -MachineName

Machine name(s) to further scope the UserValue match. Wildcards supported. When omitted, matching is done on -UserName only (regardless of machine).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -Name

Asset name(s) to target. Mandatory. Wildcards supported. Multiple values may be specified.

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

### -Path

Target folder path(s) on an Orch drive. When omitted, the current folder is used.

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

### -Recurse

Recursively traverse subfolders of -Path.

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

### -UserName

User name(s) whose UserValues to remove. Mandatory. Wildcards supported. Use `*` to target all users.

```yaml
Type: System.String[]
DefaultValue: ''
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

Name of the asset.

### System.String[]

Array of asset names.

## OUTPUTS

### None

This cmdlet does not produce output on success. Errors are written via WriteError (per-asset).

## NOTES

This cmdlet is type-agnostic and works on any asset type. For Text/Bool/Integer/Credential assets, the conventional empty-delete (`Set-OrchAsset -UserName X -Value ''` or `Set-OrchCredentialAsset -UserName X -CredentialPassword ''`) also removes a UserValue and may be more convenient. For Secret assets, Remove-OrchAssetUserValue is the only supported way because empty -SecretValue is a silent no-op (to protect the masked-value round-trip).

To delete the entire asset, use Remove-OrchAsset — Remove-OrchAssetUserValue only removes PerRobot entries and never removes the asset itself.

The admin API accepts UserValue operations for any tenant user regardless of folder assignment. Assigning the user to the folder (Add-OrchFolderUser) is only required for the robot runtime to actually read the value — not for admin CRUD.

## RELATED LINKS

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)

[Set-OrchSecretAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchSecretAsset.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Remove-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md)
