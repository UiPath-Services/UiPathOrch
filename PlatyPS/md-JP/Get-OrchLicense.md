---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicense

## SYNOPSIS
UiPath Orchestrator からライセンス情報を取得します。

## SYNTAX

`
Get-OrchLicense [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
`

## DESCRIPTION
Get-OrchLicense コマンドレットは、UiPath Orchestrator から詳細なライセンス情報を取得します。これには、ライセンス有効期限、猶予期間情報、さまざまなライセンスタイプ（Unattended、Attended、StudioPro など）の許可および使用ライセンス数、サブスクリプション詳細、およびその他のライセンス機能が含まれます。

ライセンス情報は、現在のライセンスステータスの理解、使用状況の追跡、および UiPath ライセンス条件への準拠の確保に不可欠です。

-Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetLicense

OAuth 必須スコープ: OR.Settings または OR.Settings.Read

必要な権限: [PLACEHOLDER - ライセンス表示権限]

## EXAMPLES

### Example 1
`powershell
PS Orch1:\> Get-OrchLicense
`

現在の Orchestrator インスタンスの完全なライセンス情報を取得します。

### Example 2
`powershell
PS Orch1:\> UiPath.PowerShell.Entities.License = Get-OrchLicense; UiPath.PowerShell.Entities.License.Allowed
`

ライセンス情報を取得し、タイプ別の許可されたライセンス数を表示します。

### Example 3
`powershell
PS C:\> Get-OrchLicense -Path Orch1:, Orch2:
`

複数のテナントからライセンス情報を取得します。

### Example 4
`powershell
PS Orch1:\> Get-OrchLicense | ConvertTo-Json -Depth 2
`

ライセンス情報を取得し、Allowed と Used ライセンス数などのネストされたプロパティを含む完全な構造を表示します。

### Example 5
`powershell
PS Orch1:\> UiPath.PowerShell.Entities.License = Get-OrchLicense
PS Orch1:\>  = (UiPath.PowerShell.Entities.License.Used.Unattended / UiPath.PowerShell.Entities.License.Allowed.Unattended) * 100
PS Orch1:\> Write-Host "Unattended ライセンス使用率: %"
`

現在使用中の unattended ライセンスの割合を計算して表示します。

### Example 6
`powershell
PS Orch1:\> UiPath.PowerShell.Entities.License = Get-OrchLicense
PS Orch1:\>  = [DateTimeOffset]::FromUnixTimeSeconds(UiPath.PowerShell.Entities.License.ExpireDate)
PS Orch1:\>  = ( - [DateTimeOffset]::Now).Days
PS Orch1:\> Write-Host "ライセンス有効期限まで  日"
`

ライセンス有効期限までの残り日数を計算して表示します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットになります。このパラメーターはパイプライン入力を受け取り、複数のパスを指定するためのワイルドカードをサポートします。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新 (Write-Progress コマンドレットによって生成される進行状況バーなど) に対して PowerShell が応答する方法を指定します。有効な値は次のとおりです: SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None

## OUTPUTS

### UiPath.PowerShell.Entities.License

## NOTES
- ExpireDate と GracePeriodEndDate は Unix タイムスタンプとして返されます
- Allowed と Used プロパティには、タイプ別のライセンス数（Unattended、Attended、StudioPro など）を含むハッシュテーブルが含まれます
- ライセンスタイプには次が含まれます: ProcessOrchestration、AppTest、Headless、Development、StudioX、NonProduction、StudioPro、TestAutomation、Unattended、AgentService、Attended、および Hosting
- IsExpired プロパティは、現在のライセンスの有効性ステータスを示します

主要エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetLicense
OAuth 必須スコープ: OR.Settings または OR.Settings.Read
必要な権限: Settings.View

## RELATED LINKS

[Get-OrchLicenseStats](Get-OrchLicenseStats.md)
[Get-OrchLicenseRuntime](Get-OrchLicenseRuntime.md)
[Get-OrchLicenseNamedUser](Get-OrchLicenseNamedUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
