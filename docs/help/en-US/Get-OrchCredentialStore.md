---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialStore.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchCredentialStore
---

# Get-OrchCredentialStore

## SYNOPSIS

Gets credential stores from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchCredentialStore [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets credential store information from UiPath Orchestrator. Credential stores define where robot credentials and assets of type Credential are stored (e.g., Orchestrator Database, CyberArk, Azure Key Vault).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available credential store names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve credential stores from multiple drives in parallel.

Credential stores are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/CredentialStores

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.View

## EXAMPLES

### Example 1: Get all credential stores

```powershell
PS Orch1:\> Get-OrchCredentialStore
```

Gets all credential stores from the current Orchestrator tenant.

### Example 2: Get credential stores by name

```powershell
PS Orch1:\> Get-OrchCredentialStore a*
```

Gets credential stores whose names match `a*`.

### Example 3: Get credential stores from multiple drives

```powershell
PS C:\> Get-OrchCredentialStore -Path Orch1:\,Orch2:\
```

Gets all credential stores from both Orch1 and Orch2 tenants.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Name

Specifies the names of the credential stores to retrieve. Supports wildcards. Tab completion dynamically suggests credential store names from the target drives.

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

You can pipe credential store names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.CredentialStore

Returns CredentialStore objects with properties including Name, Type, and AdditionalConfiguration.

## NOTES

Credential stores are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve credential stores from multiple drives in parallel.

## RELATED LINKS

Remove-OrchCredentialStore

Copy-OrchCredentialStore
