---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmRobotAccount

## SYNOPSIS
ロボットアカウントを組織間でコピーします。

## SYNTAX

```
Copy-PmRobotAccount [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-PmRobotAccount コマンドレットは、UiPath Process Mining 内で、ソース組織から宛先組織にロボットアカウントをコピーします。このコマンドレットは、認証設定、権限、グループ関連付けを含むロボットアカウント構成のコピーを作成し、複数の組織環境間でのロボットアカウント管理を可能にします。

このコマンドレットは、複数の宛先組織に同時にロボットアカウントをコピーすることをサポートします。ロボットアカウントは Name パラメーターで識別でき、コマンドレットは複数のロボットアカウントを効率的にコピーするためのワイルドカードパターンをサポートしています。

ロボットアカウントが所属するグループが宛先組織に存在しない場合、コピー操作中に自動的に作成され、完全なアカウント構成の転送が保証されます。

-Name パラメーターを使用してコピーするロボットアカウントを指定し、-Destination パラメーターを使用してターゲット組織を指定します。-Path パラメーターを使用すると、特定の組織コンテキスト内で操作していない場合に、複数のソース組織で作業できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:、Orch2:）を指定し、-Destination はロボットアカウントをコピーする宛先組織ドライブを指定します。

プライマリエンドポイント: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth 必要なスコープ: [PLACEHOLDER]

必要な権限: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmRobotAccount ProcessBot Orch2:
```

現在の組織（Orch1）からOrch2組織にProcessBotロボットアカウントをコピーします。

### Example 2
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: DataCollector Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方の組織にDataCollectorロボットアカウントをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-PmRobotAccount AutomationBot, AnalyticsBot Orch2: -WhatIf
```

現在の組織からOrch2にAutomationBotとAnalyticsBotをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: *Service* Orch2:
```

ワイルドカードパターンを使用して、Orch1からOrch2にServiceを含む名前のすべてのロボットアカウントをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-PmRobotAccount *Monitor* | Copy-PmRobotAccount -Destination Orch2:, Orch3:
```

Monitorを含む名前のすべてのロボットアカウントを取得し、パイプライン入力を使用してOrch2とOrch3の両方の組織にコピーします。

### Example 6
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: IntegrationBot Orch2: -Confirm
```

確認プロンプトを表示して、Orch1からOrch2にIntegrationBotロボットアカウントをコピーします。

## PARAMETERS

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

### -Destination
ロボットアカウントをコピーする宛先組織ドライブを指定します。

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
コピーするロボットアカウントの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ソース組織ドライブを指定します。指定しない場合、現在の組織がソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### System.Object
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースおよび宛先組織のドライブ名（例：Orch1:、Orch2:）を指定します。

ロボットアカウントには、認証設定、権限、グループ関連付けが含まれます。関連するグループが宛先組織に存在しない場合、自動的に作成されます。環境間でコピーする際は、ロボットアカウント構成が宛先環境に適していることを確認してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmRobotAccount](Get-PmRobotAccount.md)

[New-PmRobotAccount](New-PmRobotAccount.md)

[Remove-PmRobotAccount](Remove-PmRobotAccount.md)

[Set-PmRobotAccount](Set-PmRobotAccount.md)
