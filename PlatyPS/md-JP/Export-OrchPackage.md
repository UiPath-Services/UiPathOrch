---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchPackage

## SYNOPSIS
指定したフォルダーからパッケージをローカルファイルにエクスポートします。

## SYNTAX

```
Export-OrchPackage [-Id] <String[]> [[-Version] <String[]>] [[-Destination] <String>] [-Path <String[]>]
 [-Recurse] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Export-OrchPackage コマンドレットは、UiPath Orchestratorフォルダーから自動化パッケージをローカルの.nupkgファイルにエクスポートします。このコマンドレットは、異なるOrchestrator環境間での自動化パッケージのバックアップ、アーカイブ、または移行、またはオフラインストレージを可能にします。

パッケージには、コンパイルされた自動化ワークフローとその依存関係が含まれています。パッケージをエクスポートすると、ローカルに保存したり、他の環境に転送したり、バックアップ目的で使用したりできる.nupkgファイルが作成されます。これは、バージョン管理、災害復旧、および環境移行シナリオで不可欠です。

-IdパラメーターでパッケージIDを指定してエクスポートするパッケージを指定します。-Versionパラメーターでは特定のパッケージバージョンをターゲットにでき、-Destinationではエクスポートされた.nupkgファイルの保存先を指定します。このコマンドレットは、複数のパッケージを効率的にエクスポートするためのワイルドカードパターンをサポートしています。

これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。-Recurseパラメーターは、すべてのサブフォルダーからパッケージをエクスポートします。

主要エンドポイント: GET /odata/Processes, GET /odata/Processes/UiPath.Server.Configuration.OData.GetVersions, GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage

必要なOAuthスコープ: OR.Processes または OR.Processes.Read

必要な権限: Packages.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Export-OrchPackage ProcessAutomation
```

現在のフォルダーからProcessAutomationパッケージの最新バージョンをデフォルトの宛先にエクスポートします。

### Example 2
```powershell
PS C:\> Export-OrchPackage -Path Orch1:\Development DataProcessor 1.0.5 "C:\Exports"
```

DevelopmentフォルダーからDataProcessorパッケージのバージョン1.0.5をC:\Exportsディレクトリにエクスポートします。

### Example 3
```powershell
PS Orch1:\Development> Export-OrchPackage *Automation*, *Workflow* -Destination "C:\Backup" -WhatIf
```

AutomationやWorkflowを含む名前の複数のパッケージをC:\Backupディレクトリにエクスポートした場合の動作を表示します。

### Example 4
```powershell
PS C:\> Export-OrchPackage -Path Orch1:\Development CustomerProcess 1.0.* "C:\Versions"
```

DevelopmentフォルダーからCustomerProcessパッケージの1.0で始まるすべてのバージョンをC:\Versionsディレクトリにエクスポートします。

### Example 5
```powershell
PS Orch1:\> Export-OrchPackage -Recurse *Critical* "C:\CriticalBackup" -Confirm
```

すべてのサブフォルダーから名前にCriticalを含むすべてのパッケージを確認プロンプトとともにC:\CriticalBackupに再帰的にエクスポートします。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchPackage *Production* | Export-OrchPackage -Destination "C:\ProductionBackup"
```

名前にProductionを含むすべてのパッケージを取得し、パイプライン入力を使用してC:\ProductionBackupにエクスポートします。

## PARAMETERS

### -Destination
エクスポートされた.nupkgファイルが保存される宛先ディレクトリを指定します。指定しない場合、ファイルは現在のディレクトリに保存されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
エクスポートするパッケージIDを指定します。

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
ソースフォルダーを指定します。指定しない場合、現在のフォルダーがソースとして使用されます。

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
すべてのサブフォルダーからパッケージを再帰的にエクスポートすることを指定します。

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

### -Version
エクスポートするパッケージのバージョンを指定します。指定しない場合、最新バージョンがエクスポートされます。

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

### -WhatIf
コマンドレットが実行された場合の動作を表示します。
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

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。

エクスポートされたパッケージは、Import-OrchPackageを使用して他のOrchestrator環境にインポートできる.nupkgファイルとして保存されます。大きなパッケージ用の十分なディスク容量を確保してください。複数のバージョンをエクスポートする場合はバージョンワイルドカードを使用してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには-WhatIfを使用してください。

## RELATED LINKS

[Import-OrchPackage](Import-OrchPackage.md)

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Update-OrchPackage](Update-OrchPackage.md)
