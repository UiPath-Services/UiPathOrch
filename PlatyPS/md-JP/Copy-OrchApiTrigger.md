---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchApiTrigger

## SYNOPSIS
API トリガーを宛先フォルダーにコピーします。

## SYNTAX

```
Copy-OrchApiTrigger [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchApiTrigger コマンドレットは、UiPath Orchestrator テナント内のソースフォルダーから宛先フォルダーへ、または異なるテナント間で API トリガーをコピーします。このコマンドレットは、設定、webhook URL、およびメタデータを含む API トリガーの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。API トリガーは、異なる環境間での一貫性を保つため、または展開自動化のためにコピーできます。

コピーする API トリガーを指定するには -Name パラメーターを使用し、対象フォルダーを指定するには -Destination パラメーターを使用します。このコマンドレットは、複数の API トリガーを効率的にコピーするためのワイルドカードパターンをサポートしています。

これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダーから API トリガーをコピーし、宛先でフォルダー構造を維持できます。

主要エンドポイント: GET /odata/HttpTriggers, POST /odata/HttpTriggers

OAuth 必要スコープ: OR.Jobs または OR.Jobs.Read または OR.Jobs.Write

必要なアクセス許可: Jobs.View, Jobs.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchApiTrigger WebhookTrigger Orch1:\Production
```

位置パラメーターを使用して、WebhookTrigger API トリガーを現在のフォルダー（Development）から同じテナント内の Production フォルダーにコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchApiTrigger -Path Orch1:\Development ProcessWebhook Orch2:\Production
```

ProcessWebhook API トリガーを Orch1:\Development から Orch2:\Production にコピーし、テナント間 API トリガーコピーを実演します。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchApiTrigger *API*, *Webhook* Orch1:\Production -WhatIf
```

安全のため -WhatIf を使用して、API または Webhook を含む名前の複数の API トリガーを現在のフォルダーから Production フォルダーにコピーする際に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchApiTrigger -Path Orch1:\Development *Process* Orch2:\Production
```

ワイルドカードを使用してテナント間コピーを行い、名前に プロセス を含むすべての API トリガーを Orch1:\Development から Orch2:\Production にコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchApiTrigger -Recurse *External* Orch2:\Finance -WhatIf
```

External を含むすべての API トリガーをすべてのサブフォルダーから再帰的に Orch2:\Finance にコピーする際に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchApiTrigger *Integration* | Copy-OrchApiTrigger -Destination Orch2:\Production
```

名前に Integration を含むすべての API トリガーを取得し、ワイルドカードフィルタリングでパイプライン入力を使用して Orch2:\Production にコピーします。

## PARAMETERS

### -Destination
宛先フォルダーを指定します。

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
コピーする API トリガーの名前を指定します。

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
ソースフォルダーを指定します。指定されていない場合は、現在のフォルダーがソースとして使用されます。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合の動作を表示します。
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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダーレベルの最大数を指定します。

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
API トリガーをすべてのサブフォルダーから再帰的にコピーし、宛先でフォルダー構造を維持することを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。

このコマンドレットは、テナント内とテナント間の両方のコピーをサポートします。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)

