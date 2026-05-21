---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchQueueLink
---

# Get-OrchQueueLink

## SYNOPSIS

Gets the folder links of queues.

## SYNTAX

### __AllParameterSets

```
Get-OrchQueueLink [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the folder links of queues in UiPath Orchestrator. Queue linking allows a queue defined in one folder to be shared with other folders. This cmdlet shows which folders each queue is accessible from.

Only queues that are linked to multiple folders (more than one accessible folder) are included in the output. Queues accessible from only their owning folder are not displayed.

The output is grouped by queue name and shows all folders that have access to each linked queue.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

The -Name and -Path parameters support tab completion. The -Name completion dynamically lists queues from the target folders.

Primary Endpoint: GET /odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetFoldersForQueue(id={queueId})

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View

## EXAMPLES

### Example 1: Get all queue links in the current folder

```powershell
PS Orch1:\Shared> Get-OrchQueueLink
```

Gets all queues in the Shared folder that are linked to multiple folders. The output is grouped by queue name, showing which folders each queue is accessible from.

### Example 2: Get links for a specific queue

```powershell
PS Orch1:\Shared> Get-OrchQueueLink InvoiceQueue
```

Gets the folder links for the "InvoiceQueue" queue. Shows all folders that can access this queue.

### Example 3: Get queue links from a specific folder

```powershell
PS C:\> Get-OrchQueueLink -Path Orch1:\Shared Test*
```

Gets queue links for queues matching "Test*" in the Shared folder. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Get queue links recursively

```powershell
PS Orch1:\> Get-OrchQueueLink -Recurse
```

Gets all queue links from all folders. This shows the complete queue sharing topology across the Orchestrator instance.

## PARAMETERS

### -Name

Specifies the names of queues whose links are to be retrieved. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists queues from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueLink

Returns QueueLink objects describing which folders have access to each linked queue. Use Get-Member on the output for the full property surface.

## NOTES

Only queues linked to multiple folders appear in the output. A queue accessible from only its owning folder is not considered "linked" and is omitted.

When the same queue is discovered from multiple source folders (e.g., with -Recurse), duplicate link groups are suppressed — each unique set of linked folders is shown only once.

## RELATED LINKS

[Add-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchQueueLink.md)

[Remove-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueLink.md)

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Get-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAssetLink.md)
