# テナント移行シナリオ一覧

## 環境の組み合わせ

| # | Source | Destination | 頻度 | 検証状況 |
|---|--------|-------------|------|----------|
| A | オンプレ (AD あり) | クラウド (AD/AAD あり) | 高 | 未検証（オンプレ AD 環境なし） |
| B | オンプレ (AD あり) | クラウド (AD なし) | 中 | 未検証（オンプレ AD 環境なし） |
| C | オンプレ (AD なし) | クラウド (AD/AAD あり) | 低 | 未検証（オンプレ環境接続不可） |
| D | オンプレ (AD なし) | クラウド (AD なし) | 低 | 未検証（オンプレ環境接続不可） |
| E | クラウド (AD あり) | クラウド (AD なし) | 中 | **部分検証済み**（kzsai → Orch2） |
| F | クラウド (AD あり) | クラウド (AD あり) | 低 | 未検証 |
| G | クラウド (AD なし) | クラウド (AD なし) | 中 | 未検証（同一組織は対象外） |
| H | クラウド → オンプレ | 逆移行 | まれ | 未検証 |

## 各環境に存在しうるユーザー種別

| ユーザー種別 | 識別子の形式例 | SourceSource |
|---|---|---|
| オンプレ AD ユーザー | `DOMAIN\user` or `user@domain.local` | 未確認（オンプレ環境なし） |
| AAD ユーザー（ネイティブ） | `user@tenant.onmicrosoft.com` | `aad` |
| AAD ユーザー（ゲスト/EXT） | `user_ext#@tenant.onmicrosoft.com` | `aad` |
| クラウド ローカルユーザー | メールアドレス（例: `user@company.com`） | `local` |
| オンプレ ローカルユーザー | 任意の文字列（例: `admin`, `robot_user`） | 未確認 |

## シナリオ別の詳細

### シナリオ A: オンプレ (AD) → クラウド (AD/AAD)

最も一般的な移行パターン。ひとつのテナント内に複数種類のユーザーが混在しうる。

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| AD ユーザー → AAD に存在 | 既存ユーザーを割り当て | AAD の identityName | 不要（既存） |
| AD ユーザー → AAD に不在 | ローカル PmUser を作成 | メール or UPN | `Get-ADUser` or `Get-AzADUser` |
| ローカルユーザー → AAD に存在 | 既存ユーザーを割り当て | AAD の identityName | 不要（既存） |
| ローカルユーザー → AAD に不在 | ローカル PmUser を作成 | SourceEmail or SourceUserName | ソーステナント情報 or 手動 |

**検証状況**: 未検証（オンプレ AD 環境なし）

### シナリオ B: オンプレ (AD) → クラウド (AD なし)

全ユーザーをローカル PmUser として作成する必要がある。

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| AD ユーザー | ローカル PmUser を作成 | メール or UPN | `Get-ADUser` or `Get-AzADUser` |
| ローカルユーザー | ローカル PmUser を作成 | SourceEmail or SourceUserName | ソーステナント情報 or 手動 |

**検証状況**: 未検証（オンプレ AD 環境なし）

### シナリオ C: オンプレ (AD なし) → クラウド (AD/AAD あり)

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| ローカルユーザー → AAD に存在 | 既存ユーザーを割り当て | AAD の identityName | 不要（既存） |
| ローカルユーザー → AAD に不在 | ローカル PmUser を作成 | SourceEmail or 手動 | ソーステナント情報 or 手動 |

**検証状況**: 未検証（オンプレ環境接続不可）

### シナリオ D: オンプレ (AD なし) → クラウド (AD なし)

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| ローカルユーザー | ローカル PmUser を作成 | SourceEmail or SourceUserName | SourceDisplayName 分割 or 手動 |

**検証状況**: **部分検証済み**（local → Orch2）
- [x] `New-OrchUserMappingCsv` で CSV 生成（5ユーザー検出、SourceSource=local）
- [x] AI が DestinationUserName, Name, SurName, DisplayName を補完
- [x] `Test-OrchUserMappingCsv` で検証（0 Error 達成）
- [x] `New-PmUser` でローカルユーザー作成（5名）
- [x] `Copy-OrchFolderUser -UserMappingCsv` でフォルダユーザーコピー（ロール含む）
- [x] `Copy-OrchAsset -UserMappingCsv` で per-user アセットコピー
- [x] `Copy-Item -Recurse -UserMappingCsv` で統合テスト（フォルダユーザー・アセット OK、パッケージ不在エラーは既知）

### シナリオ E: クラウド (AD あり) → クラウド (AD なし)

kzsai（AAD 連携）→ Orch2（AD なし）で検証。

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| AAD ネイティブ | ローカル PmUser を作成 | UPN（`@tenant.onmicrosoft.com`） | `Get-AzADUser` |
| AAD ゲスト (#EXT#) | ローカル PmUser を作成 | 実メール（SourceEmail） | `Get-AzADUser` |
| ローカルユーザー | ローカル PmUser を作成 | SourceEmail | SourceDisplayName 分割 or 手動 |

**検証状況**:
- [x] `New-OrchUserMappingCsv` で CSV 生成
- [x] AI が `Get-AzADUser` で AAD ユーザー情報を取得し CSV 補完
- [x] ゲストユーザーの DestinationUserName を実メールに解決
- [x] `Test-OrchUserMappingCsv` で検証（0 Error 達成）
- [ ] `New-PmUser` でローカルユーザー作成
- [ ] `Copy-OrchFolderUser -UserMappingCsv` でエンティティコピー
- [ ] `Copy-Item -Recurse -UserMappingCsv` で統合テスト

### シナリオ F: クラウド (AD あり) → クラウド (AD あり)

異なる AAD テナント間の移行。

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| AAD ユーザー → 宛先 AAD に存在 | 既存ユーザーを割り当て | 宛先 AAD の identityName | 不要（既存） |
| AAD ユーザー → 宛先 AAD に不在 | ローカル PmUser を作成 | メール or 手動 | `Get-AzADUser` |
| ローカルユーザー | 宛先で検索 or 作成 | SearchDirectory or 手動 | ソーステナント情報 |

**検証状況**: 未検証

### シナリオ G: クラウド (AD なし) → クラウド (AD なし)

異なる組織間の移行。同一組織内は UserMappingCsv 不要。

| ユーザー種別 | 宛先での対応 | DestinationUserName の決定方法 | Name/SurName の取得元 |
|---|---|---|---|
| ローカルユーザー → 宛先に存在 | 既存ユーザーを割り当て | SearchDirectory で自動解決 | 不要（既存） |
| ローカルユーザー → 宛先に不在 | ローカル PmUser を作成 | SourceEmail or 手動 | SourceDisplayName or 手動 |

**検証状況**: 未検証（同一組織は対象外のため、異組織の AD なし環境が必要）

### シナリオ H: クラウド → オンプレ

逆移行。まれなケース。

**検証状況**: 未検証・未設計

## 移行ワークフロー

```
1. New-OrchUserMappingCsv -SourceTenant src: -DestinationTenant dst: -ExportCsv mapping.csv
   → ソーステナントのユーザー一覧を CSV に出力

2. AI が CSV を補完
   - SourceSource に応じて Get-AzADUser / Get-ADUser でユーザー詳細を取得
   - DestinationUserName, Name, SurName, DisplayName を埋める
   - ゲストユーザー (#EXT#) は実メールアドレスに解決

3. Test-OrchUserMappingCsv -CsvFile mapping.csv -SourceTenant src: -DestinationTenant dst:
   → CSV の検証（Error 0 を確認）

4. Import-Csv mapping.csv | New-PmUser -Path dst:
   → 宛先に存在しないユーザーをローカル PmUser として作成

5. Copy-Item src:\ dst:\ -Recurse -UserMappingCsv mapping.csv
   → エンティティ移行（ユーザー名マッピング適用）
```

## 実装済みコンポーネント

| コンポーネント | 状態 | 備考 |
|---|---|---|
| `New-OrchUserMappingCsv` | 実装済み | Name/SurName/DisplayName カラム追加済み |
| `Test-OrchUserMappingCsv` | 実装済み | 宛先未登録時の Name/DisplayName チェック追加済み |
| `New-PmUser` | 実装済み | `[Alias("DestinationUserName")]` 追加済み |
| `Copy-OrchUser -UserMappingCsv` | 実装済み | SearchDirectory 前にマッピング適用 |
| `Copy-OrchFolderUser -UserMappingCsv` | 実装済み | SearchDirectory 前にマッピング適用 |
| `Copy-OrchAsset -UserMappingCsv` | 実装済み | FindDstUser でマッピング適用 |
| `Copy-PmUser -UserMappingCsv` | 実装済み | userName 決定時にマッピング適用 |
| `Copy-Item -UserMappingCsv` | 実装済み | DynamicParameters 経由で全コピーに適用 |
| CSV パーサーの引用符対応 | 実装済み | `LoadUserMappingCsv` / `ReadMappingCsv` |
| 個人用 ws パッケージコピー後のプロセスキャッシュクリア | 実装済み | `CopyItem.cs` |
