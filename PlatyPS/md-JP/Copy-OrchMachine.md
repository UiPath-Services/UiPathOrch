---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchMachine

## SYNOPSIS
UiPath Orchestrator テナント間でマシンテンプレートをコピーします。

## SYNTAX

```
Copy-OrchMachine [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
`Copy-OrchMachine` コマンドレットは、現在の UiPath Orchestrator テナントから 1 つ以上の宛先テナントにマシンテンプレートをコピーします。これは、複数の Orchestrator 環境間でマシン構成を複製したり、標準化されたマシンテンプレートで新しいテナントを設定したりする際に便利です。

このコマンドレットはテナントエンティティで動作し、スロット割り当て、スコープ、自動化タイプ、その他のプロパティなどの構成設定を含むマシンテンプレートをコピーします。ライセンスキーやクライアントシークレットなどの機密情報は、宛先テナント用に再生成されることに注意してください。

主要エンドポイント: GET /odata/Machines, POST /odata/Machines

OAuth 必要スコープ: OR.Machines

必要な権限: Machines.View, Machines.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchMachine "[Default] Agent Service" Orch2:
```

"[Default] Agent Service" マシンテンプレートを Orch2 テナントにコピーします。

### Example 2
```powershell
PS Orch1:\> Copy-OrchMachine "*Template*" Orch2:, Orch3: -WhatIf
```

名前に "Template" を含むすべてのマシンテンプレートを Orch2 と Orch3 の両方のテナントにコピーする場合に何が起こるかを、実際のコピー操作を実行せずに表示します。

### Example 3
```powershell
PS Orch1:\> Get-OrchMachine | Where-Object {$_.Type -eq "Template"} | Copy-OrchMachine Orch2:
```

すべてのマシンテンプレートを取得し、パイプライン入力を使用して Orch2 テナントにコピーします。

### Example 4
```powershell
PS Orch1:\> Copy-OrchMachine "Production Template" Orch2: -Confirm
```

実行前に確認プロンプトを表示して、"Production Template" マシンを Orch2 にコピーします。

### Example 5
```powershell
PS C:\> Copy-OrchMachine -Path Orch1: "Cloud Robot Template" Orch2:, Orch3:
```

ソーステナントを明示的に指定して、"Cloud Robot Template" を Orch1 テナントから Orch2 と Orch3 の両方のテナントにコピーします。

## PARAMETERS

### -Destination
マシンテンプレートをコピーする宛先テナントを指定します。テナントドライブ名（例：Orch2:、Orch3:）を使用してください。コンマ区切りの値を使用して複数の宛先を指定できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
コピーするマシンテンプレートの名前を指定します。パターンマッチングのためのワイルドカード（* と ?）をサポートしています。コンマ区切りの値を使用して複数のマシン名を指定できます。

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
ソーステナントパスを指定します。指定しない場合、現在のテナントが使用されます。テナントドライブ名（例：Orch1:、Orch2:）を使用してください。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current tenant
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
コマンドレットで生成される進行状況の更新に対して PowerShell がどのように応答するかを指定します。

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
コマンドレットの実行前に確認を求めます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。コマンドレットは実行されません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedMachine
## NOTES

**重要なセキュリティ上の考慮事項:**
- ライセンスキーとクライアントシークレットは、セキュリティ上の理由により宛先テナントで再生成されます
- コピーされたマシンテンプレートは新しい一意の識別子を持ちます
- ソーステナントと宛先テナントの両方で適切な権限を持っていることを確認してください

**コピーされるマシンテンプレートのプロパティ:**
- 名前と説明
- マシンタイプとスコープ
- スロット割り当て（Unattended、NonProduction、TestAutomation など）
- 自動化タイプとターゲットフレームワーク設定
- 更新ポリシー設定
- タグとメンテナンスウィンドウの設定

**使用例:**
- 複数のテナント間でマシン構成を標準化
- 既存のテンプレートで新しい Orchestrator 環境を設定
- 開発環境と本番環境間でマシンテンプレートを移行
- 重要なマシン構成のバックアップコピーを作成

**ベストプラクティス:**
- 実行前に変更をプレビューするために -WhatIf を使用
- 重要な本番操作には -Confirm を使用
- コピー後に宛先テナントでマシンテンプレートの機能を確認
- コピー後にテナント固有の設定を更新

コピー前に利用可能なマシンテンプレートを表示するには Get-OrchMachine を使用してください。

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[New-OrchMachine](New-OrchMachine.md)

[Remove-OrchMachine](Remove-OrchMachine.md)
