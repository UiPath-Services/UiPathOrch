---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchLicenseRuntime

## SYNOPSIS
ランタイムライセンスを無効にします。

## SYNTAX

```
Disable-OrchLicenseRuntime [-RobotType] <String[]> [-Key] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchLicenseRuntimeコマンドレットは、特定のロボットタイプとマシンキーに対してランタイムライセンスを無効にします。ランタイムライセンスは、ロボットが自動化プロセスを実行する能力を制御します。

これはテナントエンティティコマンドレットです。-Pathパラメータは、ドライブ名（例：Orch1:、Orch2:）を使用してターゲットテナントを指定します。指定されない場合、現在のテナントがターゲットになります。

ランタイムライセンスを無効にすると、ロボットは新しいジョブを実行できなくなりますが、実行中のジョブは完了まで継続されます。これは、メンテナンスシナリオやライセンス管理に役立ちます。

主要エンドポイント: POST /odata/LicensesRuntime('{machineName}')/UiPath.Server.Configuration.OData.ToggleEnabled

OAuth必要スコープ: OR.License

必要な権限: Machines.Edit

## EXAMPLES

### Example 1
```powershell
Disable-OrchLicenseRuntime Unattended Machine01 -WhatIf
```

Machine01のUnattendedランタイムライセンスを無効にする際の動作を表示します。

### Example 2
```powershell
Disable-OrchLicenseRuntime Unattended Machine01
```

現在のテナントでMachine01のUnattendedランタイムライセンスを無効にします。

### Example 3
```powershell
Disable-OrchLicenseRuntime Studio, StudioX *DevMachine*
```

キーに"DevMachine"が含まれるすべてのマシンのStudioおよびStudioXランタイムライセンスを無効にします。

### Example 4
```powershell
Disable-OrchLicenseRuntime -Path Orch1:, Orch2: Unattended TestMachine01, TestMachine02 -Confirm
```

複数のテナントでTestMachine01とTestMachine02のUnattendedランタイムライセンスを確認付きで無効にします。

### Example 5
```powershell
Disable-OrchLicenseRuntime NonProduction *Test*
```

キーに"Test"が含まれるすべてのマシンのNonProductionランタイムライセンスを無効にします。

### Example 6
```powershell
Get-OrchLicenseRuntime -RobotType Unattended | Where-Object {$_.IsEnabled -eq $true} | Disable-OrchLicenseRuntime
```

現在有効になっているすべてのUnattendedランタイムライセンスを無効にします。ライセンス情報はByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

### -Key
無効にするランタイムライセンスのキーを指定します。これは通常、マシン名または識別子に対応します。

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
ドライブ名を使用してターゲットテナントの名前を指定します。指定されない場合、現在のテナントがターゲットになります。

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
無効にするランタイムライセンスのロボットタイプを指定します。一般的な値には、Unattended、Attended、Studio、StudioX、NonProductionがあります。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
