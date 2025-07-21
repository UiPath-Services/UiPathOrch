---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Start-OrchJob

## SYNOPSIS
指定されたプロセスに対して UiPath ロボットジョブを開始します。

## SYNTAX

```
Start-OrchJob [-Name] <String[]> [[-RuntimeType] <String>] [[-JobsCount] <Int32>] [-InputArguments <String>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Orchestrator フォルダー内の指定された UiPath プロセスに対してジョブ実行を開始します。このコマンドレットは、フォルダーのロボット割り当てとプロセス構成に従って利用可能なロボットによって実行されるジョブインスタンスを作成します。

ジョブは Pending ステータスで作成され、対象フォルダーにアクセスでき、プロセス要件を満たす利用可能なロボットによって実行されます。

-Name および -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depth パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能します。

プライマリ エンドポイント: POST /odata/Jobs/UiPath.Server.Configuration.OData.StartJobs

OAuth 必要なスコープ: OR.Jobs

必要な権限: Jobs.Create Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Start-OrchJob MyProcess -WhatIf
```

実際にジョブを開始せずに、ジョブを開始した場合に何が起こるかを表示します。

### Example 2
```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19
```

現在のフォルダー内の指定されたプロセスに対してジョブを開始します。

### Example 3
```powershell
PS Orch1:\> Start-OrchJob -Recurse InvoiceProcess -JobsCount 3
```

すべてのフォルダーで再帰的に見つかった InvoiceProcess に対して 3 つのジョブを開始します。

### Example 4
```powershell
PS Orch1:\> Start-OrchJob MyProcess -InputArguments '{\"FilePath\": \"C:\\\\Data\\\\input.xlsx\", \"ProcessCount\": 100}'
```

JSON 文字列として渡される入力引数を使用してジョブを開始します。

### Example 5
```powershell
PS Orch1:\> Start-OrchJob -Path Orch1:\Shared, Orch1:\Finance ProcessName
```

特定のフォルダー内の ProcessName に対してジョブを開始します。

### Example 6
```powershell
PS Orch1:\> Start-OrchJob TestProcess -RuntimeType Unattended
```

特定のランタイムタイプでジョブを開始します。

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess *Critical* | Start-OrchJob -Confirm
```

確認プロンプトでクリティカルなプロセスのジョブを開始します。

## PARAMETERS

### -Depth
フォルダー再帰の深度を指定します。深度 0 は現在のフォルダーのみを対象とします。

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

### -JobsCount
各プロセスに対して作成するジョブインスタンスの数を指定します。デフォルトは 1 です。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: 1
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
ジョブを開始するプロセスの名前を指定します。ワイルドカードと複数の値をサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象フォルダーを指定します。複数のフォルダーにはカンマ区切りの値を使用します。ワイルドカードをサポートします。指定されていない場合は、現在のフォルダーが対象になります。

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
コマンドレット実行中の進行状況情報の表示方法を制御します。

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
操作に対象フォルダーとそのすべてのサブフォルダーを含めます。

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

### -RuntimeType
ジョブ実行のランタイムタイプを指定します。有効な値には Unattended、NonProduction が含まれます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
ジョブを開始する前に確認を求めます。複数のジョブを開始する場合に推奨されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
実際にジョブを開始せずに、コマンドレットが実行された場合の動作を表示します。安全性の確認に推奨されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputArguments
プロセスの入力引数を JSON 文字列として指定します。引数はプロセスの入力パラメーター定義と一致する必要があります。

```yaml
Type: String
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

### System.Object
## NOTES
プロセスエンティティはフォルダースコープです。フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定する必要があります。

ジョブは Pending ステータスで作成され、対象フォルダーにアクセスでき、プロセス要件を満たすロボットによって実行されます。

特にワイルドカードや再帰操作を使用する場合は、実際の実行前に -WhatIf を使用してジョブ作成をプレビューしてください。

入力引数は有効な JSON 文字列として提供し、プロセスの定義された入力パラメーターと一致する必要があります。

複数のジョブを開始する場合は -Confirm を使用して、各ジョブ作成を個別に確認してください。

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Stop-OrchJob](Stop-OrchJob.md)

[Get-OrchProcess](Get-OrchProcess.md)

[Open-OrchJob](Open-OrchJob.md)
