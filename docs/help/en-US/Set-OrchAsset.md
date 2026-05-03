---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchAsset
---

# Set-OrchAsset

## SYNOPSIS

Creates, updates, and removes non-credential assets.

## SYNTAX

### DefaultParameterSet (Default)

```
Set-OrchAsset [[-ValueType] <string>] [-Name] <string[]> [[-Value] <string>]
 [[-UserName] <string[]>] [[-MachineName] <string[]>] [-Description <string>] [-Path <string[]>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates, updates, and removes non-credential assets in UiPath Orchestrator folders. This cmdlet handles Text, Bool, and Integer asset types. For credential assets, use Set-OrchCredentialAsset instead. If -ValueType is set to Credential, the command is silently ignored.

The cmdlet determines the operation automatically based on the asset's existence and the -Value parameter:

- **Create**: When -Name specifies a name that does not exist in the folder, a new asset is created. -ValueType defaults to Text if not specified.
- **Update**: When -Name matches an existing asset, the asset's value and/or description are updated. If the specified value is identical to the current value, no update is performed.
- **Remove**: When -Value is set to an empty string (`''`) on a global asset, the asset is removed. When -Value is `''` with -UserName specified, only the per-robot value for that user is removed.

Assets can be scoped as Global (same value for all users) or PerRobot (different values per user/machine combination). Use -UserName and -MachineName to set per-robot values. If -MachineName is specified without -UserName, it is ignored with a warning.

The -Name parameter supports wildcards to update multiple assets at once. When a wildcard pattern matches no existing assets and contains wildcard characters, no action is taken (no new asset is created). When -Name is a literal name that does not exist, a new asset is created.

All parameters support pipeline input by property name (ValueFromPipelineByPropertyName). This allows importing assets from CSV files exported by Get-OrchAsset -ExportCsv.

The -Name, -ValueType, -Value, -UserName, -MachineName, -Description, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion lists existing assets and suggests 'New asset name here' for creating new assets. The -Value completion shows the current value of the asset specified by -Name. The -UserName completion lists users assigned to the folder (excluding directory groups). The -MachineName completion lists machines assigned to the folder. The -Description completion shows the current description or suggests 'Description here'.

Primary Endpoint: POST /odata/Assets, PUT /odata/Assets({asset.Id}), DELETE /odata/Assets({assetId})

OAuth required scopes: OR.Assets

Required permissions: Assets.Create, Assets.Edit, Assets.Delete

## EXAMPLES

### Example 1: Create a new text asset

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name MyConfig -Value 'https://api.example.com'
```

Creates a new text asset named "MyConfig" with the specified value. When -ValueType is omitted, it defaults to Text.

### Example 2: Preview asset creation with -WhatIf

```powershell
PS Orch1:\Shared> Set-OrchAsset -ValueType Integer -Name RetryCount -Value 3 -Description 'Max retry attempts' -WhatIf
```

```output
What if: Performing the operation "Add Asset" on target "Orch1:\Shared\RetryCount".
```

Shows what would happen without executing the command. The -WhatIf output shows "Add Asset" for new assets, "Update Asset" for existing assets, and "Remove Asset" for deletions.

### Example 3: Update an existing asset value

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name TestAsset1 -Value NewValue
```

Updates the value of an existing asset. The -ValueType and -Description remain unchanged. No output is produced on update; output is only returned when creating a new asset.

### Example 4: Create a boolean asset

```powershell
PS Orch1:\Shared> Set-OrchAsset Bool MaintenanceMode False
```

Creates a boolean asset using positional parameters. Parameter positions are: 0=ValueType, 1=Name, 2=Value. For Bool assets, -Value must be "True" or "False" (case-insensitive).

### Example 5: Update multiple assets with wildcards

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name Test* -Value bulk-update
```

Updates all assets whose names match the wildcard pattern. If no assets match, no action is taken and no error is raised.

### Example 6: Set a per-robot asset value

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name AppConfig -Value robot-specific-value -UserName testrobot01
```

Sets a per-robot value for the specified user. The asset's ValueScope changes to PerRobot. When -MachineName is omitted, the value applies to the user regardless of machine.

### Example 7: Set a per-robot value with machine assignment

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name AppConfig -Value machine-specific -UserName testrobot01 -MachineName aiai
```

Sets a per-robot value for a specific user and machine combination. Both -UserName and -MachineName support wildcards and multiple comma-separated values.

### Example 8: Remove an asset

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name ObsoleteAsset -Value ''
```

Removes the asset by specifying an empty string as the value. For global assets, this deletes the entire asset. For per-robot assets with -UserName, only the specified user's value is removed.

### Example 9: Update only the description

```powershell
PS Orch1:\Shared> Set-OrchAsset -Name TestAsset1 -Description 'Updated description for the asset'
```

Updates only the description without changing the value. When -Value is not specified, the current value is preserved.

### Example 10: Create an asset in a specific folder

```powershell
PS C:\> Set-OrchAsset -Path Orch1:\Production -Name DatabaseUrl -Value 'Server=prod-db;Database=App'
```

Creates or updates an asset in the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 11: Import assets from CSV

```powershell
PS Orch1:\Shared> Import-Csv c:assets.csv | Set-OrchAsset
```

Imports assets from a CSV file exported by Get-OrchAsset -ExportCsv. The CSV columns (Path, Name, ValueType, Value, UserName, MachineName, Description) are bound to parameters via ValueFromPipelineByPropertyName.

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

### -Description

Specifies a description for the asset. Tab completion suggests the current description of the asset specified by -Name, or 'Description here' if no description exists. The completer excludes credential assets.

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
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MachineName

Specifies machine names for per-robot asset values. Used together with -UserName to assign values to specific user/machine combinations. If specified without -UserName, the machine names are ignored with a warning. Supports wildcards and multiple comma-separated values. Tab completion lists machines assigned to the target folder.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 4
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of assets to create or update. This is a mandatory parameter. If the name matches an existing asset, the asset is updated. If the name does not exist, a new asset is created. Supports wildcards for bulk updates; wildcard patterns that match no existing assets are skipped without error. Tab completion lists existing asset names in the target folder and suggests 'New asset name here'.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies usernames for per-robot asset values. When specified, the asset's ValueScope is set to PerRobot. Supports wildcards and multiple comma-separated values. Tab completion lists users assigned to the target folder, excluding directory groups.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Value

Specifies the value to assign to the asset. The value is always specified as a string regardless of the asset's ValueType. For Bool assets, use "True" or "False" (case-insensitive). For Integer assets, provide a numeric string. An empty string (`''`) removes the asset or the per-robot value. When -Value is not specified at all (null), only the -Description update is applied and the value is not changed. Tab completion suggests the current value of the asset specified by -Name.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ValueType

Specifies the type of asset to create. Valid values are Text, Integer, and Bool. Credential is not accepted; use Set-OrchCredentialAsset for credential assets. When creating a new asset without specifying -ValueType, it defaults to Text. When updating an existing asset, the existing ValueType is preserved. Tab completion suggests the three valid value types.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: DefaultParameterSet
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run. The output indicates the operation type: "Add Asset" for creation, "Update Asset" for modification, and "Remove Asset" for deletion.

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

You can pipe asset property values to this cmdlet via property names. All parameters accept ValueFromPipelineByPropertyName, enabling CSV import with Import-Csv.

### System.String[]

You can pipe multiple asset names via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset

Returns an Asset object only when creating a new asset. Update and remove operations produce no output. The returned object includes properties such as Name, ValueType, ValueScope, Value, Description, and Id.

## NOTES

This cmdlet handles Text, Bool, and Integer assets only. The -ValueType parameter does not accept Credential; if specified, the command is silently ignored. Use Set-OrchCredentialAsset for credential asset management.

The behavior of -Value differs depending on whether it is omitted or set to an empty string: omitting -Value preserves the current value (useful for description-only updates), while setting -Value to `''` triggers asset removal.

When processing multiple assets via pipeline input, the cmdlet batches all parameter sets in ProcessRecord and executes them in EndProcessing with a progress bar. This means errors for individual assets do not stop the pipeline.

## RELATED LINKS

Get-OrchAsset

Set-OrchCredentialAsset

Remove-OrchAsset

Copy-OrchAsset
