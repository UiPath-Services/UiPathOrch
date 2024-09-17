using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using Path = System.IO.Path;

namespace UiPath.PowerShell.Core
{
    public static class PathTools
    {
        // コマンドレットの completer で候補を表示するときは、こちらを使う。
        public static string EscapePSText(string? input)
        {
//            return "'" + WildcardPattern.Escape(input) + "'";
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            string ret = input
                .Replace("`", "``")
                .Replace("'", "''")
                .Replace("*", "`*")
                .Replace("?", "`?")
                .Replace("[", "`[")
                .Replace("]", "`]");

            if (input != ret || input.Contains(' ') || input.Contains('\\') || input.Contains('\'')) //★★★
                //ret = '\'' + ret + '\'';
                ret = $"'{ret}'";

            return ret;
        }

        // コマンドレットの completer で候補を表示するときは、こちらを使う。
        public static string EscapeNonWildcardText(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return "''";
            return "'" + input.Replace("'", "''") + "'";
        }

        // プロバイダの GetChildItems で WriteItemObject() するときは、こちらを使う。
        // [ や ] などは自動で処理されるっぽい。* と ? は、ファイル名として想定されない？
        public static string EscapePSText2(string? input)
        {
            //            return "'" + WildcardPattern.Escape(input) + "'";
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            string ret = input
                .Replace("*", "`*")
                .Replace("?", "`?");
            //.TrimStart('\\');

            //if (input != ret || input.Contains(' ')) //★★★
            //    ret = '\'' + ret + '\'';

            return ret;
        }

        public static bool IsEscapedPSText(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return (input.Length >= 2 && input.StartsWith('\'') && input.EndsWith('\''));
        }

        // this return value should be passed to WildcardPattern ctor
        public static string UnescapePSText(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            if (IsEscapedPSText(input))
            {
                input = input.Substring(1, input.Length - 2);
                input = input.Replace("''", "'");
            }

            return input;
        }

        private static string ExtractVersion(string fullPath)
        {
            string fileName = System.IO.Path.GetFileName(fullPath);
            var match = Regex.Match(fileName, @"(\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?)");
            return match.Success ? match.Value : "";
        }

        // OrchDriveInfo.ExpandLocalPath() と組み合わせて使う
        public static IEnumerable<(string FullPath, string RelativePath)> OrderByFileNameVersion(
            this IEnumerable<(string FullPath, string RelativePath)> files)
        {
            return files
                .Select(file => new
                {
                    file.FullPath,
                    file.RelativePath,
                    Version = ExtractVersion(file.FullPath)
                })
                .OrderBy(item => System.IO.Path.GetDirectoryName(item.FullPath))
                .ThenBy(item => item.Version, VersionComparer.Instance)
                .Select(item => (item.FullPath, item.RelativePath));
        }
    }

    public class PathInfoComparar : IEqualityComparer<PathInfo>
    {
        bool IEqualityComparer<PathInfo>.Equals(PathInfo? x, PathInfo? y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x!.Path, y!.Path);
        }

        int IEqualityComparer<PathInfo>.GetHashCode(PathInfo obj)
        {
            return obj.GetHashCode();
        }
    }

    //internal class UsersCache
    //{
    //    private Lazy<List<User>> _lazyUsers;
    //    internal readonly ExceptionCachePerTenant _dicUsersException = new ExceptionCachePerTenant();
    //    internal OrchAPISession OrchAPISession { get; set; }
    //    internal string NameColonSeparator { get; set; }

    //    public UsersCache()
    //    {
    //        // Lazy<T>の初期化に、スレッドセーフな方法でUserリストを取得するLambda式を渡します。
    //        _lazyUsers = new Lazy<List<User>>(() => FetchUsers(), LazyThreadSafetyMode.ExecutionAndPublication);
    //    }

    //    public ReadOnlyCollection<User> GetUsers()
    //    {
    //        _dicUsersException.ThrowCachedExceptionIfAny();

    //        // Lazy<T>.Valueにアクセスすることで、必要な時に一度だけUserリストを取得します。
    //        return _lazyUsers.Value.AsReadOnly();
    //    }

    //    private List<User> FetchUsers()
    //    {
    //        try
    //        {
    //            var users = OrchAPISession.GetUsers().ToList();
    //            string driveName = NameColonSeparator;
    //            foreach (var user in users)
    //            {
    //                user.Path = driveName;
    //            }
    //            return users;
    //        }
    //        catch (HttpResponseException ex)
    //        {
    //            _dicUsersException.CacheException(ex);
    //            throw;
    //        }
    //    }
    //}

    #region テナントキャッシュの実験的な実装
    //public class TenantEntityCache<T> where T : class
    //{
    //    private readonly OrchDriveInfo _drive;
    //    private Lazy<T>? _cache;
    //    private readonly Func<OrchDriveInfo, T> _fetchEntities;
    //    private readonly ExceptionCachePerTenant _exceptionCache = new();

    //    public TenantEntityCache(OrchDriveInfo drive, Func<OrchDriveInfo, T> fetchEntities)
    //    {
    //        _drive = drive;
    //        _fetchEntities = fetchEntities;
    //        ResetCache();
    //    }

    //    private T InitializeCache()
    //    {
    //        try
    //        {
    //            return _fetchEntities(_drive);
    //        }
    //        catch (HttpResponseException ex)
    //        {
    //            _exceptionCache.CacheException(ex);
    //            throw;
    //        }
    //    }

    //    public T GetCache()
    //    {
    //        _exceptionCache.ThrowCachedExceptionIfAny();
    //        return _cache!.Value;
    //    }

    //    public void ResetCache()
    //    {
    //        _cache = new Lazy<T>(() => InitializeCache(), LazyThreadSafetyMode.ExecutionAndPublication);
    //    }
    //}
    #endregion

    //public class ConsoleCancelHandler : IDisposable
    //{
    //    private readonly CancellationTokenSource _cts;
    //    private readonly ConsoleCancelEventHandler _handler;
    //    private bool _cancelKeyPressed;

    //    // キャンセルキーが押されたかどうかを示すプロパティ
    //    public bool CancelKeyPressed => _cancelKeyPressed;

    //    // デフォルトコンストラクタ
    //    public ConsoleCancelHandler()
    //    {
    //        _cts = new CancellationTokenSource();
    //        _cancelKeyPressed = false;
    //        _handler = (sender, args) =>
    //        {
    //            _cancelKeyPressed = true;
    //            _cts.Cancel();
    //            args.Cancel = true; // Prevent the process from terminating
    //        };
    //        Console.CancelKeyPress += _handler;
    //    }

    //    // キャンセル処理のデリゲートを受け取るコンストラクタ
    //    public ConsoleCancelHandler(Action onCancel)
    //    {
    //        _cts = new CancellationTokenSource();
    //        _cancelKeyPressed = false;
    //        _handler = (sender, args) =>
    //        {
    //            _cancelKeyPressed = true;
    //            onCancel.Invoke();
    //            args.Cancel = true; // Prevent the process from terminating
    //        };
    //        Console.CancelKeyPress += _handler;
    //    }

    //    // CancellationTokenSource を公開するプロパティ
    //    public CancellationToken Token => _cts.Token;

    //    public void Dispose()
    //    {
    //        Console.CancelKeyPress -= _handler;
    //        _cts?.Dispose();
    //        GC.SuppressFinalize(this);
    //    }
    //}

    public class ConsoleCancelHandler : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly ConsoleCancelEventHandler _handler;
        private bool _cancelKeyPressed;

        // キャンセルキーが押されたかどうかを示すプロパティ
        public bool CancelKeyPressed => _cancelKeyPressed;

        // CancellationTokenSource を公開するプロパティ
        public CancellationToken Token => _cts.Token;

        // デフォルトコンストラクタ
        public ConsoleCancelHandler()
        {
            _cts = new CancellationTokenSource();
            _handler = CreateHandler(null);
            Console.CancelKeyPress += _handler;
        }

        // キャンセル処理のデリゲートを受け取るコンストラクタ
        public ConsoleCancelHandler(Action onCancel)
        {
            _cts = new CancellationTokenSource();
            _handler = CreateHandler(onCancel);
            Console.CancelKeyPress += _handler;
        }

        // 共通の初期化処理
        private ConsoleCancelEventHandler CreateHandler(Action? onCancel)
        {
            return (sender, args) =>
            {
                _cancelKeyPressed = true;
                try
                {
                    onCancel?.Invoke();
                }
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"An error occurred in the cancellation handler: {ex.Message}");
                //}
                finally
                {
                    _cts.Cancel(); // Ensure the token is cancelled
                    args.Cancel = true; // Prevent the process from terminating
                }
            };
        }

        // Dispose パターンの実装
        public void Dispose()
        {
            Console.CancelKeyPress -= _handler;
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class OrchTask<TSource, TResult> : IDisposable
    {
        public ManualResetEventSlim CompletedEvent { get; }
        public TSource? Source { get; private set; }
        public string? Path { get; private set; }
        public object? Target { get; private set; }
        public TResult? Result { get; private set; }
        public Exception? Exception { get; private set; }

        public OrchTask()
        {
            CompletedEvent = new ManualResetEventSlim(false);
        }

        public void SetResult(TSource source, TResult result)
        {
            Source = source;
            Result = result;
            CompletedEvent.Set(); // 処理が完了したことを示す
        }

        public void SetException(TSource source, string path, object target, Exception ex)
        {
            Source = source;
            Path = path;
            Target = target;
            Exception = ex;
            CompletedEvent.Set(); // 処理が完了したことを示す
        }

        public TResult? GetResult(CancellationToken token)
        {
            CompletedEvent.Wait(token); // スレッドの結果がセットされるまで待機

            if (Exception != null)
            {
                var e = new OrchException(Path!, Exception!)
                {
                    Target = Target!
                };
                throw e;
            }
            return Result;
        }

        public void Dispose()
        {
            CompletedEvent.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // このクラスの RunForEach() は、メインスレッドをブロックしない。
    // このクラスのインスタンスから foreach で取り出した各 item に対して
    // GetResult() を呼び出すと、対応するスレッドが終了するまで待機する。
    public class OrchThreadPoolImpl<TSource, TResult> : IDisposable, IEnumerable<OrchTask<TSource, TResult>>
    {
        private readonly OrchTask<TSource, TResult>[] _threads;

        private readonly SemaphoreSlim _semaphore;

        private OrchThreadPoolImpl(OrchTask<TSource, TResult>[] threads, SemaphoreSlim semaphore)
        {
            _threads = threads;
            _semaphore = semaphore;
        }

        // await Task.Yield() ではなく、SynchronizationContext を使ってメインスレッドに制御を渡す実装
        // semaphore は、スレッドリソースが枯渇しないようにして、スレッドを起こせる状態を維持するために必要
        // semaphore がないと、メインスレッドの GetResult() とデッドロックする可能性がある
        public static OrchThreadPoolImpl<TSource, TResult> RunForEach(IEnumerable<TSource> sources,
            Func<TSource, string> getPathFunc,
            Func<TSource, object> getTargetFunc,
            Func<TSource, TResult> getResultFunc, int maxDegreeOfParallelism = 4)
        {
            var threads = new OrchTask<TSource, TResult>[sources is ICollection<TSource> ? ((ICollection<TSource>)sources).Count : sources.Count()];
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var mainContext = SynchronizationContext.Current;

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new OrchTask<TSource, TResult>();
            }

            foreach (var (source, index) in sources.Select((source, index) => (source, index)))
            {
                Task.Run(async () =>
                {
                    await semaphore.WaitAsync(); // 並列度の制限を管理
                    try
                    {
                        var result = await Task.Run(() => getResultFunc(source));

                        // メインスレッドで SetResult() する
                        // これにより、一時的にメインスレッドに制御を渡せる
                        if (mainContext != null)
                        {
                            mainContext.Post(_ =>
                            {
                                threads[index].SetResult(source, result);
                            }, null);
                        }
                        else
                        {
                            threads[index].SetResult(source, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        threads[index].SetException(source, getPathFunc(source), getTargetFunc(source), ex);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }

            return new OrchThreadPoolImpl<TSource, TResult>(threads, semaphore);
        }

        public void Dispose()
        {
            foreach (var thread in _threads)
            {
                thread.Dispose(); // リソースの解放
            }
            GC.SuppressFinalize(this);
        }

        public IEnumerator<OrchTask<TSource, TResult>> GetEnumerator()
        {
            foreach (var thread in _threads)
            {
                yield return thread;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class OrchThreadPool
    {
        public static OrchThreadPoolImpl<TSource, TResult> RunForEach<TSource, TResult>(
            IEnumerable<TSource> sources,
            Func<TSource, string> getPathFunc,
            Func<TSource, object> getTargetFunc,
            Func<TSource, TResult> getResultFunc)
        {
            return OrchThreadPoolImpl<TSource, TResult>.RunForEach(sources, getPathFunc, getTargetFunc, getResultFunc);
        }
    }

    // 本当は、このクラスのコンストラクタを private にして
    // ForEach() をこのクラスの static method として持たせたい。
    // でも、そうしてしまうと、コンパイラによる型推論ができなくなってしまうのでとても不便。。
    // そのため、型推論を可能にするためのヘルパとして ParallelResults クラスを用意した。
    public class ParallelResult<TSource, TResult>(TSource source, TResult? result, Exception? ex)
    {
        public TSource Source { get; private set; } = source;
        public TResult? Result { get; private set; } = result;
        public Exception? Exception { get; private set; } = ex;
        public bool TryGetValue(out TResult? result)
        {
            if (Result != null)
            {
                result = Result;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
        public TResult? GetValue()
        {
            if (Exception == null)
            {
                return Result;
            }
            else
            {
                throw Exception;
            }
        }
    }

    // この ForEach は、すべてのスレッドが終了するまでブロックする。
    // 標準の Parallel.ForEach と同様だが、こちらは各スレッドの戻りを受け取れる。
    // ブロックしたくないときは、この代わりに OrchThreadPoolImpl.RunForEach() を使う。
    // コンパイラによる型推論ができるように、ForEach() は型パラメータをもたないクラスの
    // static method にしておく。
    //public static class ParallelResults
    //{
    //    public static ParallelResult<TSource, TResult>[] ForEach<TSource, TResult>(IEnumerable<TSource> sources, Func<TSource, TResult> forEachBody)
    //    {
    //        var resultsArray = new ParallelResult<TSource, TResult>[sources is ICollection<TSource> collection ? collection.Count : sources.Count()];
    //        using var cancelHandler = new ConsoleCancelHandler();
    //        var task = Task.Run(() =>
    //        {
    //            Parallel.ForEach(sources, new ParallelOptions { CancellationToken = cancelHandler.Token }, (source, state, index) =>
    //            {
    //                try
    //                {
    //                    var result = forEachBody(source);
    //                    resultsArray[index] = new ParallelResult<TSource, TResult>(source, result, null);
    //                }
    //                catch (OperationCanceledException)
    //                {
    //                    state.Break(); // This will stop processing cleanly once all active iterations complete.
    //                    throw;
    //                }
    //                catch (Exception ex)
    //                {
    //                    resultsArray[index] = new ParallelResult<TSource, TResult>(source, default, ex);
    //                }
    //            });
    //        }, cancelHandler.Token);

    //        try
    //        {
    //            task.Wait(cancelHandler.Token); // This will throw an exception if the task is canceled.
    //        }
    //        catch (AggregateException ae)
    //        {
    //            ae.Handle(e => e is OperationCanceledException); // Handle the cancellation exception
    //            throw new OperationCanceledException("The operation was canceled.", ae);
    //        }

    //        return resultsArray;
    //    }
    //}

    public static class ParallelResults
    {
        public static ParallelResult<TSource, TResult>[] ForEach<TSource, TResult>(IEnumerable<TSource> sources, Func<TSource, TResult> forEachBody)
        {
            var resultsArray = new ParallelResult<TSource, TResult>[sources is ICollection<TSource> collection ? collection.Count : sources.Count()];
            using var cancelHandler = new ConsoleCancelHandler();

            // コンカレントなスレッド数の上限を4に制限
            var semaphore = new SemaphoreSlim(4);

            var tasks = sources.Select((source, index) => Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancelHandler.Token);
                try
                {
                    var result = forEachBody(source);
                    resultsArray[index] = new ParallelResult<TSource, TResult>(source, result, null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    resultsArray[index] = new ParallelResult<TSource, TResult>(source, default, ex);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancelHandler.Token)).ToArray();

            try
            {
                Task.WhenAll(tasks).Wait(cancelHandler.Token); // This will throw an exception if the task is canceled.
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is OperationCanceledException); // Handle the cancellation exception
                throw new OperationCanceledException("The operation was canceled.", ae);
            }

            return resultsArray;
        }
    }

    // このクラスのインスタンスを保持する変数のスコープが終わると、自動でプログレスバーを破棄する。
    // この変数は、using を伴って定義することが必要。
    public class ProgressReporter(IWritableHost provider, int id, int totalNum, string activity, string statusDescription) : IDisposable
    {
        private IWritableHost? provider = provider;
        //private Cmdlet? cmdlet;
        private readonly ProgressRecord progressRecord = new(id, activity, statusDescription);
        private int totalNum = totalNum;

        public int TotalNum
        {
            get { return totalNum; }
            set { totalNum = value; }
        }

        private void WriteProgress()
        {
            provider?.WriteProgress(progressRecord);
        }

        public void WriteProgress(int index, string? statusDescription = null, string? activity = null)
        {
            progressRecord.PercentComplete = (index * 100) / totalNum;
            if (!string.IsNullOrEmpty(activity))
            {
                progressRecord.Activity = activity;
            }
            if (!string.IsNullOrEmpty(statusDescription))
            {
                progressRecord.StatusDescription = statusDescription;
            }
            WriteProgress();
        }

        private void CompleteProgress()
        {
            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress();
        }

        public void Dispose()
        {
            CompleteProgress();
            GC.SuppressFinalize(this); // Dispose が呼ばれた後はファイナライザを抑制
        }
    }
}
