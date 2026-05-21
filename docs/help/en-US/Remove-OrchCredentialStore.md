---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCredentialStore.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchCredentialStore
---

# Remove-OrchCredentialStore

## SYNOPSIS

Removes credential stores from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchCredentialStore [-Path <string[]>] [-Name] <string[]> [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes credential stores from UiPath Orchestrator. This cmdlet deletes credential store configurations from the specified tenants.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available credential store names. Multiple values can be specified using comma-separated text that includes wildcards.

Credential stores are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: DELETE /odata/CredentialStores({credentialStoreId})

OAuth required scopes: OR.Settings

Required permissions: Settings.Delete

## EXAMPLES

### Example 1: Remove a credential store

```powershell
PS Orch1:\> Remove-OrchCredentialStore MyVault
```

Removes the credential store named 'MyVault' from the current tenant.

### Example 2: Remove credential stores using wildcards

```powershell
PS Orch1:\> Remove-OrchCredentialStore Test*
```

Removes all credential stores whose names match 'Test*'.

### Example 3: Remove a credential store from a specific drive

```powershell
PS C:\> Remove-OrchCredentialStore -Path Orch1:\ OldVault
```

Removes the credential store 'OldVault' from the Orch1 tenant.

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

Specifies the names of the credential stores to remove. Supports wildcards. Tab completion dynamically suggests credential store names from the target drives.

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

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Credential stores are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

## RELATED LINKS

[Get-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialStore.md)

[Copy-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCredentialStore.md)
