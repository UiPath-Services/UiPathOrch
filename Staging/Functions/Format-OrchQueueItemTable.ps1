function Format-OrchQueueItemTable {
    <#
    .SYNOPSIS
        Formats Get-OrchQueueItem output as one table per queue, flattening SpecificContent keys into columns.

    .DESCRIPTION
        Groups piped queue items by QueueDefinitionId and renders one Format-Table per queue.
        Each row uses the QueueItem's Expanded property: leading columns for the standard fields
        (Id, Reference, Status, Priority, DeferDate, DueDate, StartProcessing, EndProcessing)
        followed by the keys of SpecificContent.

        Because Format-Table locks its column set on the first object seen, rendering items from
        multiple queues with a single Format-Table silently hides keys that aren't present in the
        first queue's schema. Grouping first avoids that problem.

    .EXAMPLE
        Get-OrchQueueItem -Name OrderQueue -First 20 | Format-OrchQueueItemTable

    .EXAMPLE
        Get-OrchQueueItem -Name 'Order*' -Status New | Format-OrchQueueItemTable
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
        $key = $InputObject.QueueDefinitionId
        if (-not $groups.Contains($key)) {
            $groups[$key] = [System.Collections.Generic.List[object]]::new()
        }
        $groups[$key].Add($InputObject.Expanded)
    }

    End {
        foreach ($kv in $groups.GetEnumerator()) {
            $kv.Value | Format-Table -AutoSize
        }
    }
}
