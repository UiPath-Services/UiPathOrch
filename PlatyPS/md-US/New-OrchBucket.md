---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchBucket

## SYNOPSIS
Creates new storage buckets.

## SYNTAX

```
New-OrchBucket [[-Name] <String[]>] [-Description <String>] [-StorageProvider <String>]
 [-StorageParameters <String>] [-StorageContainer <String>] [-CredentialStore <String>] [-Password <String>]
 [-Options <String[]>] [-ExternalName <String>] [-Tags <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The New-OrchBucket cmdlet creates new storage buckets in UiPath Orchestrator. Buckets provide file storage capabilities for automation processes, allowing them to store, retrieve, and manage files during execution.

**This is a folder entity cmdlet.** To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using the -Path parameter. If you attempt to run this cmdlet without being in a folder context, you will receive the error: "Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters."

Buckets can be configured with various storage providers including FileSystem, Azure Blob Storage, Amazon S3, and others. You can specify custom storage containers, credential stores for secure authentication, and organize buckets using tags. Options allow you to control bucket behavior such as read-only access or encryption settings.

Primary Endpoint: POST /odata/Buckets
OAuth required scopes: OR.Buckets or OR.Buckets.Write
Required permissions: Buckets.Create

## EXAMPLES

### Example 1
```powershell
New-OrchBucket ProjectFiles
```

Creates a new bucket named "ProjectFiles" in the current folder using positional parameters.

### Example 2
```powershell
New-OrchBucket -Path Orch1:\Production ProcessData -Description "Production process data storage"
```

Creates a bucket named "ProcessData" in the Production folder with a description.

### Example 3
```powershell
New-OrchBucket BackupBucket -StorageProvider "Azure" -StorageContainer "prod-backups" -CredentialStore "AzureCredentials"
```

Creates a bucket configured with Azure Blob Storage as the storage provider.

### Example 4
```powershell
New-OrchBucket ArchiveData -Options ReadOnly -Tags Archive, Historical, Q4-2024
```

Creates a read-only bucket with organizational tags for archival purposes.

### Example 5
```powershell
"TempFiles", "LogFiles", "ReportFiles" | ForEach-Object { New-OrchBucket $_ -WhatIf }
```

Shows what would happen when creating multiple buckets using pipeline processing.

### Example 6
```powershell
New-OrchBucket CustomerData -Path Orch1:\Finance -StorageProvider "FileSystem" -StorageContainer "\\fileserver\customerdata" -ExternalName "CustomerDataBucket" -Tags Production, Finance
```

Creates a bucket in the Finance folder using a file server as the storage backend with external naming and production tags.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

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
Specifies the name of the credential store to use for authentication with the storage provider.

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
Specifies a description for the bucket that explains its purpose or contents.

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
Specifies an external name for the bucket, useful when the bucket needs to be referenced differently in external systems.

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
Specifies the name(s) of the bucket(s) to create. The name must be unique within the folder.

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
Specifies bucket options such as ReadOnly, Encrypted, or other provider-specific options.

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
Specifies the password for authentication with the storage provider.

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
Specifies the path(s) to the target folder(s) where the bucket(s) will be created. Use this parameter to create buckets in specific folders without changing your current location.

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
Specifies the storage container or path where the bucket data will be stored. The format depends on the storage provider.

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
Specifies additional storage-specific parameters as a JSON string or parameter string.

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
Specifies the storage provider to use for the bucket. Common providers include FileSystem, Azure, AmazonS3, and others.

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
Specifies tags to associate with the bucket for organization, categorization, and management purposes.

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
Shows what would happen if the cmdlet runs. The cmdlet is not run.

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
- Bucket names must be unique within the folder
- Some storage providers may require additional configuration or credentials
- Consider using -WhatIf to preview the operation before actual creation
- Tags are useful for organizing buckets and implementing governance policies
- The bucket creation may take time depending on the storage provider and configuration

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)
[Remove-OrchBucket](Remove-OrchBucket.md)
[Copy-OrchBucket](Copy-OrchBucket.md)
