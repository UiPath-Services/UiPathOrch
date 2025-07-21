---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchMachine

## SYNOPSIS
マシンを作成します。

## SYNTAX

```
New-OrchMachine [-Name] <String[]> [-Description <String>] [-Type <String>] [-Scope <String>]
 [-UnattendedSlots <Int32>] [-NonProductionSlots <Int32>] [-TestAutomationSlots <Int32>]
 [-AutomationType <String>] [-TargetFramework <String>] [-RobotUsers <String[]>] [-Tags <String[]>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
New-OrchMachineコマンドレットは、UiPath Orchestratorに新しいマシンを作成します。マシンは、ロボットがオートメーションプロセスを実行できる実行環境を定義します。本番ワークロード用のStandardマシンや、標準化された設定を作成するためのTemplateマシンなど、さまざまなタイプのマシンを設定できます。

**これはテナントエンティティコマンドレットです。** ドライブ名（例：Orch1:、Orch2:）を使用して-Pathパラメータでターゲットテナントを指定できます。-Pathが指定されていない場合、マシンは現在の場所に作成されます。

マシンは、さまざまなオートメーションシナリオに対して異なるスロットタイプで設定できます：バックグラウンドオートメーション用のUnattendedSlots、開発およびテスト用のNonProductionSlots、自動テストシナリオ用のTestAutomationSlots。また、オートメーションタイプ（Any、Foreground、Background）およびターゲットフレームワーク（Portable、Windows）も指定できます。

このコマンドレットは、CSVインポート機能をサポートしています。一括マシン作成の形式を取得するには、Get-OrchMachine -ExportCsvを使用してください。

プライマリ エンドポイント: POST /odata/Machines
OAuth 必要なスコープ: OR.Machines または OR.Machines.Write
必要な権限: Machines.Create

## EXAMPLES

### Example 1
```powershell
New-OrchMachine ProdMachine01
```

位置パラメータを使用して"ProdMachine01"という名前の新しいマシンを作成します。

### Example 2
```powershell
New-OrchMachine DevMachine -Type "Standard" -Description "Development environment machine" -UnattendedSlots 2
```

開発作業用に2つの無人スロットを持つ標準マシンを作成します。

### Example 3
```powershell
New-OrchMachine TemplateMachine -Type "Template" -AutomationType "Background" -TargetFramework "Windows" -Tags Template, StandardConfig
```

組織タグ付きでWindows上のバックグラウンドオートメーション用に設定されたテンプレートマシンを作成します。

### Example 4
```powershell
New-OrchMachine TestMachine -UnattendedSlots 1 -NonProductionSlots 2 -TestAutomationSlots 1 -RobotUsers "domain\user1", "domain\user2"
```

さまざまなスロットタイプを持ち、このマシンで実行できるロボットユーザーを指定したマシンを作成します。

### Example 5
```powershell
"Machine01", "Machine02", "Machine03" | ForEach-Object { New-OrchMachine $_ -WhatIf -Tags Production, Cluster-A }
```

パイプライン処理を使用して共通タグ付きで複数のマシンを作成する場合の結果を表示します。

### Example 6
```powershell
New-OrchMachine -Path Orch2: BackupMachine -Type "Standard" -Scope "Global" -NonProductionSlots 1 -Description "Backup processing machine"
```

バックアップ操作用にグローバルスコープでOrch2テナントにマシンを作成します。

## PARAMETERS

### -AutomationType
マシンがサポートするオートメーションタイプを指定します。有効な値は、"Any"（フォアグラウンドとバックグラウンドの両方をサポート）、"Foreground"（ユーザー操作が必要）、または"Background"（ユーザー操作なしで実行）です。

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

### -Description
マシンの目的や設定を説明する説明を指定します。

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

### -Name
作成するマシンの名前を指定します。マシン名はテナント内で一意である必要があります。

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

### -NonProductionSlots
開発およびテスト目的の非本番スロット数を指定します。これらのスロットは通常、開発環境で使用されます。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
マシンが作成されるターゲットテナントへのパスを指定します。特定のテナントをターゲットにするには、Orch1:、Orch2:などのドライブ名を使用します。

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

### -RobotUsers
このマシンでオートメーションを実行する権限があるロボットユーザーを指定します。ドメインユーザーにはdomain\username形式を使用します。

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

### -Scope
マシンのスコープを指定します。これにより、組織内でのマシンの可視性とアクセスレベルが決まります。

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

### -Tags
組織化、分類、管理目的でマシンに関連付けるタグを指定します。

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

### -TargetFramework
マシンのターゲットフレームワークを指定します。有効な値には、"Portable"（クロスプラットフォーム）と"Windows"（Windows固有フレームワーク）があります。

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

### -TestAutomationSlots
自動テストシナリオ用のテストオートメーションスロット数を指定します。これらのスロットは、テストケースとテストセットの実行専用です。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
作成するマシンのタイプを指定します。有効な値は、通常のマシン用の"Standard"と、設定を標準化するために使用されるテンプレートマシン用の"Template"です。

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

### -UnattendedSlots
バックグラウンドオートメーション用の無人スロット数を指定します。これらのスロットにより、ロボットはユーザー操作なしで実行できます。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合の結果を表示します。コマンドレットは実行されません。

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
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.CreatedMachine
## NOTES
- マシン名はテナント内で一意である必要があります
- 実際の作成前に操作をプレビューするには、-WhatIfの使用を検討してください
- スロット割り当てはライセンスに影響するため、オートメーションニーズに応じて計画する必要があります
- テンプレートマシンは、環境間でマシン設定を標準化するために使用できます
- ロボットユーザーは、マシンに割り当てられる前にシステムに存在している必要があります
- タグは、マシンの整理とガバナンスポリシーの実装に役立ちます

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)
[Update-OrchMachine](Update-OrchMachine.md)
[Remove-OrchMachine](Remove-OrchMachine.md)
[Copy-OrchMachine](Copy-OrchMachine.md)
