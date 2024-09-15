using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Boolean")]
[PrototypeBoot(6)]
public partial class BooleanPrototype : Prototype<BooleanPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("Boolean");


    public override List<string> Aliases { get; set; } = [
        "boolean", "bool",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Boolean;

    public BooleanPrototype() : base(RTVT.Boolean, PrototypeObject.Build, ValuePrototype.Instance) { }

}