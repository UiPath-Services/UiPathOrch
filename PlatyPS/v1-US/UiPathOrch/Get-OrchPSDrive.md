---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPSDrive
---

# Get-OrchPSDrive

## SYNOPSIS

Gets information about UiPathOrch PSDrives.

## SYNTAX

### __AllParameterSets

```
Get-OrchPSDrive [[-Path] <string[]>] [-Force] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets information about UiPathOrch PSDrives that are currently available in the session. This cmdlet enumerates all drive types managed by the UiPathOrch module, including Orchestrator drives (Orch), Document Understanding drives (DU), and Test Manager drives (TM).

When the -Path parameter is specified, only the matching drives are returned. Without -Path, all registered drives of all types are enumerated.

When -Force is specified, the cmdlet actively verifies the connection to each drive by ensuring authentication and retrieving the partition global ID.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available drives.

This cmdlet is typically the first command to run after connecting to an Orchestrator instance, to verify the drive configuration and connection status.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Get all UiPathOrch drives

```powershell
PS C:\> Get-OrchPSDrive
```

Gets information about all UiPathOrch PSDrives registered in the current session.

### Example 2: Get a specific drive

```powershell
PS C:\> Get-OrchPSDrive Orch1:
```

Gets information about the Orch1: drive.

### Example 3: Verify drive connections

```powershell
PS C:\> Get-OrchPSDrive -Force
```

Gets all UiPathOrch drives and actively verifies the connection to each by authenticating and retrieving partition information.

### Example 4: Get multiple drives

```powershell
PS C:\> Get-OrchPSDrive Orch1:,Orch2:
```

Gets information about the Orch1: and Orch2: drives by specifying multiple drive names.

## PARAMETERS

### -Path

Specifies the name of the target drives to retrieve.
If not specified, all UiPathOrch drives are returned.

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Force

When specified, actively verifies the connection to each drive by performing authentication and retrieving the partition global ID.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.OrchPSDrive

Returns OrchPSDrive objects containing drive information such as drive name, root, description, and connection details.

## NOTES

This cmdlet enumerates Orchestrator, Document Understanding, and Test Manager drives in sequence. It is recommended to run this cmdlet first when starting a new session to verify your drive configuration.

## RELATED LINKS

Mount-OrchPSDrive

New-OrchPSDrive

Clear-OrchCache

Get-OrchHelp
