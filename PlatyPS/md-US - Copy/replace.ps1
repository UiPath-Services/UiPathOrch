#

#
#

$folderPath = "."
$searchText = "Specifies the source drive name."
$replaceText = "Specifies the source drive name. If not specified, the current drive will be used as the source."

Get-ChildItem -Path $folderPath -Filter *.md -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace $searchText, $replaceText | Set-Content $_.FullName
}

