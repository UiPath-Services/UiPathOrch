---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchApiTrigger

## SYNOPSIS
Copies API triggers to destination folders.

## SYNTAX

```
Copy-OrchApiTrigger [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchApiTrigger cmdlet copies API triggers from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of API triggers, including their configurations, webhook URLs, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). API triggers can be copied to maintain consistency across different environments or for deployment automation.

Use the -Name parameter to specify which API triggers to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple API triggers efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying API triggers from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/HttpTriggers, POST /odata/HttpTriggers

OAuth required scopes: OR.Jobs or OR.Jobs.Read or OR.Jobs.Write

Required permissions: Jobs.View, Jobs.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchApiTrigger WebhookTrigger Orch1:\Production
```

Copies the WebhookTrigger API trigger from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchApiTrigger -Path Orch1:\Development ProcessWebhook Orch2:\Production
```

Copies the ProcessWebhook API trigger from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant API trigger copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchApiTrigger *API*, *Webhook* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple API triggers with names containing API or Webhook from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchApiTrigger -Path Orch1:\Development *Process* Orch2:\Production
```

Copies all API triggers containing Process in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchApiTrigger -Recurse *External* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all API triggers containing External from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchApiTrigger *Integration* | Copy-OrchApiTrigger -Destination Orch2:\Production
```

Gets all API triggers containing Integration in their names and copies them to Orch2:\Production using pipeline input with wildcard filtering.

## PARAMETERS

### -Destination
Specifies the destination folders.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the API triggers to be copied.

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
Specifies the source folders. If not specified, the current folder will be used as the source.

```yaml
Type: String
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

### -Recurse
Specifies that API triggers should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

The cmdlet supports both intra-tenant and inter-tenant copying. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)
