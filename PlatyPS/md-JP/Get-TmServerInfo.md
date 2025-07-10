---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmServerInfo

## SYNOPSIS
Test Managerのサーバー情報を取得します。

## SYNTAX

`
Get-TmServerInfo [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
`

## DESCRIPTION
`Get-TmServerInfo` コマンドレットは、バージョン、サーバータイプ、動作状態などのUiPath Test Managerからサーバー情報を取得します。この情報は、接続性の検証、サーバーの正常性の確認、およびTest Managerのバージョンの確認に役立ちます。

このコマンドレットは、UiPathOrchTmプロバイダーのPSDrive上で動作します。設定ファイルに"TM."を含むスコープを記載すると、UiPathOrchTmプロバイダーのPSDriveが自動で追加されます。Get-PSDriveコマンドレットで確認してください。設定ファイルは、Edit-OrchConfigコマンドレットで開けます。

-Pathパラメーターには、カンマ区切りのテキストを使用して複数の値を指定できます。また、[Ctrl+Space]または[Tab]を押すことでこれらの値の自動補完を使用できます。

プライマリ エンドポイント: GET /testmanager_/api/serverinfo

OAuth 必要なスコープ: [PLACEHOLDER - Test Manager server info scopes]

必要な権限: [PLACEHOLDER - Test Manager server info permissions]

## EXAMPLES

### Example 1
`powershell
PS C:\> Set-Location Orch1Tm:\
PS Orch1Tm:\> Get-TmServerInfo
`

現在のTest Managerインスタンスのサーバー情報を取得します。

### Example 2
`powershell
PS Orch1Tm:\> $serverInfo = Get-TmServerInfo
PS Orch1Tm:\> Write-Host "Test Manager Version: $($serverInfo.version)"
PS Orch1Tm:\> Write-Host "Server Status: $($serverInfo.status)"
`

サーバー情報を取得し、バージョンとステータスを表示します。

### Example 3
`powershell
PS Orch1Tm:\> Get-TmServerInfo | Format-List
`

サーバー情報を取得し、詳細なリスト形式で表示します。

### Example 4
`powershell
PS C:\> Get-TmServerInfo -Path Orch1Tm:,Orch2Tm:
`

比較のために複数のTest Managerインスタンスからサーバー情報を取得します。

### Example 5
`powershell
PS Orch1Tm:\> $info = Get-TmServerInfo
PS Orch1Tm:\> if ($info.status -eq "OK") {
>>     Write-Host "Test Manager server is healthy"
>> } else {
>>     Write-Warning "Test Manager server status: $($info.status)"
>> }
`

サーバーステータスを確認し、適切な正常性情報を表示します。

## PARAMETERS

### -Path
ターゲットとするTest Managerドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。このパラメーターは、複数のTest Managerインスタンスを指定するためのパイプライン入力を受け付けます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）に対するPowerShellの応答方法を指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### UiPath.PowerShell.Entities.TmServerInfo
## NOTES
- このコマンドレットは、UiPathOrchTmプロバイダーを通じてTest Managerインスタンスへのアクセスが必要です
- UiPathOrchTm PSDriveは、設定に"TM."スコープが含まれている場合に自動的に追加されます
- 必要に応じて、Edit-OrchConfigを使用して設定ファイルを変更してください
- statusプロパティはサーバーの正常性を示します（正常なサーバーの場合は"OK"）
- サーバーバージョン情報は、互換性チェックとトラブルシューティングに役立ちます

## RELATED LINKS

[Get-TmConfiguration](Get-TmConfiguration.md)
[Get-TmProjectSetting](Get-TmProjectSetting.md)
[Edit-OrchConfig](Edit-OrchConfig.md)
[about_UiPathOrch](about_UiPathOrch.md)
