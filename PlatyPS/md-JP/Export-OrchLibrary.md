---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchLibrary

## SYNOPSIS
テナントからライブラリをローカルファイルにエクスポートします。

## SYNTAX

```
Export-OrchLibrary [[-Id] <String[]>] [[-Version] <String[]>] [[-Destination] <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Export-OrchLibraryコマンドレットは、UiPath Orchestratorテナントから再利用可能なコンポーネントライブラリをローカルの.nupkgファイルにエクスポートします。このコマンドレットにより、アクティビティライブラリのバックアップ、アーカイブ、または異なるOrchestrator環境間での移行やオフラインストレージが可能になります。

ライブラリには、複数のオートメーションプロジェクト間で共有可能な再利用可能なアクティビティ、カスタムアクティビティ、およびワークフローコンポーネントが含まれています。ライブラリをエクスポートすると、ローカルに保存、他の環境への転送、またはバックアップ目的で使用できる.nupkgファイルが作成されます。これは、コンポーネント管理、バージョン管理、および環境移行シナリオに不可欠です。

-Idパラメーターを使用して、ライブラリIDによってエクスポートするライブラリを指定します。-Versionパラメーターでは特定のライブラリバージョンをターゲットにでき、-Destinationではエクスポートした.nupkgファイルを保存する場所を指定します。このコマンドレットは、複数のライブラリを効率的にエクスポートするためのワイルドカードパターンをサポートしています。

これはテナントエンティティコマンドレットです。-Pathパラメーターは、ライブラリをエクスポートするソーステナントドライブ（例：Orch1:、Orch2:）を指定します。

主要エンドポイント: GET /odata/Libraries, GET /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions, GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage

OAuth必須スコープ: OR.Libraries または OR.Libraries.Read

必要な権限: Libraries.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Export-OrchLibrary CustomActivities
```

現在のテナントからCustomActivitiesライブラリの最新バージョンをデフォルトの宛先にエクスポートします。

### Example 2
```powershell
PS C:\> Export-OrchLibrary -Path Orch1: SharedComponents 2.1.0 "C:\Exports"
```

Orch1テナントからSharedComponentsライブラリのバージョン2.1.0をC:\Exportsディレクトリにエクスポートします。

### Example 3
```powershell
PS Orch1:\> Export-OrchLibrary *Utilities*, *Helpers* -Destination "C:\Backup" -WhatIf
```

UtilitiesまたはHelpersを含む名前の複数のライブラリをC:\Backupディレクトリにエクスポートした場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Export-OrchLibrary -Path Orch1: CommonLibrary 1.* "C:\Versions"
```

Orch1テナントからCommonLibraryの1.で始まるすべてのバージョンをC:\Versionsディレクトリにエクスポートします。

### Example 5
```powershell
PS Orch1:\> Export-OrchLibrary *Critical* "C:\CriticalBackup" -Confirm
```

現在のテナントから名前にCriticalを含むすべてのライブラリを確認プロンプトとともにC:\CriticalBackupにエクスポートします。

### Example 6
```powershell
PS Orch1:\> Get-OrchLibrary *Enterprise* | Export-OrchLibrary -Destination "C:\EnterpriseLibs"
```

名前にEnterpriseを含むすべてのライブラリを取得し、パイプライン入力を使用してC:\EnterpriseLibsにエクスポートします。

## PARAMETERS

### -Destination
エクスポートした.nupkgファイルが保存される宛先ディレクトリを指定します。指定されていない場合、ファイルは現在のディレクトリに保存されます。

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
エクスポートするライブラリIDを指定します。指定されていない場合、すべてのライブラリがエクスポートされます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソーステナントドライブを指定します。指定されていない場合、現在のテナントがソースとして使用されます。

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

### -Version
エクスポートするライブラリバージョンを指定します。指定されていない場合、最新バージョンがエクスポートされます。

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
コマンドレットを実行する前に確認を求めます。

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
コマンドレットが実行された場合に何が起こるかを表示します。
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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはテナントエンティティコマンドレットです。-Pathパラメーターは、ソーステナントのテナントドライブ名（例：Orch1:、Orch2:）を指定します。

エクスポートされたライブラリは、Import-OrchLibraryを使用して他のOrchestrator環境にインポートできる.nupkgファイルとして保存されます。ライブラリには再利用可能なアクティビティとコンポーネントが含まれています。大きなライブラリに対しては十分なディスク容量を確保してください。複数のバージョンをエクスポートするにはバージョンワイルドカードを使用してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには-WhatIfを使用してください。

## RELATED LINKS

[Import-OrchLibrary](Import-OrchLibrary.md)

[Get-OrchLibrary](Get-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

[Update-OrchLibrary](Update-OrchLibrary.md)
