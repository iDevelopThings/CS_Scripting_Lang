using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;

namespace CSScriptingLang.Core.Http;

[LanguageClassBind("HttpResponse")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpResponse
{
    private readonly HttpRequestContext _context;

    public HttpResponse(HttpRequestContext context) {
        _context = context;
    }

    public Task Initialize() {
        return Task.CompletedTask;
    }

}