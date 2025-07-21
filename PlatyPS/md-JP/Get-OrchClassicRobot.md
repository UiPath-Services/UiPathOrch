---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchClassicRobot

## SYNOPSIS
クラシック環境からロボットを取得します。

## SYNTAX

```
Get-OrchClassicRobot [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchClassicRobot コマンドレットは、UiPath Orchestrator からクラシックロボット構成を取得します。クラシックロボットは、最新のフォルダーベースのアプローチ以前に使用されたレガシーロボットエンティティで、最新のフォルダー構造ではなくクラシック環境に関連付けられています。

これはフォルダーエンティティコマンドレットです。このコマンドレットを使用するには、最初に Set-Location（cd）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定する必要があります。

クラシックロボットは、既存のロボット展開に対する下位互換性を提供し、主に古い Orchestrator バージョンから最新のフォルダー構造への移行に使用されます。これらのロボットは、レガシーシステムからの環境関連付けとプロセス割り当てを維持します。

主要エンドポイント: GET /odata/Robots?$expand=Machine,Environment

OAuth 必須スコープ: OR.Robots または OR.Robots.Read

必要なアクセス許可: Robots.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Get-OrchClassicRobot
```

現在のフォルダー（Production）からすべてのクラシックロボットを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchClassicRobot -Recurse
```

すべてのフォルダーから再帰的にクラシックロボットを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchClassicRobot -Path Orch1:\Production Robot*
```

Production フォルダーから名前が "Robot" で始まるクラシックロボットを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchClassicRobot -Recurse | Select-Object Path, Name, Type, MachineName, EnvironmentName
```

すべてのクラシックロボットを再帰的に取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS C:\> Get-OrchClassicRobot -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Production および Development フォルダーから最大深度 2 レベルでクラシックロボットを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchClassicRobot -ExportCsv "classic-robots-report.csv" -CsvEncoding UTF8
```

すべてのクラシックロボット情報を UTF8 エンコーディングで CSV ファイルにエクスポートします。

### Example 7
```powershell
PS Orch1:\> Get-OrchClassicRobot | Where-Object Type -eq "Attended"
```

Attended タイプのクラシックロボットのみを取得します。

### Example 8
```powershell
PS Orch1:\> Get-OrchClassicRobot | Group-Object EnvironmentName | Select-Object Name, Count
```

環境名でクラシックロボットをグループ化し、各環境のロボット数を表示します。

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
取得するクラシックロボットの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのロボットが返されます。

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
クラシックロボットを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにする場合にこのパラメーターを使用します。

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
サブフォルダーからクラシックロボットを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から開始してフォルダー階層全体を横断します。

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

### -CsvEncoding
CSV エクスポート時の文字エンコーディングを指定します。-ExportCsv パラメーターと組み合わせて使用します。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
クラシックロボット情報をエクスポートする CSV ファイルのパスを指定します。このパラメーターが指定された場合、通常のオブジェクト出力の代わりに CSV ファイルが作成されます。

```yaml
Type: String
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

### UiPath.PowerShell.Entities.Session
## NOTES

このコマンドレットはフォルダーエンティティで動作します。つまり、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

クラシックロボットは UiPath Orchestrator のレガシー機能であり、主に下位互換性のために維持されています。新しい展開では、最新のフォルダーベースのロボット管理アプローチ（Get-OrchRobot および関連コマンドレット）の使用が推奨されます。

大きなフォルダー階層で -Recurse パラメーターを使用する場合、操作の完了にかなりの時間がかかる場合があります。必要に応じて -Depth を使用して横断範囲を制限することを検討してください。

クラシックロボットは、Type（Attended、Unattended、StudioX、StudioPro）、MachineName、EnvironmentName などのプロパティを持ち、従来のロボット管理システムからの完全な情報を提供します。

主要エンドポイント: GET /odata/Robots?$expand=Machine,Environment
OAuth 必須スコープ: OR.Robots または OR.Robots.Read
必要なアクセス許可: Robots.View

## RELATED LINKS

[Get-OrchClassicEnvironment](Get-OrchClassicEnvironment.md)

[Get-OrchRobot](Get-OrchRobot.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)

[Get-OrchMachine](Get-OrchMachine.md)
