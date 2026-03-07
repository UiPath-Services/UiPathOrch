---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Export-OrchJobMedia
---

# Export-OrchJobMedia

## SYNOPSIS

Exports execution media recordings from Orchestrator to the local file system.

## SYNTAX

### __AllParameterSets

```
Export-OrchJobMedia [[-JobId] <long[]>] [[-Destination] <string>] [-Path <string[]>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Export-OrchJobMedia` cmdlet downloads execution media recordings from UiPath Orchestrator to the local file system. You can export media for specific jobs by specifying the `-JobId` parameter, or export all media in the target folders by omitting `-JobId`.

Downloaded files are named using the pattern: `fid{folderId}_{ReleaseName}_{JobId}_{Name}`.

If the `-Destination` parameter is not specified, files are saved to the current file system location. The cmdlet displays progress during the download.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: Downloads execution media via the Orchestrator API

OAuth required scopes: OR.Execution

Required permissions: ExecutionMedia.View

## EXAMPLES

### Example 1: Export all media to the current directory

```powershell
PS Orch1:\Shared> Export-OrchJobMedia
```

Exports all execution media recordings from the current Orchestrator folder to the current file system location.

### Example 2: Export media for a specific job

```powershell
PS Orch1:\Shared> Export-OrchJobMedia -JobId 12345
```

Exports execution media recordings associated with job ID 12345 from the current folder.

### Example 3: Export media to a specified destination

```powershell
PS Orch1:\Shared> Export-OrchJobMedia -Destination C:\MediaExports
```

Exports all execution media from the current folder to the `C:\MediaExports` directory.

### Example 4: Preview export with -WhatIf

```powershell
PS Orch1:\Shared> Export-OrchJobMedia -JobId 12345 -WhatIf
```

Shows what would happen if the cmdlet runs without actually downloading any files. Use this to verify which media would be exported.

### Example 5: Export media recursively

```powershell
PS Orch1:\Shared> Export-OrchJobMedia -Recurse -Destination C:\MediaExports
```

Exports all execution media from the current folder and all its subfolders to the `C:\MediaExports` directory.

## PARAMETERS

### -Path

Specifies the target folder.
If not specified, the current folder will be targeted.

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

Specifies that the operation should include the target folder and all its subfolders.

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

### -Destination

Specifies the destination folder for the export.
Please specify a folder on the FileSystem drive.

```yaml
Type: System.String
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

### -JobId

Specifies the job IDs for which to export execution media. If not specified, all media in the target folders are exported. Tab completion shows job IDs that have media available.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
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

### System.Int64[]

You can pipe the **JobId** values to this cmdlet.

### System.String

You can pipe the **Destination** value to this cmdlet.

### System.String[]

You can pipe the **Path** values to this cmdlet.

## OUTPUTS

### System.Object

This cmdlet does not generate specific output objects. Files are downloaded to the destination folder.

## NOTES

## RELATED LINKS

Get-OrchJobMedia

Remove-OrchJobMedia
