---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuClassifier.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-DuClassifier
---

# Get-DuClassifier

## SYNOPSIS

Gets classifiers of Document Understanding.

## SYNTAX

### __AllParameterSets

```
Get-DuClassifier [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves classifiers from Document Understanding projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned in alphabetical order.
This cmdlet operates on the PSDrive of the UiPathOrchDu provider.
If the scope in the configuration file includes "Du.", the PSDrive of the UiPathOrchDu provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /du_/api/framework/projects/{projectId}/classifiers?api-version=1

OAuth Required scopes: Du.Digitization.Api or Du.Classification.Api or Du.Extraction.Api or Du.Validation.Api

## EXAMPLES

### Example 1: Get all classifiers in the current project

```powershell
PS Orch1Du:\Predefined> Get-DuClassifier
```

Gets all classifiers from the current Document Understanding project.

### Example 2: Get classifiers by name with wildcards

```powershell
PS Orch1Du:\Predefined> Get-DuClassifier ML*
```

Gets classifiers whose name starts with "ML" from the current project.

### Example 3: Get classifiers from a specific project

```powershell
PS C:\> Get-DuClassifier -Path Orch1Du:\Predefined -Recurse
```

Gets all classifiers from the specified project and all its subfolders.

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

### -Name

Specifies the name of the classifiers to be retrieved.
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe classifier names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.DuClassifier

This cmdlet returns DuClassifier objects representing Document Understanding classifiers.

## NOTES

This cmdlet operates on the UiPathOrchDu provider PSDrive. Ensure the configuration file includes "Du." scopes so that the PSDrive is automatically created.


## RELATED LINKS

[Get-DuDocumentType](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuDocumentType.md)

[Get-DuExtractor](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuExtractor.md)
