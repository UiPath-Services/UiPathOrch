---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchJobMedia
---

# Get-OrchJobMedia

## SYNOPSIS

Gets execution media recordings associated with jobs from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchJobMedia [-Skip <ulong>] [-First <ulong>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchJobMedia` cmdlet retrieves execution media recordings associated with jobs from UiPath Orchestrator. Execution media includes screen recordings captured during job execution.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/ExecutionMedia

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: ExecutionMedia.View

## EXAMPLES

### Example 1: Get all execution media in the current folder

```powershell
PS Orch1:\Shared> Get-OrchJobMedia
```

Gets all execution media recordings from the current Orchestrator folder.

### Example 2: Get media with paging

```powershell
PS Orch1:\Shared> Get-OrchJobMedia -First 10 -Skip 20
```

Gets 10 execution media records, skipping the first 20 results. This is useful for paging through large result sets.

### Example 3: Get media recursively

```powershell
PS Orch1:\Shared> Get-OrchJobMedia -Recurse
```

Gets all execution media from the current folder and all its subfolders.

### Example 4: Get media from a specific folder

```powershell
PS C:\> Get-OrchJobMedia -Path Orch1:\Production
```

Gets all execution media from the specified Orchestrator folder.

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

### -First

Gets only the specified number of objects.
Enter the number of objects to get.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### -Skip

Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.UInt64

You can pipe the **First** and **Skip** values to this cmdlet.

### System.String[]

You can pipe the **Path** values to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.ExecutionMedia

This cmdlet returns ExecutionMedia objects representing recordings associated with jobs.

## NOTES

Execution media must be enabled on the Orchestrator and the process must be configured to record execution for media to be available.

## RELATED LINKS

Export-OrchJobMedia

Remove-OrchJobMedia
