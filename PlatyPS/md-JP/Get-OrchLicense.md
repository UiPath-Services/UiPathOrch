---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicense

## SYNOPSIS
UiPath Orchestratorからライセンス情報を取得します。

## SYNTAX

```
Get-OrchLicense [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchLicense コマンドレットは、UiPath Orchestratorから詳細なライセンス情報を取得します。これには、ライセンスの有効期限、猶予期間情報、異なるライセンスタイプ（Unattended、Attended、StudioProなど）の許可および使用済みライセンス数、サブスクリプション詳細、その他のライセンス機能が含まれます。

ライセンス情報は、現在のライセンスステータスを理解し、使用状況を追跡し、UiPathライセンス条件への準拠を確保するために不可欠です。

-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /odata/Settings/UiPath.Server.Configuration.OData.GetLicense

OAuth必須スコープ：OR.Settings または OR.Settings.Read

必須権限：[PLACEHOLDER - License view permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchLicense
```

現在のOrchestratorインスタンスの完全なライセンス情報を取得します。

### Example 2
```powershell
PS Orch1:\> $license = Get-OrchLicense; $license.Allowed
```

ライセンス情報を取得し、タイプ別の許可されたライセンス数を表示します。

### Example 3
```powershell
PS C:\> Get-OrchLicense -Path Orch1:, Orch2:
```

複数のテナントからライセンス情報を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchLicense | ConvertTo-Json -Depth 2
```

ライセンス情報を取得し、AllowedとUsedライセンス数などのネストされたプロパティを含む完全な構造を表示します。

### Example 5
```powershell
PS Orch1:\> $license = Get-OrchLicense
PS Orch1:\> $usage = ($license.Used.Unattended / $license.Allowed.Unattended) * 100
PS Orch1:\> Write-Host "Unattended license usage: $usage%"
```

現在使用中のUnattendedライセンスのパーセンテージを計算して表示します。

### Example 6
```powershell
PS Orch1:\> $license = Get-OrchLicense
PS Orch1:\> $expiryDate = [DateTimeOffset]::FromUnixTimeSeconds($license.ExpireDate)
PS Orch1:\> $daysLeft = ($expiryDate - [DateTimeOffset]::Now).Days
PS Orch1:\> Write-Host "License expires in $daysLeft days"
```

ライセンス有効期限までの残り日数を計算して表示します。

## PARAMETERS

### -Path
対象ドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、パイプライン入力を受け入れ、複数のパスを指定するためのワイルドカードをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### UiPath.PowerShell.Entities.License
## NOTES
- ExpireDateとGracePeriodEndDateは、Unixタイムスタンプとして返されます
- AllowedとUsedプロパティには、タイプ別のライセンス数（Unattended、Attended、StudioProなど）を含むハッシュテーブルが含まれます
- ライセンスタイプには次のものがあります：ProcessOrchestration、AppTest、Headless、Development、StudioX、NonProduction、StudioPro、TestAutomation、Unattended、AgentService、Attended、Hosting
- IsExpiredプロパティは、現在のライセンスの有効性ステータスを示します

主要エンドポイント：GET /odata/Settings/UiPath.Server.Configuration.OData.GetLicense
OAuth必須スコープ：OR.Settings または OR.Settings.Read
必須権限：Settings.View

## RELATED LINKS

[Get-OrchLicenseStats](Get-OrchLicenseStats.md)
[Get-OrchLicenseRuntime](Get-OrchLicenseRuntime.md)
[Get-OrchLicenseNamedUser](Get-OrchLicenseNamedUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
