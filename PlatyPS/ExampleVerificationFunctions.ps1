# Example検証記録管理関数

function Add-ExampleVerificationRecord {
    param(
        [Parameter(Mandatory)]
        [string]$FileName,
        
        [Parameter(Mandatory)]
        [string]$EntityType,
        
        [Parameter(Mandatory)]
        [hashtable[]]$Examples,
        
        [string[]]$ParameterTests = @(),
        
        [string]$Notes = ""
    )
    
    $dashboardPath = "品質チェック_進捗ダッシュボード.txt"
    $timestamp = Get-Date -Format "MM/dd HH:mm"
    
    # 検証記録テキストを生成
    $verificationRecord = @"

### ✅ $FileName [$EntityType] - 検証完了 ($timestamp)
"@
    
    # 各Exampleの記録を追加
    foreach ($example in $Examples) {
        $status = if ($example.Status -eq "Success") { "✅" } else { "❌" }
        $command = $example.Command
        $comment = if ($example.Comment) { " → $($example.Comment)" } else { "" }
        
        $verificationRecord += "`n**$($example.Name)**: $status ``$command``$comment"
    }
    
    # パラメータテスト記録
    if ($ParameterTests.Count -gt 0) {
        $parameterList = $ParameterTests -join ", "
        $verificationRecord += "`n**パラメータ検証**: ✅ $parameterList → 検証済み"
    }
    
    # 特記事項
    if ($Notes) {
        $verificationRecord += "`n**特記事項**: $Notes"
    }
    
    # ダッシュボードに追加
    $content = Get-Content $dashboardPath -Encoding UTF8
    $sectionIndex = ($content | Select-String -Pattern "## 🔍 Example実行検証記録" | Select-Object -First 1).LineNumber + 2
    
    $newContent = $content[0..$sectionIndex] + $verificationRecord.Split("`n") + $content[($sectionIndex+1)..($content.Length-1)]
    
    $newContent | Out-File $dashboardPath -Encoding UTF8
    Write-Host "✅ $FileName の検証記録を追加しました" -ForegroundColor Green
}

function New-ExampleTest {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        
        [Parameter(Mandatory)]
        [string]$Command,
        
        [Parameter(Mandatory)]
        [ValidateSet("Success", "Failed", "Modified")]
        [string]$Status,
        
        [string]$Comment = ""
    )
    
    return @{
        Name = $Name
        Command = $Command
        Status = $Status
        Comment = $Comment
    }
}

function Add-SimpleVerificationRecord {
    param(
        [Parameter(Mandatory)]
        [string]$FileName,
        
        [Parameter(Mandatory)]
        [string]$EntityType,
        
        [Parameter(Mandatory)]
        [string[]]$SuccessfulExamples,
        
        [string[]]$FailedExamples = @(),
        
        [string[]]$ParameterTests = @(),
        
        [string]$Notes = ""
    )
    
    $examples = @()
    
    # 成功したExampleを追加
    for ($i = 0; $i -lt $SuccessfulExamples.Count; $i++) {
        $examples += New-ExampleTest -Name "Example $($i+1)" -Command $SuccessfulExamples[$i] -Status "Success" -Comment "動作確認済み"
    }
    
    # 失敗したExampleを追加
    foreach ($failed in $FailedExamples) {
        $examples += New-ExampleTest -Name "Example (修正前)" -Command $failed -Status "Failed" -Comment "修正済み"
    }
    
    Add-ExampleVerificationRecord -FileName $FileName -EntityType $EntityType -Examples $examples -ParameterTests $ParameterTests -Notes $Notes
}
