# Live regression test for the OData single-quote escaping fix (the "O'Brien
# problem"). Requires a live Orch1: connection (like the other Pester tests here).
#
# A name containing a single quote interpolated raw into an OData string literal
# produces a malformed filter -- e.g. Name eq 'O'Brien' -- which the server
# rejects with 400. OData escapes a single quote by doubling it, and
# PathTools.EscapeODataLiteral does exactly that, so the server accepts the
# filter (200). /odata/Machines is a tenant-level collection, so no folder
# context is needed.

Describe "OData single-quote escaping (O'Brien problem)" {

    BeforeAll {
        Import-Module UiPathOrch -Force
        Import-OrchConfig 3>$null | Out-Null
    }

    It "PathTools.EscapeODataLiteral doubles single quotes and adds no quotes" {
        [UiPath.PowerShell.Core.PathTools]::EscapeODataLiteral("O'Brien") | Should -BeExactly "O''Brien"
        [UiPath.PowerShell.Core.PathTools]::EscapeODataLiteral("a'b'c")   | Should -BeExactly "a''b''c"
        [UiPath.PowerShell.Core.PathTools]::EscapeODataLiteral("plain")   | Should -BeExactly "plain"
    }

    It "the server rejects an unescaped single quote (400) and accepts the doubled one (200)" {
        $rawStatus = $null
        Invoke-OrchApi -Path Orch1: -ApiPath "/odata/Machines?`$filter=Name eq 'O'Brien'" `
            -Method Get -SkipHttpErrorCheck -StatusCodeVariable rawStatus *> $null
        $rawStatus | Should -Be 400

        $esc = [UiPath.PowerShell.Core.PathTools]::EscapeODataLiteral("O'Brien")
        $escStatus = $null
        Invoke-OrchApi -Path Orch1: -ApiPath "/odata/Machines?`$filter=Name eq '$esc'" `
            -Method Get -SkipHttpErrorCheck -StatusCodeVariable escStatus *> $null
        $escStatus | Should -Be 200
    }
}
