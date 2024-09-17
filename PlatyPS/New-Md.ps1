$OutputFolder = "new"
$parameters = @{
    Module = "UiPathOrch"
    OutputFolder = $OutputFolder
    AlphabeticParamsOrder = $false
    WithModulePage = $true
    ExcludeDontShow = $true
    Encoding = [System.Text.Encoding]::UTF8
}
New-MarkdownHelp @parameters

New-MarkdownAboutHelp -OutputFolder $OutputFolder -AboutName "UiPathOrch"
