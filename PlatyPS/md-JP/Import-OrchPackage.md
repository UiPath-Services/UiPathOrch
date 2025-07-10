---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchPackage

## SYNOPSIS
ローカルファイルから指定されたフォルダーにパッケージをインポートします。

## SYNTAX

`
Import-OrchPackage [-Source] <String[]> [[-Path] <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
`

## DESCRIPTION
Import-OrchPackageコマンドレットは、ローカルの.nupkgファイルからUiPath Orchestratorフォルダーに自動化パッケージをインポートします。このコマンドレットにより、バックアップファイルからの自動化パッケージの展開、環境間の移行、またはオフラインで作成されたパッケージのインストールが可能になります。

パッケージには、コンパイルされた自動化ワークフローとその依存関係が含まれています。パッケージをインポートすると、.nupkgファイルがOrchestratorにアップロードされ、プロセスの展開と実行に利用できるようになります。これは、環境のセットアップ、災害復旧、および自動化の展開シナリオにとって不可欠です。

-Sourceパラメーターを使用して、インポートするローカルの.nupkgファイルまたはパッケージを含むディレクトリを指定します。-Pathパラメーターは、パッケージをアップロードするターゲットOrchestratorフォルダーを指定します。このコマンドレットは、複数のパッケージを効率的にインポートするためのワイルドカードパターンをサポートします。

これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーにナビゲートするか、-Path、-Recurse、-Depthパラメーターを使用してターゲットフォルダーを指定してください。-Recurseパラメーターは、ローカルディレクトリからインポートする際にすべてのサブディレクトリを処理することを可能にします。

プライマリ エンドポイント: POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth 必要なスコープ: OR.Execution

必要な権限: Packages.Create and FolderPackages.Create

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Development> Import-OrchPackage "C:\Packages\ProcessAutomation.1.0.0.nupkg"
`

指定された.nupkgファイルからProcessAutomationパッケージを現在のフォルダー（Development）にインポートします。

### Example 2
`powershell
PS C:\> Import-OrchPackage -Source "C:\Exports\*.nupkg" -Path Orch1:\Development
`

C:\Exportsディレクトリからすべての.nupkgファイルをOrch1:\Developmentフォルダーにインポートします。

### Example 3
`powershell
PS Orch1:\Development> Import-OrchPackage "C:\Backup\DataProcessor.*.nupkg" -WhatIf
`

C:\BackupディレクトリからDataProcessorパッケージのすべてのバージョンを現在のフォルダーにインポートする際に何が起こるかを表示します。

### Example 4
`powershell
PS C:\> Import-OrchPackage -Source "C:\Packages" -Path Orch1:\Production -Recurse
`

C:\Packagesディレクトリとそのサブディレクトリからすべての.nupkgファイルをOrch1:\Productionフォルダーにインポートします。

### Example 5
`powershell
PS Orch1:\Development> Import-OrchPackage "C:\Critical\*.nupkg", "C:\Backup\*.nupkg" -Confirm
`

C:\CriticalとC:\Backupディレクトリの両方からすべての.nupkgファイルを確認プロンプトとともに現在のフォルダーにインポートします。

### Example 6
`powershell
PS C:\> Get-ChildItem "C:\Exports\*.nupkg" | Import-OrchPackage -Path Orch1:\Development
`

C:\Exportsディレクトリからすべての.nupkgファイルを取得し、パイプライン入力を使用してOrch1:\Developmentにインポートします。

## PARAMETERS

### -Path
パッケージをインポートするターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーがターゲットとして使用されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に対するPowerShellの応答方法を指定します。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -Source
インポートするソース.nupkgファイルまたはパッケージを含むディレクトリを指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

### -Confirm
コマンドレットを実行する前に確認を求めます。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。
コマンドレットは実行されません。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -Recurse
ローカルディレクトリからインポートする際にすべてのサブディレクトリを処理することを指定します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString
## NOTES
これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーにナビゲートするか、-Path、-Recurse、-Depthパラメーターを使用してターゲットフォルダーを指定してください。

インポートするパッケージは、UiPath Studioで作成されたか、Export-OrchPackageを使用してエクスポートされた有効な.nupkgファイルである必要があります。パッケージの依存関係がターゲット環境で利用可能であることを確認してください。パッケージ名とバージョンは、フォルダー内で一意である必要があります。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには-WhatIfを使用してください。

## RELATED LINKS

[Export-OrchPackage](Export-OrchPackage.md)

[Get-OrchPackage](Get-OrchPackage.md)

[Remove-OrchPackage](Remove-OrchPackage.md)

[Update-OrchPackage](Update-OrchPackage.md)
