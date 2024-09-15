using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Number")]
public partial class NumberPrototype : Prototype<NumberPrototype>
{
    public override bool IsPrimitive => true;

    public override Symbol Symbol => Symbol.For("Number");

    
    public override List<string> Aliases { get; set; } = [
        "number",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Number;

    public NumberPrototype() : base(RTVT.Number, PrototypeObject.Build, ValuePrototype.Instance) { }

}