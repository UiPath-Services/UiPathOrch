---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchLibrary
---

# Remove-OrchLibrary

## SYNOPSIS

Removes library packages from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchLibrary [-Id] <string[]> [[-Version] <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes library packages (NuGet packages containing reusable workflows) from the UiPath Orchestrator tenant feed. The cmdlet deletes specific versions of libraries. Both -Id and -Version parameters support wildcards, allowing bulk removal of multiple versions or multiple libraries in a single operation.

When -Version is not specified, all versions of the matching libraries are removed. The cmdlet supports -WhatIf and -Confirm for safe operation.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the target tenant. The -Version completer filters based on the currently specified -Id value.

Primary Endpoint: DELETE /odata/Libraries('{libraryId}:{libraryVersion}')

OAuth required scopes: OR.Execution

Required permissions: Libraries.Delete

## EXAMPLES

### Example 1: Remove all versions of a library

```powershell
PS Orch1:\> Remove-OrchLibrary MyShared.Utilities
```

Removes all versions of the library "MyShared.Utilities" from the tenant feed. The -Id parameter is positional (position 0) and mandatory. Since -Version is not specified, all versions are removed.

### Example 2: Remove a specific version of a library

```powershell
PS Orch1:\> Remove-OrchLibrary MyShared.Utilities 1.0.0
```

Removes version 1.0.0 of the library "MyShared.Utilities" from the tenant feed. Both -Id (position 0) and -Version (position 1) are positional.

### Example 3: Remove libraries using wildcards

```powershell
PS Orch1:\> Remove-OrchLibrary MyShared.* -Confirm
```

Removes all versions of all libraries whose Id starts with "MyShared." from the tenant feed. The -Confirm parameter prompts for confirmation before each deletion.

### Example 4: Preview removal with WhatIf

```powershell
PS Orch1:\> Remove-OrchLibrary * 1.0.0-beta* -WhatIf
```

Shows what would happen if all pre-release versions matching "1.0.0-beta*" were removed across all libraries, without actually deleting anything.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. Use tab completion to see available drives.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Id

Specifies the Id of the library packages to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the target tenant. This parameter is mandatory.

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

Specifies the version of the library packages to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests available versions based on the specified -Id. If not specified, all versions of the matching libraries are removed.

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

You can pipe library IDs and versions to this cmdlet via the Id and Version properties.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This operation is irreversible. Use -WhatIf to preview which library versions would be removed before executing the command. Consider using `Export-OrchLibrary` to back up library packages before deletion.

## RELATED LINKS

Get-OrchLibrary

Get-OrchLibraryVersion

Import-OrchLibrary

Export-OrchLibrary

Copy-OrchLibrary
