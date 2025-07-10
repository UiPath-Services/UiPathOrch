---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachineSession

## SYNOPSIS
UiPath Orchestratorからマシンランタイムセッションを取得します。

## SYNTAX

`
Get-OrchMachineSession [-Status <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
`

## DESCRIPTION
UiPath Orchestratorからマシンランタイムセッションを取得し、フォルダー間でのマシンの接続状態とランタイム情報を表示します。このコマンドレットは、マシンの接続性、ランタイムの可用性、およびロボット実行容量を含むセッション詳細に関する情報を提供します。

プライマリ エンドポイント: /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})

OAuth 必要なスコープ: OR.Robots or OR.Robots.Read

必要な権限: (Machines.View or Jobs.Create)

このコマンドレットはフォルダーエンティティに対して動作し、ターゲットフォルダーへのナビゲーションまたは-Path、-Recurse、-Depthパラメーターを使用したターゲットフォルダーの指定が必要です。

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Shared> Get-OrchMachineSession
`

現在のフォルダー内のすべてのマシンセッションを取得します。

### Example 2
`powershell
PS Orch1:\> Get-OrchMachineSession -Recurse -Status Available
`

-Recurseと-Statusフィルターを使用して、現在のドライブ内のすべてのフォルダーから利用可能なマシンセッションを取得します。

### Example 3
`powershell
PS C:\> Get-OrchMachineSession -Path Orch1:\Shared -Status Disconnected
`

Sharedフォルダーから切断されたマシンセッションを取得し、-Pathと-Statusの使用法を示します。

### Example 4
`powershell
PS Orch1:\> Get-OrchMachineSession -Recurse | Select-Object Path, MachineName, Status, Runtimes | ConvertTo-Json
`

すべてのマシンセッションを取得し、主要な情報をJSON形式で表示して、出力カスタマイゼーション技術を組み合わせます。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深さを指定します。深さ0は現在の場所のみを示し、サブフォルダーは含まれません。

`yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -Path
マシンセッションを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにしたい場合に、このパラメーターを使用します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

### -Recurse
操作がターゲットフォルダーとそのすべてのサブフォルダーを含むことを指定します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### -Status
取得するマシンセッションのステータスを指定します。一般的な値には、Connected、Disconnected、その他があります。パターンマッチング用のワイルドカード文字（*と?）をサポートします。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
操作中に進行状況情報がどのように表示されるかを指定します。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime
## NOTES

このコマンドレットはフォルダーエンティティに対して動作し、以下のいずれかが必要です：
- Set-Location (cd)を使用したターゲットフォルダーへのナビゲーション、または
- -Path、-Recurse、-Depthパラメーターを使用したターゲットフォルダーの指定

**重要:** 最適なPowerShell IntelliSenseサポートのために、複数のパラメーターを使用する場合は、他のパラメーターの前に-Path、-Recurse、-Depthを指定してください。

**マシンセッション情報:**
- SessionId: マシンセッションの一意識別子
- Status: 接続状態（Connected、Disconnected等）
- RuntimeType: ランタイムの種類（Headless、AutomationCloud等）
- Runtimes: 利用可能なランタイムスロットの総数
- UsedRuntimes: 現在使用中のランタイムスロット数
- MaintenanceMode: 現在のメンテナンスモード設定

**一般的なステータス値:**
- Connected: マシンがアクティブに接続されている
- Disconnected: マシンが現在接続されていない
- Busy: マシンがアクティブにジョブを実行している

**使用例:**
- 環境全体でのマシン接続性の監視
- ランタイムの可用性と使用率の確認
- マシン接続問題のトラブルシューティング
- ロボット展開のキャパシティプランニング

**パス選択に関する重要な注意事項:**
フォルダーエンティティでSelect-Objectを使用する場合は、各エンティティがどのフォルダーに属するかを識別するために、常にPathを最初のプロパティとして含めてください。これは、複数のフォルダー間でエンティティを管理するために不可欠です。

詳細な接続情報とランタイム統計を含む完全なマシンセッションオブジェクト構造を探索するには、ConvertTo-Jsonを使用してください。

プライマリ エンドポイント: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId
OAuth 必要なスコープ: OR.Robots or OR.Robots.Read
必要な権限: Robots.View

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
