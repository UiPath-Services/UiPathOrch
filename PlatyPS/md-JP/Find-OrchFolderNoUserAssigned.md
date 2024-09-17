---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Find-OrchFolderNoUserAssigned

## SYNOPSIS
This script lists folders that have no direct user assignments.

## SYNTAX

```
Find-OrchFolderNoUserAssigned [[-Path] <String>] [-IncludeInherited] [<CommonParameters>]
```

## DESCRIPTION
This script recursively checks all folders under a specified path and lists folders where no users are assigned. 
Users inherited from parent folders are not considered by default, but you can include them using the -IncludeInherited switch.

## EXAMPLES

### EXAMPLE 1
```
.\Find-FoldersNoUserAssigned.ps1 Orch1:\
This will recursively check all folders under "Orch1:\" and list those without direct user assignments.
```

### EXAMPLE 2
```
.\Find-FoldersNoUserAssigned.ps1 Orch1:\ -IncludeInherited
This will recursively check all folders under "Orch1:\" and list those without any user assignments. Folders with inherited user assignments from parent folders will not be included, even if they have no direct user assignments.
```

## PARAMETERS

### -Path
Specifies the path to the folder where the script should start the recursive search.
The default value is "Orch1:\".

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeInherited
When this switch is used, the script will consider users inherited from parent folders as part of the user assignments. 
Without this switch, only direct user assignments will be checked.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.Folder
## NOTES
By default, users inherited from parent folders are ignored in this script.
If the -IncludeInherited switch is provided, inherited users will be included in the check.

## RELATED LINKS
