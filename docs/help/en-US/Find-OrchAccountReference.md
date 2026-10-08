---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Find-OrchAccountReference.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/08/2026
PlatyPS schema version: 2024-05-01
title: Find-OrchAccountReference
---

# Find-OrchAccountReference

## SYNOPSIS

Finds where a Windows account is used in Orchestrator: users, robots, credential assets and triggers.

## SYNTAX

### __AllParameterSets

```
Find-OrchAccountReference [-Account] <string[]> [-Path <string[]>] [-LiteralPath <string[]>]
 [-Recurse] [-Depth <uint>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Find-OrchAccountReference` cmdlet lists every place in Orchestrator that holds a Windows account, so that you know what to update before the account's password changes or the account is retired. You do not have to know which entities can hold an account: the cmdlet looks at all of them.

Once per drive, it looks at the tenant entities:

- **User, UserName**: the account a user signs in with (`DOMAIN\name` for a directory user).
- **User, UnattendedRobot.UserName**: the account the user's unattended robot runs as. Its password is stored in the credential store shown.
- **User, RobotProvision.UserName**: the account of the user's attended robot.
- **Robot, Username**: a robot that does not belong to a user, as in classic folders. A modern robot shows its user's account and is reported on the user row only.

In each target folder, it looks at the assets and triggers:

- **Asset, CredentialUsername**: the global value of a credential asset.
- **Asset, UserValues.CredentialUsername**: a per-user (per-robot) value of a credential asset. **Via** names the user or machine the value belongs to.
- **Asset, UserValues.UserName**: a per-user value of any type that belongs to a user whose account matched. The value does not hold the account, but the robot reads it while running as that account.
- **Trigger, MachineRobots** or **ExecutorRobots**: a trigger that runs on the robot of a matched user, or on a matched robot. **Via** names the robot.

A value without `\` or `@` is compared with the name part of each account only, so `svc_rpa` finds `CORP\svc_rpa`, `svc_rpa@corp.example.com` and `svc_rpa`. A value with `\` or `@` must match the whole account. Both forms accept wildcards, and the comparison ignores case.

**CredentialStore** is filled on the rows that hold a password. `Orchestrator Database` means the password is stored in Orchestrator and has to be entered there again; another name is an external vault, where the password is changed instead. Without permission to read credential stores, the store's id is shown in place of its name.

The cmdlet only reads. It does not look inside processes, where an account may be written in a workflow, or outside Orchestrator.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Users, GET /odata/Robots, GET /odata/CredentialStores, GET /odata/Assets, GET /odata/ProcessSchedules

OAuth required scopes: OR.Users or OR.Users.Read, OR.Robots or OR.Robots.Read, OR.Assets or OR.Assets.Read, OR.Jobs or OR.Jobs.Read, and optionally OR.Settings or OR.Settings.Read (credential store names)

Required permissions: Users.View, Robots.View, Assets.View, Schedules.View, and optionally Settings.View

## EXAMPLES

### Example 1: Find every use of an account in the tenant

```powershell
PS C:\> Find-OrchAccountReference svc_rpa -Path Orch1:\ -Recurse

Type    Path                      Property                      Account                  Via                       CredentialStore
----    ----                      --------                      -------                  ---                       ---------------
User    Orch1:\robot1             UnattendedRobot.UserName      CORP\svc_rpa                                       Orchestrator Database
Asset   Orch1:\Finance\MailLogin  CredentialUsername            svc_rpa@corp.example.com                           Orchestrator Database
Asset   Orch1:\Finance\SapLogin   UserValues.CredentialUsername CORP\svc_rpa             robot1                    Orchestrator Database
Asset   Orch1:\Finance\OutputDir  UserValues.UserName           CORP\svc_rpa             robot1
Trigger Orch1:\Finance\Daily      MachineRobots                 CORP\svc_rpa             robot1-unattended
```

Lists the user `robot1`, whose unattended robot runs as `CORP\svc_rpa`; the credential assets that store the account, globally and as robot1's own value; another per-user value robot1 reads; and the trigger that runs on robot1's robot.

### Example 2: Only the passwords to re-enter in Orchestrator

```powershell
PS C:\> Find-OrchAccountReference 'CORP\svc_rpa' -Path Orch1:\ -Recurse | Where-Object CredentialStore -eq 'Orchestrator Database'
```

Keeps the rows whose password is stored in Orchestrator's own database.

### Example 3: Several accounts, exported

```powershell
PS C:\> Find-OrchAccountReference svc_*, 'CORP\batch01' -Path Orch1:\, Orch2:\ -Recurse | Export-Csv accounts.csv
```

Searches two tenants for every account whose name starts with `svc_` and for `CORP\batch01`, and saves the result.

## PARAMETERS

### -Account

Specifies the accounts to find. A value without `\` or `@` is compared with the name part of each account; a value with one of them is compared with the whole account. Wildcards are accepted. Tab completion suggests the accounts the target drive's users and robots hold.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the target folder. If not specified, the current folder is targeted. The users and robots of the folder's drive are searched once, whatever the folder.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Recurse

Includes the target folder and all its subfolders in the operation.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Depth

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe string arrays to the **Account**, **Path** and **LiteralPath** parameters by property name.

## OUTPUTS

### UiPath.PowerShell.Commands.AccountReference

One object per place the account appears, with **Path**, **Type** (User, Robot, Asset or Trigger), **Name**, **Property**, **Account**, **Via** and **CredentialStore**.

## NOTES

Tenant rows come first, then each folder's asset and trigger rows. A user matched by its sign-in is followed into its per-user asset values and the triggers on its robot, even when the robot runs as another account.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Get-OrchRobot](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRobot.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)
