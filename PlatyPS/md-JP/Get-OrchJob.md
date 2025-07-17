---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJob

## SYNOPSIS
ジョブを取得します。

## SYNTAX

### JobId (Default)
```
Get-OrchJob [-Id <Int64[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Filter
```
Get-OrchJob [-Last <String>] [-CreationTimeAfter <DateTime>] [-CreationTimeBefore <DateTime>]
 [-StartTimeAfter <DateTime>] [-StartTimeBefore <DateTime>] [-EndTimeAfter <DateTime>]
 [-EndTimeBefore <DateTime>] [-ResumeTimeAfter <DateTime>] [-ResumeTimeBefore <DateTime>] [-Priority <String>]
 [-ReleaseName <String[]>] [-SourceType <String[]>] [-State <String[]>] [-ProcessType <String[]>]
 [-Robot <String[]>] [-Skip <UInt64>] [-OrderBy <String>] [-OrderAscending] [-First <UInt64>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorフォルダーからジョブ情報を取得します。ジョブはプロセス実行を表し、ステータス、タイミング、ロボット割り当て、実行結果など、自動化実行に関する詳細情報を含みます。

このコマンドレットは、時間範囲、ステータス、プロセス名、ロボット、優先度などのさまざまな条件でジョブをクエリする強力なフィルタリング機能を提供します。フォルダーエンティティで動作し、フォルダー階層全体での再帰的取得をサポートします。

パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depthパラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能します。

主要エンドポイント: GET /odata/Jobs

OAuth必要スコープ: OR.Jobs または OR.Jobs.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJob -First 5
```

現在のフォルダーから最初の5つのジョブを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJob
```

API呼び出しを行わずに、現在のフォルダー内のキャッシュされたジョブを取得します。

### Example 3
```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared -State Faulted -First 10
```

すべてのフォルダーから最初の10個の失敗したジョブを取得します。

### Example 4
```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared, Orch1:\Production -Recurse -State Successful -First 10
```

指定されたフォルダーとそのサブフォルダー内のすべての成功したジョブを取得します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJob -ReleaseName BlankProcess1 -First 5
```

特定のプロセスの最初の5つのジョブを取得します。

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchJob -Last Hour
```

現在のフォルダー内で過去1時間に作成されたジョブを取得します。

### Example 7
```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared,Orch1:\Production -Recurse -State Successful -First 5
```

-Pathパラメーターを優先して、特定のフォルダーから成功したジョブを取得します。

## PARAMETERS

### -CreationTimeAfter
取得するジョブの最も早い作成時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CreationTimeBefore
取得するジョブの最も遅い作成時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Depth
フォルダー再帰の深度を指定します。深度0は現在のフォルダーのみをターゲットにします。

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

### -Last
最近のジョブの期間を指定します。有効な値：Hour、Day、Week、Month、3Months、6Months、Year、3Years。

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットフォルダーを指定します。複数のフォルダーにはカンマ区切りの値を使用します。ワイルドカードをサポートします。指定しない場合は、現在のフォルダーをターゲットにします。

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

### -Priority
フィルタリングするジョブの優先度を指定します。有効な値：Low、Normal、High。

```yaml
Type: String
Parameter Sets: Filter
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

### -Recurse
操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。

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

### -SourceType
フィルタリングするジョブのソースタイプを指定します。有効な値には、Manual、Schedule、Agent、Robotが含まれます。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -State
フィルタリングするジョブの状態を指定します。有効な値には、Pending、Running、Successful、Faulted、Stopped、Suspendedが含まれます。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。スキップするオブジェクトの数を入力します。

```yaml
Type: UInt64
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
返すジョブの最大数を指定します。

```yaml
Type: UInt64
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ReleaseName
フィルタリングするプロセス名を指定します。ワイルドカードと複数の値をサポートします。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -OrderAscending
結果を昇順でソートします。デフォルトは降順です。

```yaml
Type: SwitchParameter
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -OrderBy
ソートするプロパティを指定します。有効な値には、Id、CreationTime、StartTime、EndTime、Stateが含まれます。

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProcessType
フィルタリングするプロセスタイプを指定します。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -EndTimeAfter
取得するジョブの最も早い終了時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EndTimeBefore
取得するジョブの最も遅い終了時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResumeTimeAfter
取得するジョブの最も早い再開時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResumeTimeBefore
取得するジョブの最も遅い再開時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeAfter
取得するジョブの最も早い開始時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeBefore
取得するジョブの最も遅い開始時刻を指定します。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
取得するジョブIDを指定します。複数の値をサポートします。

```yaml
Type: Int64[]
Parameter Sets: JobId
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Robot
フィルタリングするロボット名を指定します。ワイルドカードと複数の値をサポートします。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Job
## NOTES
ジョブエンティティはフォルダースコープです。フォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定する必要があります。

時間範囲、ステータスフィルター、ソートオプションを使用した複雑なクエリには、Filterパラメーターセットを使用します。JobIdパラメーターセットは、IDによる特定のジョブの取得に最適化されています。

ジョブは自動化プロセスの実行履歴を表し、ロボットのパフォーマンスとプロセス結果に関する詳細情報を提供します。

主要エンドポイント: GET /odata/Jobs
OAuth必要スコープ: OR.Jobs または OR.Jobs.Read
必要な権限: Jobs.View

## RELATED LINKS

[Start-OrchJob](Start-OrchJob.md)

[Stop-OrchJob](Stop-OrchJob.md)

[Get-OrchJobMedia](Get-OrchJobMedia.md)

[Open-OrchJob](Open-OrchJob.md)
