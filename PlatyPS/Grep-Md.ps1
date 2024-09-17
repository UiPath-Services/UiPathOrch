param (
    [Parameter(Position = 0)]
    [string]$SearchText
)

function Search-TextInCurrentFolder {
    param (
        [string]$SearchText
    )
    Get-ChildItem -Path . -Recurse -Filter *.md -File | Select-String -Pattern $SearchText
}

# 指定されたテキストで現在のフォルダーを検索し、結果を表示
Search-TextInCurrentFolder -SearchText $SearchText
