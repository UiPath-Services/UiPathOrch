---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-PmRobotAccount

## SYNOPSIS
組織のロボットアカウントを新規作成/更新します。

## SYNTAX

### ConsoleInput (Default)
```
Set-PmRobotAccount [-UserName] <String[]> [[-GroupName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### CsvInput
```
Set-PmRobotAccount [-UserName] <String[]> [[-GroupName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
組織のロボットアカウントが、どのグループに所属するかを更新します。もし、存在しないロボットアカウント名を指定したときは、その名前で新規にロボットアカウントを作成し、このアカウントを指定のグループに追加します。

ロボットアカウント名には、カンマ区切りで複数の名前を指定できます。ただし、アカウント名にワイルドカードは使えません。これは、このコマンドレットが新規作成と更新の両方を行えることの代償です。グループ名には、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。

CSV ファイルをインポートすることにより、ロボットアカウントを作成/更新することもできます。インポートできる形式の CSV ファイルを生成するには、`Get-PmRobot -ExportCsv` を実行します。この CSV ファイルをインポートする方法は、Example 2 を参照してください。

CSV ファイルの列名は、各パラメータ名と対応します。各パラメータのヘルプを参照してください。

主に呼び出すエンドポイント:

OAuth に必要なスコープ:

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Set-PmRobotAccount RobotAccountName MyGroup,YourGroup
```

ロボットアカウント RobotAcountName が所属するグループを、MyGroup と YourGroup に更新します。もし RobotAcountName という名前のロボットアカウントが存在しなければ、この名前でロボットアカウントを新規作成し、これを MyGroup と YourGroup に追加します。

### Example 2
```powershell
PS Orch1:\> Import-Csv c:ExportedPmRobotAccount.csv | Set-PmRobotAccount
```

CSV ファイルをインポートして、Identity サーバーのロボットアカウントを新規作成もしくは更新します。インポートできる形式の CSV ファイルを生成するには、`Get-PmRobot -ExportCsv` を実行します。カレントドライブよりも、CSV ファイルに記載された Path 列が優先されることに注意してください。

### Example 3
```powershell
PS Orch1:\> Import-Csv c:ExportedPmRobotAccount.csv | Set-PmRobotAccount -Path .
```

CSV ファイルの任意の列は、PS コンソール上のコマンドラインパラメータ指定で上書きできます。たとえば、CSV ファイルの Path 列に指定されたドライブではなく、カレントドライブのテナントにロボットアカウントを追加するには、`-Path .` を指定します。あるいは単純に、Path 列を削除した CSV ファイルをインポートすることにより、カレントドライブのテナントにロボットアカウントを追加できます。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に、あなたの確認を求めます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupName
指定のロボットアカウントが所属するグループの名前を指定します。指定のロボットアカウントは、ここに指定しなかったグループからは除外されます。なお、CSV ファイルに GroupName を指定するには、列名を GroupName0, GroupName1, ... として、それぞれにひとつずつグループ名を記載します。GroupName9 まで利用できます。

```yaml
Type: String[]
Parameter Sets: ConsoleInput
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: CsvInput
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

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

### -UserName
{{ Fill UserName Description }}

```yaml
Type: String[]
Parameter Sets: ConsoleInput
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: CsvInput
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行すると、何が起こるかを表示します。
コマンドレットは実行されません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount
## NOTES

## RELATED LINKS
