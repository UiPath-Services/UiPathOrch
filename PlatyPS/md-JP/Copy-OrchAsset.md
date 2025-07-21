---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchAsset

## SYNOPSIS
アセットを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchAsset [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchAsset コマンドレットは、UiPath Orchestrator テナント内またはテナント間で、ソースフォルダから宛先フォルダにアセットをコピーします。このコマンドレットは、値、設定、メタデータを含む完全なアセットのコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートしています。テナント間で認証情報アセットをコピーする場合、コピー操作後に Set-OrchCredentialAsset を使用してパスワードを更新する必要があります。

-Name パラメーターを使用してコピーするアセットを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、Name 列と Destination 列を持つ CSV ファイル入力をサポートしており、一括コピー操作を可能にし、複雑なアセット移行シナリオを実現します。

これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからアセットをコピーし、宛先でフォルダ構造を維持します。

主要エンドポイント: GET /odata/Assets, GET /odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={id}), POST /odata/Assets

OAuth 必要スコープ: OR.Assets

必要な権限: Assets.View, Assets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchAsset DatabaseConnection Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内の Production フォルダに DatabaseConnection アセットをコピーします。

### Example 2
```powershell
PS Orch2:\> Copy-OrchAsset -Path Orch1:\Development APIKey Orch2:\Production
```

テナント間アセットコピーを実証して、Orch1:\Development から Orch2:\Production に APIKey アセットをコピーします。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchAsset ConfigAsset, DatabaseConnection Orch1:\Production -WhatIf
```

安全のため -WhatIf を使用して、現在のフォルダから Production フォルダに複数のアセットをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\Scripts> Import-Csv asset-migration.csv | Copy-OrchAsset
```

一括操作のため、Name 列と Destination 列を持つ CSV ファイルを使用してアセットをコピーします。CSV ファイルは現在の場所（C:\Scripts）にあります。
CSV 形式:
Path,Name,Destination
Orch1:\Development,DatabaseConnection,Orch2:\Production
Orch1:\Development,APIKey,Orch3:\Development

### Example 5
```powershell
PS Orch1:\> Copy-OrchAsset -Path Orch1:\Development *Config* Orch2:\Production
```

テナント間コピーでワイルドカードを使用して、Orch1:\Development から Orch2:\Production に名前に Config を含むすべてのアセットをコピーします。

### Example 6
```powershell
PS Orch1:\> Copy-OrchAsset -Recurse *Database* Orch2:\Finance -WhatIf
```

すべてのサブフォルダから Database を含むすべてのアセットを再帰的に Orch2:\Finance にコピーする場合に何が起こるかを表示します。

## PARAMETERS

### -Destination
宛先フォルダを指定します。

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
コピーするアセットの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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

### -Depth
{{ Fill Depth Description }}

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
{{ Fill Recurse Description }}

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

### UiPath.PowerShell.Entities.Asset
## NOTES
これはフォルダエンティティコマンドレットです。-Path パラメーターを使用してソースフォルダを指定するか、Set-Location を使用してソースフォルダに移動してください。

このコマンドレットは、テナント内とテナント間の両方のコピーをサポートしています。テナント間で認証情報アセットをコピーする場合、コピー後に Set-OrchCredentialAsset を使用してパスワードを更新してください。

一括操作には、Name 列と Destination 列を持つ CSV ファイルを使用してください。-Recurse パラメーターは、フォルダ構造を維持しながらすべてのサブフォルダからアセットをコピーします。

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Remove-OrchAsset](Remove-OrchAsset.md)

[Set-OrchAsset](Set-OrchAsset.md)

[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)
