---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchLibrary

## SYNOPSIS
テナント間でライブラリをコピーします。

## SYNTAX

```
Copy-OrchLibrary [-Id] <String[]> [[-Version] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchLibrary コマンドレットは、UiPath Orchestrator 内のソーステナントから宛先テナントにライブラリをコピーします。このコマンドレットは、ライブラリの完全なコピーを作成します。

このコマンドレットは、複数の宛先テナントに同時にライブラリをコピーすることをサポートしています。ライブラリには、再利用可能な自動化コンポーネント、カスタムアクティビティ、複数のプロセスで使用できる共有コードが含まれています。

-Id パラメーターを使用して一意の識別子でコピーするライブラリを指定し、-Destination パラメーターを使用してターゲットテナントを指定します。-Version パラメーターを使用してコピーするライブラリの特定のバージョンを指定できます。-Path パラメーターを使用すると、複数の Orchestrator インスタンスで作業する際にソーステナントを指定できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:、Orch2:）を指定し、-Destination はライブラリをコピーするターゲットテナントドライブを指定します。

主要エンドポイント: GET /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='{packageId}'), GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{key}'), POST /odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage

OAuth 必要スコープ: OR.Execution

必要な権限: Libraries.View, Libraries.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchLibrary UiPath.Excel.Activities * Orch2:
```

現在のテナント（Orch1）から Orch2 テナントに UiPath.Excel.Activities ライブラリをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: UiPath.Mail.Activities * Orch2:, Orch3:
```

Orch1 から Orch2 と Orch3 の両方のテナントに UiPath.Mail.Activities ライブラリをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchLibrary UiPath.Excel.Activities 2.20.1 Orch2: -WhatIf
```

現在のテナントから Orch2 に UiPath.Excel.Activities ライブラリの特定のバージョン（2.20.1）をコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: *Custom* * Orch2:
```

ワイルドカードを使用して、Orch1 から Orch2 に ID に Custom を含むすべてのライブラリをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed *Excel* | Copy-OrchLibrary -Destination Orch2:, Orch3:
```

ID に Excel を含むすべてのホストフィードライブラリを取得し、パイプライン入力を使用して Orch2 と Orch3 の両方のテナントにコピーします。

### Example 6
```powershell
PS C:\> Copy-OrchLibrary -Path Orch1: UiPath.WebAPI.Activities 1.18.0, 1.19.0 Orch2:
```

Orch1 から Orch2 に UiPath.WebAPI.Activities ライブラリの特定のバージョン（1.18.0 と 1.19.0）をコピーします。

## PARAMETERS

### -Destination
ライブラリをコピーする宛先テナントドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
コピーするライブラリの ID を指定します。

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
ソーステナントドライブを指定します。指定しない場合、現在のテナントがソースとして使用されます。

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

### -Version
コピーするライブラリのバージョンを指定します。すべてのバージョンをコピーする場合は * を使用し、-Destination を直接指定するためにこの位置パラメーターを省略したい場合にも使用します。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先テナントのドライブ名（例：Orch1:、Orch2:）を指定します。

ライブラリは Name ではなく Id（パッケージ名）で識別されます。-Id と -Version は両方とも位置パラメーターです。パラメーター名を省略する場合、-Destination を直接指定したい場合は -Version に * を使用してください（例：Copy-OrchLibrary MyLibrary * Orch2:）。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchLibrary](Get-OrchLibrary.md)

[Remove-OrchLibrary](Remove-OrchLibrary.md)

[Import-OrchLibrary](Import-OrchLibrary.md)
