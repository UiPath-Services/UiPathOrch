function Format-OrchTestDataQueueItem {
    <#
    .EXTERNALHELP UiPathOrch-Help.xml

    .SYNOPSIS
        Formats Get-OrchTestDataQueueItem output as one table per test data queue, flattening ContentJson keys into columns.

    .DESCRIPTION
        Groups piped test data queue items by their containing queue and renders one Format-Table
        per queue. ContentJson (stored as a JSON string on each item) is parsed, and each top-level
        property becomes a column, preceded by Id and IsConsumed.

        Because Format-Table locks its column set on the first object seen, rendering items from
        multiple test data queues with a single Format-Table silently hides keys that aren't
        present in the first queue's schema. Grouping by queue avoids that problem.

    .EXAMPLE
        Get-OrchTestDataQueueItem | Format-OrchTestDataQueueItem

    .EXAMPLE
        Get-OrchTestDataQueueItem -Path Orch1:\root -Recurse | Format-OrchTestDataQueueItem
    #>
    [CmdletBinding()]
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [object]$InputObject
    )

    Begin {
        $groups = [ordered]@{}
    }

    Process {
        if ($null -eq $InputObject) { return }
        # Prefer the provider PSPath surface (Path) for a human-friendly group key.
        # Falls back to TestDataQueueId if Path is not populated.
        $key = if ($InputObject.Path) { $InputObject.Path } else { $InputObject.TestDataQueueId }
        if (-not $groups.Contains($key)) {
            $groups[$key] = [System.Collections.Generic.List[object]]::new()
        }

        $row = [ordered]@{
            Id         = $InputObject.Id
            IsConsumed = $InputObject.IsConsumed
        }
        if ($InputObject.ContentJson) {
            try {
                $json = $InputObject.ContentJson | ConvertFrom-Json -ErrorAction Stop
                foreach ($p in $json.PSObject.Properties) {
                    $row[$p.Name] = $p.Value
                }
            } catch {
                # Malformed JSON — surface it as a single column rather than failing the whole run.
                $row['ContentJson'] = $InputObject.ContentJson
            }
        }
        $groups[$key].Add([pscustomobject]$row)
    }

    End {
        foreach ($kv in $groups.GetEnumerator()) {
            $kv.Value | Format-Table -AutoSize
        }
    }
}
