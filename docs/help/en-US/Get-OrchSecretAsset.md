---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSecretAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchSecretAsset
---

# Get-OrchSecretAsset

## SYNOPSIS

Gets Secret-type assets from UiPath Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchSecretAsset [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-ExpandUserValues] [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets Secret-type assets only. Text, Bool, Integer, and Credential assets are filtered out. Use Get-OrchAsset for all asset types or Get-OrchCredentialAsset for Credential assets.

Secret assets are a v20+ feature that stores opaque secret values (as opposed to Credential's username+password pair). The server never returns the secret value itself (always masked), so the SecretValue and Value fields are always empty. Use HasDefaultValue to detect whether a Global value is configured.

The -ExportCsv parameter writes the results as CSV with columns suitable for round-trip editing and re-import with Set-OrchSecretAsset. The CSV column layout is: Path, Name, Description, CredentialStore, UserName, MachineName, SecretValue, ExternalName. The SecretValue column is always empty on export because the API masks it.

For PerRobot-only access, pipe the Asset objects and use `$_.UserValues`. For a flattened stream including the Global row, use -ExpandUserValues.

The -Name and -Path parameters support tab completion. The -Name completion is dynamically populated from Secret-type assets in the target folders.

Primary Endpoint: GET /odata/Assets/UiPath.Server.Configuration.OData.GetFiltered (Secret assets are only returned by the filtered endpoint on v20+ servers)

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1: Get all Secret assets in the current folder

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset
```

Returns all Secret assets. Non-Secret assets are excluded.

### Example 2: Filter by wildcard name

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -Name 'ApiKey*'
```

Returns Secret assets whose names start with 'ApiKey'.

### Example 3: Access PerRobot UserValues via $_.UserValues

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -Name ApiKey | ForEach-Object UserValues
```

Returns only the PerRobot rows. The PowerShell-native alternative to -ExpandUserValues when the Global row is not needed.

### Example 4: Export to CSV for editing

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -ExportCsv C:\temp\secrets.csv
```

Exports Secret assets to CSV. The SecretValue column is empty by design. Users fill in the SecretValue column locally and re-import with Import-Csv | Set-OrchSecretAsset.

### Example 5: Round-trip safe Description update

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -ExportCsv C:\temp\secrets.csv
PS Orch1:\Shared> Import-Csv C:\temp\secrets.csv | Set-OrchSecretAsset
```

Export, edit descriptions in the CSV externally, and pipe back to Set-OrchSecretAsset. Empty SecretValue cells are preserved silently (the existing secret is not clobbered or removed). To remove a UserValue, use Remove-OrchAssetUserValue.

### Example 6: Expand Global and PerRobot into a single row stream

```powershell
PS Orch1:\Shared> Get-OrchSecretAsset -Name ApiKey -ExpandUserValues
```

Flattens Global and PerRobot values into a single stream of AssetUserValue rows. The Global row is detected via HasDefaultValue (since the Value field is always empty for Secret).

### Example 7: Search recursively from a drive root

```powershell
PS Orch1:\> Get-OrchSecretAsset -Path Orch1:\ -Recurse
```

Recursively lists Secret assets across all folders accessible from the drive root.

## PARAMETERS

### -CsvEncoding

Encoding for the exported CSV file. Defaults to UTF-8 with BOM for Excel compatibility.

```yaml
Type: System.Text.Encoding
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

Depth limit for -Recurse. 0 means unlimited.

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

### -ExpandUserValues

Expands user-specific values for PerRobot Secret assets, showing each user/machine assignment as a separate AssetUserValue row. Use this when you need Global and PerRobot entries flattened into one uniform stream. The Global row is detected via HasDefaultValue because the Secret value itself is always masked.

Alternative for PerRobot-only access: pipe the Asset object and use `$_.UserValues` — this is the PowerShell-native pattern when the Global row is not needed. Example: `Get-OrchSecretAsset Foo | ForEach-Object UserValues`.

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

### -ExportCsv

Filesystem path to export Secret assets as CSV. Columns: Path, Name, Description, CredentialStore, UserName, MachineName, SecretValue, ExternalName. The SecretValue column is always empty on export because the server never returns the stored secret.

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

### -Name

Name of the Secret asset to retrieve. Wildcards are supported. Multiple values may be specified comma-separated.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Path

Target folder path(s) on an Orch drive. When omitted, the current folder is used. Wildcards are supported.

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

Returns Secret asset objects. When -ExpandUserValues is specified, PerRobot entries are flattened into AssetUserValue objects alongside the Global row.

## NOTES

Secret assets require UiPath Orchestrator v20+. On older servers, the ValueType 'Secret' is not supported and this cmdlet returns no rows.

The secret value itself is never returned by the API. The SecretValue column in CSV exports is always empty. Use HasDefaultValue to detect whether a Global secret is configured.

To remove a PerRobot UserValue from a Secret asset, use Remove-OrchAssetUserValue. The empty-delete convention used by Set-OrchAsset/Set-OrchCredentialAsset does not apply to Secret because the masked SecretValue breaks round-trip semantics — an empty -SecretValue is silently skipped to protect the existing secret.

## RELATED LINKS

[Set-OrchSecretAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchSecretAsset.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Get-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialAsset.md)

[Remove-OrchAssetUserValue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetUserValue.md)
