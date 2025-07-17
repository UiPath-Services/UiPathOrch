---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchApiTrigger

## SYNOPSIS
指定されたフォルダ内のAPIトリガーを無効にします。

## SYNTAX

```
Disable-OrchApiTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchApiTrigger コマンドレットは、UiPath Orchestrator 内の指定されたフォルダ内のAPIトリガーを無効にします。このコマンドレットを使用すると、トリガー構成を削除することなく、一時的にAPIトリガーの動作を停止できるため、メンテナンスシナリオや自動化ワークフローのトラブルシューティング時に役立ちます。

APIトリガーは、外部システムがHTTP API呼び出しを通じてUiPathプロセスを開始することを可能にします。これらを無効にすると、これらのトリガーを通じて新しいプロセスインスタンスが開始されることを防ぎますが、将来の再有効化のためにトリガー構成は保持されます。

-Name パラメーターを使用して、無効にするAPIトリガーを指定します。コマンドレットは、複数のトリガーを効率的に無効にするためのワイルドカードパターンをサポートしています。-Path パラメーターを使用すると特定のフォルダを対象にでき、-Recurse を使用するとすべてのサブフォルダを処理できます。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからAPIトリガーを無効にできます。

主要エンドポイント: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth 必要なスコープ: OR.Execution or OR.Execution.Write

必要な権限: Triggers.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Disable-OrchApiTrigger ProcessTrigger
```

位置パラメーターを使用して、現在のフォルダ（Development）内のProcessTrigger APIトリガーを無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchApiTrigger -Path Orch1:\Development DataProcessingTrigger
```

Orch1:\DevelopmentフォルダのDataProcessingTrigger APIトリガーを無効にします。

### Example 3
```powershell
PS Orch1:\Development> Disable-OrchApiTrigger *Daily*, *Weekly* -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダでDailyまたはWeeklyを含む名前の複数のAPIトリガーを無効にした場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Disable-OrchApiTrigger -Path Orch1:\Development *API* -Confirm
```

確認プロンプトを表示して、DevelopmentフォルダでAPIを含む名前のすべてのAPIトリガーを無効にします。

### Example 5
```powershell
PS Orch1:\> Disable-OrchApiTrigger -Recurse *Integration*
```

すべてのサブフォルダから再帰的にIntegrationを含む名前のすべてのAPIトリガーを無効にします。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchApiTrigger *External* | Disable-OrchApiTrigger -WhatIf
```

Externalを含む名前のすべてのAPIトリガーを取得し、パイプライン入力を使用してそれらを無効にした場合に何が起こるかを表示します。

## PARAMETERS

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

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
無効にするAPIトリガーの名前を指定します。

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
ターゲットフォルダを指定します。指定しない場合、現在のフォルダがターゲットになります。

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
すべてのサブフォルダからAPIトリガーを再帰的に無効にすることを指定します。

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
コマンドレットを実行する前に確認を求めます。

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
コマンドレットを実行した場合の動作を示します。
コマンドレットは実行されません。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

APIトリガーは、外部システムがHTTP API呼び出しを通じてプロセスを開始することを可能にします。これらを無効にすると、構成を保持したままトリガー動作が一時的に停止されます。無効にしたトリガーを再度有効にするには、Enable-OrchApiTrigger を使用してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Enable-OrchApiTrigger](Enable-OrchApiTrigger.md)

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)

[Set-OrchApiTrigger](Set-OrchApiTrigger.md)
