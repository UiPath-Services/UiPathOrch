---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmExternalApplication.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmExternalApplication
---

# Remove-PmExternalApplication

## SYNOPSIS

Removes an external application from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmExternalApplication [-Name] <string[]> [-Force] [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more external applications (OAuth clients) at the organization (platform management) level from a UiPath Automation Cloud organization. The cmdlet calls the identity service API to permanently delete the external application and its associated credentials.

The -Name parameter supports wildcards, allowing batch deletion of external applications matching a pattern. Tab completion dynamically suggests available external application names.

The -Force switch skips the safety check that prevents deleting the currently authenticated external application (i.e., the application used by the current session).

The -Path parameter supports tab completion for available drives.

Primary Endpoint: DELETE /api/ExternalClient/{partitionGlobalId}/{id} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific external application

```powershell
PS Orch1:\> Remove-PmExternalApplication MyApp
```

Removes the specified external application from the organization. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Remove multiple external applications with a wildcard

```powershell
PS Orch1:\> Remove-PmExternalApplication Test*
```

Removes all external applications whose names start with "Test".

### Example 3: Remove an external application with Force

```powershell
PS Orch1:\> Remove-PmExternalApplication MyApp -Force
```

Removes the specified external application, skipping the safety check that prevents deleting the currently authenticated application.

### Example 4: Preview external application removal

```powershell
PS Orch1:\> Remove-PmExternalApplication * -WhatIf
```

Shows which external applications would be removed without actually deleting them.

### Example 5: Remove with confirmation prompt

```powershell
PS Orch1:\> Remove-PmExternalApplication MyApp -Confirm
```

Prompts for confirmation before removing the external application.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Force

Skips the safety check that prevents deleting the currently authenticated external application. Without this switch, the cmdlet refuses to delete the application that is being used by the current session.

```yaml
Type: System.Management.Automation.SwitchParameter
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

Specifies the name(s) of the external application(s) to remove. Supports wildcards for pattern matching. Tab completion dynamically suggests external application names from the target organizations.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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
DefaultValue: ''
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

You can pipe external application names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClient

Returns the deleted ExternalClient object(s).

## NOTES

By default, the cmdlet prevents deleting the external application that is being used by the current session to avoid breaking the active connection. Use the -Force switch to override this safety check.

## RELATED LINKS

[Get-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApplication.md)

[Copy-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmExternalApplication.md)

[Get-PmExternalApiResource](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApiResource.md)
