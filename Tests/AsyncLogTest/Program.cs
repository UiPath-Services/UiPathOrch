using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using UiPath.OrchAPI;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== AsyncLogWriter High Volume Test Suite ===");
        Console.WriteLine("Select test to run:");
        Console.WriteLine("1. Basic test (100 messages)");
        Console.WriteLine("2. High volume test (10,000 messages)");
        Console.WriteLine("3. Concurrent test (10 parallel tasks)");
        Console.WriteLine("4. Stress test (100,000 messages)");
        //Console.WriteLine("5. Synchronous write test (1,000 messages)");
        //Console.WriteLine("6. Mixed sync/async test");
        //Console.WriteLine("7. Performance comparison (sync vs async)");
        Console.WriteLine("8. All tests");
        Console.WriteLine("Enter choice (1-8): ");

        var choice = Console.ReadLine();

        try
        {
            switch (choice)
            {
                case "1":
                    await RunBasicTest();
                    break;
                case "2":
                    await RunHighVolumeTest();
                    break;
                case "3":
                    await RunConcurrentTest();
                    break;
                case "4":
                    await RunStressTest();
                    break;
                case "5":
                    await RunSynchronousTest();
                    break;
                case "6":
                    await RunMixedSyncAsyncTest();
                    break;
                case "7":
                    await RunPerformanceComparisonTest();
                    break;
                case "8":
                default:
                    await RunAllTests();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task RunBasicTest()
    {
        Console.WriteLine("\n=== Basic Test (100 messages) ===");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"basic_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 1000);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                await logWriter.WriteAsync($"Test message {i} at {DateTime.Now:HH:mm:ss.fff}\n");
            }

            stopwatch.Stop();
            await Task.Delay(1000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {100 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunHighVolumeTest()
    {
        Console.WriteLine("\n=== High Volume Test (10,000 messages) ===");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"high_volume_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 20000, batchSize: 500);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 10000; i++)
            {
                await logWriter.WriteAsync($"High volume message {i} with timestamp {DateTime.Now:HH:mm:ss.fff} and some additional data\n");

                if (i % 1000 == 0)
                {
                    Console.WriteLine($"  Written {i} messages");
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Waiting for all messages to be flushed...");
            await Task.Delay(3000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {10000 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
            Console.WriteLine($"  Avg Batch Size: {stats.AverageEntriesPerBatch:F1}");

            if (File.Exists(testLogPath))
            {
                var fileInfo = new FileInfo(testLogPath);
                Console.WriteLine($"  File Size: {fileInfo.Length:N0} bytes");
            }
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunConcurrentTest()
    {
        Console.WriteLine("\n=== Concurrent Test (10 parallel tasks, 1,000 messages each) ===");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"concurrent_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 50000, batchSize: 1000);

            var stopwatch = Stopwatch.StartNew();

            // 10個の並列タスクを作成
            var tasks = Enumerable.Range(0, 10).Select(async taskId =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    await logWriter.WriteAsync($"Concurrent Task{taskId:D2} Message{i:D4} at {DateTime.Now:HH:mm:ss.fff}\n");
                }
                Console.WriteLine($"  Task {taskId} completed");
            }).ToArray();

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine("Waiting for all messages to be flushed...");
            await Task.Delay(5000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {10000 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
            Console.WriteLine($"  Avg Batch Size: {stats.AverageEntriesPerBatch:F1}");
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunStressTest()
    {
        Console.WriteLine("\n=== Stress Test (100,000 messages) ===");
        Console.WriteLine("This may take a while...");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"stress_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 100000, batchSize: 2000);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                await logWriter.WriteAsync($"Stress test message {i} at {DateTime.Now:HH:mm:ss.fff} with additional content for realistic size\n");

                if (i % 10000 == 0)
                {
                    Console.WriteLine($"  Written {i} messages");
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Waiting for all messages to be flushed...");
            await Task.Delay(10000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {100000 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
            Console.WriteLine($"  Avg Batch Size: {stats.AverageEntriesPerBatch:F1}");

            if (File.Exists(testLogPath))
            {
                var fileInfo = new FileInfo(testLogPath);
                Console.WriteLine($"  File Size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0 / 1024.0:F1} MB)");
            }
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunSynchronousTest()
    {
        Console.WriteLine("\n=== Synchronous Write Test (1,000 messages) ===");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"sync_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 5000, batchSize: 100);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                // 同期メソッドWrite()を使用
                //                logWriter.Write($"Sync message {i} at {DateTime.Now:HH:mm:ss.fff}\n");

                if (i % 100 == 0)
                {
                    Console.WriteLine($"  Written {i} sync messages");
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Waiting for all messages to be flushed...");
            await Task.Delay(2000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {1000 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
            Console.WriteLine($"  Avg Batch Size: {stats.AverageEntriesPerBatch:F1}");

            if (File.Exists(testLogPath))
            {
                var fileInfo = new FileInfo(testLogPath);
                Console.WriteLine($"  File Size: {fileInfo.Length:N0} bytes");
            }
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunMixedSyncAsyncTest()
    {
        Console.WriteLine("\n=== Mixed Sync/Async Test (500 sync + 500 async messages) ===");

        var testLogPath = Path.Combine(Path.GetTempPath(), $"mixed_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Console.WriteLine($"Log file: {testLogPath}");

        try
        {
            using var logWriter = new AsyncLogWriter(testLogPath, maxQueueSize: 5000, batchSize: 200);

            var stopwatch = Stopwatch.StartNew();

            // 同期と非同期を交互に実行
            for (int i = 0; i < 500; i++)
            {
                // 同期書き込み
                //logWriter.Write($"Sync message {i} at {DateTime.Now:HH:mm:ss.fff}\n");

                // 非同期書き込み
                await logWriter.WriteAsync($"Async message {i} at {DateTime.Now:HH:mm:ss.fff}\n");

                if (i % 50 == 0)
                {
                    Console.WriteLine($"  Written {i * 2} mixed messages");
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Waiting for all messages to be flushed...");
            await Task.Delay(2000); // フラッシュ待機

            var stats = logWriter.GetStatistics();
            Console.WriteLine($"Results:");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {1000 * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {stats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {stats.DroppedEntries}");
            Console.WriteLine($"  Batches: {stats.BatchesWritten}");
            Console.WriteLine($"  Avg Batch Size: {stats.AverageEntriesPerBatch:F1}");
        }
        finally
        {
            if (File.Exists(testLogPath))
                File.Delete(testLogPath);
        }
    }

    static async Task RunPerformanceComparisonTest()
    {
        Console.WriteLine("\n=== Performance Comparison Test (Sync vs Async) ===");

        const int messageCount = 2000;

        // 同期テスト
        Console.WriteLine($"\nTesting {messageCount} synchronous writes...");
        var syncLogPath = Path.Combine(Path.GetTempPath(), $"perf_sync_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        try
        {
            using var syncLogWriter = new AsyncLogWriter(syncLogPath, maxQueueSize: 10000, batchSize: 100);

            var syncStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                //syncLogWriter.Write($"Sync perf test message {i} at {DateTime.Now:HH:mm:ss.fff}\n");
            }
            syncStopwatch.Stop();
            await Task.Delay(2000); // フラッシュ待機

            var syncStats = syncLogWriter.GetStatistics();

            Console.WriteLine($"Sync Results:");
            Console.WriteLine($"  Time: {syncStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {messageCount * 1000.0 / syncStopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {syncStats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {syncStats.DroppedEntries}");
        }
        finally
        {
            if (File.Exists(syncLogPath))
                File.Delete(syncLogPath);
        }

        // 非同期テスト
        Console.WriteLine($"\nTesting {messageCount} asynchronous writes...");
        var asyncLogPath = Path.Combine(Path.GetTempPath(), $"perf_async_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        try
        {
            using var asyncLogWriter = new AsyncLogWriter(asyncLogPath, maxQueueSize: 10000, batchSize: 100);

            var asyncStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                await asyncLogWriter.WriteAsync($"Async perf test message {i} at {DateTime.Now:HH:mm:ss.fff}\n");
            }
            asyncStopwatch.Stop();
            await Task.Delay(2000); // フラッシュ待機

            var asyncStats = asyncLogWriter.GetStatistics();

            Console.WriteLine($"Async Results:");
            Console.WriteLine($"  Time: {asyncStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Messages/sec: {messageCount * 1000.0 / asyncStopwatch.ElapsedMilliseconds:F0}");
            Console.WriteLine($"  Total Entries: {asyncStats.TotalEntriesWritten}");
            Console.WriteLine($"  Dropped: {asyncStats.DroppedEntries}");
        }
        finally
        {
            if (File.Exists(asyncLogPath))
                File.Delete(asyncLogPath);
        }
    }

    static async Task RunAllTests()
    {
        Console.WriteLine("\n=== Running All Tests ===");

        await RunBasicTest();
        await RunHighVolumeTest();
        await RunConcurrentTest();
        await RunSynchronousTest();
        await RunMixedSyncAsyncTest();
        await RunPerformanceComparisonTest();
        // Skip stress test in "all tests" mode as it takes too long
        Console.WriteLine("\nNote: Stress test skipped in 'all tests' mode. Run individually if needed.");

        Console.WriteLine("\n=== All Tests Completed ===");
    }
}
