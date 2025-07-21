---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmServerInfo

## SYNOPSIS
Test Managerのサーバー情報を取得します。

## SYNTAX

```
Get-TmServerInfo [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-TmServerInfo コマンドレットは、バージョン、サーバータイプ、運用ステータスなど、UiPath Test Managerからサーバー情報を取得します。これは、Test Managerインスタンスの健全性、可用性、システム詳細を監視するために役立ちます。

このコマンドレットは、Test Managerサーバーの現在の状態と設定情報を提供し、管理者がシステムの監視と診断を行うのに役立ちます。

-Pathパラメーターの複数の値は、カンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /api/serverinfo

OAuth必須スコープ：[PLACEHOLDER - Test Manager server info scopes]

必須権限：[PLACEHOLDER - Test Manager server info permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1Tm:\> Get-TmServerInfo
```

現在のTest Managerインスタンスからサーバー情報を取得します。

### Example 2
```powershell
PS C:\> Get-TmServerInfo -Path Orch1Tm:, Orch2Tm:
```

複数のTest Managerインスタンスからサーバー情報を取得します。

### Example 3
```powershell
PS Orch1Tm:\> Get-TmServerInfo | ConvertTo-Json
```

サーバー情報を取得し、詳細な分析のために完全な構造をJSON形式で表示します。

## PARAMETERS

### -Path
対象のTest Managerドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のTest Managerインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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
- このコマンドレットは、Test Managerドライブ（Orch1Tm:など）で動作します
- サーバー情報には、バージョン、サーバータイプ、運用ステータスが含まれます
- Test Managerインスタンスの健全性と可用性の監視に役立ちます
- システムの診断とトラブルシューティングに有用な情報を提供します

主要エンドポイント：GET /api/serverinfo
OAuth必須スコープ：[PLACEHOLDER - Test Manager server info scopes]
必須権限：[PLACEHOLDER - Test Manager server info permissions]

## RELATED LINKS

[Get-TmTestCase](Get-TmTestCase.md)
[Get-TmTestSet](Get-TmTestSet.md)
[about_UiPathOrch](about_UiPathOrch.md)
