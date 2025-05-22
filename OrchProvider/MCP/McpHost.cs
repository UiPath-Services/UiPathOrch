using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Net;
using System.Security.Policy;
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
        public static readonly ConcurrentQueue<string> Queue = new();

        /// <summary>
        /// Starts the HTTP listener on the given prefix.
        /// </summary>
        public static void StartServer(string prefix, CancellationToken token)
        {
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; Console.WriteLine("[MCP] Ctrl+C ignored."); };
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            Console.WriteLine($"[MCP] Listening on {prefix}");

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
                    JsonRpcRequest rpcReq;
                    try
                    {
                        rpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(body)!;
                    }
                    catch
                    {
                        await WriteError(resp, null, -32700, "Parse error");
                        return;
                    }

                    var name = rpcReq.Params.GetProperty("name").GetString()!;
                    var argsElement = rpcReq.Params.GetProperty("arguments");

                    string command = null;
                    if (name == "Execute-Script")
                    {
                        // 'script' プロパティを取り出して、直接コマンドとして使用
                        if (rpcReq.Params.TryGetProperty("arguments", out var execArgs) &&
                            execArgs.TryGetProperty("script", out var scriptProp) &&
                            scriptProp.ValueKind == JsonValueKind.String)
                        {
                            command = scriptProp.GetString()!;
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
                    else
                    {
                        // Build command line dynamically
                        var sb = new StringBuilder(name);
                        foreach (var prop in argsElement.EnumerateObject())
                        {
                            var key = prop.Name;
                            var val = prop.Value;
                            if (val.ValueKind == JsonValueKind.True)
                            {
                                // Boolean switch, include flag only
                                sb.Append(' ').Append(key);
                            }
                            else if (val.ValueKind == JsonValueKind.False)
                            {
                                // skip false flags
                                continue;
                            }
                            else
                            {
                                // Key and quoted value
                                var raw = val.GetString()?.Replace("\"", "\\\"") ?? string.Empty;
                                sb.Append(' ').Append(key).Append(' ').Append('"').Append(raw).Append('"');
                            }
                        }
                        command = sb.ToString();
                    }

                    // Enqueue and execute
                    Queue.Enqueue(command);

                    var response = new JsonRpcResponse
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
                                    //text = "Command enqueued successfully."
                                    text = "The command has been sent to the PowerShell console. Please check the execution results there."
                                }
                            }
                        }
                    };

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

                    // 1) リクエストから id を取得
                    string reqJson;
                    using (var sr = new StreamReader(req.InputStream, Encoding.UTF8))
                        reqJson = await sr.ReadToEndAsync();
                    using var reqDoc = JsonDocument.Parse(reqJson);
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

                    // 3) コンパクト１行 JSON に変換
                    using var doc = JsonDocument.Parse(prettyJson);
                    string compactJson = JsonSerializer.Serialize(doc.RootElement);

                    // 4) レスポンス書き込み
                    byte[] respBytes = Encoding.UTF8.GetBytes(compactJson);
                    resp.ContentType = "application/json; charset=utf-8";
                    resp.ContentLength64 = respBytes.Length;
                    await resp.OutputStream.WriteAsync(respBytes, 0, respBytes.Length);
                    return;
                }

                // Not found
                resp.StatusCode = 404;
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
