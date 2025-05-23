using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Entities.JsonConverter;

namespace OrchProvider.MCP
{
    // JSON-RPC 2.0 request model
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";
        [JsonPropertyName("id")]
        public object? Id { get; set; }
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
        [JsonPropertyName("params")]
        public JsonElement Params { get; set; }
    }

    // JSON-RPC 2.0 response model
    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";
        [JsonPropertyName("id")]
        public object? Id { get; set; }
        [JsonPropertyName("result")]
        public object? Result { get; set; }
        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }

    public static class McpHost
    {
        public static BlockingCollection<string>? Queue = null;
        public static BlockingCollection<string>? ResponseQueue = null;
        public static bool executeImmediately = false;

        /// <summary>
        /// Starts the HTTP listener on the given prefix.
        /// </summary>
        public static void StartServer(string prefix, CancellationToken token)
        {
            //Console.CancelKeyPress += (s, e) => { e.Cancel = true; Console.WriteLine("[MCP] Ctrl+C ignored."); };
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            //Console.WriteLine($"[MCP] Listening on {prefix}");

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var ctx = await listener.GetContextAsync();
                    _ = HandleContextAsync(ctx);
                }
                listener.Stop();
            }, token);
        }

        private static async Task ReturnWithTemplateFile(HttpListenerRequest req, HttpListenerResponse resp, string templateFileName)
        {
            JsonDocument reqDoc, doc;

            // 1) リクエストから id を取得
            string reqJson;
            using (var sr = new StreamReader(req.InputStream, Encoding.UTF8))
                reqJson = await sr.ReadToEndAsync();
            reqDoc = JsonDocument.Parse(reqJson);
            string idRaw = reqDoc.RootElement.GetProperty("id").GetRawText();

            // 2) テンプレートを読み込み、{0} を置換
            var templatePath = Path.GetFullPath(Path.Combine("MCP", templateFileName));
            if (!File.Exists(templatePath))
            {
                resp.StatusCode = 500;
                await WriteJsonAsync(resp, new { error = "Initialize template not found.", path = templatePath });
                return;
            }
            string templateContent = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
            //string prettyJson = templateContent.Replace("\"id\":0", idRaw);

            string pattern = @"(""" + "id" + @"""\s*:\s*)(\d+)";
            string prettyJson = Regex.Replace(
                templateContent,
                pattern,
                m => $"{m.Groups[1].Value}{idRaw}"
            );
            doc = JsonDocument.Parse(prettyJson);
            string compactJson = JsonSerializer.Serialize(doc.RootElement);

            // 4) レスポンス書き込み
            byte[] respBytes = Encoding.UTF8.GetBytes(compactJson);
            resp.ContentType = "application/json; charset=utf-8";
            resp.ContentLength64 = respBytes.Length;
            await resp.OutputStream.WriteAsync(respBytes, 0, respBytes.Length);
            return;
        }

        /// <summary>
        /// Handles incoming HTTP requests and routes to JSON-RPC or REST endpoints.
        /// </summary>
        private static async Task HandleContextAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var resp = context.Response;
            try
            {
                if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/tools/call")
                {
                    var body = await new StreamReader(req.InputStream, req.ContentEncoding).ReadToEndAsync();
                    JsonRpcRequest rpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(body)!;

                    var name = rpcReq.Params.GetProperty("name").GetString();
                    var argsElement = rpcReq.Params.GetProperty("arguments");

                    // 即時実行は今のところサポートできない。。
                    if (argsElement.TryGetProperty("executeImmediately", out var executeImmediatelyElement))
                    {
                        executeImmediately = executeImmediatelyElement.GetBoolean();
                    }

                    string command = null;
                    if (name == "Execute-Script")
                    {
                        // 'script' プロパティを取り出して、直接コマンドとして使用
                        if (rpcReq.Params.TryGetProperty("arguments", out var execArgs) &&
                            execArgs.TryGetProperty("script", out var scriptProp) &&
                            scriptProp.ValueKind == JsonValueKind.String)   
                        {
                            command = scriptProp.GetString();
                        }
                        else
                        {
                            // 'script' が無い／文字列でない場合はエラー返却
                            var err = new
                            {
                                jsonrpc = "2.0",
                                id = rpcReq.Id,
                                error = new { code = -32602, message = "Missing or invalid 'script' parameter" }
                            };
                            var errJson = JsonSerializer.Serialize(err, JsonTools.jsoWhenWritingNull);
                            resp.ContentType = "application/json";
                            await resp.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errJson));
                            return;                           
                        }
                    }
                    //else if (name == "Get-Examples")
                    //{
                    //    // テンプレートを読み込み、{0} を置換
                    //    var templatePath = Path.GetFullPath(Path.Combine("MCP", "examples_list.json"));
                    //    if (!File.Exists(templatePath))
                    //    {
                    //        resp.StatusCode = 500;
                    //        await WriteJsonAsync(resp, new { error = "Initialize template not found.", path = templatePath });
                    //        return;
                    //    }

                    //    string templateContent = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

                    //    // id:0 を実際のリクエストIDに置換
                    //    string pattern = @"(""" + "id" + @"""\s*:\s*)(\d+)";
                    //    string responseJson = Regex.Replace(
                    //        templateContent,
                    //        pattern,
                    //        m => $"{m.Groups[1].Value}{rpcReq.Id}"
                    //    );

                    //    // MCPプロトコルに準拠した形式で返す
                    //    var mcpResponse = new
                    //    {
                    //        content = new[]
                    //        {
                    //            new
                    //            {
                    //                type = "text",
                    //                text = responseJson
                    //            }
                    //        }
                    //    };

                    //    await WriteJsonAsync(resp, mcpResponse);
                    //    return;
                    //}

                    // シンプル版
                    else if (name == "Get-Examples")
                    {
                        var templatePath = Path.GetFullPath(Path.Combine("MCP", "examples_list.json"));
                        if (!File.Exists(templatePath))
                        {
                            resp.StatusCode = 500;
                            await WriteJsonAsync(resp, new { error = "Examples template not found.", path = templatePath });
                            return;
                        }

                        try
                        {
                            string templateContent = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
                            var templateData = JsonSerializer.Deserialize<object>(templateContent);
                            await WriteJsonAsync(resp, templateData);
                        }
                        catch (Exception ex)
                        {
                            resp.StatusCode = 500;
                            await WriteJsonAsync(resp, new { error = "Failed to load examples template.", details = ex.Message });
                        }
                        return;
                    }

                    //else if (name == "Get-Examples")
                    //{
                    //    // 2) テンプレートを読み込み、{0} を置換
                    //    var templatePath = Path.GetFullPath(Path.Combine("MCP", "examples_list.json"));
                    //    if (!File.Exists(templatePath))
                    //    {
                    //        resp.StatusCode = 500;
                    //        await WriteJsonAsync(resp, new { error = "Initialize template not found.", path = templatePath });
                    //        return;
                    //    }
                    //    string templateContent = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
                    //    //string prettyJson = templateContent.Replace("\"id\":0", idRaw);

                    //    string pattern = @"(""" + "id" + @"""\s*:\s*)(\d+)";
                    //    string prettyJson = Regex.Replace(
                    //        templateContent,
                    //    pattern,
                    //        m => $"{m.Groups[1].Value}{rpcReq.Id}"
                    //    );
                    //    JsonDocument doc = JsonDocument.Parse(prettyJson);
                    //    string compactJson = JsonSerializer.Serialize(doc.RootElement);

                    //    // 4) レスポンス書き込み
                    //    byte[] respBytes = Encoding.UTF8.GetBytes(compactJson);
                    //    resp.ContentType = "application/json; charset=utf-8";
                    //    resp.ContentLength64 = respBytes.Length;
                    //    await resp.OutputStream.WriteAsync(respBytes, 0, respBytes.Length);
                    //    return;
                    //}
                    else
                    {
                        // Build command line dynamically
                        var sb = new StringBuilder(name);
                        foreach (var prop in argsElement.EnumerateObject())
                        {
                            var key = prop.Name;
                            var val = prop.Value;

                            switch (val.ValueKind)
                            {
                                case JsonValueKind.True:
                                    // Boolean switch: フラグのみ出力
                                    sb.Append(' ').Append(key);
                                    break;

                                case JsonValueKind.False:
                                    // false はスキップ
                                    break;

                                case JsonValueKind.Number:
                                    // 数値はそのまま出力（GetRawText() で元の文字列表現を取得）
                                    sb.Append(' ')
                                      .Append(key)
                                      .Append(' ')
                                      .Append(val.GetRawText());
                                    break;

                                case JsonValueKind.String:
                                    // 文字列はエスケープして引用符付きで出力
                                    //var raw = val.GetString()?.Replace("\"", "\\\"") ?? string.Empty;
                                    var raw = val.GetString() ?? string.Empty;
                                    sb.Append(' ')
                                      .Append(key)
                                      .Append(' ')
                                      .Append(raw);
                                    break;

                                default:
                                    // オブジェクトや配列、Null などは JSON 全体を文字列化して引用符付きで出力
                                    //var rawText = val.GetRawText().Replace("\"", "\\\"");
                                    var rawText = val.GetRawText();
                                    sb.Append(' ')
                                      .Append(key)
                                      .Append(' ')
                                      .Append(rawText);
                                    break;
                            }
                        }



                        command = sb.ToString();
                    }

                    // Enqueue and execute
                    Queue = [];

                    //Queue.Enqueue(command!);

                    JsonRpcResponse response = null;
                    if (!executeImmediately)
                    {
                        Queue.Add(command!);
                        Queue.CompleteAdding();
                        response = new JsonRpcResponse
                        {
                            Jsonrpc = "2.0",
                            Id = rpcReq.Id,
                            Result = new
                            {
                                content = new[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = "The command has been sent to the PowerShell console. Please instruct the user to press Enter there to execute it and view the results in the console."
                                    }
                                }
                            }
                        };
                    }
                    //else
                    //{
                    //    ResponseQueue = [];

                    //    // ResponseQueue から結果をすべて取り出し、content リストを構築
                    //    var results = OrchProvider.MCP.McpHost.ResponseQueue!
                    //        .GetConsumingEnumerable()    // CompleteAdding() が呼ばれるまでブロック
                    //        .Select(obj => new
                    //        {
                    //            type = "text",
                    //            text = obj
                    //        })
                    //        .ToArray();

                    //    // JSON-RPC レスポンスを組み立て
                    //    response = new JsonRpcResponse
                    //    {
                    //        Jsonrpc = "2.0",
                    //        Id = rpcReq.Id,
                    //        Result = new
                    //        {
                    //            content = results
                    //        }
                    //    };
                    //}

                    else
                    {
                        ResponseQueue = new BlockingCollection<string>();
                        Queue.Add(command!);
                        Queue.CompleteAdding();

                        // 短時間待機してPowerShell側の処理を確実にする
                        await Task.Delay(100);

                        var results = new List<object>();
                        var maxWaitTime = DateTime.Now.AddSeconds(30);

                        while (DateTime.Now < maxWaitTime)
                        {
                            if (ResponseQueue.IsCompleted)
                            {
                                // CompleteAdding()が呼ばれた後、残りの結果を取得
                                while (ResponseQueue.TryTake(out var result, 0))
                                {
                                    results.Add(new { type = "text", text = result });
                                }
                                break;
                            }

                            if (ResponseQueue.TryTake(out var item, 100))
                            {
                                results.Add(new { type = "text", text = item });
                            }
                        }

                        if (results.Count == 0)
                        {
                            results.Add(new { type = "text", text = "No response received" });
                        }

                        response = new JsonRpcResponse
                        {
                            Jsonrpc = "2.0",
                            Id = rpcReq.Id,
                            Result = new { content = results.ToArray() }
                        };
                    }


                    // ResponseQueue から結果をすべて取り出し、content リストを構築
                    //var results = ResponseQueue
                    //    .GetConsumingEnumerable()    // CompleteAdding までブロック
                    //    .Select(json => new { type = "json", text = json })
                    //    .ToArray();

                    // JSON - RPC レスポンスを組み立て
                    //var response = new JsonRpcResponse
                    //{
                    //    Jsonrpc = "2.0",
                    //    Id = rpcReq.Id,
                    //    Result = new
                    //    {
                    //        content = 
                    //    }
                    //};

#if false // ResponseQueue が ConcurrentQueue の実装
                    // ResponseQueue から結果をすべて取り出して content 配列を構築
                    // ResponseQueue からすべて取り出し
                    //var resultList = new List<object>();
                    //while (OrchProvider.MCP.McpHost.ResponseQueue.TryDequeue(out var respObj))
                    //{
                    //    resultList.Add(respObj);
                    //}

                    //// content 配列を作成
                    //var content = resultList
                    //    .Select(obj => new
                    //    {
                    //        type = "json",
                    //        text = obj
                    //    })
                    //    .ToArray();

                    //// JSON-RPC レスポンスを組み立て
                    //var response = new JsonRpcResponse
                    //{
                    //    Jsonrpc = "2.0",
                    //    Id = rpcReq.Id,
                    //    Result = new
                    //    {
                    //        content = content
                    //    }
                    //};
#endif

                    // 即時実行でない場合はこれ。
                    //var response = new JsonRpcResponse
                    //{
                    //    Jsonrpc = "2.0",
                    //    Id = rpcReq.Id,
                    //    Result = new
                    //    {
                    //        content = new[]
                    //        {
                    //            new
                    //            {
                    //                type = "text",
                    //                //text = "Command enqueued successfully."
                    //                text = "The command has been sent to the PowerShell console. Please check the execution results there."
                    //            }
                    //        }
                    //    }
                    //};

                    var json = JsonSerializer.Serialize(response, JsonTools.jsoWhenWritingNull);
                    resp.ContentType = "application/json";
                    await resp.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(json));
                    return;
                }

                if (req.HttpMethod == "POST" && 
                    (req.Url?.AbsolutePath == "/initialize" ||
                     req.Url?.AbsolutePath == "/tools/list" ||
                     req.Url?.AbsolutePath == "/resources/list" ||
                     req.Url?.AbsolutePath == "/prompts/list"))
                {
                    var templateFileName = req.Url?.AbsolutePath.Substring(1).Replace('/', '_') + ".json";
                    await ReturnWithTemplateFile(req, resp, templateFileName);
                    return;
                }

                // Not found
                resp.StatusCode = 404;
            }
            catch (JsonException jex)
            {
                // パース／バリデーションエラーは JSON-RPC エラー応答で返す
                Console.Error.WriteLine($"[MCP] Json Error: {jex}");
                resp.StatusCode = 500;
                await WriteJsonAsync(resp, new { error = "Internal Server Error", detail = jex.Message});
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MCP] Error: {ex}");
                resp.StatusCode = 500;
                await WriteJsonAsync(resp, new { error = "Internal Server Error", detail = ex.Message });
            }
            finally
            {
                resp.Close();
            }
        }

        /// <summary>
        /// Writes a JSON-RPC error response.
        /// </summary>
        private static Task WriteError(HttpListenerResponse resp, object? id, int code, string message)
        {
            resp.ContentType = "application/json";
            var err = new JsonRpcResponse { Jsonrpc = "2.0", Id = id, Error = new { code, message } };
            return WriteJsonAsync(resp, err);
        }

        /// <summary>
        /// Serializes an object to JSON and writes it to the response.
        /// </summary>
        private static Task WriteJsonAsync(HttpListenerResponse resp, object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            var buffer = Encoding.UTF8.GetBytes(json);
            resp.ContentType = "application/json";
            resp.ContentLength64 = buffer.Length;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            return Task.CompletedTask;
        }
    }
}
