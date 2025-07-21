---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchMaintenanceMode

## SYNOPSIS
無人セッションのメンテナンスモードを無効にします。

## SYNTAX

```
Disable-OrchMaintenanceMode [[-MachineName] <String[]>] [[-HostMachineName] <String[]>]
 [[-ServiceUserName] <String[]>] [[-SessionId] <Int64[]>] [-Force] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchMaintenanceModeコマンドレットは、UiPath Orchestrator内の無人ロボットセッションのメンテナンスモードを無効にします。このコマンドレットにより、ロボットセッションはメンテナンス作業後に通常の動作を再開し、新しいジョブを受け入れて実行できるようになります。

メンテナンスモードは、現在実行中のジョブが完了するまで待機しつつ、ロボットセッションが新しいジョブを受け入れることを一時的に防ぐために使用されます。メンテナンスモードを無効にすると、通常のロボット動作が復元され、セッションはジョブキューを処理し、新しい自動化タスクを受け入れることができるようになります。

特定のロボットセッションをメンテナンスモード無効化の対象とするには、さまざまなフィルタリングパラメータを使用します。MachineName、HostMachineName、ServiceUserName、または特定のSessionId値でフィルタリングできます。-Pathパラメータを使用すると、特定のフォルダを対象にできます。

-Forceパラメータを使用すると、複数のセッションのメンテナンスモードを同時に無効にする際の確認プロンプトをバイパスできます。

主要エンドポイント: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes, POST /odata/Sessions/UiPath.Server.Configuration.OData.SetMaintenanceMode

OAuth必要スコープ: OR.Robots または OR.Robots.Write

必要な権限: Robots.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Disable-OrchMaintenanceMode -MachineName Robot01
```

現在のフォルダのマシンRobot01上のすべてのセッションのメンテナンスモードを無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchMaintenanceMode -Path Orch1:\Production -HostMachineName Server01
```

ProductionフォルダのホストマシンServer01上のすべてのセッションのメンテナンスモードを無効にします。

### Example 3
```powershell
PS Orch1:\Development> Disable-OrchMaintenanceMode -ServiceUserName ServiceAccount01, ServiceAccount02 -WhatIf
```

ServiceAccount01とServiceAccount02を使用しているセッションのメンテナンスモードを無効にする際の動作を表示します。

### Example 4
```powershell
PS C:\> Disable-OrchMaintenanceMode -Path Orch1:\Development -SessionId 12345, 67890 -Force
```

確認プロンプトなしで、DevelopmentフォルダのID 12345と67890の特定のセッションのメンテナンスモードを無効にします。

### Example 5
```powershell
PS Orch1:\Production> Disable-OrchMaintenanceMode -MachineName *Robot* -Confirm
```

名前にRobotが含まれるマシン上のすべてのセッションのメンテナンスモードを確認プロンプト付きで無効にします。

### Example 6
```powershell
PS C:\> Get-OrchUnattendedSession -MaintenanceMode $true | Disable-OrchMaintenanceMode -WhatIf
```

現在メンテナンスモードになっているすべてのセッションを取得し、パイプライン入力を使用してメンテナンスモードを無効にする際の動作を表示します。

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

### -HostMachineName
メンテナンスモード無効化の対象となるホストマシン名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MachineName
メンテナンスモード無効化の対象となるマシン名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象フォルダを指定します。指定されない場合、現在のフォルダが対象になります。

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

### -ServiceUserName
メンテナンスモード無効化の対象となるサービスユーザー名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SessionId
メンテナンスモード無効化の対象となるセッションIDを指定します。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
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

### -Force
確認プロンプトなしで操作を強制実行します。

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

### System.Object
## NOTES
メンテナンスモードは、現在のジョブが完了するまで待機しつつ、ロボットセッションが新しいジョブを受け入れることを防ぎます。メンテナンスモードを無効にすると、通常のロボット動作が復元されます。特定のセッションを対象とするためにフィルタリングパラメータを使用してください。-Forceパラメータは、一括操作での確認プロンプトをバイパスします。

## RELATED LINKS

[Enable-OrchMaintenanceMode](Enable-OrchMaintenanceMode.md)

[Get-OrchUnattendedSession](Get-OrchUnattendedSession.md)

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
