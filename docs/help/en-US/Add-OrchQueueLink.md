---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchQueueLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchQueueLink
---

# Add-OrchQueueLink

## SYNOPSIS

Links queues to additional folders.

## SYNTAX

### __AllParameterSets

```
Add-OrchQueueLink [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Link] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Links queues to additional folders in UiPath Orchestrator. Queue linking allows a queue defined in one folder to be shared with other folders without duplicating the queue. Linked folders can read from and write to the shared queue, and queue items are visible across all linked folders.

The -Name parameter specifies which queues to link, and the -Link parameter specifies the destination folders to share the queues with. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name, -Path, and -Link parameters support tab completion. The -Name completion dynamically lists queues from the source folder(s). The -Link parameter accepts folder paths on the same Orch: drive.

Use -Recurse (optionally with -Depth) to walk subfolders of -Path looking for matching queues — useful when the same queue name appears under multiple department folders and you want to link them all to a common destination in one call.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Queues or OR.Queues.Write

Required permissions: Queues.Edit

## EXAMPLES

### Example 1: Link a queue to another folder

```powershell
PS Orch1:\Shared> Add-OrchQueueLink -Name InvoiceQueue -Link Orch1:\Dept#2
```

Links the "InvoiceQueue" queue from the Shared folder to the Dept#2 folder. The Dept#2 folder can now read and write items on this queue.

### Example 2: Preview link operation with -WhatIf

```powershell
PS Orch1:\Shared> Add-OrchQueueLink -Name InvoiceQueue -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Add QueueLink to Orch1:\Dept#2" on target "Orch1:\Shared\InvoiceQueue".
```

Shows what would happen without executing the command.

### Example 3: Link multiple queues to multiple folders

```powershell
PS Orch1:\Shared> Add-OrchQueueLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Links all queues matching "Test*" to both the Dept#2 and Dept#3 folders. Both -Name and -Link accept wildcards and comma-separated values.

### Example 4: Recursively process subfolders

```powershell
PS C:\> Add-OrchQueueLink -Path Orch1:\Depts\* -Recurse -Depth 2 -Name SharedQueue -Link Orch1:\Common
```

Walks the subfolders of Orch1:\Depts up to 2 levels deep, finds queues named "SharedQueue", and links each to the Common folder.

## PARAMETERS

### -Name

Specifies the names of queues to link. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists queues from the source folder(s).

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

Specifies the destination folder paths to link the queues to. This is a mandatory parameter. The destination folders must be on the same Orchestrator instance (same drive) as the source. Supports wildcards and multiple comma-separated values.

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

Specifies the source folder(s) containing the queues to link. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

The source and destination must be on the same Orchestrator drive. Cross-instance linking is not supported. To share queue definitions across instances, recreate them with Copy-OrchQueue (or its equivalent) on the destination drive.

If the queue is already linked to the specified folder, the operation succeeds without error.

Queue items themselves live on the source queue; linking only grants additional folders access to read and write items. There is no per-folder partitioning of items.

## RELATED LINKS

[Get-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueLink.md)

[Remove-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueLink.md)

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)

[Add-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchBucketLink.md)
