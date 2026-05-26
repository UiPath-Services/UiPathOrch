---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchPackage.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchPackage
---

# Remove-OrchPackage

## SYNOPSIS

Removes the process packages.

## SYNTAX

### __AllParameterSets

```
Remove-OrchPackage [-Path <string[]>] [-Recurse] [-Id] <string[]> [[-Version] <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes process packages (NuGet packages containing automation processes) from UiPath Orchestrator folder feeds. Individual versions can be removed by specifying -Version, or all versions of a package can be removed at once.

The -Id and -Version parameters support wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id completion lists package IDs from the target folders, and the -Version completion lists versions for the selected packages. The -Path parameter also supports tab completion, listing available folder feed paths.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Use -WhatIf to preview which package versions would be removed before executing the operation.

Primary Endpoint: DELETE /odata/Processes('{processId}:{processVersion}')?feedId={feedId}

OAuth required scopes: OR.Execution

Required permissions: (Packages.Delete - Deletes a package in a Tenant Feed) and (FolderPackages.Delete - Deletes a package in a Folder Feed)

## EXAMPLES

### Example 1: Remove all versions of a specific package

```powershell
PS Orch1:\Shared> Remove-OrchPackage MyProcess
```

Removes all versions of the package "MyProcess" from the current folder. Because -Id is at position 0, the parameter name can be omitted.

### Example 2: Remove a specific version of a package

```powershell
PS Orch1:\Shared> Remove-OrchPackage MyProcess 1.0.0
```

Removes only version 1.0.0 of "MyProcess" from the current folder. Both -Id and -Version are specified positionally.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchPackage *Test* -WhatIf
```

Shows which package versions would be removed without executing the operation. Useful for verifying wildcard patterns before deletion.

### Example 4: Remove packages from a specific folder using -Path

```powershell
PS C:\> Remove-OrchPackage -Path Orch1:\Shared -Id BlankProcess19 -Version 1.0.*
```

Removes all 1.0.x versions of "BlankProcess19" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Bulk removal of a hand-picked set of versions via CSV

```powershell
PS Orch1:\Shared> Get-OrchPackageVersion MyProcess | Select-Object Id,Version | Export-Csv c:RemovePackages.csv
```

Enumerate the package versions in the current folder feed and export them to a CSV. Because the current location is the Orch1: drive, qualify the file with a filesystem drive — `c:RemovePackages.csv` writes to the current directory of the C: drive; a bare path would resolve against the Orchestrator drive, which cannot store files. Open the file in its associated editor, keep only the rows you want to delete, then pipe the curated file back into the cmdlet:

```powershell
PS Orch1:\Shared> c:RemovePackages.csv   # Press Tab to expand to the absolute path
```

```powershell
PS Orch1:\Shared> Import-Csv c:RemovePackages.csv | Remove-OrchPackage -WhatIf
```

The Id and Version columns bind to the parameters of the same name, so each row removes one specific package version. Use this for an arbitrary, hand-picked set of (Id, Version) pairs that a single wildcard cannot express — for example, retaining different versions for different packages.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders. Tab completion lists available folder feed paths.

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

### -Id

Specifies the Id of the process packages to remove. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Version

Specifies the version of the process packages to remove. If not specified, all versions of the matching packages are removed. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests versions for the selected package IDs.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
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

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe package IDs and versions to this cmdlet via the Id and Version properties.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

Packages are folder-scoped entities stored in folder feeds. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Removing a package version that is currently linked to a process may cause errors when the process attempts to run. Ensure that no active processes reference the package version before removal.

The internal package and version caches are automatically invalidated after each successful removal.

## RELATED LINKS

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)

[Get-OrchPackageVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md)

[Import-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchPackage.md)

[Export-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchPackage.md)

[Copy-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchPackage.md)
