using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;

namespace CSScriptingLang.Core.Http;

[LanguageClassBind("HttpRoutePathParameter")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpRoutePathParameter
{
    [LanguageFunction]
    public bool IsOptional {
        [LanguageMetaDefinition("def getIsOptional() bool")]
        get;
        [LanguageMetaDefinition("def setIsOptional(bool value) void")]
        set;
    }

    [LanguageFunction]
    public string Name {
        [LanguageMetaDefinition("def getName() string")]
        get;
        [LanguageMetaDefinition("def setName(string value) void")]
        set;
    }

    [LanguageFunction]
    public string RawParam {
        [LanguageMetaDefinition("def getRawParam() string")]
        get;
        [LanguageMetaDefinition("def setRawParam(string value) void")]
        set;
    }

    // [LanguageFunction]
    public List<string> Args { get; set; } = new();

    [LanguageValueConstructor]
    public HttpRoutePathParameter(string param) {
        Name     = param;
        RawParam = param;

        var raw = RawParam;
        IsOptional = raw.EndsWith('?');
        if (IsOptional) {
            raw = raw[..^1];
        }

        var argsIndex = raw.IndexOf(':');

        if (argsIndex > 0) {
            Name = raw[..argsIndex];
            raw  = raw[(argsIndex + 1)..];
            Args = raw.Split(',').ToList();
        }
    }
}