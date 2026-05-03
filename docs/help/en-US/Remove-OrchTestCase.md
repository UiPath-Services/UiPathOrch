---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchTestCase
---

# Remove-OrchTestCase

## SYNOPSIS

Removes test case definitions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchTestCase [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes (deletes) test case definitions from UiPath Orchestrator folders. The cmdlet first retrieves test cases matching the specified -Name pattern from the target folders, then calls the BulkDelete endpoint to remove each matching test case.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which test cases would be removed, or -Confirm to be prompted before each removal.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual test case names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TestCaseDefinitions/UiPath.Server.Configuration.OData.BulkDelete

OAuth required scopes: OR.TestSets

Required permissions: TestSets.Delete

## EXAMPLES

### Example 1: Remove a test case by name

```powershell
PS Orch1:\Shared> Remove-OrchTestCase LoginTest
```

Removes the test case definition named "LoginTest" from the current folder.

### Example 2: Remove test cases using a wildcard

```powershell
PS Orch1:\Shared> Remove-OrchTestCase *Deprecated*
```

Removes all test case definitions matching the wildcard pattern "*Deprecated*" from the current folder.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchTestCase *Old* -WhatIf
```

```output
What if: Performing the operation "Remove TestCase" on target "OldLoginTest [Shared]".
```

Shows which test cases would be removed without actually removing them.

### Example 4: Remove a test case from a specific folder

```powershell
PS C:\> Remove-OrchTestCase -Path Orch1:\QA LoginTest
```

Removes the test case definition named "LoginTest" from the QA folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Remove test cases recursively with confirmation

```powershell
PS Orch1:\> Remove-OrchTestCase -Recurse *Legacy* -Confirm
```

Removes all test case definitions matching "*Legacy*" from all folders, prompting for confirmation before each removal.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

### -Depth

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the Name of the test cases to be removed.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test case names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

Test cases are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspaces are excluded from enumeration.

Removing a test case definition does not delete the underlying package from the Orchestrator feed. The package remains available for republishing.

## RELATED LINKS

[Get-OrchTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestCase.md)

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)

[Remove-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSet.md)
