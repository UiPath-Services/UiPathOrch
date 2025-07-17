---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLibraryVersion

## SYNOPSIS
Orchestratorまたは外部ホストフィードからライブラリパッケージの特定のバージョンを取得します。

## SYNTAX

```
Get-OrchLibraryVersion [[-Id] <String[]>] [[-Version] <String[]>] [-HostFeed] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchLibraryVersion` コマンドレットは、Orchestrator環境または外部ホストフィードからライブラリパッケージの特定のバージョンを取得します。ライブラリの最新バージョンを返すGet-OrchLibraryとは異なり、このコマンドレットでは、特定のライブラリのすべての利用可能なバージョンをクエリしたり、特定のバージョンを取得したりできます。

このコマンドレットは、バージョン管理、依存関係分析、および互換性の目的でライブラリの特定のバージョンを操作する必要がある場合に特に有用です。ライブラリのすべての利用可能なバージョンを特定し、バージョン履歴を確認し、展開またはロールバックシナリオ用の特定のバージョンを取得するのに役立ちます。

-HostFeedパラメーターと一緒に使用すると、外部パッケージリポジトリをクエリして、Orchestratorにインポートする前に利用可能なバージョンを見つけることができます。

主要エンドポイント: /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='libraryId}')

OAuth必要スコープ: OR.Execution または OR.Execution.Read

必要な権限: Libraries.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-OrchLibraryVersion UiPath.Excel.Activities
```

"UiPath.Excel.Activities"ライブラリのすべての利用可能なバージョンを取得します。

### Example 2
```powershell
PS C:\> Get-OrchLibraryVersion UiPath.Excel.Activities | ConvertTo-Json -Depth 2
```

構造分析のために、"UiPath.Excel.Activities"ライブラリのすべてのバージョンを詳細なプロパティとともにJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-OrchLibraryVersion MyLibrary 1.0.*
```

"MyLibrary"のパターン"1.0.*"（すべての1.0.xバージョン）に一致するすべてのバージョンを取得します。

### Example 4
```powershell
PS C:\> Get-OrchLibraryVersion -HostFeed UiPath.System.Activities
```

外部ホストフィードから"UiPath.System.Activities"のすべての利用可能なバージョンを取得します。

### Example 5
```powershell
PS C:\> Get-OrchLibraryVersion -Path Orch1:, Orch2: MyCustomLibrary
```

複数のOrchestrator環境から"MyCustomLibrary"のすべてのバージョンを取得します。

### Example 6
```powershell
PS C:\> Get-OrchLibraryVersion MyLibrary | Sort-Object Version -Descending | Select-Object -First 5
```

"MyLibrary"のすべてのバージョンを取得し、最新の5つのバージョンを表示します。

### Example 7
```powershell
PS C:\> Get-OrchLibraryVersion MyLibrary | Where-Object {$_.IsPrerelease -eq $false} | Select-Object Id, Version, Published
```

"MyLibrary"の安定版（プレリリースではない）バージョンのみを取得し、主要なバージョン情報を表示します。

## PARAMETERS

### -Id
バージョンを取得するライブラリのIDを指定します。パターンマッチングのためにワイルドカード文字（*と?）をサポートします。-Versionなしで指定すると、一致するライブラリのすべての利用可能なバージョンを返します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットとなります。このパラメーターを使用して、複数のOrchestrator環境から同時にライブラリバージョンをクエリします。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### -Version
取得するライブラリのバージョンを指定します。パターンマッチングのためにワイルドカード文字（*と?）をサポートします。このパラメーターを使用して、特定のバージョンパターンまたは正確なバージョンをフィルタリングします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -HostFeed
指定すると、Orchestratorライブラリリポジトリの代わりに外部ホストフィードからライブラリバージョンを取得します。これにより、公式NuGetギャラリーなどの外部ソースからインポート可能なパッケージの利用可能なバージョンを確認できます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

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

### UiPath.PowerShell.Entities.Library
## NOTES

このコマンドレットはテナントエンティティで動作するため、特定のフォルダーではなくOrchestrator全体から情報を取得します。

-HostFeedパラメーターを使用する場合、これは外部ソースから情報を取得するため、ネットワーク条件と外部フィードのサイズによっては完了に時間がかかる場合があることに注意してください。

このコマンドレットは、バージョン管理と依存関係分析に特に有用です。以下の用途に使用してください：
- ライブラリをアップグレードまたはダウングレードする前にすべての利用可能なバージョンを確認
- テスト目的でプレリリースバージョンを特定
- バージョン履歴とリリースパターンを分析
- 展開前に特定のバージョンが利用可能であることを確認

返されるオブジェクトには、安定版とプレリリースバージョンを区別するIsPrereaseプロパティが含まれます。

適切な深度設定でConvertTo-Jsonを使用して、ライブラリバージョンオブジェクトの完全な構造を探索してください。これらには、デフォルトの表示形式では表示されない可能性がある詳細なメタデータが含まれています。

主要エンドポイント: GET /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions
OAuth必要スコープ: OR.Execution または OR.Execution.Read
必要な権限: Libraries.View

## RELATED LINKS

[Get-OrchLibrary](Get-OrchLibrary.md)
[Import-OrchLibrary](Import-OrchLibrary.md)
[Export-OrchLibrary](Export-OrchLibrary.md)
[Remove-OrchLibrary](Remove-OrchLibrary.md)
[Copy-OrchLibrary](Copy-OrchLibrary.md)
