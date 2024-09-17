$folderPath = "."
$searchText = "コピー元のドライブの名前を指定します。"
$replaceText = "コピー元のドライブの名前を指定します。指定しない場合は、現在のドライブをコピー元とします。"

Get-ChildItem -Path $folderPath -Filter *.md -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace $searchText, $replaceText | Set-Content $_.FullName
}

