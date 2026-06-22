# ヘルプ ビルド・デプロイ手順書

## 概要

PlatyPS v1 マークダウン (.md) から MAML XML ヘルプを生成し、モジュールディレクトリにデプロイする手順。

## ディレクトリ構成

```
UiPathOrch/
├── docs/help/en-US/               # マークダウンソース (locale 別、262 ファイル)
│                                  # 将来 ja-JP 等を追加する場合は docs/help/ja-JP/
├── PlatyPS/
│   ├── Build-Help.ps1             # ビルドスクリプト (一括実行)
│   ├── Reorder-SyntaxParameters.ps1  # パラメータ並び替え
│   └── help-build-procedure.md    # この手順書
├── Staging/en-US/                 # ビルド出力先 (XML)
│   ├── UiPathOrch.dll-Help.xml  # C# cmdlet ヘルプ
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
cd C:\MyProj\UiPathOrch\PlatyPS
.\Build-Help.ps1                    # フルビルド + デプロイ
.\Build-Help.ps1 -SkipDeploy        # ビルドのみ (デプロイしない)
.\Build-Help.ps1 -WhatIf            # ドライラン
```

## 手動実行 (ステップごと)

### Step 1-2: マークダウン → XML 変換

```powershell
Import-Module Microsoft.PowerShell.PlatyPS

# マークダウン読み込み
$help = Import-MarkdownCommandHelp -Path ..\docs\help\en-US

# XML エクスポート (一時ディレクトリに出力)
$help | Export-MamlCommandHelp -OutputFolder ..\Staging\en-US\temp -Force

# サブディレクトリから XML をコピー
Copy-Item ..\Staging\en-US\temp\UiPathOrch\*.xml ..\Staging\en-US\ -Force
Remove-Item ..\Staging\en-US\temp -Recurse -Force
```

**注意**: `Export-MamlCommandHelp` はモジュール名のサブディレクトリ (`UiPathOrch/`) を作成する。XML はそこから親ディレクトリにコピーする必要がある。

### Step 3: パラメータ並び替え

```powershell
.\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPathOrch.dll-Help.xml
.\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPathOrch-Help.xml
```

`Get-Help` の SYNTAX 表示で `-Path`, `-Recurse`, `-Depth` を先頭に配置する。PlatyPS はアルファベット順で出力するため、このスクリプトで並び替える。冪等（2回実行しても変化なし）。

### Step 4: デプロイ

```powershell
$dest = 'C:\Program Files\PowerShell\7\Modules\UiPathOrch\en-US'
Copy-Item ..\Staging\en-US\UiPathOrch.dll-Help.xml $dest -Force
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
| `UiPathOrch.dll-Help.xml` | C# cmdlet (312 個) | `UiPathOrch.dll-Help.xml` |
| `UiPathOrch-Help.xml` | PS1 関数 (8 個) | `UiPathOrch-Help.xml` |

PS1 関数 (8 個):
- Enable-OrchPersonalWorkspace / Disable-OrchPersonalWorkspace
- Enable-OrchUserAttended / Disable-OrchUserAttended
- Find-OrchFolderNoUserAssigned
- Format-OrchQueueItem
- Format-OrchTestDataQueueItem
- Get-OrchJobVideo

各 .ps1 には MAML ヘルプを使うために `.EXTERNALHELP UiPathOrch-Help.xml` 指示が必要。詳細は下の注意点を参照。

## 既知の注意点

### external help file の大文字小文字
マークダウンの `external help file` フィールドが PlatyPS の出力ファイル名を決める。PS1 関数の 7 ファイルは `UiPathOrch-Help.xml` (大文字 H)。Windows NTFS は case-insensitive だが、すべてのファイルで大文字小文字を統一しておくこと。

### RELATED LINKS は絶対 URL のリンク
`[CmdletName](CmdletName.md)` のような相対 .md パスは PS1 関数ヘルプで `GetUriForOnlineHelp()` エラーになる。絶対 URL なら PS1 ヘルプでも問題なく、Get-Help 出力で関連 cmdlet からも直接オンライン版へ飛べる。形式: `[CmdletName](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/CmdletName.md)`。本リポジトリに存在しない cmdlet を参照している場合は 404 になるため、そのエントリだけはプレーンテキストのまま残す。

### DESCRIPTION に `**` (太字) を使わない
`**text**` を含む段落は Get-Help で表示されない (MAML XML 内の `<maml:para>` が丸ごとスキップされる)。

### Get-Help の SYNTAX パラメータ順序
Get-Help は XML の `<command:syntaxItem>` 内のパラメータ順序をそのまま使う。PlatyPS はアルファベット順で出力するため、`Reorder-SyntaxParameters.ps1` で並び替えが必要。

### PS1 関数の `.EXTERNALHELP` は関数ボディの中に置く
`Staging/Functions/*.ps1` で MAML ヘルプを使わせるには `.EXTERNALHELP UiPathOrch-Help.xml` 指示が必要。配置場所:

- 既存のコメントベース help ブロックがある関数 (Find-OrchFolderNoUserAssigned, Format-OrchQueueItem, Format-OrchTestDataQueueItem) — そのブロック内に `.EXTERNALHELP UiPathOrch-Help.xml` 行を追加。
- それ以外 — `function ... { Param(...) }` の Param ブロック直後・実コードの前に `# .EXTERNALHELP UiPathOrch-Help.xml` を入れる。

**関数の外 (`function` キーワードの直前) に `<# .EXTERNALHELP ... #>` を置かないこと**。`[ArgumentCompleter([Generic[T]])]` 属性を持つ関数 (Disable/Enable-OrchPersonalWorkspace, Disable/Enable-OrchUserAttended) で `Get-Help` が "Multiple ambiguous overloads found for '.ctor'" エラーになる。関数ボディ内に置けば回避できる。
