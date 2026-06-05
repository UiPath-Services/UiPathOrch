---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAssetLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchAssetLink
---

# Get-OrchAssetLink

## SYNOPSIS

Gets the folder links of assets.

## SYNTAX

### __AllParameterSets

```
Get-OrchAssetLink [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [-CsvEncoding <Encoding>] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the folder links of assets in UiPath Orchestrator. Asset links allow a global-scope asset defined in one folder to be shared with other folders without duplicating the asset. This cmdlet shows which folders each asset is accessible from.

Only assets that are linked to multiple folders (more than one accessible folder) are included in the output. Assets accessible from only their owning folder are not displayed.

The output is grouped by asset name and shows all folders that have access to each linked asset. Each output object is a SimpleFolder representing one accessible folder, with Path and PathName properties indicating the source asset.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

The -Name and -Path parameters support tab completion. The -Name completion dynamically lists assets from the target folders.

Primary Endpoint: GET /odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={assetId})

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1: Get all asset links in the current folder

```powershell
PS Orch1:\Shared> Get-OrchAssetLink
```

```output
Name: Orch1:\Shared\DatabaseConnection

     Id DisplayName FullyQualifiedName
     -- ----------- ------------------
1005605 Dept#2      Dept#2
1005606 fuga        Dept#2/fuga
1095993 Development Development
1095994 Finance     Finance
1096003 Production  Production
1005610 root        root
1005611 Shared      Shared

   Name: Orch1:\Shared\TestAsset2024

     Id DisplayName FullyQualifiedName
     -- ----------- ------------------
1095993 Development Development
1005611 Shared      Shared
```

Gets all assets in the Shared folder that are linked to multiple folders. The output is grouped by asset name, showing which folders each asset is accessible from.

### Example 2: Get links for a specific asset

```powershell
PS Orch1:\Shared> Get-OrchAssetLink DatabaseConnection
```

Gets the folder links for the "DatabaseConnection" asset. Shows all folders that can access this asset.

### Example 3: Get asset links from a specific folder

```powershell
PS C:\> Get-OrchAssetLink -Path Orch1:\Shared Test*
```

Gets asset links for assets matching "Test*" in the Shared folder. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Get asset links recursively

```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse
```

Gets all asset links from all folders. This shows the complete asset sharing topology across the Orchestrator instance.

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

### -Name

Specifies the names of assets whose links are to be retrieved. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists assets from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

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

### -ExportCsv

Exports asset links to the specified CSV file path. Columns: Path (source folder), Name (asset), Link (linked-to folder) — matching Add-/Remove-OrchAssetLink so `Import-Csv | Add-OrchAssetLink` round-trips. Requires a filesystem path (not an Orch: drive path).

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

## INPUTS

### System.String[]

You can pipe asset names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.EntityLink

Returns EntityLink objects — one per (asset, linked folder) pair. Each carries Path (source folder), Name (asset name), Link (the linked-to folder), Id (asset id), FolderId (source), and LinkFolderId (target). Path/Name/Link pipe by name into Add-/Remove-OrchAssetLink.

## NOTES

Only assets linked to multiple folders appear in the output. An asset accessible from only its owning folder is not considered "linked" and is omitted.

Asset linking is an Orchestrator feature that allows global-scope assets to be shared across folders without duplication. Per-robot assets (ValueScope = PerRobot) cannot be linked.

When the same asset is discovered from multiple source folders (e.g., with -Recurse), duplicate link groups are suppressed — each unique set of linked folders is shown only once.

## RELATED LINKS

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)
