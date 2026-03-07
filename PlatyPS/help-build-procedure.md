# ヘルプ ビルド・デプロイ手順書

## 概要

PlatyPS v1 マークダウン (.md) から MAML XML ヘルプを生成し、モジュールディレクトリにデプロイする手順。

## ディレクトリ構成

```
OrchProvider/
├── PlatyPS/
│   ├── v1-US/UiPathOrch/          # マークダウンソース (238 ファイル)
│   ├── Build-Help.ps1             # ビルドスクリプト (一括実行)
│   ├── Reorder-SyntaxParameters.ps1  # パラメータ並び替え
│   └── help-build-procedure.md    # この手順書
├── Staging/en-US/                 # ビルド出力先 (XML)
│   ├── UiPath.PowerShell.OrchProvider.dll-Help.xml  # C# cmdlet ヘルプ
│   └── UiPathOrch-Help.xml        # PS1 関数ヘルプ
```

**デプロイ先**: `C:\Program Files\PowerShell\7\Modules\UiPathOrch\en-US\`

## 前提条件

- PowerShell 7+
- PlatyPS v1 (`Microsoft.PowerShell.PlatyPS`) インストール済み

```powershell
Install-PSResource Microsoft.PowerShell.PlatyPS
```

## ビルドパイプライン

```
[1] マークダウン (.md)
     ↓  Import-MarkdownCommandHelp
[2] MAML XML 生成
     ↓  Export-MamlCommandHelp
[3] パラメータ並び替え
     ↓  Reorder-SyntaxParameters.ps1
[4] Staging/en-US/ に配置
     ↓  Copy-Item
[5] モジュールディレクトリにデプロイ
     ↓  Import-Module -Force
[6] 動作確認
```

## 一括実行 (Build-Help.ps1)

```powershell
cd C:\MyProj\OrchProvider\PlatyPS
.\Build-Help.ps1                    # フルビルド + デプロイ
.\Build-Help.ps1 -SkipDeploy        # ビルドのみ (デプロイしない)
.\Build-Help.ps1 -WhatIf            # ドライラン
```

## 手動実行 (ステップごと)

### Step 1-2: マークダウン → XML 変換

```powershell
Import-Module Microsoft.PowerShell.PlatyPS

# マークダウン読み込み
$help = Import-MarkdownCommandHelp -Path .\v1-US\UiPathOrch

# XML エクスポート (一時ディレクトリに出力)
$help | Export-MamlCommandHelp -OutputFolder ..\Staging\en-US\temp -Force

# サブディレクトリから XML をコピー
Copy-Item ..\Staging\en-US\temp\UiPathOrch\*.xml ..\Staging\en-US\ -Force
Remove-Item ..\Staging\en-US\temp -Recurse -Force
```

**注意**: `Export-MamlCommandHelp` はモジュール名のサブディレクトリ (`UiPathOrch/`) を作成する。XML はそこから親ディレクトリにコピーする必要がある。

### Step 3: パラメータ並び替え

```powershell
.\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPath.PowerShell.OrchProvider.dll-Help.xml
.\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPathOrch-Help.xml
```

`Get-Help` の SYNTAX 表示で `-Path`, `-Recurse`, `-Depth` を先頭に配置する。PlatyPS はアルファベット順で出力するため、このスクリプトで並び替える。冪等（2回実行しても変化なし）。

### Step 4: デプロイ

```powershell
$dest = 'C:\Program Files\PowerShell\7\Modules\UiPathOrch\en-US'
Copy-Item ..\Staging\en-US\UiPath.PowerShell.OrchProvider.dll-Help.xml $dest -Force
Copy-Item ..\Staging\en-US\UiPathOrch-Help.xml $dest -Force
```

### Step 5: モジュール再読み込み

```powershell
Import-Module UiPathOrch -Force
```

### Step 6: 動作確認

```powershell
# 全 cmdlet の Get-Help テスト
$mod = Get-Module UiPathOrch
$cmds = $mod.ExportedCmdlets.Keys + $mod.ExportedFunctions.Keys | Sort-Object
$errors = @()
foreach ($cmd in $cmds) {
    try {
        $h = Get-Help $cmd -ErrorAction Stop
        $exCount = @($h.examples.example).Count
        if ($exCount -eq 0) { $errors += "$cmd : 0 examples" }
    } catch {
        $errors += "$cmd : $_"
    }
}
"Total: $($cmds.Count), Errors: $($errors.Count)"
if ($errors) { $errors }

# パラメータ順序の確認 (Path が先頭にあるか)
(Get-Help Get-OrchAsset).syntax.syntaxItem.parameter.name
```

## 2 つの XML ファイル

| ファイル | 対象 | マークダウンの `external help file` |
|----------|------|-------------------------------------|
| `UiPath.PowerShell.OrchProvider.dll-Help.xml` | C# cmdlet (231 個) | `UiPath.PowerShell.OrchProvider.dll-Help.xml` |
| `UiPathOrch-Help.xml` | PS1 関数 (7 個) | `UiPathOrch-Help.xml` |

PS1 関数 (7 個):
- Enable-OrchPersonalWorkspace / Disable-OrchPersonalWorkspace
- Enable-OrchUserAttended / Disable-OrchUserAttended
- Find-OrchFolderNoUserAssigned
- Get-OrchJobVideo
- Get-OrchTestDataQueueItemTable

## 既知の注意点

### external help file の大文字小文字
マークダウンの `external help file` フィールドが PlatyPS の出力ファイル名を決める。PS1 関数の 7 ファイルは `UiPathOrch-Help.xml` (大文字 H)。Windows NTFS は case-insensitive だが、すべてのファイルで大文字小文字を統一しておくこと。

### RELATED LINKS はプレーンテキスト
`[CmdletName](CmdletName.md)` 形式のリンクは使わない。PS1 関数ヘルプで `GetUriForOnlineHelp()` エラーが発生する。プレーンテキストで cmdlet 名のみ記載する。

### DESCRIPTION に `**` (太字) を使わない
`**text**` を含む段落は Get-Help で表示されない (MAML XML 内の `<maml:para>` が丸ごとスキップされる)。

### Get-Help の SYNTAX パラメータ順序
Get-Help は XML の `<command:syntaxItem>` 内のパラメータ順序をそのまま使う。PlatyPS はアルファベット順で出力するため、`Reorder-SyntaxParameters.ps1` で並び替えが必要。
