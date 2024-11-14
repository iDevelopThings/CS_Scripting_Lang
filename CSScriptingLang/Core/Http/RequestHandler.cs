namespace CSScriptingLang.Core.Http;

public delegate Task HttpRequestHandlerDelegate(
    HttpRequestContext context,
    Func<Task>         next
);