using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace OrchProvider.MCP;

public static class McpHost
{
    public static ConcurrentQueue<string> Queue { get; } = new();

    private static readonly string[] RequiredProperties = ["script"];

    public class InvokeRequest
    {
        public string script { get; set; } = string.Empty;
    }

    public static void StartServer(string prefix, CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        _ = Task.Run(() =>
        {
            //while (!token.IsCancellationRequested)
            while (true)
            {
                    try
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var response = context.Response;

                    if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/manifest")
                    {
                        WriteJson(response, new
                        {
                            name = "UiPathOrch MCP Server",
                            description = "Execute UiPathOrch PowerShell cmdlets via MCP",
                            version = "0.1.0",
                            tools = new[]
                            {
                                new { name = "Get-OrchProcess", description = "Gets processes" },
                                new { name = "Get-OrchPackage", description = "Gets packages" },
                                new { name = "Get-OrchAsset", description = "Gets assets" },
                                new { name = "Get-OrchBucket", description = "Gets storage buckets" },
                            }
                        });
                        continue;
                    }

                    //if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/schema")
                    //{
                    //    WriteJson(response, new
                    //    {
                    //        type = "object",
                    //        properties = new
                    //        {
                    //            script = new
                    //            {
                    //                type = "string",
                    //                description = "PowerShell command to insert into prompt"
                    //            }
                    //        },
                    //        required = RequiredProperties
                    //    });
                    //    continue;
                    //}

                    if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/scheme")
                    {
                        var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                        var tool = query["tool"];
                        if (tool != "Execute-Script")
                        {
                            response.StatusCode = 404;
                            WriteJson(response, new { error = $"Unknown tool: {tool}" });
                            continue;
                        }

                        WriteJson(response, new
                        {
                            type = "object",
                            properties = new
                            {
                                command = new { type = "string", description = "One-line PowerShell pipeline to execute" }
                            },
                            required = RequiredProperties
                        });
                        continue;
                    }

                    // ループ内の /invoke ハンドラ例
                    if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/invoke")
                    {
                        string body;
                        try
                        {
                            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                            body = reader.ReadToEnd();
                            InvokeRequest invokeRequest = JsonSerializer.Deserialize<InvokeRequest>(body)
                                                           ?? throw new JsonException("Empty payload.");

                            if (string.IsNullOrWhiteSpace(invokeRequest.script))
                                throw new ArgumentException("Missing 'script' field.");

                            // スレッドセーフなキューにスクリプトを登録
                            Queue.Enqueue(invokeRequest.script);

                            // 結果を返す
                            response.StatusCode = 200;
                            response.ContentType = "application/json; charset=utf-8";
                            WriteJson(response, new { status = "queued" });
                        }
                        catch (JsonException je)
                        {
                            response.StatusCode = 400;
                            response.ContentType = "application/json; charset=utf-8";
                            WriteJson(response, new { error = "Invalid JSON", detail = je.Message });
                        }
                        catch (ArgumentException ae)
                        {
                            response.StatusCode = 400;
                            response.ContentType = "application/json; charset=utf-8";
                            WriteJson(response, new { error = "Bad Request", detail = ae.Message });
                        }
                        catch (Exception ex)
                        {
                            // 予期しない例外は 500 で返却
                            response.StatusCode = 500;
                            response.ContentType = "application/json; charset=utf-8";
                            WriteJson(response, new { error = "Internal Server Error", detail = ex.Message });
                        }
                        finally
                        {
                            response.Close();
                        }
                        continue;
                    }

                    response.StatusCode = 404;
                    response.Close();
                }
                catch (HttpListenerException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MCP] Error: {ex}");
                }
            }

            listener.Stop();
        }, token);
    }

    private static void WriteJson(HttpListenerResponse response, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
}
