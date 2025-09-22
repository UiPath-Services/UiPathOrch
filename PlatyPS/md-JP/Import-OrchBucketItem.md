---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchBucketItem

## SYNOPSIS
UiPath Orchestrator ストレージバケットにファイルやフォルダーをインポートします。

## SYNTAX

```
Import-OrchBucketItem [-Source] <string[]> [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION

Import-OrchBucketItem コマンドレットは、ローカルファイルシステムからUiPath Orchestratorストレージバケットにファイルやディレクトリをインポートします。単一ファイル、複数ファイル、ワイルドカードパターン、再帰的ディレクトリ構造に対する包括的なサポートを提供します。

主要エンドポイント: POST /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.UploadFiles

OAuth必要スコープ: OR.Administration

必要な権限: Buckets.Edit および BlobFiles.Create

## EXAMPLES

### Example 1:
```powershell
PS Orch1:\Shared> Import-OrchBucketItem -Source C:\Documents\report.pdf -Name DocumentsBucket
```

現在のOrchestratorフォルダーの指定されたバケットに単一のPDFファイルをインポートします。

### Example 2:
```powershell
PS Orch1:\Production> Import-OrchBucketItem -Source C:\Data\*.json, C:\Logs\*.log -Name ConfigBucket
```

ワイルドカードパターンを使用して複数のファイルタイプをインポートします。

### Example 3:
```powershell
PS C:\> Import-OrchBucketItem -Path Orch1:\Production -Source C:\Config\*.json -Name ConfigBucket
```

-Pathパラメーターを使用してファイルシステムから特定のOrchestratorフォルダーにファイルをインポートします。

### Example 4:
```powershell
PS Orch1:\Backup> Import-OrchBucketItem -Source C:\ExportedStructure\ -Recurse
```

ディレクトリ構造を再帰的にインポートします。バケット名はフォルダー構造から推測されます。対象フォルダーとバケットは事前に存在している必要があります。

### Example 5:
```powershell
PS C:\Data> Import-OrchBucketItem -Source *.* -Name DataBucket -Path Orch1:\Testing -WhatIf
```

実際の操作を実行せずに、どのファイルがインポートされるかを表示します。

### Example 6:
```powershell
PS Orch1:\Development> Import-OrchBucketItem -Source C:\Templates\*.json -Name Project*_Config
```

バケット名のワイルドカードを使用して、マッチする複数のバケットを対象にします。

### Example 7:
```powershell
PS Orch1:\> Set-Location TargetFolder
PS Orch1:\TargetFolder> New-OrchBucket -Name ImportedBucket -Description "エクスポートから復元"
PS Orch1:\TargetFolder> Import-OrchBucketItem -Source C:\Backup\*.* -Name ImportedBucket
```

適切な設定でクロスフォルダーインポートの完全なワークフローを示します。

## PARAMETERS

### -Source
インポートするソースファイルまたはディレクトリを指定します。ファイルパス、ディレクトリパス、配列、ワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: None

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
対象ストレージバケット名を指定します。複数バケット操作用のワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: None

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Orchestrator内の対象フォルダーパスを指定します。指定されない場合は、現在のフォルダーが使用されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: None

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
サブディレクトリからファイルを再帰的にインポートします。-Nameパラメーターと同時に使用できません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: None

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
再帰操作の最大深度を指定します。-Recurseが使用される場合のみ有効です。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases: None

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
実際のインポートを実行せずに、コマンドレットが実行された場合の動作を表示します。

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

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
このコマンドレットにファイルパスまたはディレクトリパスをパイプできます。

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket
インポートされたファイルを含むバケットオブジェクトを返します。

## NOTES

パラメーター制限: -Recurseと-Nameは同時に使用できません。

ファイルの上書き: 同じバケット内で同じ名前のファイルは既存のファイルを上書きします。最後にインポートされたファイルが優先されます。

Nameパラメーターの要件: -Recurseなしで個別のファイルをインポートする場合、-Nameパラメーターが必要です。-Nameが不足している場合、コマンドレットは警告してファイルをスキップします。

バケットの自動検出: -Recurseで-Nameが指定されていない場合、バケット名はソースディレクトリ構造から推測されます。

クロスフォルダーインポート: ファイルは異なるOrchestratorフォルダーにインポートできますが、対象フォルダーとバケットは事前に存在している必要があります。

ワイルドカードサポート: *.txt、report_2024_*.pdf、*.json、*.xmlなどのファイルパターンと、Project*、Test_*_Bucketなどのバケットパターンがサポートされています。

ベストプラクティス: 予測可能なバケットターゲティングのため、常に-Nameを指定してください。実行前に操作をプレビューするため-WhatIfを使用してください。意図しない上書きを避けるため、一意のファイル名を確保してください。クロスフォルダーインポート用に対象フォルダー構造を事前作成してください。効率的な一括操作にワイルドカードパターンを使用してください。

## RELATED LINKS

[Export-OrchBucketItem]()

[Get-OrchBucketItem]()

[New-OrchBucket]()

[Remove-OrchBucket]()

[UiPath Orchestrator Documentation](https://docs.uipath.com/)
