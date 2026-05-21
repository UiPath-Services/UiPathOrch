---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchLibrary.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchLibrary
---

# Import-OrchLibrary

## SYNOPSIS

Imports library packages into UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Import-OrchLibrary [[-Path] <string[]>] [-Source] <string[]> [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Imports library packages (.nupkg files containing reusable workflows) into the UiPath Orchestrator tenant feed. The -Source parameter accepts file paths or wildcard patterns to specify one or more .nupkg files to upload. If a library with the same Id and version already exists in the target tenant, the import is skipped and an error is written.

Progress is displayed during multi-file imports. The cmdlet supports -WhatIf and -Confirm for safe operation.

Primary Endpoint: POST /odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution

Required permissions: Libraries.Create

## EXAMPLES

### Example 1: Import a single library package

```powershell
PS Orch1:\> Import-OrchLibrary C:\Packages\MyShared.Utilities.1.0.0.nupkg
```

Imports the specified .nupkg file into the tenant feed of the current Orchestrator drive. The -Source parameter is positional (position 0), so the parameter name can be omitted.

### Example 2: Import multiple library packages using wildcards

```powershell
PS Orch1:\> Import-OrchLibrary C:\Packages\MyShared.*.nupkg
```

Imports all .nupkg files matching "MyShared.*" from the C:\Packages directory into the tenant feed. Files are processed in version order.

### Example 3: Import libraries to a specific Orchestrator instance

```powershell
PS C:\> Import-OrchLibrary .\Libraries\*.nupkg Orch2:\
```

Imports all .nupkg files from the .\Libraries directory into the Orch2 tenant. The -Path parameter (position 1) specifies the target drive.

### Example 4: Preview the import with WhatIf

```powershell
PS Orch1:\> Import-OrchLibrary C:\Packages\*.nupkg -WhatIf
```

Shows what library packages would be imported without actually performing the upload. Use this to verify which files will be processed before committing the operation.

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
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Source

Specifies the path to the .nupkg file(s) to import. Supports wildcards and multiple values. The path is resolved relative to the current filesystem location.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

You can pipe source file paths to this cmdlet via the Source property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString

Returns upload result objects for each successfully imported library package.

## NOTES

If a library with the same Id and version already exists in the target tenant feed, the import is skipped and an error is written. To update an existing library, first remove the old version with `Remove-OrchLibrary`, then import the new package.

## RELATED LINKS

[Get-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibrary.md)

[Export-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchLibrary.md)

[Remove-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchLibrary.md)

[Copy-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchLibrary.md)
