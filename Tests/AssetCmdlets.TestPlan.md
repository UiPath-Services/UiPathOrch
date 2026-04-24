# Asset Cmdlets Comprehensive Test Plan

## Scope

全 Asset 関連 cmdlet の動作検証。新規追加 cmdlet と Secret 型対応に伴う既存 cmdlet の挙動変化を重点確認する。

### Cmdlets under test

| cmdlet | 種別 |
|---|---|
| `Get-OrchAsset` | 既存 (動作変更あり: `-ExportCsv` で Secret 除外) |
| `Set-OrchAsset` | 既存 (動作変更あり: Secret 型を silent no-op) |
| `Remove-OrchAsset` | 既存 |
| `Copy-OrchAsset` | 既存 |
| `Set-OrchCredentialAsset` | 既存 |
| `Get-OrchCredentialAsset` | **新規** |
| `Set-OrchSecretAsset` | **新規** |
| `Get-OrchSecretAsset` | **新規** |
| `Remove-OrchAssetUserValue` | **新規** |

### 関連変更

- `OrchAPISession.GetAssets` — v20+ で `GetFiltered` エンドポイントに自動切替
- `Asset` / `AssetUserValue` entity に `SecretValue`, `AllowDirectApiAccess` フィールド追加
- `ListCachePerFolder<T>` — root folder (Id=null) サポート (DataFabric 対応の副作用)

## Test Infrastructure

**テストファイル**: `Tests/SelfContained.Tests.ps1` に追加セクション
**フォルダ**: 既存の `$script:RootFolder` / `$script:SubFolder` を使用
**プレフィクス**: `${script:Prefix}` (既存 `PesterTest_XXXX_` パターン)
**CSV 一時ディレクトリ**: 既存の `$script:TempDir`
**Cleanup**: 各 Describe の AfterAll で `Remove-OrchAsset -Name "${prefix}*"` を実行

### フォルダへのユーザー割り当て

**ベストプラクティス** として、Asset の UserValue (PerRobot 値) は asset を含むフォルダに割り当てられた user/robot に対して設定する。
ただし **admin write API は folder 未割当の tenant user でも UserValue 作成を受諾する** ことが探査で判明 (下記「探査で判明した現実の挙動」参照)。
robot runtime で値を実際に読み出すには folder 割り当てが必要なので、テストでは仕様に忠実に folder user を割り当てた状態で行う。

**BeforeAll 追加手順** (OrchTest 環境で調査済みの値):
```powershell
# OrchTest テナントには DirectoryRobot ユーザーが存在しないため、既存 DirectoryUser を使用。
# Set-OrchSecretAsset/Set-OrchCredentialAsset の -UserName は UserValue マッピング用で、
# admin API は user type (DirectoryUser/DirectoryRobot 等) を区別せず受諾する。
$script:TestUserA = 'ytsuda@gmail.com'           # 既存 DirectoryUser (テナント上に存在)
$script:TestUserB = 'yoshifumi.tsuda@uipath.com' # 2 人目 (wildcard テスト用)
$script:TestUserType = 'DirectoryUser'
$script:TestMachine = 'm3'                        # 既存 machine (Standard type)

# Assign to test folder — -Type mandatory, -Roles は folder-scoped role 名
Add-OrchFolderUser -Type $script:TestUserType -UserName $script:TestUserA `
    -Roles 'Automation User' -Path $script:RootFolder -ErrorAction SilentlyContinue
Add-OrchFolderMachine -Path $script:RootFolder -Name $script:TestMachine -ErrorAction SilentlyContinue
```

**AfterAll 追加クリーンアップ** (フォルダ削除前に実行):
```powershell
Remove-OrchFolderUser -Path $script:RootFolder -UserName $script:TestUserA -Confirm:$false -ErrorAction SilentlyContinue
Remove-OrchFolderMachine -Path $script:RootFolder -Name $script:TestMachine -Confirm:$false -ErrorAction SilentlyContinue
```

**依存**:
- 使用する user (`ytsuda@gmail.com` 等) はテナント上に事前存在する前提
- role は `Get-OrchRole` で候補確認。OrchTest 環境で確認済み: `Automation User` (Folder), `Folder Administrator` (Folder)
- `Add-OrchFolderUser -Roles` は **Folder スコープのロール名** を取る (Tenant ロール `Allow to be Automation User` ではない)

**探査で判明した現実の挙動 (重要)**:
- `Add-OrchFolderUser` は `-Type` パラメータ **必須**。未指定で実行すると interactive prompt が出るので、テストスクリプトでは必ず明示
- `-Type` の有効値: `DirectoryUser` / `DirectoryGroup` / `DirectoryRobot` / `DirectoryExternalApplication`
- 現在の server は **folder 未割り当てユーザーに対する UserValue 作成を受諾する** (admin 権限での確認)。
  「folder 割り当てユーザーに対してのみ UserValue 設定可能」は **robot runtime で値を実際に読み出せる条件** であって、admin write API では enforce されていない
- cmdlet の client-side check も tenant-wide の `drive.GetUsers()` のみ。folder 割り当てチェックはしていない
- → **テスト設計方針**: 「PerRobot テストでは明示的に folder user 割り当てを行う」ことで仕様に忠実にしつつ、
  割り当てなしの挙動も guard test (T11.9) でカバーして将来の strict enforce 変更を検知

## Test Matrix

### Section 1: `Set-OrchAsset` (Text/Bool/Integer) — regression

| ID | 内容 | 期待 |
|---|---|---|
| T1.1 | Create Text asset with value + description | Created, ValueType=Text, Value='hello' |
| T1.2 | Create Integer asset | ValueType=Integer, Value='42' |
| T1.3 | Create Bool asset | ValueType=Bool, Value='True' |
| T1.4 | Update existing asset value | New value reflected |
| T1.5 | Update description only (no -Value) | Description updated, Value unchanged |
| T1.6 | ValueType preserved on update (no -ValueType) | 既存 ValueType 維持 |
| T1.7 | Wildcard name update | 複数 asset が一括更新 |
| T1.8 | Invalid ValueType | WriteError 発生 |
| **T1.9** | `-ValueType Credential` | **silent no-op, エラーなし, asset 変化なし** |
| **T1.10** | `-ValueType Secret` | **silent no-op, エラーなし, asset 変化なし (旧: エラー)** |
| T1.11 | 既存 Global Text asset に `-Value ''` | Global 値 clear、`ValueScope='PerRobot'`, `HasDefaultValue=false` に切替 (asset 自体は存続) |
| T1.12 | PerRobot: `-UserName X -Value v` で UserValue 作成 | UserValues に該当 user 追加 |
| T1.13 | PerRobot: `-UserName X -Value ''` で empty-delete | 該当 UserValue のみ削除、他は温存 |

### Section 2: `Get-OrchAsset`

| ID | 内容 | 期待 |
|---|---|---|
| T2.1 | Name wildcard (複数型混在) | Text/Bool/Integer/Credential/Secret 全部返る |
| T2.2 | `-ValueType Text` | Text 型のみ |
| T2.3 | `-ExportCsv` | CSV 出力成功 |
| **T2.4** | `-ExportCsv` の行に Credential 行なし | 既存動作 (回帰確認) |
| **T2.5** | `-ExportCsv` の行に Secret 行なし | **新規動作 (修正版)** |
| T2.6 | `-ExportCredentialCsv` | Credential 型のみ出力 |
| T2.7 | `-ExpandUserValues` | PerRobot 値が個別行で展開 |
| T2.8 | `-Path` 指定フォルダのみ | 該当フォルダの asset のみ |
| T2.9 | `-Recurse` / `-Depth` | サブフォルダも traverse |

### Section 3: `Remove-OrchAsset`

| ID | 内容 | 期待 |
|---|---|---|
| T3.1 | `Remove-OrchAsset -Name X` 単一 asset 削除 | 削除成功、Get で不在 |
| T3.2 | `-Name` wildcard 削除 | 複数 asset 一括削除 |
| T3.3 | `-ValueType Credential` 指定削除 | Credential 型のみ対象 |
| T3.4 | `Remove-OrchAsset -Name <secret>` で Secret 型削除 | 削除成功 (Secret 型も通常の asset 削除で対応可) |
| T3.5 | `-Confirm:$false` | prompt なしで削除 |

### Section 4: `Set-OrchCredentialAsset`

| ID | 内容 | 期待 |
|---|---|---|
| T4.1 | `-CredentialUsername` + `-CredentialPassword` (Plain set) | Credential 作成 |
| T4.2 | `-Credential` PSCredential (Default set) | 同上、username/password extracted |
| T4.3 | `-ExternalName` 指定 (vault reference) | ExternalName 保存、password/username null |
| T4.4 | 既存 asset の CredentialUsername 更新 | 値反映 |
| T4.5 | 既存 asset の Description 更新 | 値反映 |
| **T4.6** | `-CredentialPassword ''` on existing asset | **Global 値不変、HasDefaultValue=True のまま (round-trip safe)** |
| T4.7 | `-UserName X` で PerRobot 作成 | UserValues に追加 |
| T4.8 | `-UserName ''` で empty-delete | 該当 UserValue 削除 |
| T4.9 | Name wildcard 更新 | 複数更新 |

### Section 5: `Get-OrchCredentialAsset` **(新規)**

| ID | 内容 | 期待 |
|---|---|---|
| T5.1 | 返る型 | **Credential のみ** (Text/Bool/Integer/Secret 排除) |
| T5.2 | Name wildcard | マッチ Credential のみ |
| T5.3 | `-ExportCsv` | Credential CSV 形式 (既存 `-ExportCredentialCsv` と同一 headers) |
| T5.4 | CSV 列: `Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName` | 正確 |
| T5.5 | CredentialPassword 列が空 (マスク) | ✓ |
| T5.6 | CredentialUsername 列は値あり | ✓ (マスクされないため) |
| T5.7 | `-ExpandUserValues` | PerRobot 展開 |
| T5.8 | **Round-trip**: Get → Export → Import → Set → Get | 元の値と一致 (password 以外) |
| T5.9 | `-Path` / `-Recurse` / `-Depth` | 期待どおり |
| **T5.10** | **`UserValues.CredentialUsername` が populated** される | GetFiltered 切替時の historical bug (旧: UserValues.CredentialUsername が空で返る) が再発していないこと |

### Section 6: `Set-OrchSecretAsset` **(新規)**

| ID | 内容 | 期待 |
|---|---|---|
| T6.1 | `-SecretValue` Plain set | Secret asset 作成、ValueType=Secret |
| T6.2 | `-Secret` SecureString Default set | 同上 |
| T6.3 | `-ExternalName` | vault reference 保存 |
| T6.4 | 既存 asset の `-SecretValue` 更新 | LastModificationTime 更新 (値はマスクで確認不可) |
| T6.5 | 既存 asset の Description 更新 (`-SecretValue` なし) | Description 反映、Secret 値不変 |
| **T6.6** | **Round-trip Global**: Get → Export → Import → Set (空 SecretValue) | **asset 不変 (round-trip safe)** |
| **T6.7** | 既存 asset + 空 SecretValue | HasDefaultValue=True のまま、CredentialStoreId 保持 |
| T6.8 | `-UserName X -SecretValue v` で PerRobot 作成 | UserValues に追加 |
| **T6.9** | `-UserName X` (空 SecretValue) | **該当 UserValue 削除しない (旧 empty-delete と異なる)** |
| T6.10 | Name wildcard 更新 | 複数 |
| T6.11 | `-CredentialStore` 指定 | 該当 store に紐付け |
| **T6.12** | `-Secret` と `-SecretValue` 同時指定 | parameter set 解決エラー (排他性) |
| **T6.13** | `-WhatIf` | 変更なし、意図を表示 |
| **T6.14** | PSCustomObject をパイプで流入 (Import-Csv 以外) | Plain set に解決、期待どおり update |

### Section 7: `Get-OrchSecretAsset` **(新規)**

| ID | 内容 | 期待 |
|---|---|---|
| T7.1 | 返る型 | **Secret のみ** |
| T7.2 | 他型排除 | Text/Credential 等を返さない |
| T7.3 | Name wildcard | マッチ Secret のみ |
| T7.4 | `-ExportCsv` | Secret CSV 形式 |
| T7.5 | CSV 列: `Path,Name,Description,CredentialStore,UserName,MachineName,SecretValue,ExternalName` | 正確 |
| T7.6 | SecretValue 列は常に空 (API マスク) | ✓ |
| T7.7 | `-ExpandUserValues` | PerRobot 展開 |
| T7.8 | `-Path` / `-Recurse` / `-Depth` | 期待どおり |

### Section 8: `Remove-OrchAssetUserValue` **(新規)**

| ID | 内容 | 期待 |
|---|---|---|
| T8.1 | `-Name X -UserName Y` で Credential の UserValue 削除 | 該当 UserValue のみ削除 |
| T8.2 | Secret の UserValue 削除 | 同上 |
| T8.3 | `-UserName Y -MachineName M` AND フィルタ | User+Machine 両方一致のみ削除 |
| T8.4 | wildcard `-UserName user*` | 複数一致を削除 |
| T8.5 | 全 UserValue 削除 | `ValueScope='Global'`, `UserValues=null`, Global 値の `HasDefaultValue` 維持 |
| T8.6 | `-WhatIf` | 変更なし |
| T8.7 | `-Confirm:$false` | prompt なし |
| T8.8 | 非存在 user 指定 | no-op, エラーなし |
| T8.9 | UserValues 無しの asset | 静かに skip |
| T8.10 | Secret 型でも動作 | 型非依存の一般ケース |
| **T8.11** | **Global-only asset (UserValues 無し)** に Remove-OrchAssetUserValue | 変化なし、エラーなし |
| **T8.12** | **Text 型 asset** の UserValue 削除 | 型非依存で動作 (Secret に限らない) |

### Section 9: Cross-cutting / Integration

| ID | 内容 | 期待 |
|---|---|---|
| T9.1 | **Round-trip Credential**: Get-OrchCredentialAsset → Export → Import → Set-OrchCredentialAsset | username/description 復元、password 元のまま (空送信で skip) |
| T9.2 | **Round-trip Secret**: Get-OrchSecretAsset → Export → Import → Set-OrchSecretAsset | description 復元、SecretValue clobber されず |
| T9.3 | **混在 CSV** (Text + Credential + Secret) → 各 Set cmdlet に分岐 | 手動分岐が必要 (警告: 1 CSV で全型は不可) — テスト書式確認 |
| T9.4 | **`Get-OrchAsset` + `Get-OrchCredentialAsset` + `Get-OrchSecretAsset` 合計 = 全 asset** | 型 partition の完全性 |
| T9.5 | 同一 asset 名で異なる型の作成試行 | 後勝ち or エラー (API 依存、実挙動を記録) |
| **T9.6** | **Unicode / 日本語名** round-trip | `Set-OrchSecretAsset -Name 'シークレット太郎'` → Get → Export → Import で等価復元 |
| **T9.7** | **N 行 × 単一 asset パイプライン** | `Import-Csv` 100 行を同一 asset に流しても `pendingAssets` で集約されて PUT は 1 回 (batching 保証) |
| **T9.8** | **Copy-OrchAsset + Secret** | 挙動確認 (API は masked 値をコピーできないはず) |

### Section 10: API Version awareness (regression for v19)

| ID | 内容 | 期待 |
|---|---|---|
| T10.1 | v20+ tenant: `GetAssets` で Secret 返る | ✓ (`GetFiltered` 使用) |
| T10.2 | v19 tenant: Secret フィールドなし、`/odata/Assets` 使用 | v19 テナントが無ければ skip |
| T10.3 | Asset entity の SecretValue フィールド | v20+ で populated (常に空文字列) |
| T10.4 | Asset entity の AllowDirectApiAccess フィールド | v20+ で `false` default |

### Section 11: Negative / Error cases

| ID | 内容 | 期待 |
|---|---|---|
| T11.1 | 権限なしフォルダでの操作 | WriteError, 例外なし |
| T11.2 | 非存在フォルダ `-Path` | WriteError |
| T11.3 | `-ValueType 不正値` | WriteError |
| T11.4 | `Set-OrchSecretAsset` を Default set で `-Secret $null` | Mandatory validation エラー |
| T11.5 | `Remove-OrchAssetUserValue` で全 UserValue 削除後、Global 値も空の場合 | asset は存続 (自動削除されない想定)、API 実挙動を記録 |
| T11.6 | `-CredentialStore 'nonexistent'` | WriteError |
| T11.7 | Personal workspace フォルダ経由での操作 | 除外される (`EnumFoldersWithoutPersonalWorkspace` 使用箇所) |
| T11.8 | 既存 Credential 名と同名の Secret 作成試行 | API 拒否 (想定 409)、実挙動を記録 |
| T11.9 | **folder 未割り当て DirectoryUser** に `Set-OrchSecretAsset -UserName X` | **現状: 成功 (admin write API は enforce しない)** — 将来仕様変更で失敗するようになったら検知する guard test |

### Section 12: Performance / Batching

| ID | 内容 | 期待 |
|---|---|---|
| T12.1 | 100 行 × 単一 asset の `Import-Csv | Set-OrchSecretAsset` | `Measure-Command` で所要時間 bound、PUT は論理的に 1 回 (proxy check via time) |
| T12.2 | N 行 × N assets | ほぼ N PUT (集約不可のケース) |
| T12.3 | Cache hit による secondary read 高速化 | 2 回目 Get が `Clear-OrchCache` なしで高速 |

## Test Execution

```powershell
Invoke-Pester -Path C:\MyProj\UiPathOrch\Tests\SelfContained.Tests.ps1 -Output Detailed
```

Pester ブロックの追加先:
- `Describe 'Asset'` 内に T1.9, T1.10, T1.11 を追加 (既存の T1.1-1.8, 1.12-1.13 はそのまま)
- `Describe 'Credential Asset'` 内に T4.6 を追加
- 新 `Describe 'Get-OrchCredentialAsset'` (Section 5)
- 新 `Describe 'Set-OrchSecretAsset'` (Section 6)
- 新 `Describe 'Get-OrchSecretAsset'` (Section 7) — Set とは別 Describe で分離 (BeforeAll/AfterAll を型ごとに独立させる)
- 新 `Describe 'Remove-OrchAssetUserValue'` (Section 8)
- 新 `Describe 'Asset Round-trip'` (Section 9)
- 新 `Describe 'Asset API Version'` (Section 10) — v20 前提テスト + v19 skip フラグ
- 新 `Describe 'Asset Negative'` (Section 11)
- 新 `Describe 'Asset Performance'` (Section 12) — Tag 'Performance' で optional 実行

## Test Data Requirements

テストに必要な環境前提:
- Orchestrator v20+ tenant (Secret 型対応)
- フォルダ作成権限 + FolderUser/FolderMachine 割り当て権限
- Asset 作成/削除権限 + Credential 読書き権限
- Credential Store (既定: Orchestrator Database) が利用可能
- **PerRobot テスト用**:
  - 少なくとも 1 名の user (DirectoryRobot or DirectoryUser) がテナント上に存在
  - OrchTest では DirectoryRobot がないため `ytsuda@gmail.com` (DirectoryUser) を使用
  - BeforeAll で `Add-OrchFolderUser -Type DirectoryUser -UserName 'ytsuda@gmail.com' -Roles 'Automation User' -Path $RootFolder` により割り当て
  - 1 マシン (`m3` 等) も同フォルダに `Add-OrchFolderMachine` で割り当て
  - AfterAll で逆順に削除 (folder user/machine → folder)
- **挙動の注意** (探査で判明):
  - `Set-OrchSecretAsset -UserName X` は `X` が tenant にさえ存在すれば成功する (admin 権限)
  - cmdlet の error message `"UserName X is not assigned to the folder"` は **実際は tenant-wide チェック**。folder 割り当てチェックではない
  - 割り当てなし UserValue は orphan になる可能性 (robot runtime 読み出しには folder user 割り当てが要件)
  - テスト上は「明示的に割り当てた状態での成功」+「割り当て無しでも現状は受諾される guard test (T11.9)」の両方をカバー

## 成果物

- テスト実行後の Pester レポート (PASS/FAIL 内訳)
- FAIL があれば原因分析
- 必要に応じて cmdlet 修正
