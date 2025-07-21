---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchFolderMachineAccountMapping

## SYNOPSIS
フォルダースコープのマシンのマシンアカウントマッピングを取得します。

## SYNTAX

```
Get-OrchFolderMachineAccountMapping [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchFolderMachineAccountMapping` コマンドレットは、特定のフォルダー内でマシンとユーザーアカウント間のアカウントマッピングを取得します。これらのマッピングは、どのユーザーがフォルダーコンテキスト内でどのマシンに接続できるかを定義します。

これはフォルダーエンティティコマンドレットです。このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定する必要があります。

このコマンドレットは、フォルダースコープ内のロボットアカウントとそれに関連するマシンを含む、マシンとユーザーの関係に関する情報を返します。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}), GET /odata/Robots/UiPath.Server.Configuration.OData.GetFolderRobots(folderId={folderId},machineId={machineId})

OAuth必要スコープ: OR.Robots または OR.Robots.Read

必要な権限: (SubFolders.View または Units.View または Jobs.Create)

## EXAMPLES

### Example 1
```powershell
Get-OrchFolderMachineAccountMapping
```

現在のフォルダー内のすべてのマシンアカウントマッピングを取得します。

### Example 2
```powershell
Get-OrchFolderMachineAccountMapping Machine01
```

現在のフォルダー内の"Machine01"という名前のマシンのアカウントマッピングを取得します。

### Example 3
```powershell
Get-OrchFolderMachineAccountMapping *Prod*
```

名前に"Prod"が含まれるすべてのマシンのアカウントマッピングを取得します。

### Example 4
```powershell
Get-OrchFolderMachineAccountMapping -Recurse
```

現在のフォルダーとそのすべてのサブフォルダーからすべてのマシンアカウントマッピングを取得します。

### Example 5
```powershell
Get-OrchFolderMachineAccountMapping -Path Orch1:\Development, Orch1:\Testing
```

DevelopmentとTestingの両方のフォルダーからマシンアカウントマッピングを取得します。

### Example 6
```powershell
Get-OrchFolderMachineAccountMapping | Where-Object {$_.RobotUserName -like "*service*"}
```

ロボットユーザー名に"service"が含まれるすべてのマシンアカウントマッピングを取得します。

### Example 7
```powershell
Get-OrchMachine *Robot* | Get-OrchFolderMachineAccountMapping
```

"Robot"を含むすべてのマシンのアカウントマッピングを取得します。マシン名は、ByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダーは含まれません。

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
アカウントマッピングを取得するマシンの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーがターゲットとなります。

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

### -Recurse
操作がターゲットフォルダーとそのすべてのサブフォルダーを含むように指定します。

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

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedRobot
## NOTES

このコマンドレットは、フォルダーコンテキスト内で特定のマシンに接続できるユーザーを定義するマシンアカウントマッピングを取得します。マッピング情報には、ロボットアカウントとそれに関連するマシンが含まれ、これはセキュリティとアクセス制御に不可欠です。

マシンアカウントマッピングはフォルダースコープであり、同じマシンが異なるフォルダーで異なるアカウントマッピングを持つ可能性があります。このコマンドレットは、アカウントマッピング情報にアクセスするために適切なフォルダー権限が必要です。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder
OAuth必要スコープ: OR.Machines または OR.Machines.Read
必要な権限: Machines.View

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)

[Enable-OrchFolderMachineAccountMapping](Enable-OrchFolderMachineAccountMapping.md)

[Disable-OrchFolderMachineAccountMapping](Disable-OrchFolderMachineAccountMapping.md)
