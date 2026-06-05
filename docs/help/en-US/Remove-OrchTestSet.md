---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchTestSet
---

# Remove-OrchTestSet

## SYNOPSIS

Removes test sets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchTestSet [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes (deletes) test sets from UiPath Orchestrator folders. Removing a test set permanently deletes the test set definition and its association with test cases.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which test sets would be removed, or -Confirm to be prompted before each removal.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual test set names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE /odata/TestSets({testSetId})

OAuth required scopes: OR.TestSets

Required permissions: TestSets.Delete

## EXAMPLES

### Example 1: Remove a test set by name

```powershell
PS Orch1:\Shared> Remove-OrchTestSet MyTestSet
```

Removes the test set named "MyTestSet" from the current folder.

### Example 2: Remove test sets using a wildcard

```powershell
PS Orch1:\Shared> Remove-OrchTestSet *Legacy*
```

Removes all test sets matching the wildcard pattern "*Legacy*" from the current folder.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchTestSet *Old* -WhatIf
```

```output
What if: Performing the operation "Remove TestSet" on target "OldSmokeTests [Shared]".
```

Shows which test sets would be removed without actually removing them.

### Example 4: Remove a test set from a specific folder

```powershell
PS C:\> Remove-OrchTestSet -Path Orch1:\Shared ObsoleteTests
```

Removes the test set named "ObsoleteTests" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

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

Specifies the names of the test sets to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests test set names from the target folders.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

You can pipe test set names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

Test sets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

## RELATED LINKS

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)

[Start-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchTestSet.md)

[Copy-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSet.md)
