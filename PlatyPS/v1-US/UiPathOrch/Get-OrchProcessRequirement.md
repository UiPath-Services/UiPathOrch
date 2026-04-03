---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProcessRequirement
---

# Get-OrchProcessRequirement

## SYNOPSIS

Gets resource requirements for processes in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchProcessRequirement [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the resource requirements (queues, assets, etc.) defined in the process packages. This cmdlet inspects the package metadata to identify external resources that the process depends on, and validates whether those resources exist in the target folder.

The output includes the process name, resource type (e.g., Queue, Asset), resource name, an optional comment, the validation result (e.g., Success, Error), and the folder where the process resides. This is useful for verifying that all required resources are properly configured before running a process.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual process names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Releases/UiPath.Server.Configuration.OData.GetPackageResources

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View

## EXAMPLES

### Example 1: Get requirements for all processes in the current folder

```powershell
PS Orch1:\Shared> Get-OrchProcessRequirement
```

```output
Directory: Orch1:\Shared

Process        ResourceType ResourceName Comment ValidationResult
-------        ------------ ------------ ------- ----------------
BlankProcess27 Queue        TestQueue2           Success
```

Gets the resource requirements for all processes in the current folder.

### Example 2: Get requirements for a specific process

```powershell
PS Orch1:\Shared> Get-OrchProcessRequirement InvoiceProcess
```

Gets the resource requirements for the process named "InvoiceProcess" in the current folder.

### Example 3: Get requirements recursively across all folders

```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse
```

Gets the resource requirements for all processes in all folders. This is useful for a full audit of resource dependencies across the Orchestrator instance.

### Example 4: Get requirements from a specific folder

```powershell
PS C:\> Get-OrchProcessRequirement -Path Orch1:\Production *Invoice*
```

Gets the resource requirements for processes matching "*Invoice*" in the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Find processes with missing resources

```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse | Where-Object ValidationResult -ne Success
```

Gets all process requirements across all folders and filters to show only resources that failed validation (e.g., missing queues or assets).

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the names of the processes to inspect for resource requirements. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe folder paths to this cmdlet via the Path property.

### System.String[]

You can pipe process names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.SubtypedPackageResource

Returns SubtypedPackageResource objects with properties including Release (process name), ResourceType, ResourceName, Comment, ValidationResult, and FolderFullyQualifiedName.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The resource requirements are defined in the package metadata at design time. The ValidationResult indicates whether the required resource exists in the target folder. A "Success" result means the resource is available; other results indicate missing or misconfigured resources.

Processes that have no resource requirements defined produce no output.

## RELATED LINKS

Get-OrchProcess

Get-OrchAsset

Get-OrchQueue
