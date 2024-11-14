using System.Net;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Core.Http;

[LanguageClassBind("HttpRouter")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpRouter
{
    private static Logger Logger = Logs.Get<HttpRouter>();

    public HttpRoute Root { get; set; } = new("/", [HttpVerb.Get]);

    public HttpRoute Add(string routePath) {
        var current = Root;

        var path         = routePath.Split('/');
        var finalSegment = path.Last();

        var builtPath = new List<string>();

        foreach (var segment in path) {
            if (segment.Length == 0)
                continue;

            builtPath.Add(segment);

            var newSegment = segment;
            if (segment.Contains('{')) {
                newSegment = "*";
            }

            var routeToAdd = new HttpRoute(string.Join("/", builtPath));

            if (current.Children.TryAdd(newSegment, routeToAdd)) {
                routeToAdd.IsLeaf = finalSegment == segment;
                // current.Children[newSegment].Combine(route);
            }

            current = current.Children[newSegment];

        }

        return current;
    }

    [LanguageFunction("Use")]
    public void Use(ExecContext ctx, Value handler) => Use(ctx, "/", handler);

    [LanguageFunction("Use")]
    public void Use(ExecContext ctx, string path, Value handler) {
        path = string.IsNullOrEmpty(path) ? "/" : path;

        var route = ResolveRoute(path);
        if (route == null) {
            throw new RouteNotFoundException($"Route not found: {path}");
        }

        if (!handler.Is.Function) {
            Logger.Error($"Handler is not a function: {handler}");
            return;
        }

        if (handler._context == null) {
            handler._context = ctx;
        }

        var fn = handler.As.Function();

        var handlerInst = new RequestHandlerDelegateHolder {
            HandlerValue = handler,
        };


        for (var i = 0; i < fn.Declaration.Parameters.Count; i++) {
            var param = fn.Declaration.Parameters[i];


            if (param.TypeIdentifier.Name == "HttpNext") {
                handlerInst.ParameterResolvers.Add(
                    (context) => Value.Function(
                        "HttpNext", (FnClosure.StaticFunction) ((FunctionExecContext c, params Value[] arguments) => {
                            var current = handlerInst.Handler;
                            return ScriptTask.Wrap(
                                c, Task.Run(
                                    () => {
                                        return current(context, () => Task.FromResult(Value.Unit()));
                                    }
                                )
                            );
                        })
                    )
                );
                continue;
            }

            var pType = param.TypeIdentifier.ResolveType();

            if (pType == null) {
                Logger.Error($"Could not resolve type: {param.TypeIdentifier}");
                continue;
            }

            var typeName = pType.Prototype.ValueType.Name;

            if (typeName == "HttpRequestContext") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context, pType.Prototype));
                continue;
            }

            if (typeName == "HttpResponse") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context.Response, pType.Prototype));
                continue;
            }

            if (typeName == "HttpRequest") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context.Request, pType.Prototype));
                continue;
            }

            throw new Exception($"Unknown parameter type: {typeName}");
        }

        var handlerIdx = route.Middlewares.Count;
        async Task HttpRequestHandlerDelegate(HttpRequestContext context, Func<Task> next) {
            using var _ = TimedScope.Scoped_Print($"HandleMiddleware[handler={handlerIdx}]: {route.Path}");

            var resolvedParams = handlerInst.ParameterResolvers.Select(x => x(context)).ToArray();

            var result = handler._context.Call(handler, null, resolvedParams);
            // var result = fn.Call(handler._context, null, resolvedParams);
            if (result == null) {
                await next();
                return;
            }

            Value resultValue = null;
            if (result.Is.ScriptTask) {
                var scriptTask = (result.DataObject as ScriptTask)!;
                await scriptTask.RunAsync();

                resultValue = scriptTask.Value;
            } else {
                resultValue = result;
            }

            if (resultValue is {Is.Unit: true}) {
                await next();
                return;
            }

            if (resultValue is {Is.Object: true} or {Is.Array: true} or {Is.Struct: true}) {
                await context.WriteJson(resultValue);
            } else if (resultValue is {Is.String: true}) {
                await context.WriteStatus(HttpStatusCode.OK, resultValue.As.String());
            } else {
                await context.WriteStatus(HttpStatusCode.InternalServerError, "Invalid result type");
            }

        }

        handlerInst.Handler = HttpRequestHandlerDelegate;

        route.Middlewares.Add(handlerInst);
    }

    public HttpRoute Add(ExecContext ctx, HttpVerb verb, string path, Value handler) {
        // var route = new HttpRoute(path)
        // .WithVerb(verb);

        var route = Add(path)
           .WithVerb(verb);

        if (!handler.Is.Function) {
            Logger.Error($"Handler is not a function: {handler}");
            return route;
        }

        if (handler._context == null) {
            handler._context = ctx;
        }

        var fn = handler.As.Function();

        var handlerInst = new RequestHandlerDelegateHolder {
            HandlerValue = handler,
        };
        handlerInst.Verbs.Add(verb);


        for (var i = 0; i < fn.Declaration.Parameters.Count; i++) {
            var param = fn.Declaration.Parameters[i];
            var pType = param.TypeIdentifier.ResolveType();

            if (pType == null) {
                Logger.Error($"Could not resolve type: {param.TypeIdentifier}");
                continue;
            }

            var typeName = pType.Prototype.ValueType.Name;

            if (typeName == "HttpRequestContext") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context, pType.Prototype));
                continue;
            }

            if (typeName == "HttpResponse") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context.Response, pType.Prototype));
                continue;
            }

            if (typeName == "HttpRequest") {
                handlerInst.ParameterResolvers.Add((context) => Value.ClassInstance(ctx, context.Request, pType.Prototype));
                continue;
            }

            throw new Exception($"Unknown parameter type: {typeName}");
        }

        async Task HttpRequestHandlerDelegate(HttpRequestContext context, Func<Task> next) {
            using var _ = TimedScope.Scoped_Print($"HandleRequest[handler={0}]: {route.Path}");

            var resolvedParams = handlerInst.ParameterResolvers.Select(x => x(context)).ToArray();
            // var resolvedParams = TimedScope.Scoped_Fn("params", () => handlerInst.ParameterResolvers.Select(x => x(context)).ToArray());

            var result = TimedScope.Scoped_Fn("body call", () => handler._context.Call(handler, null, resolvedParams));
            // var result = fn.Call(handler._context, null, resolvedParams);

            Value resultValue = null;
            if (result.Is.ScriptTask) {
                var scriptTask = (result.DataObject as ScriptTask)!;
                await scriptTask.RunAsync();

                resultValue = scriptTask.Value;
            } else {
                resultValue = result;
            }

            if (resultValue is {Is.Unit: true}) {
                await next();
                return;
            }

            if (resultValue is {Is.Object: true} or {Is.Array: true} or {Is.Struct: true}) {
                await context.WriteJson(resultValue);
            } else if (resultValue is {Is.String: true}) {
                await context.WriteStatus(HttpStatusCode.OK, resultValue.As.String());
            } else {
                await context.WriteStatus(HttpStatusCode.InternalServerError, "Invalid result type");
            }

        }

        handlerInst.Handler = HttpRequestHandlerDelegate;

        route.ResponseHandler = handlerInst;

        return route;
    }


    [LanguageFunction("GET")]
    public HttpRoute GET(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Get, path, handler);

    [LanguageFunction("POST")]
    public HttpRoute POST(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Post, path, handler);

    [LanguageFunction("PUT")]
    public HttpRoute PUT(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Put, path, handler);

    [LanguageFunction("DELETE")]
    public HttpRoute DELETE(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Delete, path, handler);

    [LanguageFunction("PATCH")]
    public HttpRoute PATCH(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Patch, path, handler);

    [LanguageFunction("HEAD")]
    public HttpRoute HEAD(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Head, path, handler);

    [LanguageFunction("OPTIONS")]
    public HttpRoute OPTIONS(ExecContext ctx, string path, Value handler) => Add(ctx, HttpVerb.Options, path, handler);

    public IEnumerable<HttpRoute> AllRoutes() {
        var stack   = new Stack<HttpRoute>();
        var visited = new HashSet<HttpRoute>();

        stack.Push(Root);

        while (stack.Count > 0) {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            yield return current;

            foreach (var child in current.Children.Values) {
                stack.Push(child);
            }
        }

    }

    public HttpRoute ResolveRoute(string path = "/") {
        var segments = path.Split('/');
        var current  = Root;

        foreach (var segment in segments) {
            if (segment.Length == 0)
                continue;

            if (current.Children.TryGetValue(segment, out var childRoute)) {
                current = childRoute;
                continue;
            }

            if (!current.Children.TryGetValue("*", out var wildcardRoute)) {
                throw new RouteNotFoundException($"Route not found: {path}");
            }

            current = wildcardRoute;
        }

        return current;
    }

}