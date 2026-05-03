---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Clear-OrchCache
---

# Clear-OrchCache

## SYNOPSIS

Clears the in-memory cache on UiPathOrch drives.

## SYNTAX

### __AllParameterSets

```
Clear-OrchCache [[-Path] <string[]>] [-AllDrives] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The UiPathOrch module caches entities retrieved from the Orchestrator on each drive to optimize response times and reduce the load on the Orchestrator server. In particular, this cache is used to display appropriate candidates for auto-completion of parameter values.

If you want the PowerShell console to reflect updates to entities on Orchestrator (such as creating or removing folders) made via Orchestrator Web or other external applications, clear the in-memory cache with this cmdlet.

For the -Path parameter, specify the drive on which you want to clear the cache. If the -Path parameter is not specified, the cache on the current drive is cleared. If the current drive is not an UiPathOrch drive, the cache on all UiPathOrch drives is cleared.

The -AllDrives switch forces clearing the cache on all UiPathOrch drives regardless of the current drive or -Path parameter.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available drives. The completions include all registered Orchestrator, Document Understanding, and Test Manager drives.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Clear cache on the current drive

```powershell
PS Orch1:\> Clear-OrchCache
```

Clears the in-memory cache on the Orch1: drive (the current drive).

### Example 2: Clear cache on all drives

```powershell
PS C:\> Clear-OrchCache
```

Clears the in-memory cache on all drives managed by UiPathOrch, because the current drive (C:) is not an UiPathOrch drive.

### Example 3: Clear cache on specific drives

```powershell
PS C:\> Clear-OrchCache Orch1:,Orch2:
```

Clears the in-memory cache on the Orch1: and Orch2: drives.

### Example 4: Clear cache using current location shorthand

```powershell
PS Orch1:\> Clear-OrchCache .,Orch2:
```

The current drive can be specified using a period (.), which represents the current location. In this command, the cache is cleared on both the current drive (Orch1:) and Orch2:.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AllDrives

Specifies that the cache should be cleared on all UiPathOrch drives, regardless of the current drive or -Path parameter value.

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

## OUTPUTS

## NOTES

When clearing cache on Document Understanding or Test Manager drives, the parent Orchestrator drive cache is also cleared. This ensures that cross-referenced data remains consistent.

## RELATED LINKS

Get-OrchPSDrive
