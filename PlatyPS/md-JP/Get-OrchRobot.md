---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchRobot

## SYNOPSIS
ユーザーから自動プロビジョニングされたロボットを取得します。

## SYNTAX

```
Get-OrchRobot [[-FullName] <String[]>] [[-Username] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

プライマリエンドポイント: GET /odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots?$expand=User

OAuth必須スコープ: OR.Robots または OR.Robots.Read

必要な権限: Users.View および Robots.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchRobot
```

現在のOrchestrator環境内のすべての自動プロビジョニングされたロボットを取得します。これは、ユーザーがUiPath Studioから接続したときに自動的に作成されたロボットを表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchRobot myrobot*
```

特定のユーザー名に関連付けられた自動プロビジョニングされたロボットを取得します。効率的なフィルタリングのためにUsernameパラメータを使用します。

### Example 3
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object Type -eq Unattended
```

フルネームにSmithを含む自動プロビジョニングされたロボットを取得します。FullNameパラメータでワイルドカードパターンフィルタリングを使用します。

### Example 4
```powershell
PS C:\> Get-OrchRobot -Path Orch1:, Orch2:
```

複数のOrchestratorインスタンスから自動プロビジョニングされたロボットを取得します。複数の環境間でのフォルダエンティティ操作を示しています。

### Example 5
```powershell
PS Orch1:\> Get-OrchRobot | ConvertTo-Json
```

すべての自動プロビジョニングされたロボットを取得し、詳細な分析や他のシステムとの統合のために出力をJSON形式に変換します。

## PARAMETERS

### -FullName
取得するロボットのFullNameを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットドライブの名前を指定します。指定されていない場合は、現在のドライブがターゲットになります。

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
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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

### -Username
取得するロボットのUserNameを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Robot
## NOTES

プライマリエンドポイント: GET /odata/Robots
OAuth必須スコープ: OR.Robots または OR.Robots.Read
必要な権限: Robots.View

## RELATED LINKS
