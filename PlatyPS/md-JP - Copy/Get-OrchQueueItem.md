---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchQueueItem

## SYNOPSIS
キューアイテムを取得します。

## SYNTAX

```
Get-OrchQueueItem [-Name] <String[]> [-Skip <UInt64>] [-First <UInt64>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

主に呼び出すエンドポイント: GET /odata/QueueItems?$filter=(QueueDefinitionId eq {queueId})&$expand=Robot,ReviewerUser&orderby=Id

OAuth に必要なスコープ:  OR.Queues or OR.Queues.Read

必要な権限: Queues.View and Transactions.View

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
取得するアイテムを含むキューの Name を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
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

### -Skip
指定された数のエンティティを無視して、残りのエンティティを取得します。
スキップするエンティティの数を指定してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -First
指定された数のエンティティのみを取得します。
取得するエンティティの数を指定してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

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

### UiPath.PowerShell.Entities.QueueItem
## NOTES

## RELATED LINKS
