# P3 キャッシュクラス化 — 検証用 cmdlet 一覧

P3.0b〜P3.5 でクラス化した 13 個のキャッシュと、それを使う cmdlet、および手作業で確認すべき検証ポイントをまとめる。

## キャッシュ → クラス対応表

| # | キャッシュプロパティ              | クラス                                                                          | スコープ     | フェーズ |
| - | -------------------------------- | ------------------------------------------------------------------------------- | ------------ | -------- |
| 1 | `SearchPmDirectoryCache`         | `KeyedSingleCachePerOrganization<string, PmDirectoryEntityInfo[]?>`             | Organization | P3.0b    |
| 2 | `PmAvailableUserBundles`         | `KeyedSingleCachePerOrganization<string, AvailableUserBundles>`                 | Organization | P3.0b    |
| 3 | `PmUserLicenseGroupAllocations`  | `KeyedListCachePerOrganization<string, NuLicensedGroupMember>`                  | Organization | P3.0b    |
| 4 | `AssetLinks`                     | `KeyedSingleCachePerTenant<(folderId, assetId), AccessibleFoldersDto?>`         | Tenant       | P3.1     |
| 5 | `QueueLinks`                     | `KeyedSingleCachePerTenant<(folderId, queueId), AccessibleFoldersDto?>`         | Tenant       | P3.1     |
| 6 | `BucketLinks`                    | `KeyedSingleCachePerTenant<(folderId, bucketId), AccessibleFoldersDto?>`        | Tenant       | P3.1     |
| 7 | `PackageVersions`                | `KeyedListCachePerTenant<(feedId, packageId), Package>`                         | Tenant       | P3.2     |
| 8 | `PackageEntryPoints`             | `KeyedListCachePerTenant<(feedId, packageId, version), PackageEntryPoint>`      | Tenant       | P3.2     |
| 9 | `JobsHavingExecutionMedia`       | `IncrementalCachePerFolder<long, ExecutionMedia>`                               | Folder       | P3.3     |
| 10 | `TestCaseExecutions`            | `IncrementalCachePerFolder<long, TestCaseExecution>`                            | Folder       | P3.3     |
| 11 | `TestSetExecutions`             | `IncrementalCachePerFolder<long, TestSetExecution>`                             | Folder       | P3.4     |
| 12 | `QueueItems`                    | `IncrementalCachePerFolder<long, QueueItem>`                                    | Folder       | P3.4     |
| 13 | `TestCaseAssertions`            | `KeyedListCachePerFolder<long, TestCaseAssertion>`                              | Folder       | P3.5     |

スコープの意味:
- **Organization** — 同一組織内の複数テナントドライブで static storage を共有 (partitionGlobalId 単位)
- **Tenant** — 1 ドライブ (=1 テナント) 内で共有
- **Folder** — 1 ドライブ内、フォルダー単位

## cmdlet → キャッシュ → 検証ポイント

### Get-OrchAssetLink — `AssetLinks` (4)

ファイル: `AssetLinkCmdlet/GetAssetLink.cs`

- 初回 Get で `(folderId, assetId)` 単位でキャッシュ、2 回目同一キーで Get → API 不発火 (Verbose で HTTP リクエストが出ないこと)
- 別の asset / 別の folder 組み合わせは独立フェッチ
- `Remove-OrchAsset` 実行後、その asset を含むエントリだけ消える (predicate ClearCache `k => k.assetId == ...`)。他 asset のキャッシュは残る
- 存在しない asset で Get → 例外。直後の同一キー再 Get → キャッシュされた例外がそのまま投げられる (`ExceptionsCachePer<(folderId, assetId)>` の挙動)

### Get-OrchQueueLink — `QueueLinks` (5)

ファイル: `QueueLinkCmdlet/GetQueueLink.cs`

- AssetLink と同型の挙動。`(folderId, queueId)` キー
- `Copy-OrchQueue` 実行後、`srcDrive.QueueLinks.ClearCache()` が走り全クリア
- `Remove-OrchQueue` で `QueueLinks.ClearCache(k => k.queueId == ...)` 部分クリア → 削除キューに紐づくキャッシュだけ消えること

### Get-OrchBucketLink — `BucketLinks` (6)

ファイル: `BucketLinkCmdlet/GetBucketLink.cs`

- `(folderId, bucketId)` キー
- `Remove-OrchBucket` / `Copy-OrchBucket` 後に `BucketLinks.ClearCache()` で全クリア。AssetLinks/QueueLinks と違い全クリアなのは API が個別 invalidation 不可能だったため (現コードコメント参照)
- 削除/コピー後の Get で API 再フェッチが走ることを確認

### Get-OrchPackage — `PackageVersions` 経由ではない、別系統 (補助情報)

ファイル: `PackageCmdlet/GetPackage.cs`

- 直接 `PackageVersions` を触らない (フォルダー単位の `Packages` キャッシュを使用)。P3 改修対象外だが、後段の `Get-OrchPackageVersion` がここで取れた id を `(feedId, packageId)` キーに渡す

### Get-OrchPackageVersion — `PackageVersions` (7)

ファイル: `PackageCmdlet/GetPackageVersion.cs`、`ProcessCmdlet/{NewProcess,UpdateProcess,UpdateProcessVersion,GetProcessDetail}.cs`、`PackageCmdlet/ExportPackage.cs`、`PackageCmdlet/CopyPackage.cs`

- `(feedId, packageId)` キーで一覧をキャッシュ
- `Import-OrchPackage` / `Copy-OrchPackage` / `Remove-OrchPackage` 後、対象 `feedId` のエントリだけ消える (`ClearCache(k => k.feedId == ...)`)。他 feed のキャッシュは残る
- 同じ feed 内の別 package のキャッシュも残る (predicate は feedId のみで絞る)
- `New-OrchProcess` / `Update-OrchProcess` での連続呼び出しで同一 package を複数回参照しても 1 度しかフェッチされない

### Get-OrchProcessDetail / Update-OrchProcess / New-OrchProcess — `PackageEntryPoints` (8)

ファイル: `ProcessCmdlet/{GetProcessDetail,UpdateProcess,NewProcess}.cs`

- `(feedId, packageId, version)` キー (3 要素 tuple)
- ClearCache 呼び出し箇所はない (read-only)
- 同一 version を別 process が参照しても 1 度だけフェッチ
- 例外発生時 (バージョン不在など) はそのキーで例外キャッシュされ、同一キー再呼び出しで即座に同じ例外

### Get-OrchJobMedia / Export-OrchJobMedia / Remove-OrchJobMedia — `JobsHavingExecutionMedia` (9)

ファイル: `JobMediaCmdlet/{GetJobMedia,ExportJobMedia,RemoveJobMedia}.cs`

- IncrementalCachePerFolder: `-Skip` / `-First` で増分フェッチし、累積される
- **動作変更**: `Get-OrchJob` への副作用クロスキャッシュ書き込み (`HasMediaRecorded` の自動セット) が廃止された
  - 検証: `Get-OrchJobMedia` 実行後に `Get-OrchJob` で同一ジョブを表示しても `HasMediaRecorded` は自動更新されないこと
- `Remove-OrchJobMedia` 後、対象フォルダーの media キャッシュ全体が ClearCache される (id 単位の部分クリアではない) — 再 `Get-OrchJobMedia` で再フェッチ

### Get-OrchTestCaseExecution — `TestCaseExecutions` (10)

ファイル: `TestCaseCmdlet/GetTestCaseExecution.cs`、`TestCaseCmdlet/GetTestCaseAssertion.cs` (cross-cache lookup 用)

- **動作変更**: フィルタなし + Skip 0 + First MaxValue でもキャッシュフルダンプをしない → 必ず API フェッチして accumulate
  - 検証: `-Verbose` で常に HTTP リクエストが発火すること。同一クエリ再実行でも再フェッチ (他の `-First` cmdlet と整合)
- TestSetExecutionName / PathTestSetExecutionName が cross-cache lookup で TestSetExecutions から転写される (TestCaseExecution 自身は TestSetExecutionId のみ持つ)
- フォルダー内に大量の execution があっても OOM しないこと (accumulate なので id 重複は排除される)

### Get-OrchTestSetExecution / Stop-OrchTestSetExecution / Start-OrchTestSet — `TestSetExecutions` (11)

ファイル: `TestSetExecutionCmdlet/{GetTestSetExecution,StopTestSetExecution}.cs`、`TestSetCmdlet/StartTestSet.cs`

- **2 モード動作**:
  - フィルタなし呼び出し → 「キャッシュダンプモード」(Warning 表示 + 既存キャッシュを WriteObject、API 不発火)
  - フィルタあり (`-Last`, `-Status`, `-TriggerType`, `-Skip`, `-First` のいずれか) → API フェッチ + accumulate
- 検証手順:
  1. mount 直後の `Get-OrchTestSetExecution` (フィルタなし) → 空 + Warning
  2. `Get-OrchTestSetExecution -Last Day` → API フェッチ、結果表示
  3. 再度 `Get-OrchTestSetExecution` (フィルタなし) → 直前の結果がキャッシュ表示
- `Start-OrchTestSet` 実行後にそのフォルダーの `TestSetExecutions` キャッシュが ClearCache される (新しい execution を含めて再フェッチさせるため)
- `Stop-OrchTestSetExecution` は filter 付き Get で in-progress を引いてくる (API フェッチを毎回行うがキャッシュにも累積される)

### Get-OrchQueueItem / Remove-OrchQueueItem / Redo-OrchQueueItem — `QueueItems` (12)

ファイル: `QueueCmdlet/{GetQueueItem,RemoveQueueItem,RedoQueueItem}.cs`

- **構造変更**: 3 レベル `[folderId][queueName][itemId]` から 2 レベル `[folderId][itemId]` にフラット化。`queueName` は `QueueItem.Name` から復元
- 検証:
  - `Get-OrchQueueItem -Name <queue>` で正しくそのキュー所属の item のみ表示される (`Where(i => i.Name == queue.Name)` フィルタ経由)
  - 同じフォルダー内の複数キューを順次取得 → どのキューの item も保持され重複なし
  - `Get-OrchQueueItem -Id <id>` (個別取得) のあと `Get-OrchQueueItem -Name <queue>` を実行すると個別取得した item がキャッシュに含まれる (`AddToCache` 経由)
  - `Remove-OrchQueueItem` 削除後、削除済み item だけがキャッシュから消える (`TryRemove(itemId)`)。他 item は残る
  - `Redo-OrchQueueItem` 後はフォルダー全体の `QueueItems.ClearCache(folder)` で全クリア (status 変化が広範に及ぶため)
- completer (`Remove-OrchQueueItem` の IdCompleter) でキューごとに正しくグルーピング表示されること

### Get-OrchTestCaseAssertion — `TestCaseAssertions` (13) + `TestCaseExecutions` (10) cross-cache

ファイル: `TestCaseCmdlet/GetTestCaseAssertion.cs`

- 3 パラメータセット (`ByTestSetExecutionName` / `ById` / `ByPipeline`) いずれも `TestCaseAssertions.Get(folder, testCaseExecId)` 経由
- 検証:
  - `-Id <tceId>` で初回 fetch → API 1 回。再実行 → API なし
  - `-TestSetExecutionName <name>` 経由でも同じ tceId に対する assertion は同じキャッシュエントリにヒット (重複フェッチなし)
  - `-TestSetExecutionName` 未指定で既存キャッシュあり → 「キャッシュダンプモード」(Warning + 既存 entries を WriteObject)
  - cache 中の assertion に対し `TestSetExecutionName` / `PathTestSetExecutionName` が TestCaseExecutions キャッシュ経由で復元される (= TestSetExecutions も事前にロード済みである必要がある)
  - `-ScreenshotPath` 指定時、各 assertion の screenshot が 1 度だけダウンロードされ `ScreenshotPath` プロパティに保存される
  - 同一 `(folder, tceId)` を複数の InputObject パターンから処理しても `_processedIds` で重複排除される
  - 異なるフォルダー間でキャッシュが混じらない (folderId 外側キー)

### Search-OrchPmDirectory / Add-OrchPmGroupMember / Add-OrchPmLicenseToPmLicensedGroup — `SearchPmDirectoryCache` (1)

ファイル: `PmDirectoryCmdlet/SearchPmDirectory.cs`、`PmGroupCmdlet/AddPmGroupMember.cs`、`PmLicenseCmdlet/AddPmLicenseToPmLicensedGroup.cs`

- **重要動作**: org-scoped。同じ partitionGlobalId を持つ複数ドライブで `_dictionary` を **static 共有**
- 検証手順:
  1. 同じ Organization の 2 つのテナントドライブ (例: `Orch1Tenant1:`, `Orch1Tenant2:`) を mount
  2. `cd Orch1Tenant1:` で `Search-OrchPmDirectory user@example.com` → API フェッチ
  3. `cd Orch1Tenant2:` で同じ key で `Search-OrchPmDirectory user@example.com` → **API 不発火** (Verbose で確認)。共有 cache から返る
- key は wrapper で `ToLower()` 変換されるので大文字小文字を変えて検索しても同じエントリにヒット
- ClearCache 発生箇所:
  - `Remove-OrchPmGroup` / `Remove-OrchPmExternalApplication` / `Remove-OrchPmRobotAccount` / `Set-OrchPmRobotAccount`
  - 上記のいずれか実行後、別ドライブの再検索で API 再発火することを確認 (同一組織の全ドライブで cache クリアが伝播)

### Add-OrchPmLicenseToPmLicensedGroup / Remove-OrchPmLicenseFromPmLicensedGroup — `PmAvailableUserBundles` (2)

ファイル: `PmLicenseCmdlet/{AddPmLicenseToPmLicensedGroup,RemovePmLicenseFromPmLicensedGroup}.cs` 経由で `GetPmUserLicenseGroupsAvailableLicenses` wrapper

- 同上の org-scoped 共有挙動。`groupId` キー
- ClearCache: ライセンス追加/削除後に `PmAvailableUserBundles.ClearCache()` で全クリア (bundle 可用情報は他 group にも波及するため部分クリア不可)
- 検証: 同一組織の別ドライブで `Get-OrchPmLicensedGroup` 系を実行した直後、ClearCache が走ったドライブでの操作後にもう一方のドライブで再フェッチが走ること

### Get-OrchPmLicensedGroup / Remove-OrchPmAllocationFromPmLicensedGroup — `PmUserLicenseGroupAllocations` (3)

ファイル: `PmLicenseCmdlet/{GetPmLicensedGroup,RemovePmAllocationFromPmLicensedGroup}.cs`

- KeyedListCachePerOrganization: group.id キーでメンバー一覧をキャッシュ。同じ group.id を 2 回 Get → 2 回目は API なし
- 同一組織内の別ドライブでも static storage 共有
- ClearCache: ライセンス追加/削除/allocation 削除 (`Add-OrchPmLicenseToPmLicensedGroup` / `Remove-OrchPmLicenseFromPmLicensedGroup` / `Remove-OrchPmAllocationFromPmLicensedGroup`) で全クリア
- 検証: 同じ group の Get を 2 回 → 2 回目キャッシュヒット。Remove-allocation 後の Get で再フェッチ

## 横断確認事項

### キャッシュ自動クリア (`_allFolderCache` / `_allTenantCache`)

- フォルダー削除/移動時に Folder スコープのキャッシュが自動で flush されること
  - 影響: `JobsHavingExecutionMedia`, `TestCaseExecutions`, `TestSetExecutions`, `QueueItems`, `TestCaseAssertions`
- テナント切替時に Tenant スコープのキャッシュが自動で flush されること
  - 影響: `AssetLinks`, `QueueLinks`, `BucketLinks`, `PackageVersions`, `PackageEntryPoints`
- Organization スコープは ドライブ unmount で各ドライブの参照は外れるが static storage 自体は残る (他ドライブで再利用可能)
- `Clear-OrchCache` (もしあれば) 実行で全カテゴリ flush されること

### Exception キャッシュ

- 各クラスは `ExceptionsCachePer<TKey>` (または tuple version) で HttpResponseException をキー単位でキャッシュ
- 検証: 存在しないリソースを Get → 例外。直後の同一キー再 Get → 同じ例外が API 再発火なしで投げられる
- 別キーの Get は影響を受けないこと

### ApiVersion gating

- 一部のキャッシュは `_supportedApiVersionFrom` で API バージョン制限を持つ可能性
- 古い Orchestrator (v11.1 など) に mount したドライブで未サポート API のキャッシュが空コレクションを返すこと (例外にならない)

## 検証セッション参考シーケンス

1. **mount + 初期化** — `Connect-Orch` してドライブ mount、`cd Orch1:`
2. **Folder cache 系** — 任意フォルダーで `Get-OrchQueueItem` / `Get-OrchTestSetExecution` / `Get-OrchTestCaseExecution` / `Get-OrchTestCaseAssertion` / `Get-OrchJobMedia` を順次実行 → 2 回目で API なしを確認
3. **Tenant cache 系** — `Get-OrchAssetLink`, `Get-OrchQueueLink`, `Get-OrchBucketLink`, `Get-OrchPackageVersion`, `Get-OrchProcessDetail` を実行 → tuple-key キャッシュヒット確認
4. **Org cache 系** — 同一組織の 2 ドライブ mount し `Search-OrchPmDirectory` で cross-drive cache 共有を確認
5. **Mutation 系** — `Remove-*`, `Copy-*`, `Import-*`, `Update-*`, `Start-OrchTestSet`, `Redo-OrchQueueItem` 系を実行し、対応する ClearCache が走ることを確認 (実行後の Get で再フェッチ)
6. **Exception caching** — 存在しない id/name で Get → 同じ例外が 2 回目即時返却されること

参考: cache 操作の Verbose ログは `$VerbosePreference = 'Continue'` で有効化、または各 cmdlet に `-Verbose` を付与。
