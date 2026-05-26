---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchQueueLink
---

# Remove-OrchQueueLink

## SYNOPSIS

Removes folder links from queues.

## SYNTAX

### __AllParameterSets

```
Remove-OrchQueueLink [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Link] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes folder links from queues in UiPath Orchestrator. The opposite of Add-OrchQueueLink: detaches the specified destination folders from the queue so they can no longer read or write items on it. The owning folder always retains access; this cmdlet only removes shared-access entries.

The -Name parameter specifies which queues to unlink, and the -Link parameter specifies the folders to remove from each queue's link list. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name, -Path, and -Link parameters support tab completion. The -Name completion dynamically lists queues from the source folder(s). The -Link completion lists the folders currently linked to each queue.

Use -Recurse (optionally with -Depth) to walk subfolders of -Path.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.ShareToFolders (same endpoint as Add-OrchQueueLink; the two are distinguished by the request body)

OAuth required scopes: OR.Queues or OR.Queues.Write

Required permissions: Queues.Edit

## EXAMPLES

### Example 1: Remove a queue link

```powershell
PS Orch1:\Shared> Remove-OrchQueueLink -Name InvoiceQueue -Link Orch1:\Dept#2
```

Removes the Dept#2 folder from "InvoiceQueue"'s link list. Dept#2 no longer has access to the queue; Shared (the owning folder) is unaffected.

### Example 2: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchQueueLink -Name InvoiceQueue -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Remove QueueLink ✗ Orch1:\Dept#2" on target "Orch1:\Shared\InvoiceQueue".
```

Shows what would happen without executing.

### Example 3: Remove multiple links across multiple queues

```powershell
PS Orch1:\Shared> Remove-OrchQueueLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Removes both the Dept#2 and Dept#3 folder links from every queue matching "Test*".

### Example 4: Recursively unlink across subfolders

```powershell
PS C:\> Remove-OrchQueueLink -Path Orch1:\Depts\* -Depth 2 -Name SharedQueue -Link Orch1:\Common
```

Walks the subfolders of Orch1:\Depts up to 2 levels deep, finds queues named "SharedQueue", and removes the Common folder from each one's link list.

## PARAMETERS

### -Name

Specifies the names of queues to unlink. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists queues from the source folder(s).

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

### -Link

Specifies the destination folder paths to remove from the queues' link lists. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion lists the folders currently linked to each target queue.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the source folder(s) containing the queues. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Recursively searches subfolders of -Path for matching queues. Combine with -Depth to limit how deep the recursion goes.

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

Limits how many levels of subfolder are searched when -Recurse is specified. 0 means only the immediate -Path folder is searched. Has no effect without -Recurse.

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

You can pipe queue names and link folder paths to this cmdlet via the Name, Link, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The owning folder of a queue always retains access; this cmdlet only removes shared-access entries from linked folders. To delete a queue entirely use Remove-OrchQueue.

If a folder in -Link is not currently linked to the queue, the operation succeeds without error for that target.

Removing a link does not delete queue items. Existing items remain on the source queue; the unlinked folders simply lose visibility to read or add items.

## RELATED LINKS

[Add-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchQueueLink.md)

[Get-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueLink.md)

[Remove-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueue.md)

[Remove-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetLink.md)
