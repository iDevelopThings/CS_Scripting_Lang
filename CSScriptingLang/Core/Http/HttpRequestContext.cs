using System.Net;
using System.Text;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core.Serialization.JsonSerialization;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Core.Logging;
using JetBrains.Annotations;

namespace CSScriptingLang.Core.Http;

public enum RequestType
{
    Html,
    Json,
}

public class RouteNotFoundException : InterpreterRuntimeException
{
    public RouteNotFoundException(string message) : base(message) { }
    public RouteNotFoundException(string message, Exception inner) : base(message, inner) { }
    [StringFormatMethod("format")]
    public RouteNotFoundException(string format, params object[] args) : base(string.Format(format, args)) { }
}

[LanguageClassBind("HttpRequestContext")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpRequestContext : IDisposable
{
    private static Logger Logger = Logs.Get<HttpRequestContext>();

    public static int RequestCount;

    public HttpServer Server { get; set; }

    public HttpListenerContext RequestCtx { get; set; }

    public HttpListenerRequest InternalRequest => RequestCtx.Request;
    public HttpRequest         Request;

    public HttpListenerResponse InternalResponse => RequestCtx.Response;
    public HttpResponse         Response;

    public HttpVerb Method => HttpVerbUtils.FromString(InternalRequest.HttpMethod);
    public string   Path   => InternalRequest.Url!.AbsolutePath;

    public HttpRoute                          Route      { get; set; }
    public List<RequestHandlerDelegateHolder> Middleware { get; set; }

    public Dictionary<string, string> PathParams { get; set; } = new();

    public RequestType Type => InternalRequest.ContentType switch {
        "application/json" => RequestType.Json,
        "text/html"        => RequestType.Html,

        _ => RequestType.Html,
    };

    [LanguageValueConstructor]
    public HttpRequestContext(HttpServer httpServer, HttpListenerContext reqCtx) {
        RequestCtx = reqCtx;
        Server     = httpServer;

        Request  = new HttpRequest(this);
        Response = new HttpResponse(this);
    }

    public async Task Initialize() {
        await Request.Initialize();
        await Response.Initialize();
    }
    private void MatchRoute() {
        var segments = Path.Split('/');
        var current  = Server.Router.Root;

        var middlewares = new List<RequestHandlerDelegateHolder>();

        middlewares.AddRange(current.Middlewares);

        foreach (var segment in segments) {
            if (segment.Length == 0)
                continue;

            if (current.Children.TryGetValue(segment, out var childRoute)) {
                current = childRoute;
                middlewares.AddRange(current.Middlewares);
                continue;
            }

            if (!current.Children.TryGetValue("*", out var wildcardRoute)) {
                throw new RouteNotFoundException($"Route not found: {Path}");
            }

            current = wildcardRoute;
            if (current.UsesParameter && current.ParameterName != null) {
                PathParams.Add(current.ParameterName, segment);
            }

            middlewares.AddRange(current.Middlewares);
        }

        if (!current.Verbs.Contains(Method))
            throw new RouteNotFoundException($"Route not found: {Path}");

        Route      = current;
        Middleware = middlewares;

        Request.Params = Value.Object(PathParams.ToDictionary(kvp => kvp.Key, kvp => Value.String(kvp.Value)));
    }



    public void ResolveRoute() {
        using var _ = TimedScope.Scoped_Print("ResolveRoute");

        MatchRoute();
    }

    public async Task HandleRequest() {
        using var _ = TimedScope.Scoped_Print("HandleRequest");

        Interlocked.Increment(ref RequestCount);

        Console.WriteLine($"[{RequestCount}] {Method.ToColoredString()} {Path}");

        var handlers = Middleware
               .Select(h => h.Handler)
               .Append(Route.ResponseHandler.Handler)
               .ToList()
            ;

        // var handlers = Route.Handlers
        //    .Where(h => h.Verbs.Contains(Method))
        //    .Select(h => h.Handler)
        //    .ToList();

        try {
            var current = 0;
            Task Next() {
                if (current >= handlers.Count)
                    return Task.CompletedTask;

                var handler = handlers[current++];

                return handler.Invoke(this, Next);
            }

            if (handlers.Count == 0) {
                await WriteStatus(HttpStatusCode.NoContent);
                return;
            }

            await Next();
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task WriteNegotiatedContent(Func<RequestType, ReadOnlyMemory<byte>> writer) {
        using var _ = TimedScope.Scoped_Print("WriteNegotiatedContent");
        
        ReadOnlyMemory<byte> buffer;

        switch (Type) {
            case RequestType.Json:
                InternalResponse.ContentType = "application/json";
                buffer                       = writer(RequestType.Json);
                if (HttpServer.CaptureResponse) {
                    HttpServer.CapturedResponseValue = null;
                    HttpServer.CapturedResponse      = Encoding.UTF8.GetString(buffer.Span);
                }
                break;

            case RequestType.Html:
                InternalResponse.ContentType = "text/html";
                buffer                       = writer(RequestType.Html);
                if (HttpServer.CaptureResponse) {
                    HttpServer.CapturedResponseValue = null;
                    HttpServer.CapturedResponse      = Encoding.UTF8.GetString(buffer.Span);
                }
                break;

            default:
                throw new Exception("Invalid request type");
        }

        await InternalResponse.OutputStream.WriteAsync(buffer);
    }

    public async Task WriteStatus(HttpStatusCode status, string message = null) {
        using var _ = TimedScope.Scoped_Print("WriteStatus");
        InternalResponse.StatusCode = (int) status;
        await WriteNegotiatedContent(t => t switch {
            RequestType.Json => status.ToJson(message),
            RequestType.Html => status.ToHtml(message),
            _                => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        });
    }

    public async Task WriteJson(Value data) {
        // using var _ = TimedScope.Scoped_Print("WriteJson");
        
        InternalResponse.ContentType = "application/json";
        var jsonStr = Lib_Json.Serialize(data);
        await InternalResponse.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(jsonStr));
        if (HttpServer.CaptureResponse) {
            HttpServer.CapturedResponseValue = data;
            HttpServer.CapturedResponse      = jsonStr;
        }
    }

    public void Dispose() {
        InternalResponse.Close();
    }
}