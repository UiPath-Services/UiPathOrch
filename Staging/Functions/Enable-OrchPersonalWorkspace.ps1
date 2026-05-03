function Enable-OrchPersonalWorkspace {
    [CmdletBinding(SupportsShouldProcess = $true)]
    Param (
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [SupportsWildcards()]
        [ArgumentCompleter([UiPath.PowerShell.Completer.TenantUserUserNameCompleter[UiPath.PowerShell.Positional.UserName]])]
        [SupportsWildcards()]
        [string[]]$UserName,

        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [ArgumentCompleter([UiPath.PowerShell.Completer.DriveCompleter[UiPath.PowerShell.Positional.UserName]])]
        [string[]]$Path
    )

    # .EXTERNALHELP UiPathOrch-Help.xml

    foreach ($user in $UserName) {
        Update-OrchUser @PSBoundParameters -Path $Path -UserName $user -MayHavePersonalWorkspace True -MayHaveRobotSession True
    }

    if ($PSBoundParameters.ContainsKey('Path')) {
        Clear-OrchCache -Path $Path
    } else {
        Clear-OrchCache
    }
}
