---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmServerInfo

## SYNOPSIS
Test Manager のサーバー情報を取得します。

## SYNTAX

```
Get-TmServerInfo [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
このコマンドレットは、UiPathOrchTm プロバイダの PSDrive 上で動作します。設定ファイルに、"TM." を含むスコープを記載すると、UiPathOrchTm プロバイダの PSDrive が自動で追加されます。Get-PSDrive コマンドレットで確認してください。設定ファイルは、Edit-OrchConfig コマンドレットで開けます。

主に呼び出すエンドポイント: GET /testmanager_/api/serverinfo

OAuth に必要なスコープ:

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TmServerInfo
## NOTES

## RELATED LINKS
