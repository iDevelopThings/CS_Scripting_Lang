using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Value", RTVT.Object, null)]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(0)]
public partial class ValuePrototype : Prototype<ValuePrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Value");

    public ValuePrototype(ExecContext ctx) : base(RTVT.Object, ctx) {
        Aliases         = [];
        Proto           = Builder.Build(this, ctx);

        Proto.UnlockedOp(v => v.Prototype = Value.Null());
    }



}