---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchActionCatalog

## SYNOPSIS
アクションカタログを宛先フォルダーにコピーします。

## SYNTAX

```
Copy-OrchActionCatalog [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchActionCatalog コマンドレットは、UiPath Orchestrator テナント内のソースフォルダーから宛先フォルダーへ、または異なるテナント間でアクションカタログをコピーします。このコマンドレットは、設定とメタデータを含むアクションカタログの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。アクションカタログは、異なる環境間での一貫性を保つため、またはバックアップ目的でコピーできます。

コピーするアクションカタログを指定するには -Name パラメーターを使用し、対象フォルダーを指定するには -Destination パラメーターを使用します。このコマンドレットは、複数のアクションカタログを効率的にコピーするためのワイルドカードパターンをサポートしています。

これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダーからアクションカタログをコピーし、宛先でフォルダー構造を維持できます。

プライマリエンドポイント: GET /odata/TaskCatalogs, POST /odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog

OAuth 必要スコープ: OR.Tasks

必要なアクセス許可: TaskCatalogs.View, TaskCatalogs.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchActionCatalog MyActionCatalog Orch1:\Production
```

位置パラメーターを使用して、MyActionCatalog アクションカタログを現在のフォルダー（Development）から同じテナント内の Production フォルダーにコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchActionCatalog -Path Orch1:\Development EmailActions Orch2:\Production
```

EmailActions アクションカタログを Orch1:\Development から Orch2:\Production にコピーし、テナント間アクションカタログコピーを実演します。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchActionCatalog *Email*, *Database* Orch1:\Production -WhatIf
```

安全のため -WhatIf を使用して、Email または Database を含む名前の複数のアクションカタログを現在のフォルダーから Production フォルダーにコピーする際に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchActionCatalog -Path Orch1:\Development *Custom* Orch2:\Production
```

ワイルドカードを使用してテナント間コピーを行い、名前に Custom を含むすべてのアクションカタログを Orch1:\Development から Orch2:\Production にコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchActionCatalog -Recurse *API* Orch2:\Finance -WhatIf
```

API を含むすべてのアクションカタログをすべてのサブフォルダーから再帰的に Orch2:\Finance にコピーする際に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchActionCatalog *Custom* | Copy-OrchActionCatalog -Destination Orch2:\Production
```

名前に Custom を含むすべてのアクションカタログを取得し、ワイルドカードフィルタリングでパイプライン入力を使用して Orch2:\Production にコピーします。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認を求めます。

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

### -Destination
宛先フォルダーを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーするアクションカタログの名前を指定します。

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
ソースフォルダーを指定します。指定されていない場合は、現在のフォルダーがソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合の動作を表示します。
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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダーレベルの最大数を指定します。

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

### -Recurse
アクションカタログをすべてのサブフォルダーから再帰的にコピーし、宛先でフォルダー構造を維持することを指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。

このコマンドレットは、テナント内とテナント間の両方のコピーをサポートします。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchActionCatalog](Get-OrchActionCatalog.md)

[Remove-OrchActionCatalog](Remove-OrchActionCatalog.md)
