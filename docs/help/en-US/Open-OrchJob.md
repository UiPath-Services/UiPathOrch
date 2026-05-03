---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Open-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Open-OrchJob
---

# Open-OrchJob

## SYNOPSIS

Opens the job details page in the default web browser.

## SYNTAX

### __AllParameterSets

```
Open-OrchJob [[-Id] <long[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Open-OrchJob` cmdlet opens the UiPath Orchestrator job details side panel in the default web browser. You specify one or more job IDs, and the cmdlet launches the browser to the corresponding job details URL for each.

The URL follows the pattern: `{base_url}/orchestrator_/jobs(sidepanel:sidepanel/jobs/{jobKey}/details)?fid={folderId}`

This cmdlet does not support `-Recurse` or `-Depth` parameters.

Primary Endpoint: N/A (opens browser URL)

OAuth required scopes: N/A

Required permissions: N/A

## EXAMPLES

### Example 1: Open job details in the browser

```powershell
PS Orch1:\Shared> Open-OrchJob -Id 150565000
```

Opens the details page for job 150565000 in the default web browser.

### Example 2: Open a job from a specific folder

```powershell
PS C:\> Open-OrchJob -Path Orch1:\Shared -Id 150565000
```

Opens the details page for job 150565000 from the Shared folder in the default web browser.

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

### -Id

Specifies the job ID or IDs to open in the browser. Tab completion suggests job IDs from the local cache.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int64[]

You can pipe job IDs to the **Id** parameter.

### System.String[]

You can pipe folder paths to the **Path** parameter.

## OUTPUTS

### None

This cmdlet does not generate output. It opens a browser window.

## NOTES

This cmdlet uses `Process.Start` with `UseShellExecute` to launch the default web browser. It does not support the `-Recurse` or `-Depth` parameters.

## RELATED LINKS

[Get-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJob.md)

[Start-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchJob.md)

[Stop-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchJob.md)
