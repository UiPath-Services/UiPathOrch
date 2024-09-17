New-ExternalHelp md-US -OutputPath xml-US -Force
copy xml-US\* "C:\Program Files\PowerShell\7\Modules\UiPathOrch\en-US"
copy xml-US\* "C:\MyProj\OrchProvider\OrchProvider\bin\Debug\net8.0\en-US"

New-ExternalHelp md-JP -OutputPath xml-JP -Force
copy xml-JP\* "C:\Program Files\PowerShell\7\Modules\UiPathOrch\ja-JP"
copy xml-JP\* "C:\MyProj\OrchProvider\OrchProvider\bin\Debug\net8.0\ja-JP"
