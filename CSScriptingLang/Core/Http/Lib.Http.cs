using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Core.Http;

[LanguageModuleBind("Http")]
public partial class Lib_Http : ILibraryImpl
{

    public IEnumerable<KeyValuePair<string, Value>> OnGetLibraryDefinitions(ExecContext ctx, ILibrary lib) {
        // yield return new KeyValuePair<string, Value>("HttpServer", result);
        yield break;
    }

    public IEnumerable<ILibrary> OnGetAdditionalLibraries(ExecContext ctx, ILibrary lib) {
        yield return new HttpServer.Library();
        yield return new HttpRouter.Library();
        yield return new HttpRoutePathParameter.Library();
        yield return new HttpRoute.Library();
        yield return new HttpRequestContext.Library();
        yield return new HttpRequest.Library();
        yield return new HttpResponse.Library();
    }

}