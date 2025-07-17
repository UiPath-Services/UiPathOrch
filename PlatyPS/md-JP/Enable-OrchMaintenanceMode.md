---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchMaintenanceMode

## SYNOPSIS
無人セッションのメンテナンスモードを有効にします。

## SYNTAX

```
Enable-OrchMaintenanceMode [[-MachineName] <String[]>] [[-HostMachineName] <String[]>]
 [[-ServiceUserName] <String[]>] [[-SessionId] <Int64[]>] [-Force] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchMaintenanceModeコマンドレットは、UiPath Orchestrator内の無人ロボットセッションのメンテナンスモードを有効にします。このコマンドレットは、現在実行中のジョブを正常に完了させながら、ロボットセッションが新しいジョブを受け入れることを防ぎ、メンテナンス活動やシステム更新に最適です。

メンテナンスモードは、実行中の自動化プロセスを中断することなく、ロボットマシンでメンテナンス作業を実行するために不可欠です。有効にすると、ロボットセッションは現在のジョブを完了しますが、メンテナンスモードが無効になるまでキューから新しいジョブを受け入れません。

さまざまなフィルタリングパラメーターを使用して、メンテナンスモードの有効化対象となる特定のロボットセッションを指定できます。MachineName、HostMachineName、ServiceUserName、または特定のSessionId値でフィルタリングできます。-Pathパラメーターを使用して特定のフォルダを対象とすることができます。

-Forceパラメーターを使用して、複数のセッションに対してメンテナンスモードを同時に有効にする際の確認プロンプトをバイパスできます。

Primary Endpoint: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes

OAuth required scopes: OR.Robots or OR.Robots.Write

Required permissions: Machines.Edit

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Development> Enable-OrchMaintenanceMode -MachineName Robot01
`

現在のフォルダでマシンRobot01上のすべてのセッションのメンテナンスモードを有効にします。

### Example 2
`powershell
PS C:\> Enable-OrchMaintenanceMode -Path Orch1:\Production -HostMachineName Server01
`

ProductionフォルダでホストマシンServer01上のすべてのセッションのメンテナンスモードを有効にします。

### Example 3
`powershell
PS Orch1:\Development> Enable-OrchMaintenanceMode -ServiceUserName ServiceAccount01, ServiceAccount02 -WhatIf
`

ServiceAccount01とServiceAccount02を使用するセッションのメンテナンスモードを有効にする場合の動作を表示します。

### Example 4
`powershell
PS C:\> Enable-OrchMaintenanceMode -Path Orch1:\Development -SessionId 12345, 67890 -Force
`

DevelopmentフォルダでID 12345と67890の特定のセッションのメンテナンスモードを確認プロンプトなしで有効にします。

### Example 5
`powershell
PS Orch1:\Production> Enable-OrchMaintenanceMode -MachineName *Robot* -Confirm
`

名前にRobotを含むすべてのマシン上のセッションのメンテナンスモードを確認プロンプト付きで有効にします。

### Example 6
`powershell
PS C:\> Get-OrchUnattendedSession -MaintenanceMode $false | Enable-OrchMaintenanceMode -WhatIf
`

現在メンテナンスモードにないすべてのセッションを取得し、パイプライン入力を使用してメンテナンスモードを有効にする場合の動作を表示します。

## PARAMETERS

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

### -Force
確認プロンプトなしで操作を強制実行します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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

### -HostMachineName
メンテナンスモードの有効化対象となるホストマシン名を指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

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
メンテナンスモードの有効化対象となるマシン名を指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

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
ターゲットフォルダを指定します。指定されていない場合、現在のフォルダが対象になります。

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

### -ServiceUserName
メンテナンスモードの有効化対象となるサービスユーザー名を指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

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
メンテナンスモードの有効化対象となるセッションIDを指定します。

`yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
メンテナンスモードは、現在のジョブを完了させながら、ロボットセッションが新しいジョブを受け入れることを防ぎます。これは、実行中のプロセスを中断することなくメンテナンスを実行するために不可欠です。特定のセッションを対象とするためにフィルタリングパラメーターを使用してください。-Forceパラメーターは、一括操作の確認プロンプトをバイパスします。

## RELATED LINKS

[Disable-OrchMaintenanceMode](Disable-OrchMaintenanceMode.md)

[Get-OrchUnattendedSession](Get-OrchUnattendedSession.md)

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
