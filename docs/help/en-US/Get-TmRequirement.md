---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmRequirement.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmRequirement
---

# Get-TmRequirement

## SYNOPSIS

Gets the requirements from projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmRequirement [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [[-Name] <string[]>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves requirements from Test Manager projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned sorted by object key.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/requirements

OAuth Required scopes: TM.Requirements or TM.Requirements.Read

Permission(s): Requirement.Read

## EXAMPLES

### Example 1: Get all requirements in the current project

```powershell
PS Orch1Tm:\MTP> Get-TmRequirement
```

Gets all requirements from the current Test Manager project.

### Example 2: Get requirements by name with wildcards

```powershell
PS Orch1Tm:\MTP> Get-TmRequirement 要*
```

Gets requirements whose name starts with "要" from the current project.

### Example 3: Get requirements from a specific project

```powershell
PS C:\> Get-TmRequirement -Path Orch1Tm:\MTP -Recurse
```

Gets all requirements from the specified project and all its subfolders.

### Example 4: Export the requirements to CSV

```powershell
PS C:\> Get-TmRequirement -Path Orch1Tm:\MTP -ExportCsv requirements.csv
```

The CSV has the columns Path, Name, Description and Labels: the parameters of New-TmRequirement. Labels holds the label names joined by commas; a comma in a label name is written as `` `, ``.

### Example 5: Recreate the exported requirements in another project

```powershell
PS C:\> Import-Csv requirements.csv | New-TmRequirement -Path Orch2Tm:\MTP
```

`-Path` on the command line takes precedence over the Path column; without it, the requirements are created in the project named in the CSV.

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

### -Name

Specifies the name of the requirements to be retrieved.
Wildcard characters are permitted.

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

### -ExportCsv

Specifies the path of a CSV file to export the requirements to, instead of writing them to the pipeline. If a folder is given, the file is named ExportedTmRequirements.csv. The columns are Path, Name, Description and Labels, so `Import-Csv | New-TmRequirement` recreates the requirements. Path and Name are wildcard-escaped. The labels are read with one request per requirement (GET /testmanager_/api/v2/{projectId}/objectlabels/requirement/{objectId}/all).

```yaml
Type: System.String
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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

You can pipe requirement names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmRequirement

This cmdlet returns TmRequirement objects representing Test Manager requirements.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Results are sorted by object key. With -ExportCsv, the labels are read with one request per requirement. The test cases assigned to the requirements are not exported.


## RELATED LINKS

[Remove-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmRequirement.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)
