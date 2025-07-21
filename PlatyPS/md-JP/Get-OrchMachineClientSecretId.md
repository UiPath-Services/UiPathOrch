---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachineClientSecretId

## SYNOPSIS
複数のマシンからクライアントシークレットの作成日時を取得します。

## SYNTAX

```
Get-OrchMachineClientSecretId [[-Name] <String[]>] [[-SecretId] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchMachineClientSecretId コマンドレットは、作成日と識別子を含むマシンのクライアントシークレット情報を取得します。クライアントシークレットは、Orchestratorに対するマシン認証に使用され、セキュリティ管理のための有効期限ポリシーを持ちます。

これはテナントエンティティコマンドレットです。-Path パラメータは、ドライブ名（例：Orch1:、Orch2:）を使用してターゲットテナントを指定します。指定されていない場合は、現在のテナントがターゲットになります。

このコマンドレットは、セキュリティ監査、シークレットローテーション管理、および作成日に基づいて更新が必要なシークレットの特定に便利です。

主要エンドポイント: GET /api/clientsecrets/{licenseKey}

OAuth必須スコープ: OR.Machines

必要な権限: Machines.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId
```

現在のテナント内のすべてのマシンのクライアントシークレットの発行日時を出力します。

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId Machine01, Machine02
```

指定されたマシンのクライアントシークレットの発行日時を出力します。

### Example 3
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId *Prod*
```

名前に"Prod"を含むすべてのマシンのクライアントシークレット情報を取得します。

### Example 4
```powershell
PS C:\> Get-OrchMachineClientSecretId -Path Orch1:, Orch2: Machine01
```

複数のテナント間でMachine01のクライアントシークレット情報を取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId -SecretId *123456*
```

IDに"123456"を含むシークレットのクライアントシークレット情報を取得します。

## PARAMETERS

### -Name
クライアントシークレット情報を取得するマシンの名前を指定します。

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
ドライブ名を使用してターゲットテナントの名前を指定します。指定されていない場合は、現在のテナントがターゲットになります。

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
取得するクライアントシークレットIDを指定します。これは、識別子によって特定のシークレットをクエリするために使用できます。

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

主要エンドポイント: GET /api/clientsecrets
OAuth必須スコープ: [PLACEHOLDER]
必要な権限: [PLACEHOLDER]

## RELATED LINKS
