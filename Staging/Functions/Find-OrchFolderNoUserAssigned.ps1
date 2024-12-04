<#
.SYNOPSIS
This script lists folders that have no direct user assignments.

.DESCRIPTION
This script recursively checks all folders under a specified path and lists folders where no users are assigned. 
Users inherited from parent folders are not considered by default, but you can include them using the -IncludeInherited switch.

.PARAMETER Path
Specifies the path to the folder where the script should start the recursive search. The default value is "Orch1:\".

.PARAMETER IncludeInherited
When this switch is used, the script will consider users inherited from parent folders as part of the user assignments. 
Without this switch, only direct user assignments will be checked.

.NOTES
By default, users inherited from parent folders are ignored in this script. If the -IncludeInherited switch is provided, inherited users will be included in the check.

.EXAMPLE
PS> .\Find-FoldersNoUserAssigned.ps1 Orch1:\
This will recursively check all folders under "Orch1:\" and list those without direct user assignments.

.EXAMPLE
PS> .\Find-FoldersNoUserAssigned.ps1 Orch1:\ -IncludeInherited
This will recursively check all folders under "Orch1:\" and list those without any user assignments. Folders with inherited user assignments from parent folders will not be included, even if they have no direct user assignments.
#>

function Find-OrchFolderNoUserAssigned {
    [OutputType([UiPath.PowerShell.Entities.Folder])]
    param(
        [string]$Path,
        [switch]$IncludeInherited
    )

    dir $Path -Recurse | % {
        if (-not (Get-OrchFolderUser -Path $_.FullName -IncludeInherited:$IncludeInherited.IsPresent)) {
            $_
        }
    }
}
