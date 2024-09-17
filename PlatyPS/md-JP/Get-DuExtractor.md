---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuExtractor

## SYNOPSIS
Document Understanding の抽出器を取得します。

## SYNTAX

```
Get-DuExtractor [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
プロジェクトからすべての抽出器を取得します。これらは、Forms AI またはディープラーニング抽出スキルのいずれかです。このコマンドレットは、UiPathOrchDu プロバイダの PSDrive 上で動作します。設定ファイルに、"Du." を含むスコープを記載すると、UiPathOrchDu プロバイダの PSDrive が自動で追加されます。Get-PSDrive コマンドレットで確認してください。設定ファイルは、Edit-OrchConfig コマンドレットで開けます。

主に呼び出すエンドポイント: GET /du_/api/framework/projects/{projectId}/extractors?api-version=1

OAuth に必要なスコープ: Du.Digitization.Api or Du.Classification.Api or Du.Extraction.Api or Du.Validation.Api

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
取得する抽出器の name を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Recurse
ターゲットフォルダーのサブフォルダーも、ターゲットとして含めることを指定します。

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

### UiPath.PowerShell.Entities.DuExtractor
## NOTES

## RELATED LINKS
