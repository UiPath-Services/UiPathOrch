---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchAsset
---

# Get-OrchAsset

## SYNOPSIS

Gets assets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchAsset [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-CsvEncoding <Encoding>] [-ExpandUserValues] [-ExportCredentialCsv <string>]
 [-ExportCsv <string>] [-ValueType <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets asset information from UiPath Orchestrator folders. Assets are data stores used by robots to store configuration settings, credentials, and other automation data.

Assets have four value types: Text, Bool, Integer, and Credential. Each asset has a ValueScope of either Global (same value for all users) or PerRobot (different values per user/machine combination). Credential assets reference a CredentialStore that determines where the credentials are stored.

This cmdlet supports filtering by asset name and value type, expanding user-specific values for per-robot assets, and exporting results to CSV files. When -ExpandUserValues is specified, per-robot assets output AssetUserValue objects instead of Asset objects, showing each user/machine assignment as a separate row.

> **Deprecated:** `-ExportCredentialCsv` is deprecated and will be removed in a future major release. Use `Get-OrchCredentialAsset -ExportCsv` instead — each asset type's CSV export pairs with its own cmdlet family (`Get-OrchCredentialAsset -ExportCsv` imports via `Set-OrchCredentialAsset`, `Get-OrchSecretAsset -ExportCsv` via `Set-OrchSecretAsset`). The on-disk CSV format is unchanged, so files exported here keep importing into `Set-OrchCredentialAsset`.

The -Name, -ValueType, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name and -ValueType completions are dynamically populated from actual data in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Assets

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1: Get all assets in the current folder

```powershell
PS Orch1:\Shared> Get-OrchAsset
```

Gets all assets from the current folder.

### Example 2: Get assets recursively with a name filter

```powershell
PS Orch1:\> Get-OrchAsset -Recurse *Config*
```

Gets assets containing "Config" in their name from all folders.

### Example 3: Get credential assets from all folders

```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ValueType Credential
```

Gets only credential-type assets from all folders. Valid values for -ValueType are Text, Bool, Integer, and Credential.

### Example 4: Get an asset from a specific folder

```powershell
PS C:\> Get-OrchAsset -Path Orch1:\Production DatabaseConnection
```

Gets the asset named "DatabaseConnection" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Expand per-robot asset values

```powershell
PS Orch1:\Shared> Get-OrchAsset -ExpandUserValues | Where-Object ValueScope -eq PerRobot
```

Gets per-robot assets and expands their user-specific values, showing actual values assigned to each user/machine combination.

### Example 6: Export assets to CSV

```powershell
PS Orch1:\Shared> Get-OrchAsset -ExportCsv C:\temp\assets.csv
```

Exports all assets in the current folder to a CSV file. The CSV uses UTF-8 with BOM encoding for Excel compatibility and converts internal IDs to human-readable names. The exported CSV can be used with Set-OrchAsset for import.

### Example 7 (deprecated): Export credential assets to CSV

```powershell
PS Orch1:\Shared> Get-OrchAsset -ExportCredentialCsv C:\temp\credentials.csv
```

Exports credential assets to a CSV file including credential store, username, and machine assignment columns. The exported CSV can be used with Set-OrchCredentialAsset for import. This parameter is deprecated; use `Get-OrchCredentialAsset -ExportCsv C:\temp\credentials.csv` instead, which produces the same CSV.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

Includes the target folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExpandUserValues

Expands user-specific values for per-robot assets, showing actual values assigned to each user/machine combination instead of a single summary row. Use this when you need Global and PerRobot entries flattened into one uniform stream.

Alternative for PerRobot-only access: pipe the Asset object and use `$_.UserValues` — this is the PowerShell-native pattern when the Global row is not needed. Example: `Get-OrchAsset Foo | ForEach-Object UserValues`.

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

### -ExportCredentialCsv

**Deprecated.** Use `Get-OrchCredentialAsset -ExportCsv` instead; the on-disk CSV format is unchanged, so exported files keep importing into `Set-OrchCredentialAsset`. Exports credential assets to the specified CSV file path. The CSV includes credential store name (resolved from CredentialStoreId), username, and machine assignment columns. Requires a filesystem path (not an Orch: drive path).

```yaml
Type: System.String
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

### -ExportCsv

Exports assets to the specified CSV file path. The CSV automatically converts internal IDs to human-readable names. Can be used with Set-OrchAsset for import. Requires a filesystem path (not an Orch: drive path).

```yaml
Type: System.String
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

### -Name

Specifies the names of assets to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests asset names from the target folders.

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

### -ValueType

Filters assets by value type. Valid values are Text, Bool, Integer, and Credential. Tab completion dynamically suggests only the value types that exist in the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe asset names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns asset objects with properties including Name, ValueType, ValueScope, StringValue, BoolValue, IntValue, CredentialUsername, CredentialStoreId, and FolderPath. When -ExpandUserValues is specified, per-robot assets are returned as AssetUserValue objects instead, with additional UserName and MachineName properties.

## NOTES

Assets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Credential assets reference a CredentialStore via CredentialStoreId. Use Get-OrchCredentialStore to list available credential stores and their IDs.

The -ExportCsv parameter exports non-credential assets with columns: Path, Name, Description, ValueType, Value, UserName, MachineName. The -ExportCredentialCsv parameter exports credential assets with columns: Path, Name, Description, CredentialStore, UserName, MachineName, CredentialUsername, CredentialPassword, ExternalName. Both require a filesystem path (e.g., C:\temp\file.csv).

## RELATED LINKS

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)

[Remove-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md)
