---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchPmRobotAccount

## SYNOPSIS
Creates or updates robot accounts in the Identity server.

## SYNTAX

### ConsoleInput (Default)
```
Set-OrchPmRobotAccount [[-GroupName] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### CsvInput
```
Set-OrchPmRobotAccount [[-GroupName] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Updates the groups to which the robot account in the Identity server belongs. If a non-existent robot account name is specified, a new robot account will be created with that name and added to the specified group.

Multiple robot account names can be specified, separated by commas. However, please note that wildcards cannot be used in the account name. This is a trade-off for this cmdlet's ability to both create and update. For the group names, you can specify multiple comma-separated texts containing wildcards.

You can also create or update a robot account by importing a CSV file. To generate a CSV file in an importable format, run `Get-OrchPmRobot -ExportCsv`. To see how to import this CSV file, refer to Example 2.

The column names in the CSV file correspond to each parameter name. Please refer to the help for each parameter.

Primary Endpoint:

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Set-OrchPmRobotAcount RobotAccountName MyGroup,YourGroup
```

Updates the groups for the robot account `RobotAccountName` to `MyGroup` and `YourGroup`. If a robot account with the name `RobotAccountName` does not exist, a new robot account will be created with this name and added to `MyGroup` and `YourGroup`.

### Example 2
```powershell
PS Orch1:\> Import-Csv c:ExportedPmRobotAccount.csv | Set-OrcPmRobotAcount
```

Imports a CSV file to create or update robot accounts in the Identity server. To generate a CSV file in an importable format, run `Get-OrchPmRobot -ExportCsv`. Please note that the Path column specified in the CSV file takes precedence over the current drive.

### Example 3
```powershell
PS Orch1:\> Import-Csv c:ExportedPmRobotAccount.csv | Set-OrchPmRobotAcount -Path .
```

Any column in the CSV file can be overridden by specifying the parameter on the command line in the PS console. For example, to add a robot account to the tenant in the current drive instead of the drive specified in the Path column of the CSV file, specify `-Path .`. Alternatively, you can simply import a CSV file with the Path column removed to add/update robot accounts in the tenant of the current drive.

## PARAMETERS

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

### -GroupName
Specifies the name of the group(s) to which the specified robot account belongs. The specified robot account will be removed from any group that is not specified here.
To specify GroupName in a CSV file, name the columns GroupName0, GroupName1, ..., with one group name in each. You can use up to GroupName9.


```yaml
Type: String[]
Parameter Sets: ConsoleInput
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: CsvInput
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount
## NOTES

## RELATED LINKS
