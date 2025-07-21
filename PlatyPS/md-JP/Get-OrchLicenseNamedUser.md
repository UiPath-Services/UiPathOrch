---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicenseNamedUser

## SYNOPSIS
UiPath Orchestratorから名前付きユーザーライセンス情報を取得します。

## SYNTAX

```
Get-OrchLicenseNamedUser [[-RobotType] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchLicenseNamedUserコマンドレットは、UiPath Orchestratorから名前付きユーザーライセンス情報を取得します。名前付きユーザーライセンスは特定のユーザーに割り当てられ、さまざまなロボットタイプ（Attended、StudioPro、Unattended、NonProduction、TestAutomationなど）でUiPathロボットを使用できるようにします。

返される各オブジェクトには、ユーザーのライセンス状況、マシン割り当て、ログイン履歴、およびさまざまなロボットタイプのライセンス消費詳細に関する情報が含まれています。

これは、Orchestratorインスタンス全体のライセンス情報を取得するテナントレベルの操作です。

主要エンドポイント: GET /odata/LicensesNamedUser/UiPath.Server.Configuration.OData.GetLicensesNamedUser(robotType='{robotType}')

OAuth必須スコープ: OR.License または OR.License.Read

必要な権限: [PLACEHOLDER - License.View]

## EXAMPLES

### Example 1: すべての名前付きユーザーライセンスを取得
```powershell
PS Orch1:\> Get-OrchLicenseNamedUser
```

現在のOrchestratorテナントからすべてのロボットタイプの名前付きユーザーライセンス情報を取得し、ロボットタイプ別にグループ化して表示します。

### Example 2: 特定のドライブから名前付きユーザーライセンスを取得
```powershell
PS C:\> Get-OrchLicenseNamedUser -Path Orch1:, Orch2:
```

複数の指定されたOrchestratorドライブから名前付きユーザーライセンス情報を取得します。

### Example 3: 複数のロボットタイプの名前付きユーザーライセンスを取得
```powershell
PS Orch1:\> Get-OrchLicenseNamedUser Attended, StudioPro
```

AttendedとStudioProの両方のロボットタイプの名前付きユーザーライセンス情報を取得します。

### Example 4: ライセンス詳細を取得して構造を確認
```powershell
PS Orch1:\> Get-OrchLicenseNamedUser | Select-Object -First 1 | ConvertTo-Json -Depth 5
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
名前付きユーザーライセンス情報を取得するロボットタイプを指定します。パターンマッチング用のワイルドカード文字（*および?）をサポートします。一般的なロボットタイプには、"Attended"、"StudioPro"、"Unattended"、"NonProduction"、"TestAutomation"などがあります。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.LicenseNamedUser
## NOTES
- 名前付きユーザーライセンスは、さまざまなロボットタイプで特定のユーザーに割り当てられます
- -RobotTypeパラメーターは、柔軟なフィルタリングのためのワイルドカード文字をサポートします
- 一般的なロボットタイプには、"Attended"、"StudioPro"、"Unattended"、"NonProduction"、"TestAutomation"などがあります
- 主要なプロパティには、IsLicensed（ライセンス状況）、MachinesCount（割り当てられたマシン）、LastLoginDate（使用状況追跡）があります
- MachineNames配列にはすべての割り当てられたマシンが含まれ、ActiveMachineNamesには現在アクティブなマシンが含まれます
- IsExternalLicensedは、ライセンスが外部で管理されているかどうかを示します
- このコマンドレットはテナントレベルで動作し、特定のフォルダーへの移動は必要ありません
- ライセンス使用パターンを分析し、非アクティブまたは過剰に割り当てられたユーザーを特定するには、フィルタリング操作を使用してください

主要エンドポイント: GET /odata/LicensesNamedUser/UiPath.Server.Configuration.OData.GetLicensesNamedUser
OAuth必須スコープ: OR.Licenses または OR.Licenses.Read
必要な権限: Licenses.View

## RELATED LINKS

[Get-OrchLicenseRuntime]()

[Get-OrchLicense]()

[Get-OrchUser]()
