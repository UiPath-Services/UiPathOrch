---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-PmLicenseToPmLicensedGroup

## SYNOPSIS
ライセンスグループにライセンスを追加します。

## SYNTAX

```
Add-PmLicenseToPmLicensedGroup [-GroupName] <String[]> [-License] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-PmLicenseToPmLicensedGroup コマンドレットは、UiPath Platform Management 内のライセンスグループにライセンスを割り当てます。このコマンドレットは組織レベルでライセンス配布を管理し、グループのメンバーにライセンスを割り当てることができるグループに UiPath ライセンスを配布できます。

ライセンスグループは、UiPath ライセンス（Studio、有人ロボット、無人ロボットライセンスなど）を保持し、メンバーに配布できる特別なグループです。これらのグループにライセンスを追加すると、グループメンバーのロールと要件に基づいて、それらのライセンスが自動割り当てに利用できるようになります。

ライセンスを割り当てるライセンスグループを指定するには -GroupName パラメーターを使用し、割り当てるライセンスタイプを指定するには -License パラメーターを使用します。-Path パラメーターを使用すると、複数のプラットフォームインスタンスで作業できます。

これはテナントエンティティコマンドレットです。-Path パラメーターは、複数の環境で作業する際に特定のプラットフォームインスタンスをターゲットにするためのドライブ名（例：Orch1:、Orch2:）を指定します。

主要エンドポイント: PUT /api/license/accountant/UserLicense/group

OAuth 必要スコープ: [PLACEHOLDER]

必要なアクセス許可: 組織レベルでのライセンス管理権限

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-PmLicenseToPmLicensedGroup Developers StudioPro
```

現在のプラットフォームインスタンスで、Developers ライセンスグループに StudioPro ライセンスを追加します。

### Example 2
```powershell
PS C:\> Add-PmLicenseToPmLicensedGroup -Path Orch1:, Orch2: "Automation Team" UnattendedRobot
```

Orch1 と Orch2 の両方のプラットフォームインスタンスで、"Automation Team" ライセンスグループに UnattendedRobot ライセンスを追加します。

### Example 3
```powershell
PS Orch1:\> Add-PmLicenseToPmLicensedGroup TestTeam StudioPro, AttendedRobot -WhatIf
```

TestTeam ライセンスグループに StudioPro と AttendedRobot の両方のライセンスを追加する際に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Add-PmLicenseToPmLicensedGroup -GroupName "Production Bots" -License UnattendedRobot -Confirm
```

確認プロンプト付きで "Production Bots" ライセンスグループに UnattendedRobot ライセンスを追加します。

### Example 5
```powershell
PS Orch1:\> Add-PmLicenseToPmLicensedGroup "Business Users", "Power Users" StudioX
```

"Business Users" と "Power Users" の両方のライセンスグループに StudioX ライセンスを追加します。

### Example 6
```powershell
PS C:\> Get-PmLicensedGroup | Where-Object {$_.Name -like "*Dev*"} | Add-PmLicenseToPmLicensedGroup -License StudioPro
```

名前に "Dev" を含むすべてのライセンスグループを取得し、パイプライン入力を使用して StudioPro ライセンスを追加します。

## PARAMETERS

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

### -GroupName
ライセンスを割り当てるライセンスグループの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -License
ライセンスグループに割り当てるライセンスタイプを指定します。

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
対象とするプラットフォームインスタンスを指定します。

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

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
## OUTPUTS

### UiPath.PowerShell.Entities.UpdateLicensedGroupResponse
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、特定のプラットフォームインスタンスをターゲットにするためのドライブ名（例：Orch1:、Orch2:）を指定します。

このコマンドレットは、ライセンス配布のために Platform Management を通じて組織レベルで動作します。ライセンスグループは、割り当てられたライセンスをメンバーに自動的に配布できます。一般的なライセンスタイプには、StudioPro、StudioX、AttendedRobot、および UnattendedRobot があります。実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmLicensedGroup](Get-PmLicensedGroup.md)

[Remove-PmLicenseFromPmLicensedGroup](Remove-PmLicenseFromPmLicensedGroup.md)

[Get-PmLicense](Get-PmLicense.md)

[Get-PmLicensedGroupMember](Get-PmLicensedGroupMember.md)
