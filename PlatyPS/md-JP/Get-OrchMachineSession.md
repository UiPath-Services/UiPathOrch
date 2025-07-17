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

```
Get-OrchMachineSession [-Status <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorからマシンランタイムセッションを取得し、フォルダ間でのマシンの接続状態とランタイム情報を表示します。このコマンドレットは、マシンの接続性、ランタイムの可用性、ロボット実行能力を含むセッションの詳細に関する情報を提供します。

主要エンドポイント：/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})

OAuth必須スコープ：OR.Robots または OR.Robots.Read

必須権限：(Machines.View または Jobs.Create)

このコマンドレットは、フォルダエンティティで動作し、対象フォルダへのナビゲーションまたは-Path、-Recurse、-Depthパラメーターを使用した対象フォルダの指定のいずれかが必要です。

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchMachineSession
```

現在のフォルダのすべてのマシンセッションを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse -Status Available
```

-Recurseと-Statusフィルターを使用して、現在のドライブのすべてのフォルダから利用可能なマシンセッションを取得します。

### Example 3
```powershell
PS C:\> Get-OrchMachineSession -Path Orch1:\Shared -Status Disconnected
```

-Pathと-Statusの使用方法を示し、Sharedフォルダから切断されたマシンセッションを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse | Select-Object Path, MachineName, Status, Runtimes | ConvertTo-Json
```

すべてのマシンセッションを取得し、出力カスタマイズ技術を組み合わせて、主要な情報をJSON形式で表示します。

## PARAMETERS

### -Depth
対象フォルダへの再帰の深さを指定します。深さ0は現在の場所のみを示し、サブフォルダは含まれません。

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

### -Path
マシンセッションを検索するフォルダパスを指定します。パターンマッチングのためのワイルドカード文字（*と?）をサポートします。現在の場所を変更せずに特定のフォルダを対象にしたい場合にこのパラメーターを使用します。

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

### -Recurse
操作に対象フォルダとすべてのサブフォルダを含めることを指定します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
取得するマシンセッションのステータスを指定します。一般的な値には、Connected、Disconnectedなどが含まれます。パターンマッチングのためのワイルドカード文字（*と?）をサポートします。

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
操作中の進行状況情報の表示方法を指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime
## NOTES

このコマンドレットは、フォルダエンティティで動作し、次のいずれかが必要です：
- Set-Location（cd）を使用した対象フォルダへのナビゲーション、または
- -Path、-Recurse、-Depthパラメーターを使用した対象フォルダの指定

**重要：**最適なPowerShell IntelliSenseサポートのため、複数のパラメーターを使用する場合は、-Path、-Recurse、または-Depthを他のパラメーターより先に指定してください。

**マシンセッション情報：**
- SessionId：マシンセッションの一意識別子
- Status：接続状態（Connected、Disconnectedなど）
- RuntimeType：ランタイムのタイプ（Headless、AutomationCloudなど）
- Runtimes：利用可能なランタイムスロットの総数
- UsedRuntimes：現在使用中のランタイムスロット数
- MaintenanceMode：現在のメンテナンスモード設定

**一般的なステータス値：**
- Connected：マシンが積極的に接続中
- Disconnected：マシンが現在接続されていない
- Busy：マシンがジョブを積極的に実行中

**使用例：**
- 環境間でのマシン接続性の監視
- ランタイムの可用性と利用率の確認
- マシン接続問題のトラブルシューティング
- ロボット展開のための容量計画

**Path選択に関する重要な注意事項：**
フォルダエンティティでSelect-Objectを使用する場合、各エンティティがどのフォルダに属しているかを特定するため、常にPathを最初のプロパティとして含めてください。これは、複数のフォルダにわたってエンティティを管理する際に不可欠です。

詳細な接続情報とランタイム統計を含む完全なマシンセッションオブジェクト構造を探索するには、ConvertTo-Jsonを使用してください。

主要エンドポイント：GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId
OAuth必須スコープ：OR.Robots または OR.Robots.Read
必須権限：Robots.View

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
