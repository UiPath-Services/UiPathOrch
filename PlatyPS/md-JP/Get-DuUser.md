---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuUser

## SYNOPSIS
UiPath Orchestrator プロジェクトから Document Understanding ユーザーを取得します。

## SYNTAX

`
Get-DuUser [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
`

## DESCRIPTION
Get-DuUser コマンドレットは、UiPath Orchestrator Document Understanding プロジェクトから Document Understanding ユーザー情報を取得します。このコマンドレットは、特定の Document Understanding プロジェクト内でロールが割り当てられているユーザーとグループを、ロール割り当て、継承情報、およびセキュリティプリンシパルの詳細と共に表示します。

これは、特定の Document Understanding プロジェクトフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットプロジェクトを指定する必要があるフォルダーエンティティ操作コマンドレットです。このコマンドレットは Document Understanding プロジェクトのコンテキスト内で動作し、それらのプロジェクト内で割り当てられたロールと権限を持つユーザーを返します。

このコマンドレットは、ロール割り当て（roleAssignmentDtos）、継承ステータス、セキュリティプリンシパル ID、およびプロジェクト固有の権限を含む包括的なユーザー情報を返します。各ユーザーは、異なるスコープと継承プロパティを持つ複数のロール割り当てを持つ場合があります。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

-Path、-Recurse、および -Depth パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターの自動補完が正しく機能します。

主要エンドポイント: [PLACEHOLDER - Document Understanding ユーザー API エンドポイント]

OAuth 必須スコープ: [PLACEHOLDER - Document Understanding ユーザースコープ]

必要な権限: [PLACEHOLDER - Document Understanding ユーザー権限]

## EXAMPLES

### Example 1
`powershell
PS C:\> Set-Location "Orch1Du:\MyProject"
PS Orch1Du:\MyProject> Get-DuUser
`

現在の Document Understanding プロジェクトに割り当てられているすべてのユーザーを取得します。

### Example 2
`powershell
PS C:\> Set-Location Orch1Du:\
PS Orch1Du:\> Get-DuUser -Recurse
`

すべての Document Understanding プロジェクトから再帰的にすべてのユーザーを取得します。

### Example 3
`powershell
PS Orch1Du:\> Get-DuUser -Path "Orch1Du:\MyProject"
`

現在の場所を変更せずに、特定の Document Understanding プロジェクトからユーザーを取得します。

### Example 4
`powershell
PS Orch1Du:\MyProject> Get-DuUser | Where-Object type -eq "DirectoryUser"
`

現在の Document Understanding プロジェクトから個人ユーザーのみ（グループではなく）を取得します。

### Example 5
`powershell
PS Orch1Du:\MyProject>  = Get-DuUser | Where-Object displayName -eq "Yoko Tsuda"
PS Orch1Du:\MyProject>  | ConvertTo-Json -Depth 3
`

特定のユーザーを取得し、ロール割り当てとセキュリティ詳細を調べるために、その完全な構造を JSON 形式で表示します。

### Example 6
`powershell
PS Orch1Du:\> Get-DuUser -Recurse | Where-Object { .roleAssignmentDtos.inherited -eq False }
`

直接的な（継承されていない）ロール割り当てを持つすべてのプロジェクト全体のユーザーを取得します。

## PARAMETERS

### -Name
取得する Document Understanding ユーザーの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。指定しない場合、すべてのユーザーが返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All users
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
Document Understanding プロジェクトパスを指定します。指定しない場合は、現在の場所が使用されます。このパラメーターはパイプライン入力を受け取り、複数のプロジェクトパスを指定するためのワイルドカードをサポートします。

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

### -Recurse
ユーザーを取得する際に、サブプロジェクトのユーザーを含めます。これは、複数のプロジェクトをスキャンできるフォルダーエンティティ操作パラメーターです。

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

### -CsvEncoding
CSV エクスポート時の文字エンコーディングを指定します。

`yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -ExportCsv
ユーザー情報を CSV ファイルにエクスポートするためのファイルパスを指定します。

`yaml
Type: String
Parameter Sets: (All)
Aliases:

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

### UiPath.PowerShell.Entities.DuUser

## NOTES
- これは、Document Understanding プロジェクトへのナビゲーションまたは -Path/-Recurse/-Depth パラメーターの使用を必要とするフォルダーエンティティ操作コマンドレットです
- "Use Set-Location cmdlet to navigate to the target folder first..." エラーが表示される場合は、プロジェクトフォルダーに移動するか、フォルダー操作パラメーターを使用する必要があります
- ユーザータイプには DirectoryUser（個人ユーザー）と DirectoryGroup（グループ）があります
- roleAssignmentDtos プロパティには、継承ステータスとスコープを含む詳細なロール割り当て情報が含まれています
- ロール割り当ては、組織単位から継承するか、プロジェクトに直接割り当てることができます
- 完全な roleAssignmentDtos 構造を調べ、ロールの詳細を理解するには、ConvertTo-Json を使用してください
- securityPrincipalId は、Document Understanding システム内でユーザーをセキュリティコンテキストにリンクします

## RELATED LINKS

[Get-DuRole](Get-DuRole.md)
[Add-DuUser](Add-DuUser.md)
[Remove-DuRoleFromDuUser](Remove-DuRoleFromDuUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
