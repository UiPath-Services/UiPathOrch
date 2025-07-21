# UiPathOrch Module ヘルプファイル品質ガイドライン準拠確認

## 📋 品質チェック項目（完全版）

### 🏗 基本構造
- [ ] ファイル名とヘッダー名の一致確認
- [ ] 構文検証エラーなし (Update-MarkdownHelp)
- [ ] 整合性チェック通過

### 📖 DESCRIPTION セクション
- [ ] エンティティタイプ明記（フォルダ/テナント/組織エンティティ）
- [ ] フォルダエンティティ: "Use Set-Location cmdlet (cd) or -Path, -Recurse, -Depth parameters" エラーメッセージ説明
- [ ] テナントエンティティ: "-Path = ドライブ名指定 (Orch1:, Orch2:)" 説明
- [ ] 組織エンティティ: "Platform Management API、組織レベル管理" 説明

### 📝 EXAMPLES セクション
- [ ] **実動作確認**: すべてのExamplesを実際に実行し、結果を確認
- [ ] **Positional Parameter優先**: 省略できる場合はパラメータ名を省略
- [ ] **パラメータ順序**: -Path, -Recurse, -Depth を他のパラメータより先に指定
- [ ] **一般的エンティティ名**: 固有環境名は使用せず、TestBucket, ProductionData等を使用
- [ ] **アルファベット名のみ**: 日本語エンティティ名は使用しない
- [ ] **ダブルクォート適切使用**: 必要な場合（スペース含む）のみ使用、単純文字列は不要
- [ ] **ワイルドカード**: *パターンはクォートしない
- [ ] **ドライブ名統一**: Orch1:\Production, Orch1:\Development, Orch1:\Finance使用
- [ ] **Where-Object vs パラメータフィルタ**: cmdletにフィルタパラメータがある場合はそれを優先
- [ ] **-WhatIf/-Confirm**: 削除系cmdletでは安全性重視

### 🔍 実行結果検証
- [ ] **ConvertTo-Json確認**: 実行結果をConvertTo-Jsonで構造確認
- [ ] **特記事項記載**: 重要な発見事項はヘルプに記載
- [ ] **エラー確認**: 期待通りの動作・エラーメッセージか確認
- [ ] **-Expand*パラメータ**: 詳細情報取得の動作確認

### ⚙ PARAMETERS セクション
- [ ] **正確なPosition番号**: Get-Help結果と一致
- [ ] **Type情報正確性**: String[], Int32等の型情報
- [ ] **Required/Optional**: 必須・任意の正確な表記

### 🔗 INPUTS/OUTPUTS セクション  
- [ ] **ByPropertyName binding説明**: "via ByPropertyName binding" 明記
- [ ] **System.String[]**: 基本入力型の説明
- [ ] **Entity型**: UiPath.PowerShell.Entities.[EntityType] 説明

### 🌐 API情報
- [ ] **Primary Endpoint**: 正確なAPIエンドポイント記載
- [ ] **OAuth scopes**: 必要なスコープ情報
- [ ] **Required permissions**: 必要な権限情報

### 📊 管理・検証
- [ ] 進捗ファイル更新済み
- [ ] 作業ログ記録済み

## 📊 作業進捗
- 総ファイル数: 281
- 品質チェック完了: 0
- 残り: 281

## 🔄 作業方針
1. **段階的チェック**: 機能グループ別に検証（Get-, Add-, Copy-, New-, Remove-, Update-等）
2. **環境準備**: 各cmdletの実行に必要なテストエンティティの作成
3. **実行確認**: すべてのExampleを実際に実行、ConvertTo-Json確認
4. **記録管理**: 各ファイルの品質チェック状況を記録

## 📁 機能グループ別作業計画

### Phase 1: Get-*系 (基本確認) - 約70ファイル
**利点**: 読み取り専用なので安全、既存エンティティで実行可能
**作業内容**:
- エンティティタイプ（フォルダ/テナント/組織）の正確性確認
- Positional Parameter使用の確認
- 実行結果のConvertTo-Json確認

### Phase 2: Add-*, Copy-*, New-*系 (作成系) - 約30ファイル
**作業内容**:
- テストエンティティ作成による実行確認
- 作成後の構造確認（ConvertTo-Json）
- 一般的エンティティ名の確認

### Phase 3: Remove-*, Update-*, Set-*系 (変更系) - 約40ファイル
**作業内容**:
- Phase 2で作成したテストエンティティを使用
- -WhatIf確認後、実際の変更実行
- 変更結果の構造確認

### Phase 4: 特殊系 (Enable-, Disable-, Start-, Stop-等) - 約30ファイル
**作業内容**:
- 既存エンティティでの状態変更確認
- 特殊機能の動作確認

### Phase 5: Pm系, Tm系, Du系 (専用API) - 約20ファイル
**作業内容**:
- Platform Management, Test Manager, Document Understanding専用機能
- 利用可能な環境での動作確認

## 🛠️ 環境準備チェック項目

### 基本環境確認
- [ ] UiPathOrch Provider利用可能
- [ ] Orch1:, Orch2:, Orch3: アクセス可能
- [ ] 基本フォルダ（Shared等）アクセス可能

### テストエンティティ作成
- [ ] テストバケット: TestBucket1, TestBucket2
- [ ] テストキュー: TestQueue1, TestQueue2  
- [ ] テストユーザー: TestUser1, TestUser2
- [ ] テストロール: TestRole1, TestRole2
- [ ] テストマシン: TestMachine1, TestMachine2
- [ ] テストプロセス: TestProcess1, TestProcess2

### 特殊環境確認
- [ ] Platform Management API利用可能性
- [ ] Test Manager API利用可能性
- [ ] Document Understanding API利用可能性
- [ ] Calendar, Trigger, Schedule機能利用可能性

## 📝 品質チェック記録テンプレート

### ファイル名: [cmdlet名].md
- [ ] エンティティタイプ正確性: ✅/❌
- [ ] Positional Parameter使用: ✅/❌  
- [ ] -Path/-Recurse/-Depth優先: ✅/❌
- [ ] 一般的エンティティ名使用: ✅/❌
- [ ] アルファベット名使用: ✅/❌
- [ ] Example実行確認: ✅/❌
- [ ] ConvertTo-Json確認: ✅/❌
- [ ] Position番号正確性: ✅/❌
- [ ] ByPropertyName説明: ✅/❌
- [ ] 構文検証: ✅/❌
- [ ] 整合性チェック: ✅/❌

**実行時の特記事項**:
- Example 1: [実行結果・注意点]
- Example 2: [実行結果・注意点]
- ConvertTo-Json結果: [重要なプロパティ・構造]

## 🚀 開始準備

**質問事項**:
1. **環境範囲**: どのOrch環境（Orch1:, Orch2:等）を使用可能か？
2. **権限確認**: 作成・変更・削除権限はどの範囲まで利用可能か？
3. **特殊API**: Platform Management, Test Manager, Document Understanding APIは利用可能か？
4. **テストデータ**: テスト用エンティティ作成時の命名規則や制約はあるか？
5. **安全性**: 本番データに影響しない安全な作業方法の確認

**次のアクション**: 環境確認後、Phase 1のGet-*系から順次開始