---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicenseRuntime

## SYNOPSIS
UiPath Orchestratorからロボットのランタイムライセンス情報を取得します。

## SYNTAX

```
Get-OrchLicenseRuntime [[-RobotType] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchLicenseRuntimeコマンドレットは、UiPath Orchestratorからロボットのランタイムライセンス情報を取得します。このコマンドレットは、ロボットがライセンス済みか、有効か、オンライン状況、およびさまざまなロボットタイプ（Unattended、NonProduction、TestAutomationなど）のランタイム容量を含むロボットライセンス状況の詳細を提供します。

返される各オブジェクトには、マシンの詳細、ライセンス状況、ランタイム数、および現在の実行状態を含むロボットマシンに関する包括的な情報が含まれています。

これは、Orchestratorインスタンス全体のライセンス情報を取得するテナントレベルの操作です。

主要エンドポイント: GET /odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{robotType}')

OAuth必須スコープ: OR.License または OR.License.Read

必要な権限: [PLACEHOLDER - License.View]

## EXAMPLES

### Example 1: すべてのランタイムライセンスを取得
```powershell
PS Orch1:\> Get-OrchLicenseRuntime
```

現在のOrchestratorテナントからすべてのロボットタイプのランタイムライセンス情報を取得し、ロボットタイプ別にグループ化して表示します。

### Example 2: 特定のドライブからランタイムライセンスを取得
```powershell
PS C:\> Get-OrchLicenseRuntime -Path Orch1:, Orch2:
```

複数の指定されたOrchestratorドライブからランタイムライセンス情報を取得します。

### Example 3: 複数のロボットタイプのランタイムライセンスを取得
```powershell
PS Orch1:\> Get-OrchLicenseRuntime Unattended, NonProduction
```

UnattendedとNonProductionの両方のロボットタイプのランタイムライセンス情報を取得します。

### Example 4: ライセンス詳細を取得して構造を確認
```powershell
PS Orch1:\> Get-OrchLicenseRuntime | Select-Object -First 1 | ConvertTo-Json -Depth 5
```

最初のライセンスレコードを取得し、詳細な分析のために完全なオブジェクト構造をJSON形式で表示します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定されていない場合、現在のドライブがターゲットになります。このパラメーターは、複数のOrchestratorインスタンス間でライセンス情報をクエリすることを可能にします。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### -RobotType
ランタイムライセンス情報を取得するロボットタイプを指定します。パターンマッチング用のワイルドカード文字（*および?）をサポートします。一般的なロボットタイプには、"Unattended"、"NonProduction"、"TestAutomation"、"Development"などがあります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.LicenseRuntime
## NOTES
- ランタイムライセンスは、さまざまなロボットタイプの実行容量を制御します
- -RobotTypeパラメーターは、柔軟なフィルタリングのためのワイルドカード文字をサポートします
- 一般的なロボットタイプには、"Unattended"、"NonProduction"、"TestAutomation"、"Development"などがあります
- 主要なプロパティには、IsLicensed（ライセンス状況）、IsOnline（接続性）、Runtimes（容量）、ExecutingCount（現在の使用状況）があります
- MachineScopeは、マシンがDefaultテナントスコープかPersonalWorkspaceかを示します
- このコマンドレットはテナントレベルで動作し、特定のフォルダーへの移動は必要ありません
- さまざまなロボットタイプ間でのライセンス消費パターンを分析するには、フィルタリングとグループ化操作を使用してください

主要エンドポイント: GET /odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime
OAuth必須スコープ: OR.Licenses または OR.Licenses.Read
必要な権限: Licenses.View

## RELATED LINKS

[Get-OrchLicenseNamedUser]()

[Enable-OrchLicenseRuntime]()

[Disable-OrchLicenseRuntime]()

[Get-OrchLicense]()
