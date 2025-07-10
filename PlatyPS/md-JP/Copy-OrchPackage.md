---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchPackage

## SYNOPSIS
パッケージを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchPackage [-Id] <String[]> [[-Version] <String[]>] [-Destination] <String[]> [-Path <String>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchPackage コマンドレットは、UiPath Orchestrator テナント内またはテナント間で、ソースフォルダから宛先フォルダにパッケージをコピーします。このコマンドレットは、自動化ワークフロー、依存関係、メタデータを含むパッケージの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートしています。パッケージには、UiPath Studio から公開された実際の自動化ワークフローが含まれており、プロセスの展開と実行に不可欠です。

-Id パラメーターを使用して一意の識別子でコピーするパッケージを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。-Version パラメーターを使用してコピーするパッケージの特定のバージョンを指定できます。-Path パラメーターを使用して、異なるフォルダ構造で作業する際にソースフォルダを指定できます。

これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからパッケージをコピーし、宛先でフォルダ構造を維持します。

プライマリエンドポイント: GET /odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions(processId='{processId}'), GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{key}'), POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth 必要スコープ: OR.Execution

必要な権限: Packages.View, Packages.Create, FolderPackages.View, FolderPackages.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchPackage InvoiceProcessor * \Production
```

すべてのバージョンに * を使用して、InvoiceProcessor パッケージを現在のフォルダ（Development）から同じテナント内の Production フォルダにコピーします。Orch1:\Production は独自のフィードを持っている必要があります。そうでない場合、テナントフィードにコピーしようとして失敗する可能性があります。

### Example 2
```powershell
PS C:\> Copy-OrchPackage -Path Orch1:\ EmailAutomation * Orch2:\
```

テナント間パッケージコピーを実証して、EmailAutomation パッケージを Orch1:\ から Orch2:\ にコピーします。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchPackage ReportGenerator 1.2.3 Orch1:\ -WhatIf
```

現在のフォルダフィードからテナントフィードに ReportGenerator パッケージの特定のバージョン（1.2.3）をコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchPackage -Path Orch1:\ *Automation* * Orch2:\
```

ワイルドカードを使用して、Orch1:\ から Orch2:\ に ID に Automation を含むすべてのパッケージをコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchPackage -Recurse *Daily* * Orch2:\ -WhatIf
```

すべてのサブフォルダから Daily を含むすべてのパッケージを再帰的に Orch2:\ にコピーする場合に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\> Get-OrchPackage *Scheduled* | Copy-OrchPackage -Destination Orch2:\Production
```

ID に Scheduled を含むすべてのパッケージを取得し、パイプライン入力を使用して Orch2:\Production にコピーします。

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
パッケージをコピーする宛先フォルダを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Id
コピーするパッケージの ID を指定します。

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
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

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

### -Version
コピーするパッケージのバージョンを指定します。すべてのバージョンをコピーする場合は * を使用し、-Destination を直接指定するためにこの位置パラメーターを省略したい場合にも使用します。

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

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。
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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

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
パッケージをすべてのサブフォルダから再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### UiPath.PowerShell.Entities.BulkItemDtoOfString

## NOTES
これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

パッケージは Name ではなく Id で識別されます。-Id と -Version は両方とも位置パラメーターです。パラメーター名を省略する場合、-Destination を直接指定したい場合は -Version に * を使用してください（例：Copy-OrchPackage MyPackage * Orch1:\Production）。

パッケージは、テナントフィード（Orch1:\）またはフォルダフィード（Orch1:\FolderName）に存在できます。宛先フォルダが独自のフィードを設定していることを確認してください。そうでない場合、フォルダへのコピーはテナントフィードにコピーしようとします。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Import-OrchPackage](Import-OrchPackage.md)

[Export-OrchPackage](Export-OrchPackage.md)
