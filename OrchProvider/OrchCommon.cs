using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Commands;

namespace UiPath.PowerShell.Core;

public static class PathTools
{
    // Use this when displaying candidates in a cmdlet completer.
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

        if (input != ret || input.Contains(' ') || input.Contains(',') || input.Contains(Path.DirectorySeparatorChar) || input.Contains('\'') || input.Contains('"')) //★★★
            //ret = '\'' + ret + '\'';
            ret = $"'{ret}'";

        return ret;
    }

    // Use this when displaying candidates in a cmdlet completer.
    public static string EscapeNonWildcardText(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "''";
        return "'" + input.Replace("'", "''") + "'";
    }

    // Use this when calling WriteItemObject() in the provider's GetChildItems.
    // Characters like [ and ] seem to be handled automatically. * and ? are presumably not expected in file names.
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

    // Used in combination with OrchDriveInfo.ExpandLocalPath()
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

public class ConsoleCancelHandler : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly ConsoleCancelEventHandler _handler;
    private bool _cancelKeyPressed;

    // Property indicating whether the cancel key was pressed
    public bool CancelKeyPressed => _cancelKeyPressed;

    // Property exposing the CancellationTokenSource
    public CancellationToken Token => _cts.Token;

    // Default constructor
    public ConsoleCancelHandler()
    {
        _cts = new CancellationTokenSource();
        _handler = CreateHandler(null);
        Console.CancelKeyPress += _handler;
    }

    // Constructor that accepts a delegate for cancellation handling
    public ConsoleCancelHandler(Action onCancel)
    {
        _cts = new CancellationTokenSource();
        _handler = CreateHandler(onCancel);
        Console.CancelKeyPress += _handler;
    }

    // Common initialization logic
    private ConsoleCancelEventHandler CreateHandler(Action? onCancel)
    {
        return (sender, args) =>
        {
            _cancelKeyPressed = true;
            try
            {
                onCancel?.Invoke();
            }
            finally
            {
                _cts.Cancel(); // Ensure the token is cancelled
                args.Cancel = true; // Prevent the process from terminating
            }
        };
    }

    // Dispose pattern implementation
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
    private TSource? _source;
    public TSource Source => _source ?? throw new InvalidOperationException("Call GetResult() before accessing Source.");
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
        _source = source;
        Result = result;
        CompletedEvent.Set(); // Signal that processing is complete
    }

    public void SetException(TSource source, string path, object target, Exception ex)
    {
        _source = source;
        Path = path;
        Target = target;
        Exception = ex;
        CompletedEvent.Set(); // Signal that processing is complete
    }

    public TResult? GetResult(CancellationToken token)
    {
        CompletedEvent.Wait(token); // Wait until the thread result is set

        if (Exception is not null)
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

// RunForEach() of this class does not block the main thread.
// When calling GetResult() on each item retrieved via foreach from an instance of this class,
// it waits until the corresponding thread has finished.
public class OrchThreadPoolImpl<TSource, TResult> : IDisposable, IEnumerable<OrchTask<TSource, TResult>>
{
    private readonly OrchTask<TSource, TResult>[] _threads;

    private readonly SemaphoreSlim _semaphore;

    private OrchThreadPoolImpl(OrchTask<TSource, TResult>[] threads, SemaphoreSlim semaphore)
    {
        _threads = threads;
        _semaphore = semaphore;
    }

    // Factory method that creates an instance from an externally provided OrchTask array
    public static OrchThreadPoolImpl<TSource, TResult> CreateInstance(
        OrchTask<TSource, TResult>[] threads, SemaphoreSlim semaphore)
    {
        return new OrchThreadPoolImpl<TSource, TResult>(threads, semaphore);
    }

    // Uses SynchronizationContext instead of await Task.Yield() to yield control to the main thread.
    // The semaphore is needed to prevent thread resource exhaustion and keep threads available.
    // Without the semaphore, there is a risk of deadlock with the main thread's GetResult().
    public static OrchThreadPoolImpl<TSource, TResult> RunForEach(IEnumerable<TSource> sources,
        Func<TSource, string> getPathFunc,
        Func<TSource, object> getTargetFunc,
        Func<TSource, TResult> getResultFunc, int maxDegreeOfParallelism = 4)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        var threads = new OrchTask<TSource, TResult>[srcList.Count];
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var mainContext = SynchronizationContext.Current;

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new OrchTask<TSource, TResult>();
        }

        foreach (var (source, index) in srcList.Select((source, index) => (source, index)))
        {
            Task.Run(async () =>
            {
                await semaphore.WaitAsync(); // Manage concurrency limits
                try
                {
                    var result = getResultFunc(source);

                    // Call SetResult() on the main thread.
                    // This temporarily yields control to the main thread.
                    if (mainContext is not null)
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
            thread.Dispose(); // Release resources
        }
        _semaphore.Dispose();
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

// planned to be deprecated
public static class ParallelResults
{
    public static ParallelResult<TSource, TResult>[] ForEach<TSource, TResult>(IEnumerable<TSource> sources, Func<TSource, TResult> forEachBody)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        var resultsArray = new ParallelResult<TSource, TResult>[srcList.Count];
        using var cancelHandler = new ConsoleCancelHandler();

        // Limit the maximum number of concurrent threads to 4
        using var semaphore = new SemaphoreSlim(4);

        var tasks = srcList.Select((source, index) => Task.Run(async () =>
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

/// <summary>
/// Holds a <typeparamref name="TItem"/> and its associated <typeparamref name="TSource"/>.<br/>
/// - Can be used directly as the item (e.g., bucket.Name) via implicit conversion.<br/>
/// - The original data is accessible via bucket.Source.
/// </summary>
public sealed class WithSource<TSource, TItem>
{
    public TSource Source { get; }
    public TItem Item { get; }

    public WithSource(TSource source, TItem item)
    {
        Source = source;
        Item = item;
    }

    /// <summary>
    /// Provides implicit conversion from <c>WithSource</c> to <typeparamref name="TItem"/>
    /// so it can be used directly in LINQ as <c>b.Name</c>.
    /// </summary>
    public static implicit operator TItem(WithSource<TSource, TItem> self) => self.Item;

    /// <summary>
    /// Allows pattern-matching deconstruction like <code>var (source, item) = withSource;</code>.
    /// </summary>
    public void Deconstruct(out TSource source, out TItem item)
    {
        source = Source;
        item = Item;
    }

    public override string ToString() => Item?.ToString() ?? string.Empty;
}

/// <summary>
/// Result container used internally by ForEach. Not exposed externally, but can be public if needed.
/// planned to be deprecated
/// Should be refactored to use ParallelResult3.
/// </summary>
public sealed class ParallelResult<TSource, TResult>
{
    public TSource Source { get; }
    public TResult? Result { get; }
    public Exception? Error { get; }

    public ParallelResult(TSource source, TResult? result, Exception? error)
    {
        Source = source;
        Result = result;
        Error = error;
    }
}

public sealed class SourceGroup<TSource, TItem> : IEnumerable<TItem>
{
    public TSource Source { get; }
    private readonly IReadOnlyList<WithSource<TSource, TItem>> _items;

    public SourceGroup(TSource source, IReadOnlyList<WithSource<TSource, TItem>> items)
    {
        Source = source;
        _items = items;
    }

    public IEnumerator<TItem> GetEnumerator() => _items.Select(x => x.Item).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<WithSource<TSource, TItem>> WithSourceItems => _items;
}

/// <summary>
/// Utility that executes in parallel for each source and returns results grouped by source.
/// </summary>
public static class ParallelResults3
{
    public static IEnumerable<SourceGroup<TSource, TItem>> GroupBy<TSource, TItem>(
        IEnumerable<TSource> sources,
        Func<TSource, IEnumerable<TItem>?> selector,
        int maxDegreeOfParallelism = 4,
        CancellationToken token = default)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        int count = srcList.Count;

        var slotArr = new List<WithSource<TSource, TItem>>[count];
        for (int i = 0; i < count; i++) slotArr[i] = [];

        using var sem = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(async () =>
            {
                await sem.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var src = srcList[idx];
                    var items = selector(src);
                    if (items != null)
                    {
                        var slot = slotArr[idx];
                        foreach (var it in items)
                            slot.Add(new WithSource<TSource, TItem>(src, it));
                    }
                }
                finally { sem.Release(); }
            }, token);
        }

        Task.WaitAll(tasks, token);

        for (int i = 0; i < count; i++)
        {
            yield return new SourceGroup<TSource, TItem>(srcList[i], slotArr[i]);
        }
    }

    public static List<WithSource<TSource, TItem>> ForEach<TSource, TItem>(
            IEnumerable<TSource> sources,
            Func<TSource, TItem?> selector,
            int maxDegreeOfParallelism = 4,
            CancellationToken token = default)
            where TItem : class
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        int count = srcList.Count;

        var results = new List<WithSource<TSource, TItem>>[count];
        for (int i = 0; i < count; i++) results[i] = [];

        using var sem = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(async () =>
            {
                await sem.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var src = srcList[idx];
                    var item = selector(src);
                    if (item != null)
                    {
                        results[idx].Add(new WithSource<TSource, TItem>(src, item));
                    }
                }
                finally { sem.Release(); }
            }, token);
        }

        Task.WaitAll(tasks, token);

        return results.SelectMany(x => x).ToList();
    }
}

// When the variable holding an instance of this class goes out of scope, the progress bar is automatically disposed.
// This variable must be declared with a using statement.
public class ProgressReporter(IWritableHost provider, int id, int totalNum, string activity) : IDisposable
{
    private IWritableHost? provider = provider;
    //private Cmdlet? cmdlet;
    private readonly ProgressRecord progressRecord = new(id, activity, activity);
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
        progressRecord.PercentComplete = (index * 100) / this.totalNum;
        if (!string.IsNullOrEmpty(activity))
        {
            progressRecord.Activity = activity;
        }
        progressRecord.StatusDescription = $"{index:D}/{totalNum} {statusDescription}".TrimEnd();
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
        GC.SuppressFinalize(this); // Suppress the finalizer after Dispose has been called
    }
}
