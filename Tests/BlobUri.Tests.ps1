#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    BlobUriHelper.SamePhysicalObject compares two pre-signed blob URIs (a read URI vs a write URI) and
    is true only when they point to the same physical storage object -- same scheme/host/port and
    decoded path, with the query (signature, expiry, verb-specific params) ignored. Copy-OrchBucketItem
    uses it to skip copying a file onto itself when the source and destination buckets resolve to the
    same external storage object.

.NOTES
    Pure unit test -- no Orchestrator tenant required.
    Run with: Invoke-Pester -Path Tests\BlobUri.Tests.ps1 -Output Detailed
#>

Describe 'BlobUriHelper.SamePhysicalObject' {
    It 'is TRUE for the same object with different signatures (a GET vs a PUT pre-sign)' {
        $get = 'https://my-bucket.s3.us-east-1.amazonaws.com/folder/file.txt?X-Amz-Signature=aaa&X-Amz-Expires=3600'
        $put = 'https://my-bucket.s3.us-east-1.amazonaws.com/folder/file.txt?X-Amz-Signature=zzz&X-Amz-Date=20260624'
        [BlobUriHelper]::SamePhysicalObject($get, $put) | Should -BeTrue
    }

    It 'is TRUE for an Azure blob with vs without an explicit :443 port' {
        $a = 'https://acct.blob.core.windows.net/container/path/blob.bin?sv=2021&sig=aaa'
        $b = 'https://acct.blob.core.windows.net:443/container/path/blob.bin?sv=2021&sig=bbb'
        [BlobUriHelper]::SamePhysicalObject($a, $b) | Should -BeTrue
    }

    It 'is TRUE despite host-name casing differences' {
        [BlobUriHelper]::SamePhysicalObject(
            'https://My-Bucket.s3.amazonaws.com/key',
            'https://my-bucket.S3.amazonaws.com/key') | Should -BeTrue
    }

    It 'is TRUE despite equivalent percent-encoding in the path (%41 == A)' {
        [BlobUriHelper]::SamePhysicalObject(
            'https://h/folder/%41/file.txt?x=1',
            'https://h/folder/A/file.txt?x=2') | Should -BeTrue
    }

    It 'is FALSE for a different object key' {
        [BlobUriHelper]::SamePhysicalObject(
            'https://b.s3.amazonaws.com/folder/file.txt?sig=a',
            'https://b.s3.amazonaws.com/folder/other.txt?sig=b') | Should -BeFalse
    }

    It 'is FALSE for a different host (a different external bucket)' {
        [BlobUriHelper]::SamePhysicalObject(
            'https://bucket-a.s3.amazonaws.com/key?sig=a',
            'https://bucket-b.s3.amazonaws.com/key?sig=b') | Should -BeFalse
    }

    It 'is FALSE when the object key case differs (keys are case-sensitive)' {
        [BlobUriHelper]::SamePhysicalObject(
            'https://h/Folder/File.txt',
            'https://h/folder/file.txt') | Should -BeFalse
    }

    It 'is FALSE for a different scheme' {
        [BlobUriHelper]::SamePhysicalObject('http://h/key', 'https://h/key') | Should -BeFalse
    }

    It 'is FALSE when either URI is null, empty, or not absolute' -ForEach @(
        @{ A = $null;            B = 'https://h/k' }
        @{ A = '';               B = 'https://h/k' }
        @{ A = 'not-a-uri';      B = 'https://h/k' }
        @{ A = '/relative/path'; B = 'https://h/k' }
    ) {
        [BlobUriHelper]::SamePhysicalObject($A, $B) | Should -BeFalse
    }
}
