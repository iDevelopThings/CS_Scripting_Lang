using System.Net;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Core.Http;

public class RequestHandlerDelegateHolder
{
    public List<HttpVerb> Verbs        { get; set; } = new();
    public Value          HandlerValue { get; set; }

    public List<Func<HttpRequestContext, Value>> ParameterResolvers { get; set; } = new();
    public HttpRequestHandlerDelegate            Handler            { get; set; }
}

[LanguageClassBind("HttpRoute")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpRoute
{
    private static Logger Logger = Logs.Get<HttpRoute>();

    /// <summary>
    /// The path this route is bound too
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The HTTP Verbs allowed for this route.
    /// </summary>
    public List<HttpVerb> Verbs { get; set; }

    /// <summary>
    /// The route path parameter names
    /// </summary>
    public Dictionary<string, HttpRoutePathParameter> Parameters { get; set; } = new();

    public IEnumerable<HttpRoutePathParameter> AllParameters => Parameters.Values.AsEnumerable().Concat(Children.Values.SelectMany(x => x.AllParameters));

    public bool UsesParameter => AllParameters.Any();

    public Dictionary<string, HttpRoute> Children = new();
    public string                        ParameterName { get; set; }

    public RequestHandlerDelegateHolder       ResponseHandler { get; set; } = new();
    public List<RequestHandlerDelegateHolder> Middlewares     { get; set; } = new();

    public bool IsLeaf { get; set; }

    // public List<MiddlewareInfo> Middlewares { get; set; } = new();

    // private static HttpRequestHandlerDelegate DefaultHandler = async (context, next) => {
//         await next(context);
    // };

    public HttpRoute(string path, HttpVerb[] verbs = null) {
        Path = path;
        if (!Path.StartsWith('/'))
            Path = "/" + Path;

        if (verbs != null)
            Verbs = verbs.ToList();

        var segments = path.Split("/");
        foreach (var segment in segments) {
            if (string.IsNullOrEmpty(segment))
                continue;

            if (!segment.StartsWith('{') || !segment.EndsWith('}'))
                continue;

            var param = new HttpRoutePathParameter(segment[1..^1]);

            Parameters.Add(param.Name, param);

            ParameterName = param.Name;
        }

        // Handlers.Add(DefaultHandler);
    }


    public HttpRoute WithVerb(HttpVerb verb) {
        (Verbs ??= []).Add(verb);
        Verbs = Verbs.Distinct().ToList();
        return this;
    }
    public HttpRoute WithVerb(IEnumerable<HttpVerb> verbs) {
        verbs.ForEach(v => WithVerb(v));
        return this;
    }

    public void Combine(HttpRoute route) {
        WithVerb(route.Verbs);
    }
    public void Print() {
        Logger.Debug($"[{Verbs.ToColoredString()}] {Path}");
    }
}