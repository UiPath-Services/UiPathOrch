---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJobVideo

## SYNOPSIS
ビデオ録画が添付されたジョブを取得します。

## SYNTAX

```
Get-OrchJobVideo [[-Path] <String[]>] [-Recurse] [[-Skip] <UInt64>] [[-First] <UInt64>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchJobVideo` コマンドレットは、Orchestrator環境からビデオ録画が添付されたジョブを取得します。このコマンドレットは、画面録画が有効になっているジョブ実行の監視と監査に有用で、デバッグ、コンプライアンス、またはトレーニング目的で自動化プロセスの視覚的証拠を提供します。

このコマンドレットは、Get-OrchJobコマンドレットを呼び出し、過去1週間のビデオが添付されたジョブを検索する関数として実装されています。

ビデオ録画は、Orchestratorで適切な録画設定が構成されている場合にジョブ実行中にキャプチャされます。これらの録画は、ロボットの動作、UI相互作用、プロセス実行フローに関する貴重な洞察を提供します。

このコマンドレットはフォルダーエンティティで動作するため、Set-Location（cd）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定する必要があります。これにより、異なる組織フォルダー間でビデオ録画付きジョブを検索できます。

このコマンドレットは、大きな結果セットを効率的に管理するために、-Skipと-Firstパラメーターを介したページ分割をサポートします。ビデオ対応ジョブ実行の包括的なビューを得るには、-Recurseを使用してサブフォルダー階層を検索します。

主要エンドポイント: GET /odata/Jobs

OAuth必要スコープ: OR.Jobs または OR.Jobs.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJobVideo
```

現在のフォルダーからビデオ録画付きのすべてのジョブを取得します。実行前に特定のフォルダーへの移動が必要です。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJobVideo | ConvertTo-Json -Depth 2
```

完全なジョブ情報とビデオメタデータ構造を示す、詳細なジョブビデオプロパティをJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-OrchJobVideo -Path Orch1:\ -Recurse
```

Orch1:ドライブのすべてのフォルダー（サブフォルダーを含む）で、ビデオ録画付きジョブを再帰的に検索します。

### Example 4
```powershell
PS C:\> Get-OrchJobVideo -Path Orch1:\Shared -First 10
```

Productionフォルダーからビデオ録画付きの最初の10個のジョブを取得します。大きな結果セットの管理に有用です。

### Example 5
```powershell
PS C:\> Get-OrchJobVideo -Path Orch1:\Production, Orch1:\Development
```

複数の指定されたフォルダーから同時にビデオ録画付きジョブを取得します。

## PARAMETERS

### -First
指定された数のオブジェクトのみを取得します。
取得するオブジェクトの数を入力します。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
ターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーがターゲットとなります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Recurse
操作がターゲットフォルダーとそのすべてのサブフォルダーを含むように指定します。

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

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。
スキップするオブジェクトの数を入力します。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### System.Object
## NOTES

このコマンドレットは、特定のフォルダーへの移動または-Pathパラメーターを使用したターゲットフォルダーの指定が必要です。Orchestrator階層内のフォルダーエンティティで動作します。

ジョブ実行ビデオをキャプチャするには、Orchestrator設定でビデオ録画を有効にし、特定のプロセス用に設定する必要があります。結果が返されない場合は、ビデオ録画が有効になっており、録画がアクティブな状態でジョブが実行されたことを確認してください。

このコマンドレットは特に以下の用途に有用です：
- 失敗した自動化プロセスのデバッグ
- トレーニングとドキュメンテーション目的
- コンプライアンスと監査要件
- 視覚的分析によるプロセス最適化

ネストされたフォルダー構造を検索するには-Recurseを、録画されたジョブが大量にある場合のページ分割には-First/-Skipを使用してください。

ビデオファイルは通常Orchestratorのメディアストレージに保存され、返されたジョブオブジェクトを通じてダウンロードまたは表示用にアクセスできます。

主要エンドポイント: 関数実装（GET /odata/Jobsを呼び出すGet-OrchJobを使用）
OAuth必要スコープ: [プレースホルダー - 確認が必要]
必要な権限: [プレースホルダー - 確認が必要]

## RELATED LINKS
