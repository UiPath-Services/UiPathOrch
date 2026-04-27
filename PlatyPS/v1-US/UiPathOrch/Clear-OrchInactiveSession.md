---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Clear-OrchInactiveSession
---

# Clear-OrchInactiveSession

## SYNOPSIS

Bulk-deletes Disconnected and Unresponsive unattended sessions.

## SYNTAX

### __AllParameterSets

```
Clear-OrchInactiveSession [[-Path] <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Sweeps inactive unattended sessions out of the tenant in a single API call per drive. A session is considered inactive when its Status is "Disconnected" OR IsUnresponsive is `$true`. Useful as a maintenance step before re-deploying robots, or to free runtime slots that are stuck on dead sessions.

This is a tenant-level operation: -Path selects which drive(s) to operate on, but folder context is not part of the API. The cmdlet writes the deleted MachineSessionRuntime entities to the pipeline so the caller can log the cleanup.

Primary Endpoint: POST /odata/Sessions/UiPath.Server.Configuration.OData.DeleteInactiveUnattendedSessions

OAuth required scopes: OR.Robots or OR.Robots.Write

Required permissions: Robots.Delete

## EXAMPLES

### Example 1: Preview what would be cleaned

```powershell
PS Orch1:\> Clear-OrchInactiveSession -WhatIf
```

Lists how many sessions would be deleted from the current drive without actually invoking the API.

### Example 2: Clean up Orch1 without prompting

```powershell
PS Orch1:\> Clear-OrchInactiveSession -Confirm:$false
```

Deletes every Disconnected/Unresponsive session in the Orch1 tenant and emits the deleted entities to the pipeline.

### Example 3: Clean up multiple tenants and group the result by machine

```powershell
PS C:\> Clear-OrchInactiveSession -Path Orch1, Orch2 -Confirm:$false |
              Group-Object MachineName | Sort-Object Count -Descending
```

Cleans Orch1 and Orch2, then summarizes the deletion count per machine.

## PARAMETERS

### -Path

Specifies the name of the target drives. If not specified, the current drive is targeted.

```yaml
Type: System.String[]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe drive names to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime

Returns the MachineSessionRuntime objects that were cleaned up.

## NOTES

Active and Available sessions are not touched — only Disconnected/Unresponsive ones. The cmdlet's filter happens client-side before the API call, so the SessionIds sent to the API are exactly the inactive ones identified locally.

## RELATED LINKS

Get-OrchUnattendedSession

Get-OrchMachineSession

Get-OrchUserSession
