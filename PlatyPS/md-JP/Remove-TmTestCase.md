---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-TmTestCase

## SYNOPSIS
Test Manager のプロジェクトからテストケースを削除します。

## SYNTAX

```
Remove-TmTestCase [-Name] <String[]> [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
プロジェクトからテストケースを削除します。このコマンドレットは UiPathOrchTm プロバイダーの PSDrive 上で動作します。構成ファイル内のスコープに "TM." が含まれている場合、UiPathOrchTm プロバイダーの PSDrive が自動的に追加されます。これは Get-PSDrive コマンドレットで確認できます。構成ファイルは Edit-OrchConfig コマンドレットで開くことができます。

プライマリ エンドポイント: DELETE /testmanager_/api/v2/{projectId}/testcases/{testCaseId}

OAuth 必要なスコープ: TM.TestCases

権限: TestCase.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

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

### -Name
削除するテストケースの名前を指定します。

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
対象フォルダーを指定します。指定されていない場合は、現在のフォルダーが対象になります。

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
操作に対象フォルダーとそのすべてのサブフォルダーを含めることを指定します。

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

### -WhatIf
コマンドレットが実行された場合の動作を表示します。
コマンドレットは実行されません。

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
このコマンドレットは共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
