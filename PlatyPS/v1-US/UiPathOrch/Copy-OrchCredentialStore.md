---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchCredentialStore
---

# Copy-OrchCredentialStore

## SYNOPSIS

Copies credential stores to another Orchestrator tenant.

## SYNTAX

### __AllParameterSets

```
Copy-OrchCredentialStore [-Name] <string[]> [-Destination] <string[]> [-Path <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies credential store configurations from one Orchestrator tenant to another. The cmdlet reads credential stores from the source drive and creates them on the destination drives with the same settings.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available credential store names. Multiple values can be specified using comma-separated text that includes wildcards.

If a credential store named 'Orchestrator Database' already exists on the destination drive, it is skipped. After copying, the cmdlet checks for masked fields in the configuration and issues a warning if manual updates are needed.

Credential stores are tenant-scoped entities. Use the -Path parameter to specify the source Orchestrator drive.

Primary Endpoint: POST /odata/CredentialStores

OAuth required scopes: OR.Settings

Required permissions: Settings.View (source) and Settings.Create (destination)

## EXAMPLES

### Example 1: Copy all credential stores to another tenant

```powershell
PS Orch1:\> Copy-OrchCredentialStore * Orch2:\
```

Copies all credential stores from Orch1 to Orch2.

### Example 2: Copy a specific credential store

```powershell
PS Orch1:\> Copy-OrchCredentialStore CyberArk* Orch2:\
```

Copies credential stores matching 'CyberArk*' from Orch1 to Orch2.

### Example 3: Copy from a specific source to multiple destinations

```powershell
PS C:\> Copy-OrchCredentialStore -Path Orch1:\ * Orch2:\,Orch3:\
```

Copies all credential stores from Orch1 to both Orch2 and Orch3.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\> Copy-OrchCredentialStore * Orch2:\ -WhatIf
```

Shows what credential stores would be copied without actually performing the operation.

## PARAMETERS

### -Path

Specifies the source Orchestrator drive. If not specified, the current drive is used as the source.

```yaml
Type: System.String
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

### -Destination

Specifies the destination Orchestrator drives to copy credential stores to. Multiple drives can be specified.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the credential stores to copy. Supports wildcards. Tab completion dynamically suggests credential store names from the source drive.

```yaml
Type: System.String[]
DefaultValue: None
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

You can pipe credential store names to this cmdlet via the Name property.

### System.String

You can pipe the source Path to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.CredentialStore

This cmdlet does not produce output.

## NOTES

Credential stores are tenant-scoped entities. The -Path and -Destination parameters specify Orchestrator drives (not folder paths).

If the destination already has a credential store named 'Orchestrator Database', it is silently skipped.

After copying, if the credential store configuration contains masked fields (secrets), a warning is issued listing the configuration keys that need manual update on the destination.

## RELATED LINKS

Get-OrchCredentialStore

Remove-OrchCredentialStore
