---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchApiTrigger

## SYNOPSIS
指定されたフォルダ内のAPIトリガーを有効にします。

## SYNTAX

```
Enable-OrchApiTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchApiTriggerコマンドレットは、UiPath Orchestrator内の指定されたフォルダで以前に無効化されたAPIトリガーを有効化します。このコマンドレットはAPIトリガーの操作を再度有効にし、外部システムがHTTP API呼び出しを通じてUiPathプロセスを開始できるようにします。

APIトリガーは外部システムがHTTP API呼び出しを通じてUiPathプロセスを開始できるようにします。これらを有効にすることで、トリガー機能が復元され、これらのトリガーを通じて新しいプロセスインスタンスを開始できるようになります。これは通常、メンテナンスやトラブルシューティング活動の後で、一時的に無効化されていたトリガーを再度有効にする際に使用されます。

-Nameパラメーターを使用して、有効化するAPIトリガーを指定します。このコマンドレットは、複数のトリガーを効率的に有効化するためのワイルドカードパターンをサポートしています。-Pathパラメーターを使用して特定のフォルダを対象とすることができ、-Recurseを使用してすべてのサブフォルダを処理できます。

これはフォルダエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダを指定してください。-Recurseパラメーターを使用すると、すべてのサブフォルダからAPIトリガーを有効化できます。

Primary Endpoint: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Triggers.Edit

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Development> Enable-OrchApiTrigger ProcessTrigger


```
位置パラメーターを使用して、現在のフォルダ（Development）でProcessTriggerというAPIトリガーを有効化します。

### Example 2
`powershell
PS C:\> Enable-OrchApiTrigger -Path Orch1:\Development DataProcessingTrigger
```

Orch1:\DevelopmentフォルダでDataProcessingTriggerというAPIトリガーを有効化します。

### Example 3
`powershell
PS Orch1:\Development> Enable-OrchApiTrigger *Daily*, *Weekly* -WhatIf


```
現在のフォルダで名前にDailyまたはWeeklyを含む複数のAPIトリガーを有効化する場合の動作を、安全性のため-WhatIfを使用して表示します。

### Example 4
`powershell
PS C:\> Enable-OrchApiTrigger -Path Orch1:\Development *API* -Confirm
```

Developmentフォルダで名前にAPIを含むすべてのAPIトリガーを確認プロンプトと共に有効化します。

### Example 5
`powershell
PS Orch1:\> Enable-OrchApiTrigger -Recurse *Integration*


```
名前にIntegrationを含むすべてのAPIトリガーを、すべてのサブフォルダから再帰的に有効化します。

### Example 6
`powershell
PS Orch1:\Development> Get-OrchApiTrigger -Enabled $false | Enable-OrchApiTrigger -WhatIf
```

無効化されたすべてのAPIトリガーを取得し、パイプライン入力を使用してそれらを有効化する場合の動作を表示します。

## PARAMETERS

### -Depth
-Recurseパラメーター使用時に含めるサブフォルダレベルの最大数を指定します。

`yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
有効化するAPIトリガーの名前を指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True

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
ターゲットフォルダを指定します。指定されていない場合、現在のフォルダが対象になります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True

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

### -Recurse
APIトリガーを全サブフォルダから再帰的に有効化することを指定します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False

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




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False




yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

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
これはフォルダエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダを指定してください。

APIトリガーは外部システムがHTTP API呼び出しを通じてプロセスを開始できるようにします。これらを有効にすることで、無効化された後にトリガー機能を復元できます。Disable-OrchApiTriggerを使用してトリガー操作を一時的に中断できます。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには-WhatIfを使用してください。

## RELATED LINKS

[Disable-OrchApiTrigger](Disable-OrchApiTrigger.md)

[Get-OrchApiTrigger](Get-OrchApiTrigger.md)

[Remove-OrchApiTrigger](Remove-OrchApiTrigger.md)

[Set-OrchApiTrigger](Set-OrchApiTrigger.md)
