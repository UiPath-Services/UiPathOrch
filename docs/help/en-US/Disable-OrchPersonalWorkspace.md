---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchPersonalWorkspace.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchPersonalWorkspace
---

# Disable-OrchPersonalWorkspace

## SYNOPSIS

Disables personal workspace for a user on UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Disable-OrchPersonalWorkspace [-Path <string[]>] [-UserName] <string[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables the personal workspace feature for the specified user on UiPath Orchestrator. This cmdlet updates the user settings by setting the MayHavePersonalWorkspace property to False.

After disabling the personal workspace, the cmdlet automatically clears the in-memory cache to reflect the changes.

The -UserName parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available tenant users. The -UserName completions are dynamically populated from actual user data on the target drives.

This is a script function (external help file: UiPathOrch-Help.xml) that wraps the Update-OrchUser cmdlet.

Primary Endpoint: GET /odata/Users, PUT /odata/Users({userId})

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View, Users.Edit

## EXAMPLES

### Example 1: Disable personal workspace for a user

```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace ytsuda@gmail.com
```

Disables the personal workspace for the user ytsuda@gmail.com on the current drive.

### Example 2: Disable personal workspace on a specific drive

```powershell
PS C:\> Disable-OrchPersonalWorkspace ytsuda@gmail.com -Path Orch1:
```

Disables the personal workspace for the specified user on the Orch1: drive.

### Example 3: Preview changes with WhatIf

```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace ytsuda@gmail.com -WhatIf
```

Shows what would happen if the cmdlet runs without actually making changes.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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

### -UserName

Specifies the user name of the user to disable personal workspace for. Accepts wildcard patterns.

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

You can pipe user names to this cmdlet via the UserName property.

## OUTPUTS

### None

This cmdlet does not produce output directly. It calls Update-OrchUser to update user settings.

## NOTES

This cmdlet sets MayHavePersonalWorkspace to False for the specified user. Unlike Enable-OrchPersonalWorkspace, it does not modify the MayHaveRobotSession setting. The cache is automatically cleared after the operation.

## RELATED LINKS

[Enable-OrchPersonalWorkspace](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchPersonalWorkspace.md)

[Get-OrchPersonalWorkspace](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPersonalWorkspace.md)

[Remove-OrchPersonalWorkspace](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchPersonalWorkspace.md)
