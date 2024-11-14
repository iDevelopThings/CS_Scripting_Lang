using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Function", RTVT.Function, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(5)]
public partial class FunctionPrototype : Prototype<FunctionPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Function");



    public override List<string> Aliases { get; set; } = [
        "fn", "function",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Function;

    public FunctionPrototype(ExecContext ctx) : base(RTVT.Function, ctx) {
        Ty    = Types.Ty.Function("Function");
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }
}