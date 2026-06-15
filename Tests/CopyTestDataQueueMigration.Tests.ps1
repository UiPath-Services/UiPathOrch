#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-OrchTestDataQueue migrates a queue whose items violate its (required) schema
    by relaxing the schema's top-level `required` for the item upload and restoring the
    original schema afterwards, so the destination ends up identical to the source.

.DESCRIPTION
    Sets up a source queue in the realistic "schema tightened after items existed"
    state — a `required` schema plus an item that omits a required field — then copies
    it to another folder and asserts the item was copied (proving `required` was relaxed
    for the upload) AND the `required` schema was restored (proving full-fidelity copy).

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE (Automation Cloud drive, ApiVersion >= 18). Defaults to 'Orch2'.
    - The test creates its own two scratch folders, so the tenant need not already
      have any non-personal folders.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyTestDataQueueMigration.Tests.ps1 -Output Detailed
    Creates/deletes a temp queue in two folders; self-cleaning.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DriveColon = "$($script:Drive):"
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    # Two own scratch folders so the test is self-sufficient and does not depend on
    # the tenant already having >= 2 non-personal folders at the root.
    $suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $script:Src = "$($script:Drive):\ZZ_MigTDQ_src_$suffix"
    $script:Dst = "$($script:Drive):\ZZ_MigTDQ_dst_$suffix"
    New-Item -Path $script:Src -ItemType Directory -ErrorAction Stop | Out-Null
    New-Item -Path $script:Dst -ItemType Directory -ErrorAction Stop | Out-Null
    $script:Qn = 'zzPesterMigTDQ'
    $script:Req = '{"type":"object","properties":{"a":{"type":"string"},"b":{"type":"string"}},"required":["a","b"]}'
    $script:Relax = '{"type":"object","properties":{"a":{"type":"string"},"b":{"type":"string"}},"required":[]}'

    function Remove-TestQueue {
        foreach ($p in $script:Src, $script:Dst) {
            Get-OrchTestDataQueue -Path $p | Where-Object Name -eq $script:Qn |
                Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
    Remove-TestQueue

    # Source: a required schema with a non-conforming item. New-OrchTestDataQueue with
    # the required schema would reject that item, so create it relaxed, add the item,
    # then tighten the schema back via the raw API. Clear the cache so the cmdlets read
    # the true (tightened) source schema.
    New-OrchTestDataQueue -Path $script:Src -Name $script:Qn -ContentJsonSchema $script:Relax | Out-Null
    $id = (Get-OrchTestDataQueue -Path $script:Src | Where-Object Name -eq $script:Qn).Id
    Invoke-OrchApi -Path $script:Src -ApiPath '/api/TestDataQueueActions/AddItem' -Method POST `
        -Body ('{"queueName":"' + $script:Qn + '","content":{"a":"x"}}') | Out-Null
    Invoke-OrchApi -Path $script:Src -ApiPath "/odata/TestDataQueues($id)" -Method PUT `
        -Body ('{"Name":"' + $script:Qn + '","Description":"","ContentJsonSchema":' + ($script:Req | ConvertTo-Json) + '}') | Out-Null
    Clear-OrchCache $script:DriveColon | Out-Null
}

Describe 'Copy-OrchTestDataQueue (relax required for upload, restore after)' {
    It 'source has a required schema and a non-conforming item' {
        $src = Get-OrchTestDataQueue -Path $script:Src | Where-Object Name -eq $script:Qn
        $src.ContentJsonSchema | Should -Match '"required"\s*:\s*\[\s*"a"'
        @(Get-OrchTestDataQueueItem -Path $script:Src -Name $script:Qn).Count | Should -Be 1
    }

    It 'copies the non-conforming item and restores the required schema' {
        Copy-OrchTestDataQueue $script:Qn $script:Dst -Path $script:Src -Confirm:$false
        Clear-OrchCache $script:DriveColon | Out-Null

        $dst = Get-OrchTestDataQueue -Path $script:Dst | Where-Object Name -eq $script:Qn
        $dst | Should -Not -BeNullOrEmpty
        # Item copied despite violating required -> required was relaxed for the upload.
        @(Get-OrchTestDataQueueItem -Path $script:Dst -Name $script:Qn).Count | Should -Be 1
        # Required restored to match the source -> full-fidelity copy.
        $dst.ContentJsonSchema | Should -Match '"required"\s*:\s*\[\s*"a"'
    }
}

AfterAll {
    foreach ($p in $script:Src, $script:Dst) {
        Get-OrchTestDataQueue -Path $p | Where-Object Name -eq $script:Qn |
            Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item -Path $p -Recurse -Force -ErrorAction SilentlyContinue
    }
}
