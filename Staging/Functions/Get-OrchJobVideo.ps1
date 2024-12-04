function Get-OrchJobVideo {
    [CmdletBinding()]
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [string[]]$Path,

        [Switch]$Recurse,

        [ulong]$Skip = 0,

        [ulong]$First = [ulong]::MaxValue
    )

    Process {
        Get-OrchJob -Path $Path -ProcessType Process -Last Week -Skip $Skip -First $First -Recurse:$Recurse | 
            Where-Object { $_.HasVideoRecorded -eq $true }
    }
}
