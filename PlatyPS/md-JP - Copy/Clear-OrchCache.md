---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Clear-OrchCache

## SYNOPSIS
UiPathOrch ドライブ内のメモリキャッシュをクリアします。

## SYNTAX

```
Clear-OrchCache [[-Path] <String[]>] [-AllDrives] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Orchestrator から取得したエンティティは、UiPathOrch モジュールが各ドライブにキャッシュし、応答時間の短縮と Orchestrator への負荷低減に利用します。特に、このキャッシュはパラメーター値を補完入力するときに使われます。

Orchestrator Web 画面やほかの外部アプリケーションで行ったエンティティの更新（フォルダーの作成や削除など）を PowerShell コンソールに反映するには、このコマンドレットでメモリ上のキャッシュをクリアしてください。

-Path パラメータには、キャッシュをクリアしたいドライブの名前を指定します。-Path パラメータを指定しない場合は、現在のドライブのキャッシュがクリアされます。現在のドライブが UiPathOrch ドライブではない場合には、すべての UiPathOrch ドライブのキャッシュがクリアされます。

主に呼び出すエンドポイント: (なし)

OAuth に必要なスコープ: (なし)

必要な権限: (なし)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Clear-OrchCache
```

Clears the in-memory cache on the Orch1: as the current drive.

### Example 2
```powershell
PS C:\> Clear-OrchCache
```

Clears the in-memory cache on all drives managed by UiPathOrch, because the current drive (C:) is not an UiPathOrch drive.

### Example 3
```powershell
PS C:\> Clear-OrchCache Orch1:,Orch2:
```

Clears the in-memory cache on the Orch1: and Orch2: drives.

### Example 4
```powershell
PS Orch1:\> Clear-OrchCache .,Orch2:
```

The current drive can be specified using a period (.), which represents the current location. In this command, the cache is cleared on both the current drive (Orch1:) and Orch2:.

## PARAMETERS

### -Path
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

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

### -Confirm
コマンドレットを実行する前に、あなたの確認を求めます。

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
コマンドレットを実行すると、何が起こるかを表示します。
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

### -AllDrives
{{ Fill AllDrives Description }}

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
