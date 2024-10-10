---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJobVideo

## SYNOPSIS
ビデオ録画が添付されたジョブを取得します。

## SYNTAX

```
Get-OrchJobVideo [[-Path] <String[]>] [-Recurse] [[-Skip] <UInt64>] [[-First] <UInt64>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -First
指定された数のエンティティのみを取得します。
取得するエンティティの数を指定してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Recurse
ターゲットフォルダーのサブフォルダーも、ターゲットとして含めることを指定します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
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
Position: 1
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
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
