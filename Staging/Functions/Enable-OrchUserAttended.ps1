function Enable-OrchUserAttended {
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
            Update-OrchUser -Path $Path -UserName $user -MayHaveRobotSession True
        }
    }
}
