using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("String")]
[PrototypeBoot(7)]
public partial class StringPrototype : Prototype<StringPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("String");


    public override List<string> Aliases { get; set; } = [
        "string", "str",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.String;

    public StringPrototype() : base(RTVT.String, PrototypeObject.Build, ValuePrototype.Instance) { }

}