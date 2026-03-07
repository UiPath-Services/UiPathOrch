---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchPersonalWorkspace
---

# Remove-OrchPersonalWorkspace

## SYNOPSIS

Removes personal workspace folders from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchPersonalWorkspace [[-Name] <string[]>] [[-OwnerName] <string[]>] [-Path <string[]>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes personal workspace folders from UiPath Orchestrator. The cmdlet first disables the personal workspace for the user, then deletes the personal workspace folder. At least one of -Name or -OwnerName must be specified.

The -Name and -OwnerName parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The completions are dynamically populated from actual personal workspace data on the target drives. When both parameters are specified, they work as an AND filter -- only workspaces matching both criteria are removed.

After removal, the folder cache and personal workspace cache are cleared automatically.

Primary Endpoint: GET /odata/PersonalWorkspaces, DELETE /odata/Folders({folderId})

OAuth required scopes: OR.Folders

Required permissions: Units.View, (Units.Delete or SubFolders.Delete - Deletes any folder or only if user has SubFolders.Delete permission on the provided folder)

## EXAMPLES

### Example 1: Remove a personal workspace by name

```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace "john.doe's workspace"
```

Removes the personal workspace with the specified name from the current drive.

### Example 2: Remove a personal workspace by owner name

```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -OwnerName john.doe@example.com
```

Removes the personal workspace owned by the specified user.

### Example 3: Remove all personal workspaces using wildcards

```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace * -Confirm:$false
```

Removes all personal workspaces from the current drive without confirmation prompts.

### Example 4: Preview removal with WhatIf

```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -OwnerName john* -WhatIf
```

Shows which personal workspaces would be removed without actually removing them.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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

### -Name

Specifies the name of the personal workspace folders to remove. Accepts wildcard patterns.

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

### -OwnerName

Specifies the owner name of the personal workspace folders to remove. Accepts wildcard patterns.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases:
- UserName
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe workspace names via the Name property, owner names via the OwnerName property, or drive names via the Path property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The personal workspace is removed from the Orchestrator.

## NOTES

This cmdlet performs two operations: (1) disables the personal workspace for the user, and (2) deletes the folder. At least one of -Name or -OwnerName must be specified; if neither is provided, an error is returned.

## RELATED LINKS

Get-OrchPersonalWorkspace

Enable-OrchPersonalWorkspace

Disable-OrchPersonalWorkspace

Get-OrchFolderUsage
