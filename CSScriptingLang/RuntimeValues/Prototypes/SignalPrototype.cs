using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Signal")]
[PrototypeBoot(9)]
public partial class SignalPrototype : Prototype<SignalPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Signal");
    
    
    public override List<string> Aliases { get; set; } = [
        "signal",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Signal;

    public SignalPrototype() : base(RTVT.Signal, PrototypeObject.Build, ValuePrototype.Instance) { }

}