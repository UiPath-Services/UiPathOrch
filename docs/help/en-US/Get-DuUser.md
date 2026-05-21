---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-DuUser
---

# Get-DuUser

## SYNOPSIS

Gets the users assigned to Document Understanding projects.

## SYNTAX

### __AllParameterSets

```
Get-DuUser [-Path <string[]>] [-Recurse] [[-Name] <string[]>] [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves users (including groups, robot accounts, and external applications) that are assigned to Document Understanding projects along with their role assignments.
Results are filtered by the `-Name` parameter using wildcard matching and returned in alphabetical order.
When `-ExportCsv` is specified, results are exported to a CSV file instead of being written to the pipeline.
This cmdlet operates on the PSDrive of the UiPathOrchDu provider.
If the scope in the configuration file includes "Du.", the PSDrive of the UiPathOrchDu provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /{partitionGlobalId}/pap_/api/userroleassignments?scope=/tenant/{tenantKey}/DocumentUnderstanding/projects/{projectId}&serviceName=DocumentUnderstanding

OAuth required scopes: (Document Understanding PAP API)

Required permissions: (managed by Document Understanding)

## EXAMPLES

### Example 1: Get all users in the current project

```powershell
PS Orch1Du:\MyProject> Get-DuUser
```

Gets all users assigned to the current Document Understanding project.

### Example 2: Get users by name with wildcards

```powershell
PS Orch1Du:\MyProject> Get-DuUser Admin*
```

Gets users whose display name starts with "Admin" from the current project.

### Example 3: Export all users to CSV

```powershell
PS Orch1Du:\> Get-DuUser -Recurse -ExportCsv C:\export\du_users.csv
```

Exports all users from all projects (including subfolders) to a CSV file.
The CSV includes columns for Path, Type, Name, and Roles.

### Example 4: Export users from a specific project

```powershell
PS C:\> Get-DuUser -Path Orch1Du:\MyProject -ExportCsv users.csv -CsvEncoding UTF8
```

Exports the users from the specified project to a UTF-8 encoded CSV file.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii). This parameter is only effective when -ExportCsv is also specified.

```yaml
Type: System.Text.Encoding
DefaultValue: ''
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

### -ExportCsv

Specifies the file path for exporting the results to a CSV file.
When this parameter is specified, the cmdlet writes results to the CSV file instead of outputting objects to the pipeline.
If only a directory is specified, the default file name "ExportedDuUsers.csv" is used.

```yaml
Type: System.String
DefaultValue: ''
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

Specifies the display name of the users to be retrieved.
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

You can pipe user display names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.DuUser

This cmdlet returns DuUser objects representing users assigned to Document Understanding projects.
When `-ExportCsv` is specified, no objects are written to the pipeline.

## NOTES

This cmdlet operates on the UiPathOrchDu provider PSDrive. Ensure the configuration file includes "Du." scopes so that the PSDrive is automatically created. Use Edit-OrchConfig to open the configuration file and Get-PSDrive to verify the drive exists.

## RELATED LINKS

[Add-DuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-DuUser.md)

[Remove-DuRoleFromDuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-DuRoleFromDuUser.md)

[Get-DuRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuRole.md)
