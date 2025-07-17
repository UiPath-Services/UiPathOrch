---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchMachineClientSecret

## SYNOPSIS
Orchestrator のマシンにクライアントシークレットを追加します。

## SYNTAX

```
Add-OrchMachineClientSecret [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchMachineClientSecret コマンドレットは、Orchestrator 環境内のマシンにクライアントシークレットを追加します。クライアントシークレットは、無人ロボットが Orchestrator サービスと安全に認証できるようにする認証資格情報です。

クライアントシークレットは、ロボットがユーザーの介入なしに Orchestrator に接続する必要がある無人自動化シナリオのための安全な認証メカニズムを提供します。各マシンは、セキュリティローテーションとバックアップ目的で複数のクライアントシークレットを持つことができます。

このコマンドレットは、指定されたマシンに新しいクライアントシークレットを生成し、ロボットマシンで設定する必要があるシークレット情報を返します。この操作は、安全な無人ロボット認証を設定するために不可欠です。

主要エンドポイント: GET /odata/Machines, POST /api/clientsecrets/{machineId}

OAuth 必要スコープ: OR.Machines

必要なアクセス許可: [PLACEHOLDER - マシンクライアントシークレット管理権限の確認が必要]

## EXAMPLES

### Example 1
```powershell
PS C:\> Add-OrchMachineClientSecret ProductionMachine
```

"ProductionMachine" という名前のマシンに新しいクライアントシークレットを追加します。

### Example 2
```powershell
PS C:\> Add-OrchMachineClientSecret Machine1, Machine2
```

カンマ区切りリストで指定された複数のマシンに新しいクライアントシークレットを追加します。

### Example 3
```powershell
PS C:\> Add-OrchMachineClientSecret Prod* -WhatIf
```

名前が "Prod" で始まるすべてのマシンにクライアントシークレットが追加される場合に何が起こるかを、実際に操作を実行することなく表示します。

### Example 4
```powershell
PS C:\> Add-OrchMachineClientSecret TestMachine -Path Orch1:, Orch2:
```

複数の Orchestrator 環境で "TestMachine" という名前のマシンにクライアントシークレットを追加します。

### Example 5
```powershell
PS C:\> $secret = Add-OrchMachineClientSecret AutomationServer -Confirm
PS C:\> $secret | Select-Object MachineKey, ClientSecret
```

確認プロンプト付きでクライアントシークレットを追加し、返されたシークレット情報を設定用にキャプチャします。

### Example 6
```powershell
PS C:\> Get-OrchMachine | Where-Object {$_.Type -eq "Standard"} | Add-OrchMachineClientSecret -WhatIf
```

標準マシンを特定し、実際に操作を実行することなく、どのクライアントシークレットが追加されるかを表示します。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認を求めます。これは、マシン認証設定に影響するため、クライアントシークレットを追加する際に重要です。

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

### -Name
クライアントシークレットを追加するマシンの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのマシンにクライアントシークレットが追加されます。

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
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。このパラメーターを使用して、複数の Orchestrator 環境のマシンに同時にクライアントシークレットを追加します。

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
コマンドレットを実行した場合の動作を表示します。コマンドレットは実行されません。実際に操作を実行する前に、どのマシンにクライアントシークレットが追加されるかをプレビューするには、このパラメーターを使用します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey
## NOTES

このコマンドレットはテナントエンティティで動作します。つまり、特定のフォルダーではなく、Orchestrator テナント全体のマシンに影響します。

生成されたクライアントシークレットは安全に保存し、対応するロボットマシンで設定する必要があります。シークレット情報は機密性が高いため、組織のセキュリティポリシーに従って処理する必要があります。

クライアントシークレットは無人ロボット認証を有効にします。生成されたシークレット情報には、認可された担当者のみがアクセスできるようにしてください。

セキュリティ上の理由から、このコマンドレットと Remove-OrchMachineClientSecret を組み合わせて、定期的なクライアントシークレットローテーションの実装を検討してください。

複数のマシンにマッチする可能性があるワイルドカードパターンを使用する場合は特に、-WhatIf パラメーターを使用して操作をプレビューしてください。

返される MachineSecretKey オブジェクトには、ロボット認証を設定するために必要なすべての情報が含まれています。生成後すぐにこの情報を安全に保存してください。

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)
[Remove-OrchMachineClientSecret](Remove-OrchMachineClientSecret.md)
[Get-OrchMachineClientSecretId](Get-OrchMachineClientSecretId.md)
[Update-OrchMachine](Update-OrchMachine.md)
