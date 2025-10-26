---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuClassifier

## SYNOPSIS
Retrieves Document Understanding classifiers from UiPath Orchestrator.

## SYNTAX

```
Get-DuClassifier [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-DuClassifier cmdlet retrieves Document Understanding classifiers from UiPath Orchestrator. Classifiers are AI models used to categorize and classify documents within Document Understanding projects, enabling automated document type identification and processing workflows.

Each classifier contains information such as Id, Name, Status (Available, Training, etc.), and API endpoints for synchronous and asynchronous classification operations. Classifiers are organized within Document Understanding projects and provide the foundation for intelligent document processing automation.

This cmdlet operates within the UiPathOrchDu drive context, requiring navigation to a specific Document Understanding project folder. **Important**: This cmdlet requires navigation to a Document Understanding project folder on the UiPathOrchDu drive (e.g., Orch1Du:\ProjectName) before execution.

Primary Endpoint: GET /du_/api/framework/projects/{projectId}/classifiers

OAuth required scopes: OR.ML or OR.ML.Read

Required permissions: ML.View

## EXAMPLES

### Example 1
```powershell
PS Orch1Du:\> Get-DuClassifier
```

Retrieves all classifiers from the current Predefined project, displaying id, name, and status information.

### Example 2
```powershell
PS Orch1Du:\Predefined> Get-DuClassifier | ConvertTo-Json -Depth 3
```

Displays detailed classifier properties in JSON format, including API endpoints (detailsUrl, syncUrl, asyncUrl).

### Example 3
```powershell
PS C:\> Get-DuClassifier -Path Orch1Du:\MyProject *generative*
```

Gets classifiers with IDs containing "generative" from the MyProject Document Understanding project.

### Example 4
```powershell
PS Orch1Du:\> Get-DuClassifier -Recurse | Where-Object {$_.status Available}
```

Retrieves all available classifiers across all Document Understanding projects.

### Example 5
```powershell
PS Orch1Du:\Predefined> Get-DuClassifier | Select-Object name, status, @{Name="HasSyncAPI";Expression={$_.syncUrl -ne $null}}
```

Displays classifier names, status, and whether they have synchronous API endpoints available.

### Example 6
```powershell
PS Orch1Du:\> Get-DuClassifier -Recurse | Group-Object status
```

Groups all classifiers by their status across Document Understanding projects.

## PARAMETERS

### -Name
Specifies the name(s) of the document classifier(s) to retrieve. Supports wildcard patterns for flexible matching. Use '*' to retrieve all classifiers.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target Document Understanding project folders to search. Must reference UiPathOrchDu drive paths (e.g., Orch1Du:\ProjectName).

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
Includes the target project folder and all its subprojects in the search operation. Essential for comprehensive classifier discovery.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.DuClassifier
## NOTES
This cmdlet operates within Document Understanding projects on the UiPathOrchDu drive. Navigation to a specific project folder is required before execution. Classifiers provide AI-powered document categorization capabilities for intelligent document processing. This operation requires ML.View permissions within Document Understanding projects.

## RELATED LINKS

[Get-DuDocumentType](Get-DuDocumentType.md)

[Get-DuExtractor](Get-DuExtractor.md)


