using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.OrchAPI;

public partial class OrchAPISession : IDisposable
{
    private static string ConvertHeadersToOneLine(Dictionary<string, List<string>> headers)
    {
        var singleValueHeaders = headers.ToDictionary(
            kvp => kvp.Key,
            kvp => string.Join(", ", kvp.Value)
        );
        return JsonSerializer.Serialize(singleValueHeaders, JsonTools.jsoOneLine);
    }

    private static string? BuildCombinedLogBlock(DateTime reqTime, HttpRequestMessage message, DateTime resTime, HttpResponseMessage? response, int callId, LoggingLevel? currentLevel)
    {
        currentLevel ??= LoggingLevel.Info;

        bool outputSummary = false;
        bool outputHeader = false;
        bool outputUiPathHeader = false;
        bool outputBody = false;

        // エラーが起きていれば、必ず summary/header/body をすべて出力
        if (!response?.IsSuccessStatusCode ?? false)
        {
            outputSummary = true;
            outputHeader = true;
            outputBody = true;
        }
        else
        {
            switch (currentLevel)
            {
                case LoggingLevel.Error:
                    // エラーが発生していなければ何も出力しない
                    break;
                case LoggingLevel.Info:
                    outputSummary = true;
                    break;
                case LoggingLevel.Trace:
                    outputSummary = true;
                    outputUiPathHeader = true; // 特定のヘッダのみ出力
                    outputBody = true;
                    break;
                case LoggingLevel.Verbose:
                    outputSummary = true;
                    outputHeader = true;
                    outputBody = true;
                    break;
            }
        }


        if (!outputSummary) return null;

        var sb = new StringBuilder();

        // --------------------
        // リクエスト部分
        // --------------------
        if (outputSummary)
        {
            sb.AppendLineLf($"{reqTime:HH:mm:ss.fff} #{callId:D4} {message.Method} {message.RequestUri}");
        }

        if (outputHeader)
        {
            // 両方のヘッダーをマージする
            var mergedReqHeaders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in message.Headers)
            {
                mergedReqHeaders[header.Key] = new List<string>(header.Value);
            }
            if (message.Content != null)
            {
                foreach (var header in message.Content.Headers)
                {
                    if (mergedReqHeaders.TryGetValue(header.Key, out var list))
                    {
                        list.AddRange(header.Value);
                    }
                    else
                    {
                        mergedReqHeaders[header.Key] = new List<string>(header.Value);
                    }
                }
            }
            sb.Append("  REQ HEAD ");
            sb.AppendLineLf(ConvertHeadersToOneLine(mergedReqHeaders));
        }
        else if (outputUiPathHeader)
        {
            var filteredHeaders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // 通常のヘッダーから "UIPATH" を含むものを抽出
            foreach (var header in message.Headers)
            {
                if (header.Key.Contains("UIPATH", StringComparison.OrdinalIgnoreCase))
                {
                    filteredHeaders[header.Key] = new List<string>(header.Value);
                }
            }

            // コンテンツヘッダーからも抽出し、既存キーがあれば追加
            if (message.Content != null)
            {
                foreach (var header in message.Content.Headers)
                {
                    if (header.Key.Contains("UIPATH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (filteredHeaders.TryGetValue(header.Key, out var list))
                        {
                            list.AddRange(header.Value);
                        }
                        else
                        {
                            filteredHeaders[header.Key] = new List<string>(header.Value);
                        }
                    }
                }
            }

            if (filteredHeaders.Count > 0)
            {
                sb.Append("  REQ HEAD ");
                sb.AppendLineLf(ConvertHeadersToOneLine(filteredHeaders));
            }
        }

        if (outputBody && message.Content != null)
        {
            string reqBody = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(reqBody))
            {
                sb.Append("  REQ BODY ");
                sb.AppendLineLf(reqBody);
            }
        }

        // --------------------
        // レスポンス部分
        // --------------------
        if (response is not null)
        {
            if (outputSummary)
            {
                sb.AppendLineLf($"{resTime:HH:mm:ss.fff} RES Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            if (outputHeader)
            {
                // 両方のレスポンスヘッダーをマージする
                var mergedResHeaders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in response.Headers)
                {
                    mergedResHeaders[header.Key] = new List<string>(header.Value);
                }
                if (response.Content != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        if (mergedResHeaders.TryGetValue(header.Key, out var list))
                        {
                            list.AddRange(header.Value);
                        }
                        else
                        {
                            mergedResHeaders[header.Key] = new List<string>(header.Value);
                        }
                    }
                }
                sb.Append("  RES HEAD ");
                sb.AppendLineLf(ConvertHeadersToOneLine(mergedResHeaders));
            }

            if (outputBody && response?.Content != null)
            {
                // ContentType を取得。存在しない場合はテキストとみなす。
                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                bool isTextual = string.IsNullOrEmpty(mediaType) ||
                                 mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

                if (isTextual)
                {
                    string resBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(resBody))
                    {
                        sb.Append("  RES BODY ");
                        sb.AppendLineLf(resBody);
                    }
                }
            }
        }

        if (sb.Length != 0)
        {
            sb.AppendLineLf();
            return sb.ToString();
        }
        else
        {
            return null;
        }
    }

    private string SanitizeDriveName()
    {
        string driveName = _drive._psDrive.Name!;

        // ファイル名に使用できない文字をすべてアンダースコアに置換する
        foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
        {
            driveName = driveName.Replace(invalidChar, '_');
        }

        return driveName;
    }

    private string? _logFolderPath = null;
    public string GetLogFolderPath()
    {
        if (_logFolderPath == null)
        {
            string baseLogFolderPath = UiPath.PowerShell.Core.OrchProvider.GetLogFolderBasePath();
            _logFolderPath = System.IO.Path.Combine(baseLogFolderPath, SanitizeDriveName());
            Directory.CreateDirectory(_logFolderPath);
        }
        return _logFolderPath;
    }

    private string? _logFilePath = null;
    public string GetLogFilePath()
    {
        if (_logFilePath == null)
        {
            string logFolderPath = GetLogFolderPath();
            Directory.CreateDirectory(logFolderPath);

            string fileName = $"{DateTime.Today:yyyy-MM-dd}_{SanitizeDriveName()}.log";
            _logFilePath = System.IO.Path.Combine(logFolderPath, fileName);
        }
        return _logFilePath;
    }


    // 新しい非同期ログシステム
    private AsyncLogWriter? _asyncLogWriter;

    private AsyncLogWriter GetAsyncLogWriter()
    {
        if (_asyncLogWriter == null)
        {
            lock (this)
            {
                _asyncLogWriter ??= new AsyncLogWriter(GetLogFilePath());
            }
        }
        return _asyncLogWriter;
    }

    // 同期版
    //private void WriteLogBlock(string? logBlock)
    //{
    //    if (string.IsNullOrEmpty(logBlock))
    //        return;

    //    // 非同期ログライターを使用（ノンブロッキング）
    //    GetAsyncLogWriter().Write(logBlock);
    //}

    private async ValueTask WriteLogBlockAsync(string? logBlock, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(logBlock)) 
            return;

        await GetAsyncLogWriter().WriteAsync(logBlock, cancellationToken);
    }

    // ログ統計情報を取得
    public LogStatistics GetLogStatistics()
    {
        return _asyncLogWriter?.GetStatistics() ?? default;
    }

    partial void DisposeAsyncLogWriter()
    {
        try
        {
            _asyncLogWriter?.Dispose();
        }
        catch (Exception ex)
        {
            // ログ処理のエラーは無視（再帰的なログ書き込みを避けるため）
            System.Diagnostics.Debug.WriteLine($"Log writer disposal error: {ex}");
        }
    }
}
