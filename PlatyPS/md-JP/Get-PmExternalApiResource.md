---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmExternalApiResource

## SYNOPSIS
Platform Management から外部 API リソースを取得します。

## SYNTAX

`
Get-PmExternalApiResource [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-PmExternalApiResource コマンドレットは、UiPath Platform Management から外部 API リソース情報を取得します。外部 API リソースは、UiPath サービスとの統合のために外部アプリケーションがアクセスできる利用可能な API と関連する OAuth スコープを定義します。

このコマンドレットは、API リソースの名前、表示名、説明、および詳細なスコープ定義を含む包括的な情報を返します。各 API リソースには、さまざまな機能領域への読み取りおよび書き込みアクセスの特定の権限を定義する複数のスコープが含まれています。

スコープ情報は、特定の API エンドポイントと機能にアクセスするために必要な正確な権限を定義するため、UiPath サービスと統合する必要がある OAuth アプリケーションを構成する際に不可欠です。

これは、任意の UiPathOrch ドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作する Platform Management API コマンドレットです。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /api/ExternalApiResource

OAuth 必須スコープ: [PLACEHOLDER]

必要な権限: [PLACEHOLDER - 外部 API リソース表示権限]

## EXAMPLES

### Example 1
`powershell
PS C:\> Set-Location Orch1:\
PS Orch1:\> Get-PmExternalApiResource
`

Platform Management で利用可能なすべての外部 API リソースを取得します。

### Example 2
`powershell
PS Orch1:\> Get-PmExternalApiResource *Orchestrator*
`

名前に "Orchestrator" を含む API リソースを取得します。

### Example 3
`powershell
PS Orch1:\>  = Get-PmExternalApiResource UiPath.Orchestrator
PS Orch1:\> .scopes | Select-Object name, description | Format-Table
`

Orchestrator API リソースを取得し、利用可能なすべての OAuth スコープをフォーマットされたテーブルで表示します。

### Example 4
`powershell
PS Orch1:\> Get-PmExternalApiResource | Where-Object { .scopes.Count -gt 10 }
`

10 個を超えるスコープが利用可能な API リソースを取得します。

### Example 5
`powershell
PS Orch1:\>  = Get-PmExternalApiResource UiPath.Orchestrator
PS Orch1:\>  | ConvertTo-Json -Depth 3
`

Orchestrator API リソースを取得し、すべてのスコープとプロパティを調べるために、その完全な構造を JSON 形式で表示します。

## PARAMETERS

### -Name
取得する外部 API リソースの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。指定しない場合、すべての API リソースが返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All API resources
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
ターゲットドライブパスを指定します。このパラメーターはパイプライン入力を受け取り、ワイルドカードをサポートします。これは Platform Management API コマンドレットであるため、任意の UiPathOrch ドライブで動作します。

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

### System.String[]

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalResource

## NOTES
- このコマンドレットは Platform Management API を使用し、任意の UiPathOrch ドライブで動作します
- API リソースには、主要な UiPath サービスが含まれます: Orchestrator、Test Manager、Document Understanding、AI Center、Data Service、その他
- scopes プロパティには、API 統合に不可欠な詳細な OAuth スコープ情報が含まれています
- 一般的な Orchestrator スコープは OR.{Area} および OR.{Area}.Read/Write のパターンに従います（例: OR.Assets、OR.Assets.Read、OR.Assets.Write）
- 完全なスコープ構造を調べ、利用可能な権限を理解するには、ConvertTo-Json を使用してください
- この情報は、OAuth を介して UiPath サービスと統合する外部アプリケーションを構成する際に重要です

## RELATED LINKS

[Get-PmExternalApplication](Get-PmExternalApplication.md)
[about_UiPathOrch](about_UiPathOrch.md)
