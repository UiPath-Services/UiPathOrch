---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchRole

## SYNOPSIS
ロールを追加または更新します。

## SYNTAX

```
Set-OrchRole [-Name] <String[]> [-Type <String>] -PermissionName <String> [-Scope <String>] [-View <String>]
 [-Edit <String>] [-Create <String>] [-Delete <String>] [-Path <String[]>] [-ExpandPermission]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchRole -ExportCsv でエクスポートされた CSV ファイルをインポートできます。ファイルがエクスポートされたテナントとは異なるテナントにインポートする場合は、CSV ファイルの Path 列を適切に変更してください。または、Set-OrchRole でインポートする際に -Path パラメーターで宛先ドライブ名を指定して、CSV ファイル内の Path 値を上書きできます。

プライマリ エンドポイント: GET /odata/Roles&$expand=Permissions, POST /odata/Roles, PUT /odata/Roles({role.Id}

OAuth 必要なスコープ: OR.Users

必要な権限: Roles.View, Roles.Create, Roles.Edit または Units.Edit または SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Import-Csv <filepath> | Set-OrchRole
```

{{ Add example description here }}

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

### -Create
{{ Fill Create Description }}

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

### -Delete
{{ Fill Delete Description }}

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

### -Edit
{{ Fill Edit Description }}

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

### -ExpandPermission
{{ Fill ExpandPermission Description }}

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

### -Name
{{ Fill Name Description }}

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
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -PermissionName
{{ Fill PermissionName Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Scope
{{ Fill Scope Description }}

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

### -Type
{{ Fill Type Description }}

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

### -View
{{ Fill View Description }}

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
このコマンドレットは共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.Role
## NOTES

## RELATED LINKS
