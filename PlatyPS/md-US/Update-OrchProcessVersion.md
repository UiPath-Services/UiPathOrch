---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchProcessVersion

## SYNOPSIS
Updates the version of processes.

## SYNTAX

### ReleaseName (Default)
```
Update-OrchProcessVersion [[-Name] <String[]>] [[-Version] <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ReleaseId
```
Update-OrchProcessVersion [-Id <Int64[]>] [[-Version] <String>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Updates the process package referenced by the specified process to the specified version. If no version is specified, it will be updated to the latest version.

Multiple values for the -Path and -Id parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false

OAuth required scopes: OR.Execution

Required permissions: Processes.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Update-OrchProcessVersion -Path Orch1:\ -Recurse * -WhatIf
```

Updates all outdated process versions in the Orch1 tenant to the latest version. Once you have confirmed that there are no issues, remove the `-WhatIf` parameter and run the command again.

### Example 2
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

Updates the package version of the `MyProcess` process in the `Orch1:\Shared` folder to the latest version.

### Example 3
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess 1.0.3
```

Updates the package version of the `MyProcess` process in the `Orch1:\Shared` folder to version `1.0.3`.

### Example 4
```powershell
PS Orch1:\Shared> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

Updates the package version of the `MyProcess` process in the current folder to the latest version.

## PARAMETERS

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.

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
Specifies the Name of the processes whose versions are to be updated.

```yaml
Type: String[]
Parameter Sets: ReleaseName
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folder. If not specified, the current folder will be targeted.

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
Specifies that the operation should include the target folder and all its subfolders.

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

### -Version
Specifies the version to which the process should be updated. If omitted, the process will be updated to the latest version.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -Id
Specifies the unique identifier of the process version to update. This is typically a GUID that identifies the specific version of the process definition.

```yaml
Type: Int64[]
Parameter Sets: ReleaseId
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
