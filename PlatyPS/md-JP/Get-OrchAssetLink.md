---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAssetLink

## SYNOPSIS
指定されたアセットのフォルダー関連付けとリンクを取得します。

## SYNTAX

```
Get-OrchAssetLink [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchAssetLink コマンドレットは、UiPath Orchestrator 内の指定されたアセットのフォルダー関連付けとリンクを取得します。アセットは Orchestrator 階層内の複数のフォルダーにリンクでき、このコマンドレットはそれらの関係を可視化します。

アセットリンクは、フォルダー構造内でアセットがアクセス可能な場所を定義し、適切なアクセス制御と組織化を可能にします。アセット-フォルダー関係の理解は、アセット管理、セキュリティポリシー、アクセス制御設定にとって重要です。

アセットリンクは、最初に適切なフォルダーコンテキストへの移動が必要なフォルダーエンティティです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。

このコマンドレットはフォルダーエンティティ操作として動作し、適切なフォルダーコンテキストへの移動または -Path パラメーターを使用したターゲットフォルダーの指定が必要です。検索にサブフォルダーを含めるには -Recurse パラメーターを使用し、再帰レベルを制御するには -Depth を使用します。

このコマンドレットは、管理者がフォルダー間でのアセット配布を理解し、アクセスの問題をトラブルシューティングし、複雑なフォルダー階層内でのアセット組織を管理するのに役立ちます。

主要エンドポイント: GET /odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={assetId})

OAuth 必須スコープ: OR.Assets または OR.Assets.Read

必要なアクセス許可: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Get-OrchAssetLink DatabaseConfig
```

現在のフォルダー内の DatabaseConfig アセットのフォルダーリンクを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse *API*
```

すべてのフォルダーから再帰的に、名前に "API" を含むすべてのアセットのフォルダーリンクを取得します。

### Example 3
```powershell
PS C:\> Get-OrchAssetLink -Path Orch1:\Production,Orch1:\Development -Name ConnectionString
```

Production および Development フォルダーから ConnectionString アセットのフォルダーリンクを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse | Select-Object Path, DisplayName, FullyQualifiedName
```

すべてのアセットリンクを再帰的に取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse -Depth 2
```

現在のフォルダーと最大 2 レベルのサブフォルダーでアセットリンクを取得します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度 0 は現在の場所のみを示します。より高い値はより多くのサブフォルダーレベルを含みます。

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

### -Name
フォルダーリンクを取得するアセットの名前を指定します。柔軟なアセット選択のためのワイルドカードパターンをサポートします。

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
検索するターゲットフォルダーを指定します。指定されていない場合、現在のフォルダーコンテキストが使用されます。パス指定が必要なフォルダーエンティティ操作用です。

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
{{ ProgressAction の説明を入力 }}

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
検索操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。包括的なアセットリンク検出に不可欠です。

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

### UiPath.PowerShell.Entities.SimpleFolder
## NOTES
このコマンドレットは、適切なフォルダーコンテキストまたはパス指定が必要なフォルダーエンティティ操作です。アセットリンクは、Orchestrator 階層内でのアセットへのフォルダーレベルアクセスを定義します。フォルダー検索の範囲を制御するには、-Recurse および -Depth パラメーターを使用してください。アセット-フォルダー関係の理解は、適切なアクセス制御とアセット管理に不可欠です。この操作には、ターゲットフォルダーでの Assets.View アクセス許可が必要です。

主要エンドポイント: GET /odata/Assets
OAuth 必須スコープ: OR.Assets または OR.Assets.Read
必要なアクセス許可: Assets.View

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Get-OrchFolder](Get-OrchFolder.md)

[Set-OrchAsset](Set-OrchAsset.md)

[Add-OrchAsset](Add-OrchAsset.md)
