---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchLicenseRuntime

## SYNOPSIS
ランタイムライセンスを有効にします。

## SYNTAX

```
Enable-OrchLicenseRuntime [-RobotType] <String[]> [-Key] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchLicenseRuntimeコマンドレットは、特定のロボットタイプとマシンキーのランタイムライセンスを有効にします。ランタイムライセンスは、ロボットが自動化プロセスを実行する機能を制御します。

これはテナントエンティティコマンドレットです。-Pathパラメーターでドライブ名（例：Orch1:、Orch2:）を使用してターゲットテナントを指定します。指定されていない場合、現在のテナントが対象になります。

ランタイムライセンスを有効にすると、ロボットが自動化ジョブを実行できるようになります。これは、ライセンス割り当てやメンテナンス期間後に自動化機能を有効化するために不可欠です。

Primary Endpoint: POST /odata/LicensesRuntime('{machineName}')/UiPath.Server.Configuration.OData.ToggleEnabled

OAuth required scopes: OR.License

Required permissions: Machines.Edit

## EXAMPLES

### Example 1
`powershell
Enable-OrchLicenseRuntime Unattended Machine01 -WhatIf
`

Machine01のUnattendedランタイムライセンスを有効にする場合の動作を表示します。

### Example 2
`powershell
Enable-OrchLicenseRuntime Unattended Machine01
`

現在のテナントでMachine01のUnattendedランタイムライセンスを有効にします。

### Example 3
`powershell
Enable-OrchLicenseRuntime Studio, StudioX *DevMachine*
`

キーに"DevMachine"を含むすべてのマシンのStudioおよびStudioXランタイムライセンスを有効にします。

### Example 4
`powershell
Enable-OrchLicenseRuntime -Path Orch1:, Orch2: Unattended ProdMachine01, ProdMachine02 -Confirm
`

複数のテナントにわたってProdMachine01とProdMachine02のUnattendedランタイムライセンスを確認付きで有効にします。

### Example 5
`powershell
Enable-OrchLicenseRuntime NonProduction *Test*
`

キーに"Test"を含むすべてのマシンのNonProductionランタイムライセンスを有効にします。

### Example 6
`powershell
Get-OrchLicenseRuntime -RobotType Unattended | Where-Object {$_.IsEnabled -eq $false} | Enable-OrchLicenseRuntime
`

現在無効になっているすべてのUnattendedランタイムライセンスを有効にします。ライセンス情報は、ByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

### -Key
有効にするランタイムライセンスのキーを指定します。これは通常、マシン名または識別子に対応します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

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

### -Path
ドライブ名を使用してターゲットテナントの名前を指定します。指定されていない場合、現在のテナントが対象になります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

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

### -ProgressAction
{{ Fill ProgressAction Description }}

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

### -RobotType
有効にするランタイムライセンスのRobotTypeを指定します。一般的な値には、Unattended、Attended、Studio、StudioX、NonProductionが含まれます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

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

### -Confirm
コマンドレットの実行前に確認メッセージを表示します。

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

## RELATED LINKS
