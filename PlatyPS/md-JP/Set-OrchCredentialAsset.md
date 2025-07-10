---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchCredentialAsset

## SYNOPSIS
クレデンシャルアセットを新規作成/更新します。

## SYNTAX

### DefaultParameterSet (Default)
```
Set-OrchCredentialAsset [-Name] <String[]> [[-UserName] <String[]>] [[-MachineName] <String[]>]
 -Credential <PSCredential> [-Description <String>] [-CredentialStore <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### SpecifyPlainPasswordParameterSet
```
Set-OrchCredentialAsset [-Name] <String[]> [[-UserName] <String[]>] [[-MachineName] <String[]>]
 [[-CredentialUsername] <String>] [[-CredentialPassword] <String>] [-ExternalName <String>]
 [-Description <String>] [-CredentialStore <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

主に呼び出すエンドポイント: POST /odata/Assets, PUT /odata/Assets({asset.Id}, DELETE /odata/Assets({assetId})

OAuth に必要なスコープ: OR.Assets

必要な権限: Assets.View, Assets.Edit, Assets.Create, Assets.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -CredentialPassword
更新するクレデンシャルアセットの CredentialPassword を指定します。

```yaml
Type: String
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CredentialStore
更新するクレデンシャルアセットの CredentialStore を指定します。

```yaml
Type: String
Parameter Sets: DefaultParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -CredentialUsername
更新するクレデンシャルアセットの CredentialUsername を指定します。

```yaml
Type: String
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Description
更新するクレデンシャルアセットの Description を指定します。

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

### -MachineName
更新するクレデンシャルアセットの MachineName を指定します。

```yaml
Type: String[]
Parameter Sets: DefaultParameterSet
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
更新するクレデンシャルアセットの Name を指定します。存在しないアセット名を指定した場合は、その名前で新規にアセットを作成します。

```yaml
Type: String[]
Parameter Sets: DefaultParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

```yaml
Type: String[]
Parameter Sets: DefaultParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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

### -UserName
更新するクレデンシャルアセットの UserName を指定します。

```yaml
Type: String[]
Parameter Sets: DefaultParameterSet
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

```yaml
Type: String[]
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Confirm
コマンドレットを実行する前に、あなたの確認を求めます。

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
コマンドレットを実行すると、何が起こるかを表示します。
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

### -ExternalName
更新するクレデンシャルアセットの ExternalName を指定します。指定すると、-CredentialUsername と -CredentialPassword は無視されます。

```yaml
Type: String
Parameter Sets: SpecifyPlainPasswordParameterSet
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Credential
{{ Fill Credential Description }}

```yaml
Type: PSCredential
Parameter Sets: DefaultParameterSet
Aliases:

Required: True
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

### UiPath.PowerShell.Entities.Asset
## NOTES

## RELATED LINKS
