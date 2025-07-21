---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuUser

## SYNOPSIS
UiPath OrchestratorプロジェクトからDocument Understandingユーザーを取得します。

## SYNTAX

```
Get-DuUser [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-DuUser コマンドレットは、UiPath OrchestratorのDocument Understandingプロジェクトからユーザー情報を取得します。このコマンドレットは、特定のDocument Understandingプロジェクト内でロールが割り当てられたユーザーとグループを表示し、ロール割り当て、継承情報、セキュリティプリンシパルの詳細を含みます。

これは、特定のDocument Understandingプロジェクトフォルダに移動するか、-Path、-Recurse、または-Depthパラメーターを使用して対象プロジェクトを指定する必要があるフォルダエンティティ操作コマンドレットです。このコマンドレットは、Document Understandingプロジェクトのコンテキスト内で動作し、そのプロジェクト内で割り当てられたロールと権限を持つユーザーを返します。

このコマンドレットは、ロール割り当て（roleAssignmentDtos）、継承ステータス、セキュリティプリンシパルID、プロジェクト固有の権限を含む包括的なユーザー情報を返します。各ユーザーは、異なるスコープと継承プロパティを持つ複数のロール割り当てを持つ場合があります。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

-Path、-Recurse、-Depthパラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターの自動補完が正しく機能します。

主要エンドポイント：[PLACEHOLDER - Document Understanding users API endpoint]

OAuth必須スコープ：[PLACEHOLDER - Document Understanding user scopes]

必須権限：[PLACEHOLDER - Document Understanding user permissions]

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location "Orch1Du:\MyProject"
PS Orch1Du:\MyProject> Get-DuUser
```

現在のDocument Understandingプロジェクトに割り当てられたすべてのユーザーを取得します。

### Example 2
```powershell
PS C:\> Set-Location Orch1Du:\
PS Orch1Du:\> Get-DuUser -Recurse
```

すべてのDocument Understandingプロジェクトからすべてのユーザーを再帰的に取得します。

### Example 3
```powershell
PS Orch1Du:\> Get-DuUser -Path "Orch1Du:\MyProject"
```

現在の場所を変更せずに、特定のDocument Understandingプロジェクトからユーザーを取得します。

### Example 4
```powershell
PS Orch1Du:\MyProject> Get-DuUser | Where-Object type -eq "DirectoryUser"
```

現在のDocument Understandingプロジェクトから個々のユーザーのみを取得します（グループではない）。

### Example 5
```powershell
PS Orch1Du:\MyProject> $user = Get-DuUser | Where-Object displayName -eq "Yoko Tsuda"
PS Orch1Du:\MyProject> $user | ConvertTo-Json -Depth 3
```

特定のユーザーを取得し、ロール割り当てとセキュリティの詳細を探索するために、完全な構造をJSON形式で表示します。

### Example 6
```powershell
PS Orch1Du:\> Get-DuUser -Recurse | Where-Object { $_.roleAssignmentDtos.inherited -eq $false }
```

直接（非継承）のロール割り当てを持つすべてのプロジェクトのすべてのユーザーを取得します。

## PARAMETERS

### -Name
取得するDocument Understandingユーザーの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのユーザーが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All users
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Document Understandingプロジェクトのパスを指定します。指定しない場合、現在の場所が使用されます。このパラメーターは、パイプライン入力を受け入れ、複数のプロジェクトパスを指定するためのワイルドカードをサポートします。

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

### -Recurse
ユーザーを取得する際に、サブプロジェクトのユーザーも含めます。これは、複数のプロジェクトをスキャンできるフォルダエンティティ操作パラメーターです。

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

### -CsvEncoding
{{ Fill CsvEncoding Description }}

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
{{ Fill ExportCsv Description }}

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.DuUser
## NOTES
- これは、Document Understandingプロジェクトへのナビゲーションまたは-Path/-Recurse/-Depthパラメーターの使用が必要なフォルダエンティティ操作コマンドレットです
- "Use Set-Location cmdlet to navigate to the target folder first..."エラーが表示される場合は、プロジェクトフォルダに移動するか、フォルダ操作パラメーターを使用する必要があります
- ユーザータイプには、DirectoryUser（個々のユーザー）とDirectoryGroup（グループ）が含まれます
- roleAssignmentDtosプロパティには、継承ステータスとスコープを含む詳細なロール割り当て情報が含まれます
- ロール割り当ては、組織単位から継承されるか、プロジェクトに直接割り当てられます
- 完全なroleAssignmentDtos構造を探索し、ロールの詳細を理解するには、ConvertTo-Jsonを使用してください
- securityPrincipalIdは、ユーザーをDocument Understandingシステム内のセキュリティコンテキストにリンクします

## RELATED LINKS

[Get-DuRole](Get-DuRole.md)
[Add-DuUser](Add-DuUser.md)
[Remove-DuRoleFromDuUser](Remove-DuRoleFromDuUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
