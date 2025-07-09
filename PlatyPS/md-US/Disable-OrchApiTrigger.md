---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchApiTrigger

## SYNOPSIS
Disables API triggers in specified folders.

## SYNTAX

```
Disable-OrchApiTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchApiTrigger cmdlet disables API triggers in specified folders within UiPath Orchestrator. This cmdlet allows you to temporarily suspend API trigger operations without deleting the trigger configurations, making it useful for maintenance scenarios or when troubleshooting automation workflows.

API triggers enable external systems to initiate UiPath processes through HTTP API calls. Disabling them prevents new process instances from being started through these triggers while preserving the trigger configuration for future re-enablement.

Use the -Name parameter to specify which API triggers to disable. The cmdlet supports wildcard patterns for disabling multiple triggers efficiently. The -Path parameter allows targeting specific folders, and -Recurse enables processing all subfolders.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables disabling API triggers from all subfolders.

Primary Endpoint: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Triggers.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Disable-OrchApiTrigger ProcessTrigger
```

Disables the ProcessTrigger API trigger in the current folder (Development) using positional parameters.

### Example 2
```powershell
PS C:\> Disable-OrchApiTrigger -Path Orch1:\Development DataProcessingTrigger
```

Disables the DataProcessingTrigger API trigger in the Orch1:\Development folder.

### Example 3
```powershell
PS Orch1:\Development> Disable-OrchApiTrigger *Daily*, *Weekly* -WhatIf
```

Shows what would happen when disabling multiple API triggers with names containing Daily or Weekly in the current folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Disable-OrchApiTrigger -Path Orch1:\Development *API* -Confirm
```

Disables all API triggers containing API in their name in the Development folder with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Disable-OrchApiTrigger -Recurse *Integration*
```

Disables all API triggers containing Integration in their names from all subfolders recursively.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchApiTrigger *External* | Disable-OrchApiTrigger -WhatIf
```

Gets all API triggers containing External in their names and shows what would happen when disabling them using pipeline input.

## PARAMETERS

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the Name of the API triggers to be disabled.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folders. If not specified, the current folder will be targeted.

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

### -Recurse
Specifies that API triggers should be disabled from all subfolders recursively.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

API triggers enable external systems to start processes through HTTP API calls. Disabling them temporarily suspends trigger operations while preserving configurations. Use Enable-OrchApiTrigger to re-enable disabled triggers. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Enable-OrchApiTrigger](Enable-OrchApiTrigger.md)

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)

[Set-OrchApiTrigger](Set-OrchApiTrigger.md)
