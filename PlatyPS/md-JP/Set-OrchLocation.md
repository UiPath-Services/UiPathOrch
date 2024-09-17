---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchLocation

## SYNOPSIS
現在の場所を、UiPathOrch モジュールのインストールディレクトリにします。

## SYNTAX

```
Set-OrchLocation [[-ModuleName] <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Set-OrchLocation コマンドレットは、現在の場所（作業ディレクトリ）を UiPathOrch モジュールのインストールディレクトリに変更します。モジュールのファイルやリソースに素早くアクセスできます。

主に呼び出すエンドポイント:

OAuth に必要なスコープ:

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-OrchLocation
```

このコマンドは、現在の位置を UiPathOrch モジュールのインストールディレクトリに設定します。

### Example 2
```powershell
PS C:\> Set-OrchLocation UiPath.PowerShell.OrchProvider
```

モジュール名を明示することもできます。

## PARAMETERS

### -ModuleName
モジュール名を指定すると、そのモジュールがインストールされたディレクトリに移動します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
