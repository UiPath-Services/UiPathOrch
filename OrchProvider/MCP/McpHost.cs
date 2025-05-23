using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UiPath.PowerShell.Positional;

namespace OrchProvider.MCP
{
    public record JsonRpcRequest(
        [property: JsonPropertyName("id")] object? Id,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("params")] JsonElement Params,
        [property: JsonPropertyName("jsonrpc")] string Jsonrpc = "2.0"
    );

    public record JsonRpcResponse(
        [property: JsonPropertyName("id")] object? Id,
        [property: JsonPropertyName("result")] object? Result = null,
        [property: JsonPropertyName("error")] object? Error = null,
        [property: JsonPropertyName("jsonrpc")] string Jsonrpc = "2.0"
    );

    public static class McpHost
    {
        private static readonly Regex IdRegex = new(@"(""id""\s*:\s*)(\d+)", RegexOptions.Compiled);
        public static string? insertCommand = null;
        public static string? executeCommand = null;
        public static string? outputFromCommand = null;

        public static void StartServer(string prefix, CancellationToken token)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

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

        private static async Task HandleContextAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;
            try
            {
                if (req.HttpMethod == "POST")
                {
                    var path = req.Url?.AbsolutePath;
                    if (path == "/tools/call")
                    {
                        await ProcessRpcCall(req, resp);
                        return;
                    }
                    if (path is "/initialize" or "/tools/list" or "/resources/list" or "/prompts/list")
                    {
                        var template = path[1..].Replace('/', '_') + ".json";
                        await ReturnTemplate(req, resp, template);
                        return;
                    }
                }

                resp.StatusCode = 404;
            }
            catch (JsonException jex)
            {
                await WriteJson(resp, new { error = "JSON parse error", detail = jex.Message });
            }
            catch (Exception ex)
            {
                await WriteJson(resp, new { error = "Internal Server Error", detail = ex.Message });
            }
            finally
            {
                resp.Close();
            }
        }

        private static async Task ProcessRpcCall(HttpListenerRequest req, HttpListenerResponse resp)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            var rpc = JsonSerializer.Deserialize<JsonRpcRequest>(body)
                      ?? throw new JsonException("Invalid RPC payload");

            var ps = rpc.Params;
            var cmdName = ps.GetProperty("name").GetString()!;

            bool executeImmediately = rpc.Params.TryGetProperty("arguments", out var args) &&
                                args.TryGetProperty("executeImmediately", out var exeFlag) &&
                                exeFlag.GetBoolean();
            string command;
            switch (cmdName)
            {
                case "Execute-Script":
                    command = args.GetProperty("script").GetString()!
                              ?? throw new JsonException("Missing 'script'");
                    break;
                case "Get-Examples":
                    await ReturnTemplate(req, resp, "examples_list.json");
                    return;
                default:
                    command = BuildCommand(rpc.Method, args);
                    break;
            }

            JsonRpcResponse response;
            if (!executeImmediately)
            {
                insertCommand = command;
                response = new JsonRpcResponse(
                    Jsonrpc: rpc.Jsonrpc,
                    Id:      rpc.Id,
                    Result:  CreateContentResponse("Command enqueued. Press Enter in console.")
                );
            }
            else
            {
                outputFromCommand = null;
                executeCommand = command;
                response = await CollectImmediateResponse(rpc);
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            resp.ContentType = "application/json";
            await resp.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(json));
        }

        private static string BuildCommand(string name, JsonElement args)
        {
            var sb = new StringBuilder(name);
            foreach (var prop in args.EnumerateObject())
            {
                if (prop.NameEquals("executeImmediately")) continue;
                sb.Append(' ').Append(prop.Name);
                if (prop.Value.ValueKind is JsonValueKind.False) continue;
                sb.Append(' ');
                sb.Append(prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    _ => prop.Value.GetRawText()
                });
            }
            return sb.ToString();
        }

        private static async Task<JsonRpcResponse> CollectImmediateResponse(JsonRpcRequest rpc)
        {
            // タイムアウト時刻を設定
            var timeout = DateTime.UtcNow.AddSeconds(30);

            // outputForCmdlet がセットされるまで、またはタイムアウトまで待機
            while (outputFromCommand == null && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }

            // 最終的なテキストを決定
            var text = outputFromCommand ?? "Timeout. No response received";

            // JSON-RPC レスポンス用の content 配列
            var content = new[]
            {
                new { type = "text", text }
            };

            // content 配列を組み立て
            var resultObj = new
            {
                content = new[]
                {
                    new { type = "text", text }
                }
            };

            return new JsonRpcResponse(
                Jsonrpc: rpc.Jsonrpc,
                Id: rpc.Id,
                Result: resultObj
            );
        }

        private static async Task ReturnTemplate(HttpListenerRequest req, HttpListenerResponse resp, string templateFile)
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine("MCP", templateFile));
            if (!File.Exists(path))
            {
                resp.StatusCode = 500;
                await WriteJson(resp, new { error = "Template not found.", path });
                return;
            }

            var content = await File.ReadAllTextAsync(path, Encoding.UTF8);
            if (templateFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !templateFile.Equals("examples_list.json", StringComparison.OrdinalIgnoreCase))
            {
                var id = JsonDocument.Parse(await new StreamReader(req.InputStream).ReadToEndAsync())
                             .RootElement.GetProperty("id").GetRawText();
                content = IdRegex.Replace(content, m => m.Groups[1].Value + id);
            }

            await WriteJson(resp, JsonSerializer.Deserialize<object>(content) ?? "");
        }

        private static Task WriteJson(HttpListenerResponse resp, object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            var data = Encoding.UTF8.GetBytes(json);
            resp.ContentType = "application/json";
            resp.ContentLength64 = data.Length;
            return resp.OutputStream.WriteAsync(data, 0, data.Length);
        }

        private static object CreateContentResponse(string message)
            => new { content = new[] { new { type = "text", text = message } } };
    }
}
