using System.Net;
using System.Net.Sockets;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Core.Logging;
using RestSharp;

namespace CSScriptingLang.Core.Http;

[LanguageClassBind("HttpServer")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpServer : Value
{
    private new static Logger Logger = Logs.Get<HttpServer>();

    public HttpListener Listener { get; }

    public static bool   CaptureResponse       { get; set; }
    public static string CapturedResponse      { get; set; }
    public static Value  CapturedResponseValue { get; set; }
    public static Uri    ListenUrl             { get; set; }

    [LanguagePropertyGetter("router", false)]
    [LanguageMetaDefinition("router HttpRouter")]
    public HttpRouter Router { get; } = new();

    /// <summary>
    /// Check if we're listening for requests
    /// </summary>
    [LanguagePropertyGetter("isListening", false)]
    [LanguageMetaDefinition("isListening bool")]
    public bool IsListening => Listener.IsListening;
    
    [LanguageValueConstructor]
    [LanguageMetaDefinition("def HttpServer(string host);")]
    public HttpServer(ExecContext ctx, string host) : base(RTVT.Object, ctx, TypesTable.GetPrototypeTypeByName("HttpServer").PrototypeInstance.Proto) {
        host ??= "localhost";

        if (!host.StartsWith("https://") && !host.StartsWith("http://")) {
            host = "http://" + host;
        }

        var uri = new Uri(host);

        ListenUrl = uri;

        Listener = new HttpListener();
        Listener.Prefixes.Add(uri.ToString());

    }

    [LanguageFunction("getLocalListenUrl")]
    public static string GetLocalListenUrl() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint) listener.LocalEndpoint).Port;
        listener.Stop();

        return $"http://localhost:{port}/";
    }

    [LanguageFunction("listen")]
    [LanguageMetaDefinition("def async listen();")]
    public async Task Listen() {
        Listener.Start();

        Logger.Debug($"Listening on {Listener.Prefixes.Join(", ")}");

        foreach (var route in Router.AllRoutes()) {
            if (!route.IsLeaf)
                continue;

            route.Print();
        }

        await ListenerLoop();
    }

    [LanguageFunction("stop")]
    public void Stop() {
        Listener.Stop();
    }

    private async Task ListenerLoop() {
        try {
            while (Listener.IsListening) {
                ScriptTask.GlobalCancellationSource.Token.ThrowIfCancellationRequested();

                var       _ctx = await Listener.GetContextAsync().WaitAsync(ScriptTask.GlobalCancellationSource.Token);
                using var ctx  = new HttpRequestContext(this, _ctx);

                await ctx.Initialize();

                try {
                    ctx.ResolveRoute();
                }
                catch (RouteNotFoundException e) {
                    await ctx.WriteStatus(HttpStatusCode.NotFound, e.Message);

                    continue;
                }

                await ctx.HandleRequest();
                /*.ContinueWith(t => {
                if (t.IsFaulted) {
                    AsyncContext.RethrowAsyncException(t.Exception);
                }
            });*/

            }
        }
        catch (OperationCanceledException) {
            Logger.Debug("Listener loop cancelled");
            Listener.Stop();
            Listener.Close();
        }
        catch (Exception e) {
            Logger.Exception(e);
        }
    }

    public static async Task<TResponse> InjectRequest<TResponse>(HttpVerb method, string path, RestRequest requestOptions) {

        // var listener = new TcpListener(IPAddress.Loopback, 0);
        // listener.Start();
        // var port = ((IPEndPoint) listener.LocalEndpoint).Port;
        // listener.Stop();

        var httpClient = new RestClient(ListenUrl.ToString());

        requestOptions.Method = method switch {
            HttpVerb.Get     => Method.Get,
            HttpVerb.Post    => Method.Post,
            HttpVerb.Put     => Method.Put,
            HttpVerb.Delete  => Method.Delete,
            HttpVerb.Head    => Method.Head,
            HttpVerb.Options => Method.Options,
            HttpVerb.Patch   => Method.Patch,
            _                => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };

        requestOptions.Resource = path;

        var responseObj = requestOptions.Method switch {
            Method.Get     => await httpClient.ExecuteGetAsync<TResponse>(requestOptions),
            Method.Post    => await httpClient.ExecutePostAsync<TResponse>(requestOptions),
            Method.Put     => await httpClient.ExecutePutAsync<TResponse>(requestOptions),
            Method.Delete  => await httpClient.ExecuteDeleteAsync<TResponse>(requestOptions),
            Method.Head    => await httpClient.ExecuteHeadAsync<TResponse>(requestOptions),
            Method.Options => await httpClient.ExecuteOptionsAsync<TResponse>(requestOptions),
            Method.Patch   => await httpClient.ExecutePatchAsync<TResponse>(requestOptions),
            _              => throw new Exception("Unsupported method")
        };

        return responseObj.Data;
    }


}