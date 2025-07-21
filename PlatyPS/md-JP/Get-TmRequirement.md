---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmRequirement

## SYNOPSIS
Test Managerのプロジェクトから要件を取得します。

## SYNTAX

```
Get-TmRequirement [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
プロジェクトからすべての要件を取得します。このコマンドレットは、UiPathOrchTmプロバイダーのPSDriveで動作します。構成ファイルのスコープに"TM."が含まれている場合、UiPathOrchTmプロバイダーのPSDriveが自動的に追加されます。これはGet-PSDriveコマンドレットで確認できます。構成ファイルは、Edit-OrchConfigコマンドレットで開くことができます。

プライマリ エンドポイント: GET /testmanager_/api/v2/{projectId}/requirements

OAuth 必要スコープ: TM.Requirements または TM.Requirements.Read

権限: Requirement.Read

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
取得する要件の名前を指定します。

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
対象フォルダーを指定します。指定されていない場合、現在のフォルダーが対象となります。

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
操作が対象フォルダーとそのすべてのサブフォルダーを含むべきことを指定します。

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TMRequirement
## NOTES

## RELATED LINKS
