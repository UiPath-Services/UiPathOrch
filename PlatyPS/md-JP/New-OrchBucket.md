---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchBucket

## SYNOPSIS
新しいストレージバケットを作成します。

## SYNTAX

```
New-OrchBucket [[-Name] <String[]>] [-Description <String>] [-StorageProvider <String>]
 [-StorageParameters <String>] [-StorageContainer <String>] [-CredentialStore <String>] [-Password <String>]
 [-Options <String[]>] [-ExternalName <String>] [-Tags <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
New-OrchBucketコマンドレットは、UiPath Orchestratorに新しいストレージバケットを作成します。バケットは、オートメーションプロセスにファイルストレージ機能を提供し、実行中にファイルの保存、取得、管理を可能にします。

**これはフォルダエンティティコマンドレットです。** このコマンドレットを使用するには、まずSet-Location（cd）を使用してターゲットフォルダに移動するか、-Pathパラメータを使用してターゲットフォルダを指定する必要があります。フォルダコンテキストにいない状態でこのコマンドレットを実行しようとすると、次のエラーが表示されます："Set-Locationコマンドレット（cdコマンド）を使用してまずターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定してください。"

バケットは、FileSystem、Azure Blob Storage、Amazon S3など、さまざまなストレージプロバイダーで設定できます。カスタムストレージコンテナ、セキュアな認証用の資格情報ストア、タグを使用したバケットの整理を指定できます。オプションを使用して、読み取り専用アクセスや暗号化設定などのバケットの動作を制御できます。

プライマリ エンドポイント: POST /odata/Buckets
OAuth 必要なスコープ: OR.Buckets または OR.Buckets.Write
必要な権限: Buckets.Create

## EXAMPLES

### Example 1
```powershell
New-OrchBucket ProjectFiles
```

位置パラメータを使用して、現在のフォルダに"ProjectFiles"という名前の新しいバケットを作成します。

### Example 2
```powershell
New-OrchBucket -Path Orch1:\Production ProcessData -Description "Production process data storage"
```

Productionフォルダに説明付きで"ProcessData"という名前のバケットを作成します。

### Example 3
```powershell
New-OrchBucket BackupBucket -StorageProvider "Azure" -StorageContainer "prod-backups" -CredentialStore "AzureCredentials"
```

Azure Blob Storageをストレージプロバイダーとして設定したバケットを作成します。

### Example 4
```powershell
New-OrchBucket ArchiveData -Options ReadOnly -Tags Archive, Historical, Q4-2024
```

アーカイブ目的で組織タグ付きの読み取り専用バケットを作成します。

### Example 5
```powershell
"TempFiles", "LogFiles", "ReportFiles" | ForEach-Object { New-OrchBucket $_ -WhatIf }
```

パイプライン処理を使用して複数のバケットを作成する場合の結果を表示します。

### Example 6
```powershell
New-OrchBucket CustomerData -Path Orch1:\Finance -StorageProvider "FileSystem" -StorageContainer "\\fileserver\customerdata" -ExternalName "CustomerDataBucket" -Tags Production, Finance
```

ファイルサーバーをストレージバックエンドとして使用し、外部名と本番タグを持つFinanceフォルダにバケットを作成します。

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

### -CredentialStore
ストレージプロバイダーでの認証に使用する資格情報ストアの名前を指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Description
バケットの目的や内容を説明する説明を指定します。

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

### -ExternalName
バケットが外部システムで異なって参照される必要がある場合に便利な、バケットの外部名を指定します。

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
作成するバケットの名前を指定します。名前はフォルダ内で一意である必要があります。

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

### -Options
ReadOnly、Encrypted、またはその他のプロバイダー固有のオプションなど、バケットオプションを指定します。

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

### -Password
ストレージプロバイダーでの認証用のパスワードを指定します。

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

### -Path
バケットが作成されるターゲットフォルダへのパスを指定します。このパラメータを使用すると、現在の場所を変更せずに特定のフォルダにバケットを作成できます。

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

### -StorageContainer
バケットデータが保存されるストレージコンテナまたはパスを指定します。形式はストレージプロバイダーによって異なります。

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

### -StorageParameters
JSON文字列またはパラメータ文字列として追加のストレージ固有パラメータを指定します。

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

### -StorageProvider
バケットに使用するストレージプロバイダーを指定します。一般的なプロバイダーには、FileSystem、Azure、AmazonS3などがあります。

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
組織化、分類、管理目的でバケットに関連付けるタグを指定します。

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
## OUTPUTS

### UiPath.PowerShell.Entities.Bucket
## NOTES
- バケット名はフォルダ内で一意である必要があります
- 一部のストレージプロバイダーでは、追加の設定または資格情報が必要な場合があります
- 実際の作成前に操作をプレビューするには、-WhatIfの使用を検討してください
- タグは、バケットの整理とガバナンスポリシーの実装に役立ちます
- バケットの作成は、ストレージプロバイダーと設定によって時間がかかる場合があります

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)
[Remove-OrchBucket](Remove-OrchBucket.md)
[Copy-OrchBucket](Copy-OrchBucket.md)
