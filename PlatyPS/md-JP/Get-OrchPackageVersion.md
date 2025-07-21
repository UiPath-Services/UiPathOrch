---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPackageVersion

## SYNOPSIS
UiPath Orchestratorからパッケージバージョンを取得します。

## SYNTAX

```
Get-OrchPackageVersion [[-Id] <String[]>] [[-Version] <String[]>] [-Path <String[]>] [-Recurse]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchPackageVersion コマンドレットは、UiPath Orchestratorからパッケージバージョンを取得します。このコマンドレットは、テナントエンティティとフォルダエンティティの両方で動作する独特のものであり、フォルダフィードとテナントフィードの両方からパッケージにアクセスできます。パッケージは、バージョン管理とデプロイメント管理を備えた自動化プロセス、ライブラリ、およびその他の実行可能コンポーネントを表します。

パッケージは2種類のフィードにデプロイできます：
- フォルダフィード：パッケージフィードが有効な特定のフォルダにデプロイされたパッケージ
- テナントフィード：テナントレベルで利用可能で、任意のフォルダにデプロイできるパッケージ

フィードのないフォルダから実行すると、コマンドレットは自動的にテナントフィードからパッケージを取得します。フィードのあるフォルダから実行すると、そのフォルダのフィードからパッケージを取得します。テナントフィード（Orch1:）またはフォルダフィード（Orch1:\FolderName）を明示的に指定するには -Path を使用します。

パッケージバージョンには、バージョン番号、公開日、IsActiveステータス、説明、TargetFramework（Windows、クロスプラットフォーム）、MainEntryPointPath、プロジェクトメタデータなどの包括的な情報が含まれます。このコマンドレットは、利用可能な自動化パッケージとフォルダレベルおよびテナントレベル全体でのデプロイメントステータスの可視性を提供します。

主要エンドポイント: GET /odata/Packages

OAuth必須スコープ: OR.Execution または OR.Execution.Read

必要な権限: Packages.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchPackageVersion
```

現在のフォルダフィードからすべてのパッケージバージョンを取得し、Id、Version、公開日、IsActiveステータス、および説明を表示します。

### Example 2
```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1:\ -Recurse
```

-Recurseを使用してテナントフィードとすべてのフォルダフィードからすべてのパッケージバージョンを取得します。これは、フォルダフィード（個別のフォルダに固有のパッケージ）とは対照的に、テナントフィード（テナント全体で利用可能なパッケージ）へのアクセスを示しています。コマンドは、テナントパスを明示的に指定することで任意の場所から実行できます。

### Example 3
```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1:\ *Process*
```

Orch1テナントフィードで"Process"を含むIDを持つすべてのパッケージバージョンを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.IsActive -eq $true}
```

アクティブなパッケージバージョンのみを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchPackageVersion | Where-Object {$_.ProjectType -eq "Library"} | Select-Object Id, Version, Description
```

ライブラリパッケージとそのバージョン、説明を表示します。

## PARAMETERS

### -Id
取得するパッケージIDを指定します。柔軟なパッケージ選択のためのワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットテナントドライブを指定します。指定されていない場合は、現在のテナントがターゲットになります。テナントレベルの操作用です。

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
操作にターゲットフォルダとそのすべてのサブフォルダを含めることを指定します。

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

### -Version
取得するプロセスパッケージのバージョンを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Package
## NOTES
このコマンドレットは、パッケージバージョン情報にアクセスするためのテナントレベルエンティティ操作です。パッケージは、バージョン管理とデプロイメント管理を備えた自動化プロセスとライブラリを表します。現在デプロイされているバージョンを特定するにはIsActiveプロパティを使用してください。ProjectTypeは実行可能プロセスと再利用可能ライブラリを区別します。TargetFrameworkはプラットフォーム互換性を示します。この操作にはPackages.View権限が必要です。

主要エンドポイント: GET /odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions
OAuth必須スコープ: OR.Execution または OR.Execution.Read
必要な権限: Packages.View

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)

[Add-OrchPackage](Add-OrchPackage.md)

[Set-OrchPackage](Set-OrchPackage.md)

[Remove-OrchPackageVersion](Remove-OrchPackageVersion.md)
