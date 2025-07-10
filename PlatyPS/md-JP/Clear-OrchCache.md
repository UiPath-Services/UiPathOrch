---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Clear-OrchCache

## SYNOPSIS
UiPathOrch ドライブのインメモリキャッシュをクリアします。

## SYNTAX

```
Clear-OrchCache [[-Path] <String[]>] [-AllDrives] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
UiPathOrch モジュールは、応答時間を最適化し、Orchestrator サーバーへの負荷を軽減するために、各ドライブで Orchestrator から取得したエンティティをキャッシュします。特に、このキャッシュはパラメーター値の自動補完に適切な候補を表示するために使用されます。

Orchestrator Web または他の外部アプリケーション経由で行われたエンティティの更新（フォルダーの作成や削除など）を PowerShell コンソールに反映させたい場合は、このコマンドレットでインメモリキャッシュをクリアしてください。

-Path パラメーターには、キャッシュをクリアするドライブを指定します。-Path パラメーターが指定されていない場合は、現在のドライブのキャッシュがクリアされます。現在のドライブが UiPathOrch ドライブでない場合は、すべての UiPathOrch ドライブのキャッシュがクリアされます。

プライマリエンドポイント: (なし)

OAuth 必要スコープ: (なし)

必要なアクセス許可: (なし)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Clear-OrchCache
```

現在のドライブが Orch1: のため、Orch1: のインメモリキャッシュをクリアします。

### Example 2
```powershell
PS C:\> Clear-OrchCache
```

現在のドライブ（C:）が UiPathOrch ドライブでないため、UiPathOrch で管理されているすべてのドライブのインメモリキャッシュをクリアします。

### Example 3
```powershell
PS C:\> Clear-OrchCache Orch1:,Orch2:
```

Orch1: と Orch2: ドライブのインメモリキャッシュをクリアします。

### Example 4
```powershell
PS Orch1:\> Clear-OrchCache .,Orch2:
```

現在のドライブはピリオド（.）を使用して指定でき、これは現在の場所を表します。このコマンドでは、現在のドライブ（Orch1:）と Orch2: の両方でキャッシュがクリアされます。

## PARAMETERS

### -Path
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
コマンドレットによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
コマンドレットを実行した場合の動作を表示します。コマンドレットは実行されません。

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

### -AllDrives
現在の場所に関係なく、すべての UiPathOrch ドライブのキャッシュをクリアします。このパラメーターは -Path パラメーターと同時に使用できません。

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

### None
## NOTES

## RELATED LINKS
