---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachineClientSecretId

## SYNOPSIS
マシンのクライアントシークレットの作成日時を取得します。

## SYNTAX

```
Get-OrchMachineClientSecretId [[-Name] <String[]>] [[-SecretId] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

主に呼び出すエンドポイント: GET /api/clientsecrets/{licenseKey}

OAuth に必要なスコープ: OR.Machines

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId
```

このテナントにあるマシンすべてについて、クライアントシークレットが払い出された日時を出力します。

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId <machine names>
```

指定したマシンについて、クライアントシークレットが払い出された日時を出力します。

### Example 3
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId | ? CreationTime -LT '2024/10/01' | Remove-OrchMachineClientSecret
```

このテナントにあるマシンすべてについて、2024/10/01 より前に払い出されたクライアントシークレットをすべて削除します。

## PARAMETERS

### -Name
対象のマシンの Name を指定します。

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
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

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

### -SecretId
対象の SecretId を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey
## NOTES

## RELATED LINKS
