---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchAssetLink

## SYNOPSIS
指定されたアセットにリンクを追加します。

## SYNTAX

```
Add-OrchAssetLink [-Name] <String[]> [-Link] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchAssetLink コマンドレットは、アセットのリンクを作成し、UiPath Orchestrator テナント内の複数のフォルダー間でアセットを共有できるようにします。このコマンドレットは、1 つのフォルダーから他の指定されたフォルダーに既存のアセットをリンクすることで、アセット共有を可能にします。

これはフォルダーエンティティコマンドレットです。"Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに最初に移動してください" というエラーが表示された場合は、ソースアセットを含むフォルダーに移動するか、-Path パラメーターを使用してターゲットフォルダーを指定してください。

-Name パラメーターを使用してリンクするアセットを指定し、-Link パラメーターを使用してアセットリンクを作成するターゲットフォルダーを指定します。アセットリンクは、アセットコンテンツを複製することなく、複数のフォルダーコンテキスト間で同じアセットデータへのアクセスを提供します。

このコマンドレットは、アセット名とフォルダーリンクの両方でワイルドカードパターンをサポートし、複数のアセットとフォルダーの一括操作を同時に可能にします。

主要エンドポイント: POST /odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth 必須スコープ: OR.Assets または OR.Assets.Write

必要なアクセス許可: Assets.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Add-OrchAssetLink SampleBooleanAsset Orch1:\root
```

位置パラメーターを使用して、現在のフォルダー（Shared）の SampleBooleanAsset を root フォルダーにリンクします。

### Example 2
```powershell
PS C:\> Add-OrchAssetLink -Path Orch1:\Shared DatabaseConfig Orch1:\Dept#2, Orch1:\root
```

明示的なパス指定を使用して、Shared フォルダーの DatabaseConfig アセットを Dept#2 と root フォルダーの両方にリンクします。

### Example 3
```powershell
PS Orch1:\Shared> Add-OrchAssetLink *Config* Orch1:\Dept#2
```

ワイルドカードを使用して、現在のフォルダーから名前に Config を含むすべてのアセットを Dept#2 フォルダーにリンクします。

### Example 4
```powershell
PS Orch1:\Shared> Add-OrchAssetLink SampleBooleanAsset, APIKey Orch1:\Dept#2 -WhatIf
```

安全のため -WhatIf を使用して、複数のアセットを Dept#2 フォルダーにリンクした場合の動作を表示します。

### Example 5
```powershell
PS C:\> Add-OrchAssetLink -Path Orch1:\Shared *Sample* Orch1:\root, Orch1:\Dept#2
```

Shared フォルダーから "Sample" を含むすべてのアセットを複数のターゲットフォルダーに同時にリンクします。

## PARAMETERS

### -Link
リンクとして追加するフォルダーを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
更新するアセットの名前を指定します。

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
ターゲットフォルダーを指定します。指定されていない場合、現在のフォルダーがターゲットになります。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。Set-Location を使用してフォルダーに移動するか、-Path パラメーターを使用してソースフォルダーを指定する必要があります。

アセットリンクにより、実際のアセットコンテンツを複製することなく、複数のフォルダーから同じアセットにアクセスできます。リンク作成の成功を確認するには Get-OrchAssetLink を使用してください。

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Get-OrchAssetLink](Get-OrchAssetLink.md)

[Remove-OrchAssetLink](Remove-OrchAssetLink.md)
