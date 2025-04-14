---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmLicensedGroup

## SYNOPSIS
User named license が割り当てられたグループを取得します。

## SYNTAX

```
Get-PmLicensedGroup [[-GroupName] <String[]>] [[-UserName] <String[]>] [-ExpandAllocation] [-Path <String[]>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
このコマンドレットは非公開の API を呼び出すことで実現されています。そのため、将来は動作しなくなる可能性があることに留意してください。

主に呼び出すエンドポイント: GET /api/license/accountant/UserLicense/group/page, GET /api/license/accountant/UserLicense/group/{groupId}/allocations

OAuth に必要なスコープ: PM.Group.Read

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -CsvEncoding
{{ Fill CsvEncoding Description }}

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpandAllocation
グループの中でライセンスが割り当てられているユーザーを表示します。

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

### -ExportCsv
{{ Fill ExportCsv Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupName
取得するグループの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

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

### -UserName
取得するユーザーの名前を指定します。これは -ExpandAllocation を指定したときのみ有効です。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
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
## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedGroup
### UiPath.PowerShell.Entities.NuLicensedGroupMember
## NOTES

## RELATED LINKS
