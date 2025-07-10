---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchUser

## SYNOPSIS
ユーザーを更新します。

## SYNTAX

```
Update-OrchUser [-UserName] <String[]> [-Name <String>] [-Surname <String>] [-Roles <String[]>]
 [-IsExternalLicensed <String>] [-MayHaveUserSession <String>] [-MayHaveRobotSession <String>]
 [-MayHaveUnattendedSession <String>] [-MayHavePersonalWorkspace <String>]
 [-RestrictToPersonalWorkspace <String>] [-UpdatePolicyType <String>] [-UpdatePolicyVersion <String>]
 [-UR_UserName <String>] [-UR_CredentialStore <String>] [-UR_Password <String>]
 [-UR_CredentialExternalName <String>] [-UR_CredentialType <String>] [-UR_LimitConcurrentExecution <String>]
 [-ES_TracingLevel <String>] [-ES_StudioNotifyServer <String>] [-ES_LoginToConsole <String>]
 [-ES_ResolutionWidth <String>] [-ES_ResolutionHeight <String>] [-ES_ResolutionDepth <String>]
 [-ES_FontSmoothing <String>] [-ES_AutoDownloadProcess <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
[PLACEHOLDER - requires verification of description]

プライマリ エンドポイント: GET /odata/Users, GET /odata/Users({userId}), PUT /odata/Users({userId})

OAuth 必要なスコープ: OR.Users

必要な権限: Users.View, Users.Edit または Robots.Create または Robots.Edit または Robots.Delete

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

### -MayHavePersonalWorkspace
[PLACEHOLDER - requires verification of MayHavePersonalWorkspace parameter]

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

### -MayHaveRobotSession
[PLACEHOLDER - requires verification of MayHaveRobotSession parameter]

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

### -MayHaveUnattendedSession
{{ Fill MayHaveUnattendedSession Description }}

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

### -MayHaveUserSession
{{ Fill MayHaveUserSession Description }}

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
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

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

### -RestrictToPersonalWorkspace
{{ Fill RestrictToPersonalWorkspace Description }}

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

### -Roles
{{ Fill Roles Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: TenantRoles

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UR_CredentialStore
{{ Fill UR_CredentialStore Description }}

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

### -UR_CredentialType
{{ Fill UR_CredentialType Description }}

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

### -UR_LimitConcurrentExecution
{{ Fill UR_LimitConcurrentExecution Description }}

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

### -UR_Password
{{ Fill UR_Password Description }}

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

### -UR_UserName
{{ Fill UR_UserName Description }}

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

### -UpdatePolicyType
{{ Fill UpdatePolicyType Description }}

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

### -UpdatePolicyVersion
{{ Fill UpdatePolicyVersion Description }}

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

### -UserName
更新するユーザーのユーザー名を指定します。

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

### -ES_AutoDownloadProcess
{{ Fill ES_AutoDownloadProcess Description }}

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

### -ES_FontSmoothing
{{ Fill ES_FontSmoothing Description }}

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

### -ES_LoginToConsole
{{ Fill ES_LoginToConsole Description }}

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

### -ES_ResolutionDepth
{{ Fill ES_ResolutionDepth Description }}

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

### -ES_ResolutionHeight
{{ Fill ES_ResolutionHeight Description }}

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

### -ES_ResolutionWidth
{{ Fill ES_ResolutionWidth Description }}

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

### -ES_StudioNotifyServer
{{ Fill ES_StudioNotifyServer Description }}

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

### -ES_TracingLevel
{{ Fill ES_TracingLevel Description }}

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

### -UR_CredentialExternalName
{{ Fill UR_CredentialExternalName Description }}

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

### -IsExternalLicensed
{{ Fill IsExternalLicensed Description }}

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
ユーザーの新しい名前（名）を指定します。このパラメーターは MSI Orchestrator でのみ有効であることに注意してください。

```yaml
Type: String
Parameter Sets: (All)
Aliases: FirstName

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Surname
ユーザーの新しい姓（姓）を指定します。このパラメーターは MSI Orchestrator でのみ有効であることに注意してください。

```yaml
Type: String
Parameter Sets: (All)
Aliases: LastName

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES

## RELATED LINKS
