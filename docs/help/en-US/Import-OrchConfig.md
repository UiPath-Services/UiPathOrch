---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/08/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchConfig
---

# Import-OrchConfig

## SYNOPSIS

Imports the UiPathOrch configuration file and creates PSDrives.

## SYNTAX

### __AllParameterSets

```
Import-OrchConfig [[-ConfigPath] <String>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Import-OrchConfig` cmdlet reads the UiPathOrchConfig.json configuration file and creates PSDrives for all enabled Orchestrator tenants. All existing UiPathOrch drives (including Document Understanding and Test Manager drives) are removed and re-created. Cached data in the existing drives is discarded.

The configuration file is always re-read and the drives are always re-created, every time the cmdlet runs. Re-creating the drives also clears their cached sign-ins, so each drive re-authenticates the next time it is used: interactive (Non-Confidential App) drives prompt to sign in, while Confidential App, Personal Access Token, and username/password drives re-authenticate without prompting. This makes `Import-OrchConfig` the way to pick up a fresh sign-in — for example, after signing in to the organization's directory in the browser so a local-user drive can switch to a directory account.

Use this cmdlet to apply configuration changes after editing UiPathOrchConfig.json with `Edit-OrchConfig`, without restarting the PowerShell session.

This cmdlet can also be used for initial setup: after the configuration file is created with `Edit-OrchConfig`, run `Import-OrchConfig` to create the drives.

By default the configuration file is read from the location reported by `Get-OrchConfigPath`. `-ConfigPath` loads a different file — for example one on a network share that several machines are pointed at — and makes it the configuration file for the rest of the session by setting the `UIPATHORCH_CONFIG_PATH` environment variable in the current process. Nothing is written to the persistent (user or machine) environment.

Both the parameter and the environment variable accept either the file or the folder holding it. Whichever form is given, the resolved full file path is what gets stored, so `Get-OrchConfigPath` always reports an unambiguous path.

For a standing setup, set `UIPATHORCH_CONFIG_PATH` itself rather than using `-ConfigPath`: put it in `$PROFILE`, or define it as a user or system environment variable. The module is loaded automatically by the first UiPathOrch command in a session, and it mounts drives from whatever file is in effect at that moment — which is before `-ConfigPath` on that same command line has had a chance to run. Setting the variable first means the intended file is used from the start, and avoids mounting the default file's drives only to replace them.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Import configuration and create drives

```powershell
PS C:\> Import-OrchConfig
```

Reads UiPathOrchConfig.json and creates PSDrives for all enabled tenants.

### Example 2: Apply configuration changes

```powershell
PS C:\> Edit-OrchConfig
PS C:\> Import-OrchConfig
```

Opens the configuration file for editing, then reloads it to apply the changes.

### Example 3: Switch a local-user drive to a directory account

```powershell
PS C:\> # 1. In the browser: sign out, then sign in at the organization URL (e.g. https://cloud.uipath.com/<org>)
PS C:\> # 2. Back in PowerShell:
PS C:\> Import-OrchConfig
```

Clears the cached sign-ins and re-creates the drives. The next use of an interactive drive re-runs the browser sign-in, which now picks up the directory account established in step 1.

### Example 4: Preview with -WhatIf

```powershell
PS C:\> Import-OrchConfig -WhatIf
```

Shows the configuration file path that would be loaded without actually removing or creating any drives. The configuration file is still read and validated, but neither the drives nor `UIPATHORCH_CONFIG_PATH` are changed.

### Example 5: Load a configuration file from a network share

```powershell
PS C:\> Import-OrchConfig -ConfigPath \\fileserver\rpa\UiPathOrchConfig.json
PS C:\> Get-OrchConfigPath
\\fileserver\rpa\UiPathOrchConfig.json
```

Loads the shared configuration file and makes it the configuration file for the rest of the session. A folder is accepted as well: `-ConfigPath \\fileserver\rpa` looks for `UiPathOrchConfig.json` inside it.

### Example 6: Standing setup for several machines

```powershell
PS C:\> # In $PROFILE, before any UiPathOrch command runs:
PS C:\> $env:UIPATHORCH_CONFIG_PATH = '\\fileserver\rpa\UiPathOrchConfig.json'
```

Every session then mounts the shared configuration from the start, with no `Import-OrchConfig` call needed. A UNC path is recommended over a mapped drive letter, which is not visible to services, scheduled tasks, or another logon session.

### Example 7: Return to the default configuration file

```powershell
PS C:\> Remove-Item Env:UIPATHORCH_CONFIG_PATH
PS C:\> Import-OrchConfig
```

Clears the override and reloads from the default per-user location.

## PARAMETERS

### -ConfigPath

Configuration file to load instead of the one currently in effect. A folder is accepted as well as a file; in that case `UiPathOrchConfig.json` inside the folder is used. The two are told apart by trying, not by the spelling: the path is read as a file first, and as a folder only if no file is there.

The path must resolve to a file-system path. A relative path is resolved against the current location, so specify a full path when the current location is on an Orchestrator drive. A UNC path is recommended for a shared file.

On success the resolved full path is stored in the `UIPATHORCH_CONFIG_PATH` environment variable of the current process, which makes it the configuration file for the rest of the session and for any child process started from it. Nothing is written to the persistent environment. If the file cannot be read or does not parse, the session is left unchanged.

Note that the file holds credentials in plain text (`AppSecret`, `Password`, personal access tokens). A file shared by several people should contain only drives that authenticate interactively, so that no secret is shared along with it.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### None

This cmdlet does not produce output. Use `-Verbose` to see the number of drives created.

## NOTES

This cmdlet removes all existing UiPathOrch, UiPathOrchDu, and UiPathOrchTm drives before creating new ones. Any cached data is discarded. Run `Get-OrchPSDrive` after importing to verify the drive configuration.

When `UIPATHORCH_CONFIG_PATH` is in effect and the file cannot be read — an unreachable network share, for instance — no drives are mounted and the default configuration file is deliberately **not** used as a fallback, so that a temporarily offline share cannot silently connect a different set of tenants. Reads of an overridden location are also given a bounded wait (10 seconds) rather than blocking for the full network timeout.

## RELATED LINKS

[Edit-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Edit-OrchConfig.md)

[Get-OrchConfigPath](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchConfigPath.md)

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)

[New-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchPSDrive.md)
