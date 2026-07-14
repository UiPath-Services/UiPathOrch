function Disable-OrchPersonalWorkspace {
    [CmdletBinding(SupportsShouldProcess = $true)]
    Param (
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [ArgumentCompleter([UiPath.PowerShell.Completer.TenantUserUserNameCompleter])]
        [SupportsWildcards()]
        [string[]]$UserName,

        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [ArgumentCompleter([UiPath.PowerShell.Completer.DriveCompleter])]
        [string[]]$Path
    )

    process {
        # .EXTERNALHELP UiPathOrch-Help.xml
        foreach ($user in $UserName) {
            Update-OrchUser @PSBoundParameters -Path $Path -UserName $user -MayHavePersonalWorkspace False
        }
    }

    end {
        if ($PSBoundParameters.ContainsKey('Path')) {
            Clear-OrchCache -Path $Path
        } else {
            Clear-OrchCache
        }
    }
}
