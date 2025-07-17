---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLibrary

## SYNOPSIS
Orchestratorまたは外部ホストフィードからライブラリパッケージを取得します。

## SYNTAX

```
Get-OrchLibrary [[-Id] <String[]>] [-HostFeed] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorテナントまたは外部ホストフィードからライブラリパッケージを取得します。ライブラリは、複数のプロセス間で共有できるアクティビティ、ワークフロー、その他の自動化リソースを含む再利用可能なコンポーネントです。

デフォルトでは、このコマンドレットは接続されたOrchestratorインスタンスからライブラリを返します。-HostFeedパラメーターが指定されている場合、Orchestratorにインポートできる外部ホストフィード（公式NuGetギャラリーやUiPathフィードなど）からパッケージを取得します。

ライブラリは、テナント全体のスコープで動作するテナントエンティティです。-Pathパラメーターを使用して、ドライブ名でターゲットテナントを指定します。

主要エンドポイント: GET /odata/Libraries

OAuth必要スコープ: OR.Execution または OR.Execution.Read

必要な権限: Libraries.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchLibrary
```

現在のテナントからすべてのライブラリを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchLibrary *Excel*
```

IDに"Excel"を含むライブラリを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed
```

外部ホストフィードから利用可能なライブラリを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchLibrary *Activities* -HostFeed
```

ホストフィードからIDに"Activities"を含むライブラリを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchLibrary -Path Orch1:, Orch2:
```

複数のテナントからライブラリを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchLibrary | Where-Object {$_.Published -gt (Get-Date).AddDays(-30)}
```

過去30日間に公開されたライブラリを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed | Where-Object {$_.Id -like "*UiPath*"} | Select-Object Id, Version, Description
```

ホストフィードから選択されたプロパティを持つUiPathライブラリを取得します。

## PARAMETERS

### -Id
取得するライブラリのIDを指定します。ワイルドカードと複数の値をサポートします。

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
ドライブ名でターゲットテナントを指定します。複数のテナントにはカンマ区切りの値を使用します。指定しない場合は、現在のテナントをターゲットにします。

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
コマンドレット実行中に進行状況情報がどのように表示されるかを制御します。

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

### -HostFeed
ローカルのOrchestratorインスタンスの代わりに外部ホストフィードからライブラリを取得します。これにより、NuGetギャラリーとUiPathフィードからインポート可能なライブラリが表示されます。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Library
## NOTES
ライブラリエンティティはテナントスコープであり、テナント全体で動作します。

Import-OrchLibraryを使用してOrchestratorに追加する前に、外部ソースからインポート可能なライブラリを参照するには-HostFeedを使用します。

ホストフィードからのライブラリはインポート可能なパッケージを表示し、ローカルライブラリはOrchestratorですでに利用可能なパッケージを表示します。

Publishedプロパティは、ライブラリが最後に公開された時期を示し、最近の更新を特定するのに有用です。

主要エンドポイント: GET /odata/Libraries
OAuth必要スコープ: OR.Execution または OR.Execution.Read
必要な権限: Libraries.View

## RELATED LINKS

[Import-OrchLibrary](Import-OrchLibrary.md)

[Export-OrchLibrary](Export-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

[Copy-OrchLibrary](Copy-OrchLibrary.md)
