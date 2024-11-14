using System.Net;
using CSScriptingLang.Core.Serialization.JsonSerialization;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Core.Http;

public struct HttpRequestBody
{
    public JsonValue JsonBody   { get; set; }
    public Value     BodyObject { get; set; }
    public Value     StructBody { get; set; }
}

[LanguageClassBind("HttpRequest")]
[LanguageBindToModule("Http")]
[LanguageClassDataObjectBind]
public partial class HttpRequest
{
    private readonly HttpRequestContext  _ctx;
    private          HttpListenerRequest _req => _ctx.InternalRequest;

    public string RawBody { get; set; }

    public HttpRequestBody BodyData = new();

    [LanguagePropertyGetter("body", false)]
    [LanguageBindTypeHint("string", typeof(StringPrototype))]
    public Value Body {
        get => BodyData.BodyObject;
        set => BodyData.BodyObject = value;
    }
    
    [LanguagePropertyGetter("params", false)]
    [LanguageBindTypeHint("array", typeof(ArrayPrototype))]
    public Value Params { get; set; }

    public HttpRequest(HttpRequestContext context) {
        _ctx = context;
    }

    public async Task Initialize() {
        await ReadBody();
    }

    private async Task ReadBody() {
        var body = _req.InputStream;

        var encoding = _req.ContentEncoding;

        var reader = new StreamReader(body, encoding);

        RawBody = await reader.ReadToEndAsync();

        body.Close();
        reader.Close();

        var val = Value.Object();

        Body              = Lib_Json.DeserializeTo(RawBody, val, out var jsonValue);
        BodyData.JsonBody = jsonValue;

    }


    [LanguageFunction]
    public Value GetBody(FunctionExecContext ctx, params Value[] args) {
        var inst = Lib_InstanceCreator.New(ctx, args);
        if (inst == null) {
            throw new InterpreterRuntimeException($"Failed to create instance of {(ctx.TypeArgs.Count > 0 ? ctx.TypeArgs[0].ToString() : "undefined type")}");
        }

        var value = Lib_Json.DeserializeToStruct(ctx, RawBody, inst);

        return value;
    }

}