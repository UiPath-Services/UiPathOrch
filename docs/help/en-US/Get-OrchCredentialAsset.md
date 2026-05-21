---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchCredentialAsset
---

# Get-OrchCredentialAsset

## SYNOPSIS

Gets credential-type assets from UiPath Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchCredentialAsset [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-CsvEncoding <Encoding>] [-ExpandUserValues] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets credential-type assets only. Text, Bool, Integer, and Secret assets are filtered out. Use Get-OrchAsset for all asset types or Get-OrchSecretAsset for Secret assets.

The cmdlet returns Asset objects. The CredentialUsername field is populated from the server, but CredentialPassword is always masked (empty). When a credential is backed by an external credential store, the ExternalName field contains the vault reference.

The -ExportCsv parameter writes the results as CSV with columns suitable for round-trip editing and re-import with Set-OrchCredentialAsset. The CSV uses the same column layout as Get-OrchAsset -ExportCredentialCsv.

For PerRobot-only access, pipe the Asset objects and use `$_.UserValues`. For a flattened stream including the Global row, use -ExpandUserValues.

The -Name and -Path parameters support tab completion. The -Name completion is dynamically populated from credential assets in the target folders.

Primary Endpoint: GET /odata/Assets, GET /odata/Assets/UiPath.Server.Configuration.OData.GetFiltered

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1: Get all credential assets in the current folder

```powershell
PS Orch1:\Shared> Get-OrchCredentialAsset
```

Returns all credential assets in the current folder. Non-credential assets are excluded.

### Example 2: Filter by wildcard name

```powershell
PS Orch1:\Shared> Get-OrchCredentialAsset -Name 'Api*'
```

Returns credential assets whose names start with 'Api'.

### Example 3: Access PerRobot UserValues via $_.UserValues

```powershell
PS Orch1:\Shared> Get-OrchCredentialAsset -Name ApiCredential | ForEach-Object UserValues
```

Returns only the PerRobot rows for the named asset. The PowerShell-native alternative to -ExpandUserValues when the Global row is not needed.

### Example 4: Export to CSV and re-import

```powershell
PS Orch1:\Shared> Get-OrchCredentialAsset -ExportCsv C:\temp\creds.csv
PS Orch1:\Shared> Import-Csv C:\temp\creds.csv | Set-OrchCredentialAsset
```

Exports credential assets to CSV, edits them externally, and pipes the CSV rows back to Set-OrchCredentialAsset. Passwords are masked in the export; to preserve the existing password on import, leave the CredentialPassword column empty. To change only the Description without changing the username or password, leave CredentialUsername unchanged from the exported value.

### Example 5: Expand Global and PerRobot into a single row stream

```powershell
PS Orch1:\Shared> Get-OrchCredentialAsset -Name ApiCredential -ExpandUserValues
```

Flattens Global and PerRobot values into AssetUserValue rows. Use this when downstream processing needs a single uniform stream.

### Example 6: Search recursively from a drive root

```powershell
PS Orch1:\> Get-OrchCredentialAsset -Path Orch1:\ -Recurse
```

Recursively lists credential assets across all folders accessible from the drive root.

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

Expands user-specific values for PerRobot credential assets, showing each user/machine assignment as a separate AssetUserValue row. Use this when you need Global and PerRobot entries flattened into one uniform stream.

Alternative for PerRobot-only access: pipe the Asset object and use `$_.UserValues` — this is the PowerShell-native pattern when the Global row is not needed. Example: `Get-OrchCredentialAsset Foo | ForEach-Object UserValues`.

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

Filesystem path to export credential assets as CSV. Columns: Path, Name, Description, CredentialStore, UserName, MachineName, CredentialUsername, CredentialPassword, ExternalName. The CredentialPassword column is always masked (empty) because the API never returns the stored password.

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

Name of the credential asset to retrieve. Wildcards are supported. Multiple values may be specified comma-separated.

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

Name of the credential asset.

### System.String[]

Array of credential asset names.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns credential asset objects. When -ExpandUserValues is specified, PerRobot entries are flattened into AssetUserValue objects alongside the Global row.

## NOTES

Credential assets reference a CredentialStore via CredentialStoreId. Use Get-OrchCredentialStore to list available credential stores.

CredentialPassword is never returned by the API. The CSV export includes the column for round-trip purposes, but it is always empty. To modify a password, supply -CredentialPassword explicitly to Set-OrchCredentialAsset.

Server limitation: re-importing a CSV that changes CredentialUsername without supplying a non-empty CredentialPassword is rejected by the server ("Credential asset cannot have an empty password for a new user"). For username-change round-trips, populate the password column before re-import.

## RELATED LINKS

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Get-OrchSecretAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSecretAsset.md)

[Remove-OrchAssetUserValue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetUserValue.md)
