---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Move-OrchFolderUser

## SYNOPSIS
フォルダユーザーをフォルダ間で移動します。

## SYNTAX

```
Move-OrchFolderUser [-UserName] <String[]> [[-Destination] <String[]>] [-KeepSource <String>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Move-OrchFolderUserコマンドレットは、ユーザーをあるフォルダから別のフォルダに移動し、アクセス権とロール割り当てをフォルダコンテキスト間で転送します。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、まずSet-Location（cd）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

デフォルトでは、ユーザーを移動すると、ソースフォルダからアクセスが削除され、ターゲットフォルダへのアクセスが付与されます。-KeepSourceを使用して、ターゲットフォルダへのアクセスを追加しながら、元のフォルダへのアクセスを維持します。

プライマリ エンドポイント: POST /odata/FolderUsers/Move

OAuth 必要なスコープ: OR.Folders または OR.Folders.Write

必要な権限: Folders.Edit

## EXAMPLES

### Example 1
```powershell
Move-OrchFolderUser "automation developers" -WhatIf
```

"automation developers"ユーザーを現在のフォルダから移動する場合の結果を表示します。

### Example 2
```powershell
Move-OrchFolderUser "john.doe" -Destination "Orch1:\Development"
```

john.doeを現在のフォルダからDevelopmentフォルダに移動します。

### Example 3
```powershell
Move-OrchFolderUser "jane.smith", "bob.jones" -Destination "Orch1:\Production"
```

複数のユーザー（jane.smithとbob.jones）をProductionフォルダに移動します。

### Example 4
```powershell
Move-OrchFolderUser "*developer*" -Destination "Orch1:\Development"
```

名前に"developer"を含むすべてのユーザーをDevelopmentフォルダに移動します。

### Example 5
```powershell
Move-OrchFolderUser "service.account" -Destination "Orch1:\Testing" -KeepSource true
```

service.accountをTestingフォルダに移動し、現在のフォルダへのアクセスを維持します。

### Example 6
```powershell
Move-OrchFolderUser -Path "Orch1:\Legacy" "migration.user" -Destination "Orch1:\Modern" -Confirm
```

migration.userをLegacyフォルダからModernフォルダに確認付きで移動します。

### Example 7
```powershell
Get-OrchFolderUser | Where-Object {$_.UserEntity.Type -eq "DirectoryGroup"} | Move-OrchFolderUser -Destination "Orch1:\GroupManagement"
```

すべてのディレクトリグループユーザーをGroupManagementフォルダに移動します。ユーザー情報は、ByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

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

### -Destination
ユーザーが移動される宛先フォルダパスを指定します。

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

### -KeepSource
宛先に移動した後、ソースフォルダにユーザーを残すかどうかを指定します。二重フォルダアクセスを維持するには"true"に設定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。

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

### -UserName
フォルダ間で移動するユーザーの名前を指定します。

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

### -WhatIf
コマンドレットを実行した場合の結果を表示します。
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
