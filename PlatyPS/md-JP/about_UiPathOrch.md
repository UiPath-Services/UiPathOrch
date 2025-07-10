# UiPathOrch
## about_UiPathOrch

# SHORT DESCRIPTION
UiPathOrch は、コマンドレットを介して UiPath Orchestrator エンティティを管理するための PowerShell プロバイダーです。

# LONG DESCRIPTION
UiPathOrch は複数の Orchestrator テナントを PSDrive としてマウントし、dir、cd、mkdir、rmdir、ren、copy などの使い慣れたコマンドを使用してそれらのフォルダーを操作できるようにします。ライブラリ、パッケージ、プロセス、ジョブ、アセット、キュー、トリガー、ロボット、マシン、ユーザー、ロールなど、幅広い Orchestrator エンティティを管理します。これにより、コマンドラインとスクリプトを通じて Orchestrator を包括的に管理できます。

## 複数テナントの同時操作
複数のテナントを PSDrive として同時にマウントできます。`Edit-OrchConfig` を使用して設定ファイルを開き、複数のテナント接続を追加します。利用可能な PSDrive を確認するには、`Get-PSDrive` または `Get-OrchPSDrive` を使用します。

## 利用可能なコマンドレット
`Get-Command -Module UiPathOrch` を使用して、モジュール内のすべてのコマンドレットを一覧表示します。すべてのコマンドレット名の名詞は `Orch`、`Tm` (Test Manager)、`Du` (Document Understanding)、または `Pm` (Platform Management) で始まります。

## 複数フォルダーの同時ターゲット指定
-Path、-Recurse、-Depth パラメーターを使用して、対象のテナントとフォルダーを指定します。後続のパラメーターのオートコンプリートが適切に機能するように、各コマンドレットでこれらのパラメーターを最初に指定してください。-Path パラメーターには、ワイルドカードを含むコンマ区切りのパス文字列を指定できます。これらのパラメーターが指定されていない場合、現在のフォルダーが操作の対象になります。

## オートコンプリート機能
コマンドレット名、パラメーター名、パラメーター値は、[Ctrl+Space] または [Tab] を押すことで自動補完できます。この機能を最適に動作させるには、PSReadLine モジュールをインポートし、適切なパラメーターの順序を確保してください。

## 複数エンティティの同時操作
パラメーター値でワイルドカードとコンマを使用して、複数のエンティティ名を指定できます。多くのコマンドレットは効率的な管理のための一括操作をサポートしています。

## エンティティタイプ
このモジュールは 2 つの主要なエンティティタイプを処理します:
- **テナントエンティティ**: テナント全体で操作（ウェブフック、ロール、マシン、ユーザー、ライブラリ）
- **フォルダーエンティティ**: 特定のフォルダー内で操作（プロセス、アセット、キュー、トリガー、ロボット）

## キャッシュメカニズム
このモジュールはパフォーマンス向上のためにインテリジェントなキャッシュを実装しています。必要に応じて `Clear-OrchCache` を使用してキャッシュされたデータを更新してください。

## モジュールのアップグレード
`Update-Module UiPathOrch` を定期的に実行して、新機能と改善を含む最新バージョンのモジュールをダウンロードしてください。

# Examples
## Example 1
```powershell
PS Orch1:\> cd Shared
```
`cd` コマンド（`Set-Location` コマンドレット）は現在の場所を変更します。[Ctrl+Space] により、移動可能なフォルダー名のリストが表示されます。

## Example 2
```powershell
PS Orch1:\Shared> cd ..
```
`..` は親フォルダーを表します。`.` は現在のフォルダーを表します。

## Example 3
```powershell
PS Orch1:\> dir -Recurse
```
`-Recurse` パラメーターは、現在のフォルダーとそのすべてのサブフォルダーを対象とすることを指定します。このモジュールに含まれるほぼすべてのコマンドレットは `-Recurse` パラメーターを受け入れます。

## Example 4
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -Name *Invoice*
```
現在のフォルダーとすべてのサブフォルダーから、名前に "Invoice" を含むすべてのプロセスを取得します。

## Example 5
```powershell
PS C:\> Get-OrchPSDrive
```
利用可能なすべての Orchestrator PSDrive とその接続状態を表示します。

## Example 6
```powershell
PS Orch1:\> Get-OrchUser | Export-Csv users.csv -NoTypeInformation
```
現在のテナントからすべてのユーザーを CSV ファイルにエクスポートします。

# NOTE
包括的なドキュメントについては、`Get-OrchHelp` を使用してモジュールドキュメントとクイックスタートガイドを表示してください。必須ドキュメントはモジュールの Docs フォルダーで利用できます。

フォルダーエンティティではパラメーターの順序が重要です - 適切な自動補完を有効にするため、他のパラメーターの前に -Path、-Recurse、-Depth パラメーターを指定してください。

# TROUBLESHOOTING NOTE
## Orchestrator Web インターフェースまたは他の外部アプリケーションで行われたエンティティの変更が反映されない
`Clear-OrchCache` コマンドレットを実行して、このモジュールがメモリに保持しているキャッシュをクリアしてください。

## アクセス許可エラー
アクセスしようとしているエンティティに対して、Orchestrator ユーザーアカウントが適切なアクセス許可を持っていることを確認してください。一部の操作には、特定のフォルダーアクセス許可またはテナントレベルのアクセス許可が必要です。

## 接続の問題
`Get-OrchCurrentUser` を使用して接続状態を確認してください。接続の問題が続く場合は、`Edit-OrchConfig` で設定を確認してください。

# SEE ALSO
- Get-OrchHelp
- Get-OrchPSDrive  
- Get-OrchCurrentUser
- Clear-OrchCache
- Edit-OrchConfig

# KEYWORDS
UiPath, Orchestrator, PowerShell, プロバイダー, 自動化, RPA
