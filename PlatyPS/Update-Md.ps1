$parameters = @{
    Path = "./md-US"
    RefreshModulePage = $true
    AlphabeticParamsOrder = $false
    UpdateInputOutput = $true
    ExcludeDontShow = $true
    LogPath = "./log.txt"
    Encoding = [System.Text.Encoding]::UTF8
}
Update-MarkdownHelpModule @parameters


$parametersJ = @{
    Path = "./md-JP"
    RefreshModulePage = $true
    AlphabeticParamsOrder = $false
    UpdateInputOutput = $true
    ExcludeDontShow = $true
    LogPath = "./logJ.txt"
    Encoding = [System.Text.Encoding]::UTF8
}
Update-MarkdownHelpModule @parametersJ

