---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchLocation

## SYNOPSIS
現在の場所を UiPathOrch モジュールのインストールディレクトリに設定します。

## SYNTAX

```
Set-OrchLocation [[-ModuleName] <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Set-OrchLocation コマンドレットは、現在の場所（作業ディレクトリ）を UiPathOrch モジュールがインストールされているディレクトリに変更します。これにより、モジュールのファイルとリソースへの迅速なナビゲーションが可能になります。

プライマリ エンドポイント: (none)

OAuth 必要なスコープ: (none)

必要な権限: (none)

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-OrchLocation
PS C:\Program Files\PowerShell\7\Modules\UiPathOrch>
```

現在の場所を UiPathOrch モジュールのインストールディレクトリに設定します。プロンプトは実行後の新しい場所を示します。

### Example 2
```powershell
PS C:\> Push-Location; Set-OrchLocation; Get-ChildItem
PS C:\Program Files\PowerShell\7\Modules\UiPathOrch> Pop-Location
```

Push-Location を使用して現在の場所を保存し、UiPathOrch モジュールディレクトリに移動し、内容をリストしてから、Pop-Location を使用して元の場所に戻ります。

### Example 3
```powershell
PS C:\> Set-OrchLocation
PS C:\Program Files\PowerShell\7\Modules\UiPathOrch> Get-ChildItem Docs
```

UiPathOrch モジュールディレクトリに移動してから、Docs サブディレクトリを探索してドキュメントファイルにアクセスします。

## PARAMETERS

### -ModuleName
モジュールの名前を指定します。指定された場合、そのモジュールがインストールされているディレクトリに移動します。

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
このコマンドレットによって生成される進行状況の更新に PowerShell がどのように応答するかを決定します。既定値は Continue です。

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
このコマンドレットは共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

このコマンドレットは、現在の PowerShell の場所を変更する便利な関数です。出力は返しません（Void 戻り値タイプ）。

対象ディレクトリは、読み込まれた UiPathOrch モジュールの ModuleBase プロパティによって決定されます。モジュールが読み込まれていない場合、コマンドレットは失敗する可能性があります。

このコマンドレットを呼び出す前に Push-Location を使用して現在の場所を保存し、モジュールディレクトリを探索した後に Pop-Location を使用して元の場所に戻ることができます。

モジュールディレクトリには通常、次のものが含まれます：
- Docs: ドキュメントファイル
- Examples: サンプルスクリプトと使用例
- Functions: PowerShell 関数定義
- ローカライズされたヘルプファイル（en-US、ja-JP など）
- モジュールマニフェストとスクリプトファイル

このコマンドレットは、モジュール開発、トラブルシューティング、またはモジュールリソースに直接アクセスする必要がある場合に特に役立ちます。

## RELATED LINKS
