using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("String", RTVT.String, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(7)]
public partial class StringPrototype : Prototype<StringPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("String");


    public override List<string> Aliases { get; set; } = [
        "string", "str",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.String;


    public StringPrototype(ExecContext ctx) : base(RTVT.String, ctx) {
        Ty    = Types.Ty.String();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }

    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        if (type.IsString()) return (true, (value => value));
        if (type.IsInt32()) return (true, (value => Value.Int32(int.Parse(value.GetUntypedValue<string>()))));
        if (type.IsInt64()) return (true, (value => Value.Int64(long.Parse(value.GetUntypedValue<string>()))));
        if (type.IsFloat()) return (true, (value => Value.Float(float.Parse(value.GetUntypedValue<string>()))));
        if (type.IsDouble()) return (true, (value => Value.Double(double.Parse(value.GetUntypedValue<string>()))));
        if (type.IsBoolean()) return (true, (value => Value.Boolean(bool.Parse(value.GetUntypedValue<string>()))));

        return (false, null);
    }


    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.String);

        var value = inst.As.String();

        var enumerator = Value.Object(ctx);
        var i          = 0;

        enumerator["current"]      = value[i].ToString();
        enumerator["currentIndex"] = i;
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= value.Length) {
                return false;
            }

            enumerator["current"]      = value[i++].ToString();
            enumerator["currentIndex"] = i;
            return true;
        });

        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());

        return enumerator;
    }
}