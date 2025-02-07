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
 [-Skip <UInt64>] [-OrderBy <String>] [-OrderAscending] [-First <UInt64>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Filter パラメーターセットでは、さまざまなパラメーターで取得するジョブの条件を指定できます。

JobId パラメーターセットの -Id パラメーターには、取得するジョブの Id を直接指定できます。この -Id パラメーターの補完リストに表示されるのは、いちど取得して UiPathOrch のメモリ内にキャッシュされたジョブの Id のみです。最初のパラメーターセットで複数のジョブを取得した後、詳細に確認したいジョブがあれば、その Id を -Id パラメーターに指定してください。キャッシュ内に当該の Id をもつジョブエンティティが存在する場合でも、Get-OrchJob はそのジョブの最新の状態を確認して表示します。

-Path パラメータには、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。また、この値は [Ctrl+Space] もしくは [Tab] を押下することで自動補完入力できます。

-Path、-Recurse、-Depth パラメータを指定するときは、これらをコマンドレット名の直後に指定してください。これにより、後続のパラメータの自動補完が適切に動作するようになります。

主に呼び出すエンドポイント: /odata/Jobs?{filter}&$expand=Robot,Machine,Release&$orderby=CreationTime%20desc, GET /odata/Jobs({jobId})?$expand=Robot,Machine,Release

OAuth に必要なスコープ: OR.Jobs or OR.Jobs.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchJob -Recurse -First 10
```

すべてのフォルダーにおいて、直近の10個のジョブを出力します。

### Example 2
```powershell
PS Orch1:\> Get-OrchJob -Recurse
```

すべてのフォルダーにおいて、キャッシュ済みのジョブを出力します。Orchestrator Web API を呼び出すことなく、一度取得したジョブを高速に再出力できます。最新の情報を取得するには、任意のパラメータを指定してください。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchJob -Last Day
```

現在のフォルダーの過去1日間のジョブを表示します。-Lastパラメーターの値には、Hour、Day、Week、Monthなどを補完で指定できます。

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchJob -State Pending,Suspended
```

現在のフォルダーで、ステートが保留中もしくは中断となっているジョブを表示します。失敗したジョブだけを表示するには、-Stateパラメータに Faulted を指定します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJob -SourceType Queue
```

現在のフォルダーで、キュートリガーにより開始されたジョブを表示します。

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchJob -CreationTimeAfter '2025/01/15 14:00:00' -CreationTimeBefore '2025/01/30 15:00:00'
```

-CreationTimeAfter と -CreationTimeBefore パラメータで、ジョブの作成日時でフィルターできます。このほか、-StartTimeAfter、-StartTimeBefore、-EndTimeAfter、-EndTimeBefore などのパラメータを利用できます。

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchJob | ? StopStrategy -eq Kill
```

キャッシュ済みのジョブのうち、強制終了されたジョブのみを表示します。

### Example 8
```powershell
PS Orch1:\Shared> Get-OrchJob | group ReleaseName -NoElement
```

キャッシュ済みのジョブを、プロセス名でグループ化します。項目名は補完入力できます。group は Group-Object の別名です。

### Example 9
```powershell
PS Orch1:\Shared> Get-OrchJob | group HostMachineName -NoElement
```

キャッシュ済みのジョブを、マシン名でグループ化します。

### Example 10
```powershell
PS Orch1:\Shared> Get-OrchJob | group HostMachineName,State -NoElement
```

キャッシュ済みのジョブを、マシン名とステータスでグループ化します。

### Example 11
```powershell
PS Orch1:\Shared> Get-OrchJob | group LocalSystemAccount,State -NoElement | Format-Table -AutoSize
```

キャッシュ済みのジョブを、実行したアカウント名でグループ化します。Format-Table -AutoSize は、出力が見切れないようにするために指定しています。

### Example 12
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.Date } -NoElement
```

キャッシュ済みのジョブを、作成日でグループ化します。

### Example 13
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.ToString('yyyy/MM') } -NoElement
```

キャッシュ済みのジョブを、作成月でグループ化します。

### Example 14
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.DayOfWeek } -NoElement
```

キャッシュ済みのジョブを、作成曜日でグループ化します。

### Example 15
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.DayOfWeek },State -NoElement
```

キャッシュ済みのジョブを、作成曜日とステータスでグループ化します。

### Example 16
```powershell
PS Orch1:\Shared> Get-OrchJob | ? { $_.CreationTime.DayOfWeek -eq 'Sunday' }
```

キャッシュ済みのジョブのうち、日曜日に作成したジョブだけを出力します。

### Example 17
```powershell
PS Orch1:\Shared> Get-OrchJob | ? StartTime -ne $null | group { $_.StartTime.Hour },State -NoElement
```

キャッシュ済みのジョブを、開始した時間帯とステータスでグループ化します。ジョブがどの時間帯で実行されていたか、ステートごとに傾向を簡単に分析できます。

### Example 18
```powershell
PS Orch1:\Shared> Get-OrchJob | group ReleaseName -NoElement | sort Count -Descending
```

キャッシュ済みのジョブをグループ化した内容を、数が大きい順に表示します。sort は Sort-Object の別名です。

### Example 19
```powershell
PS Orch1:\Shared> Get-OrchJob -State Faulted | group ReleaseName | sort Count -Descending
```

失敗したジョブだけをプロセス名でグループ化し、数が大きい順に表示します。より頻繁に失敗しているプロセスはどれか、簡単に分析できます。-State パラメータを指定すると、Orchestrator に問い合わせることに注意してください。キャッシュしたジョブを使って高速に処理するには、-State パラメータを指定する代わりに | ? State -eq Faulted にリダイレクトします。

### Example 20
```powershell
PS Orch1:\Shared> Get-OrchJob | sort { ($_.EndTime - $_.StartTime).TotalSeconds } -Descending
```

キャッシュ済みのジョブを、実行時間が長い順に並べます。

### Example 21
```powershell
PS Orch1:\Shared> Get-OrchJob | select Id, ReleaseName, @{ Name='TotalSeconds'; Expression={ ($_.EndTime - $_.StartTime).TotalSeconds }} | sort TotalSeconds -Descending
```

キャッシュ済みのジョブを、実行時間が長い順に並べます。その実行時間も出力します。

### Example 22
```powershell
PS Orch1:\Shared> Get-OrchJob | % { ($_.EndTime - $_.StartTime).TotalMinutes } | Measure-Object -Average -Sum -Maximum -Minimum
```

キャッシュ済みのジョブの実行時間の平均、最長、最短を出力します。% は ForEach-Object の別名です。

## PARAMETERS

### -CreationTimeAfter
{{ Fill CreationTimeAfter Description }}

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
{{ Fill CreationTimeBefore Description }}

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
ターゲットフォルダーへの再帰の深さを指定します。深さが0の場合は、現在のフォルダーのみが対象となり、サブフォルダーは含まれません。

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
{{ Fill Last Description }}

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
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

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
{{ Fill Priority Description }}

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
{{ Fill ProgressAction Description }}

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
ターゲットフォルダーのサブフォルダーも、ターゲットとして含めることを指定します。

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

### -SourceType
{{ Fill SourceType Description }}

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
{{ Fill State Description }}

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

### -Skip
指定された数のエンティティを無視して、残りのエンティティを取得します。
スキップするエンティティの数を指定してください。

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
指定された数のエンティティのみを取得します。
取得するエンティティの数を指定してください。

```yaml
Type: UInt64
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReleaseName
{{ Fill ReleaseName Description }}

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
{{ Fill OrderAscending Description }}

```yaml
Type: SwitchParameter
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OrderBy
{{ Fill OrderBy Description }}

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
{{ Fill ProcessType Description }}

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
{{ Fill EndTimeAfter Description }}

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
{{ Fill EndTimeBefore Description }}

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
{{ Fill ResumeTimeAfter Description }}

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
{{ Fill ResumeTimeBefore Description }}

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
{{ Fill StartTimeAfter Description }}

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
{{ Fill StartTimeBefore Description }}

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
{{ Fill Id Description }}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Job
## NOTES

## RELATED LINKS
