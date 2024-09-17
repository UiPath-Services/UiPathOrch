---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchPersonalWorkspace

## SYNOPSIS
�l�p���[�N�X�y�[�X�t�H���_�[��L���ɂ��܂��B

## SYNTAX

```
Enable-OrchPersonalWorkspace [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

��ɌĂяo���G���h�|�C���g: GET /odata/Users, GET /odata/Users({userId}), PUT /odata/Users({userId})

OAuth �ɕK�v�ȃX�R�[�v: OR.Users

�K�v�Ȍ���: Users.View, Users.Edit or Robots.Create or Robots.Edit or Robots.Delete.

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Confirm
�R�}���h���b�g�����s����O�ɁA���Ȃ��̊m�F�����߂܂��B

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

### -Path
�^�[�Q�b�g�Ƃ���h���C�u�̖��O���w�肵�܂��B�w�肵�Ȃ��ꍇ�́A���݂̃h���C�u���^�[�Q�b�g�Ƃ��܂��B

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

### -UserName
�l�p���[�N�X�y�[�X�t�H���_�[��L���ɂ��郆�[�U�[�� UserName ���w�肵�܂��B

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

### -WhatIf
�R�}���h���b�g�����s����ƁA�����N���邩��\�����܂��B
�R�}���h���b�g�͎��s����܂���B

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
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
