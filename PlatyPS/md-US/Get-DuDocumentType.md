---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuDocumentType

## SYNOPSIS
Retrieves Document Understanding document types from UiPath Orchestrator.

## SYNTAX

```
Get-DuDocumentType [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-DuDocumentType cmdlet retrieves Document Understanding document types from UiPath Orchestrator. Document types define the structure and processing rules for specific document categories such as invoices, receipts, forms, and other business documents within Document Understanding projects.

Each document type contains information such as Id, Name, and detailsUrl for accessing the complete document type definition. Document types serve as templates that define field extraction rules, validation logic, and processing workflows for specific document categories.

This cmdlet operates within the UiPathOrchDu drive context, requiring navigation to a specific Document Understanding project folder. The system includes numerous predefined document types for common business documents including invoices, receipts, tax forms, ID cards, bank statements, and specialized forms.

**Important**: This cmdlet requires navigation to a Document Understanding project folder on the UiPathOrchDu drive (e.g., Orch1Du:\ProjectName) before execution.

Primary Endpoint: GET /du_/api/framework/projects/{projectId}/document-types

OAuth required scopes: OR.ML or OR.ML.Read

Required permissions: ML.View

## EXAMPLES

### Example 1
```powershell
PS Orch1Du:\> Get-DuDocumentType
```

Retrieves all document types from the current Predefined project, displaying extensive list of available document types including Invoices, Receipts, Tax Forms, etc.

### Example 2
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | Where-Object {$_.name -like "*Invoice*"}
```

Gets all document types related to invoices from the predefined collection.

### Example 3
```powershell
PS C:\> Get-DuDocumentType -Path Orch1Du:\MyProject *tax*
```

Gets document types with IDs containing "tax" from the MyProject Document Understanding project.

### Example 4
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | ConvertTo-Json -Depth 3 | Select-Object -First 1
```

Displays detailed document type properties in JSON format, including API endpoints and project context.

### Example 5
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | Where-Object {$_.name -match "Japan|China"} | Select-Object name, id
```

Retrieves document types for specific regional variants (Japan, China localized documents).

### Example 6
```powershell
PS Orch1Du:\> Get-DuDocumentType -Recurse | Group-Object Project
```

Groups all document types by their containing Document Understanding project.

## PARAMETERS

### -Name
Specifies the name(s) of the document type(s) to retrieve. Supports wildcard patterns for flexible matching. Use '*' to retrieve all document types.

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
Includes the target project folder and all its subprojects in the search operation. Essential for comprehensive document type discovery.

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

### UiPath.PowerShell.Entities.DuDocumentType
## NOTES
This cmdlet operates within Document Understanding projects on the UiPathOrchDu drive. Navigation to a specific project folder is required before execution. Document types define the structure, field extraction rules, and processing logic for specific document categories. The Predefined project contains numerous pre-built document types for common business scenarios. Custom document types can be created for specialized processing requirements. This operation requires ML.View permissions within Document Understanding projects.

## RELATED LINKS

[Get-DuClassifier](Get-DuClassifier.md)

[Get-DuExtractor](Get-DuExtractor.md)


