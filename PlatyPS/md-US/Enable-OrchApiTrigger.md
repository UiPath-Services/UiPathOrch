---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchApiTrigger

## SYNOPSIS
Enables API triggers in specified folders.

## SYNTAX

```
Enable-OrchApiTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchApiTrigger cmdlet enables previously disabled API triggers in specified folders within UiPath Orchestrator. This cmdlet reactivates API trigger operations, allowing external systems to initiate UiPath processes through HTTP API calls again.

API triggers enable external systems to start UiPath processes through HTTP API calls. Enabling them restores trigger functionality, allowing new process instances to be started through these triggers. This is typically used after maintenance or troubleshooting activities where triggers were temporarily disabled.

Use the -Name parameter to specify which API triggers to enable. The cmdlet supports wildcard patterns for enabling multiple triggers efficiently. The -Path parameter allows targeting specific folders, and -Recurse enables processing all subfolders.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables enabling API triggers from all subfolders.

Primary Endpoint: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Triggers.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Enable-OrchApiTrigger ProcessTrigger
```

Enables the ProcessTrigger API trigger in the current folder (Development) using positional parameters.

### Example 2
```powershell
PS C:\> Enable-OrchApiTrigger -Path Orch1:\Development DataProcessingTrigger
```

Enables the DataProcessingTrigger API trigger in the Orch1:\Development folder.

### Example 3
```powershell
PS Orch1:\Development> Enable-OrchApiTrigger *Daily*, *Weekly* -WhatIf
```

Shows what would happen when enabling multiple API triggers with names containing Daily or Weekly in the current folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Enable-OrchApiTrigger -Path Orch1:\Development *API* -Confirm
```

Enables all API triggers containing API in their name in the Development folder with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Enable-OrchApiTrigger -Recurse *Integration*
```

Enables all API triggers containing Integration in their names from all subfolders recursively.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchApiTrigger -Enabled $false | Enable-OrchApiTrigger -WhatIf
```

Gets all disabled API triggers and shows what would happen when enabling them using pipeline input.

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
Specifies the Name of the API triggers to be enabled.

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
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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
Specifies that API triggers should be enabled from all subfolders recursively.

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

API triggers enable external systems to start processes through HTTP API calls. Enabling them restores trigger functionality after being disabled. Use Disable-OrchApiTrigger to temporarily suspend trigger operations. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Disable-OrchApiTrigger](Disable-OrchApiTrigger.md)

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)

