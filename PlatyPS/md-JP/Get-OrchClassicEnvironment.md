---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchClassicEnvironment

## SYNOPSIS
クラシックフォルダーから環境を取得します。

## SYNTAX

```
Get-OrchClassicEnvironment [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchClassicEnvironment コマンドレットは、UiPath Orchestrator からクラシック環境構成を取得します。クラシック環境は、最新のフォルダーベースのアプローチ以前に使用されたレガシーロボットグループ化メカニズムで、ロボットコレクションとそれらに関連するプロセスが含まれます。

クラシック環境は、特定のフォルダー内に存在するフォルダーエンティティです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。

これはフォルダーエンティティコマンドレットです。このコマンドレットを使用するには、最初に Set-Location（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定する必要があります。

クラシック環境は、既存のロボット展開に対する下位互換性を提供し、主に古い Orchestrator バージョンから最新のフォルダー構造への移行に使用されます。

主要エンドポイント: GET /odata/Environments?$expand=Robots

OAuth 必須スコープ: OR.Robots または OR.Robots.Read

必要なアクセス許可: Environments.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Get-OrchClassicEnvironment
```

現在のフォルダー（Production）からすべてのクラシック環境を取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Recurse
```

すべてのフォルダーから再帰的にクラシック環境を取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Path Orch1:\Production Prod*
```

Production フォルダーから名前が "Prod" で始まるクラシック環境を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Recurse | Select-Object Path, Name, Id, RobotsCount
```

すべてのクラシック環境を再帰的に取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS C:\> Get-OrchClassicEnvironment -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Production および Development フォルダーから最大深度 2 レベルでクラシック環境を取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchClassicEnvironment | Where-Object RobotsCount -gt 0
```

ロボットが関連付けられているクラシック環境のみを取得します。

## PARAMETERS

### -Depth
再帰操作の最大深度レベルを指定します。-Recurse が指定されている場合に、再帰検索の深度を制限するためにこのパラメーターを使用します。

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
取得するクラシック環境の名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべての環境が返されます。

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

### -Path
クラシック環境を検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにする場合にこのパラメーターを使用します。

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

### -Recurse
サブフォルダーからクラシック環境を再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から開始してフォルダー階層全体を横断します。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Environment
## NOTES

このコマンドレットはフォルダーエンティティで動作します。つまり、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

クラシック環境は UiPath Orchestrator のレガシー機能であり、主に下位互換性のために維持されています。新しい展開では、最新のフォルダーベースのロボット管理アプローチの使用が推奨されます。

大きなフォルダー階層で -Recurse パラメーターを使用する場合、操作の完了にかなりの時間がかかる場合があります。必要に応じて -Depth を使用して横断範囲を制限することを検討してください。

主要エンドポイント: GET /odata/Environments?$expand=Robots
OAuth 必須スコープ: OR.Robots または OR.Robots.Read
必要なアクセス許可: Environments.View

## RELATED LINKS

[Get-OrchClassicRobot](Get-OrchClassicRobot.md)

[Get-OrchRobot](Get-OrchRobot.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)
