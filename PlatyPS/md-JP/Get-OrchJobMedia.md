---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJobMedia

## SYNOPSIS
UiPath Orchestratorからジョブメディアファイル（スクリーンショット、ビデオ、録画）を取得します。

## SYNTAX

```
Get-OrchJobMedia [-Skip <UInt64>] [-First <UInt64>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchJobMedia` コマンドレットは、UiPath Orchestratorからスクリーンショット、ビデオ、録画を含むジョブメディアファイルを取得します。ジョブメディアは自動化実行の視覚的ドキュメントを提供し、キャプチャされた画像とビデオを通じてデバッグ、監査証跡、プロセス検証を可能にします。

メディアファイルは特定のジョブ実行に関連付けられ、ファイルタイプ、作成時刻、ファイルサイズ、ダウンロードリンクなどの情報を含みます。このコマンドレットは、トラブルシューティング、コンプライアンス、プロセス最適化の目的で自動化実行の視覚的証拠にアクセスするのに役立ちます。

このコマンドレットはフォルダーエンティティ操作として動作し、適切なフォルダーコンテキストへの移動または-Pathパラメーターを使用したターゲットフォルダーの指定が必要です。サブフォルダー内のジョブからメディアを含めるには-Recurseパラメーターを、再帰レベルを制御するには-Depthを使用します。

主要エンドポイント: GET /odata/Jobs/{jobKey}/Media

OAuth必要スコープ: OR.Execution または OR.Execution.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJobMedia
```

現在のSharedフォルダーからすべてのジョブメディアファイルを取得し、MediaType、FileName、CreatedTimeなどの基本プロパティを表示します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | ConvertTo-Json -Depth 2
```

完全なファイル情報とメタデータ構造を含む、詳細なジョブメディアプロパティをJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-OrchJobMedia -Path Orch1:\Production -Recurse | Where-Object {$_.CreatedTime -gt (Get-Date).AddDays(-7)}
```

過去7日間に作成されたProductionフォルダーとサブフォルダーからジョブメディアを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchJobMedia -Recurse -First 10
```

パフォーマンス最適化のために-Firstパラメーターを使用して、すべてのフォルダーから最初の10個のジョブメディアファイルを取得します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJobMedia | Where-Object {$_.MediaType -eq "Screenshot"} | Select-Object FileName, FileSize, CreatedTime
```

スクリーンショットメディアファイルをフィルタリングし、主要なプロパティを表示します。専用のMediaTypeパラメーターが存在しないため、Where-Objectを使用します。

### Example 6
```powershell
PS Orch1:\> Get-OrchJobMedia -Recurse | Measure-Object FileSize -Sum
```

フォルダー全体のすべてのジョブメディアの総ファイルサイズを計算します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度0は現在の場所のみを示します。より高い値はより多くのサブフォルダーレベルを含みます。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
検索するターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーコンテキストが使用されます。パス指定が必要なフォルダーエンティティ操作用です。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。

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

### -Recurse
検索操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。包括的なジョブメディア発見に不可欠です。

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

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。スキップするオブジェクトの数を入力します。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
指定された数のオブジェクトのみを取得します。取得するオブジェクトの数を入力します。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExecutionMedia
## NOTES
このコマンドレットは、スクリーンショット、ビデオ、録画を含むジョブメディアファイルにアクセスするためのフォルダーエンティティ操作です。ジョブメディアは、デバッグ、監査、プロセス検証のための自動化実行の視覚的ドキュメントを提供します。メディアファイルは特定のジョブ実行に関連付けられ、可用性に影響する保持ポリシーがある場合があります。特定のジョブをターゲットにするにはJobKeyパラメーターを使用するか、最近の実行のために時間範囲でフィルタリングします。この操作には、ターゲットフォルダーでのJobs.View権限が必要です。

主要エンドポイント: [プレースホルダー]
OAuth必要スコープ: [プレースホルダー]
必要な権限: [プレースホルダー]

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchJobVideo](Get-OrchJobVideo.md)

[Download-OrchJobMedia](Download-OrchJobMedia.md)

[Remove-OrchJobMedia](Remove-OrchJobMedia.md)
