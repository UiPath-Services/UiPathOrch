---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-DuUser

## SYNOPSIS
Adds users in Document Understanding projects.

## SYNTAX

```
Add-DuUser [-Type] <String[]> [-Name] <String[]> [-Roles] <String[]> [-Path <String[]>] [-Recurse]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-DuUser cmdlet adds users to Document Understanding projects within UiPath Orchestrator folders. This cmdlet manages user access and role assignments for Document Understanding functionality, allowing users to participate in document processing, validation, and machine learning training workflows.

Document Understanding projects require specific user roles for different activities such as document annotation, model training, and validation. Adding users with appropriate roles enables collaborative document processing and improves machine learning model accuracy through human feedback.

Use the -Type parameter to specify the user type, -Name to specify user names, and -Roles to assign specific Document Understanding roles. The -Path parameter enables targeting specific folders, and -Recurse allows adding users to Document Understanding projects across folder hierarchies.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables adding users to Document Understanding projects in all subfolders.

Primary Endpoint: PATCH /{partitionGlobalId}/pap_/api/userroleassignments

OAuth required scopes: [PLACEHOLDER]

Required permissions: Document Understanding project management permissions

## EXAMPLES

### Example 1
```powershell
PS Orch1:\DocumentProcessing> Add-DuUser User john.doe Validator
```

Adds user john.doe with the Validator role to Document Understanding projects in the current folder (DocumentProcessing).

### Example 2
```powershell
PS C:\> Add-DuUser -Path Orch1:\InvoiceProcessing User jane.smith "Data Entry", Reviewer
```

Adds user jane.smith with "Data Entry" and Reviewer roles to Document Understanding projects in the InvoiceProcessing folder.

### Example 3
```powershell
PS Orch1:\DocumentProcessing> Add-DuUser User admin.user, lead.user Administrator -WhatIf
```

Shows what would happen when adding admin.user and lead.user with Administrator roles to Document Understanding projects in the current folder.

### Example 4
```powershell
PS C:\> Add-DuUser -Path Orch1:\Reports User *analyst* Validator -Recurse
```

Adds all users with usernames containing "analyst" with Validator role to Document Understanding projects in the Reports folder and all its subfolders.

### Example 5
```powershell
PS Orch1:\> Add-DuUser -Recurse User document.reviewer "Data Entry" -WhatIf
```

Shows what would happen when adding document.reviewer with "Data Entry" role to Document Understanding projects across all folders recursively.

### Example 6
```powershell
PS Orch1:\DocumentProcessing> Get-OrchUser | Where-Object {$_.Email -like "*@contoso.com"} | Add-DuUser -Type User -Roles Validator
```

Gets all users with contoso.com email domain and adds them with Validator role to Document Understanding projects using pipeline input.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the names of the users to add to Document Understanding projects.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the target folders containing Document Understanding projects.

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
Specifies that users should be added to Document Understanding projects in all subfolders recursively.

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

### -Roles
Specifies the Document Understanding roles to assign to the users.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Type
Specifies the user type for Document Understanding access.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

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

### System.String[]
User names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.User
User objects from Get-OrchUser can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet manages Document Understanding project access. Common roles include Validator, Reviewer, Data Entry, and Administrator. Users need appropriate roles to participate in document annotation, validation, and machine learning training workflows. Use -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-DuUser](Get-DuUser.md)

[Remove-DuUser](Remove-DuUser.md)

[Get-OrchUser](Get-OrchUser.md)
