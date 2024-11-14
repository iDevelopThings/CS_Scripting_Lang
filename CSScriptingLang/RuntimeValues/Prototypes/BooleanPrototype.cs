using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Boolean", RTVT.Boolean, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(6)]
public partial class BooleanPrototype : Prototype<BooleanPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("Boolean");


    public override List<string> Aliases { get; set; } = [
        "boolean", "bool",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Boolean;

    public BooleanPrototype(ExecContext ctx) : base(RTVT.Boolean, ctx) {
        Ty    = Types.Ty.Bool();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }
  
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Boolean => (true, value => value),
            RTVT.Int32   => (true, value => value.GetUntypedValue<bool>() ? 1 : 0),
            RTVT.Int64   => (true, value => value.GetUntypedValue<bool>() ? 1L : 0L),
            RTVT.Float   => (true, value => value.GetUntypedValue<bool>() ? 1f : 0f),
            RTVT.Double  => (true, value => value.GetUntypedValue<bool>() ? 1d : 0d),
            RTVT.String  => (true, value => value.GetUntypedValue<bool>().ToString()),

            _ => (false, null),
        };
    }
}