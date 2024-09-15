using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Unit")]
[PrototypeBoot(2)]
public partial class UnitPrototype : Prototype<UnitPrototype>
{
    public override bool IsPrimitive => true;

    public override Symbol Symbol => Symbol.For("Unit");

    
    public override List<string> Aliases { get; set; } = [
        "void", "unit",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Unit;

    public UnitPrototype() : base(RTVT.Unit, PrototypeObject.Build, ValuePrototype.Instance) { }
}

[LanguagePrototype("Null")]
[PrototypeBoot(3)]
public partial class NullPrototype : Prototype<NullPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("Null");
    
    public override List<string> Aliases { get; set; } = [
        "null",
    ];
    
    public override ZeroValueConstructor GetZeroValue() => Value.Null;
    
    public NullPrototype() : base(RTVT.Null, PrototypeObject.Build, ValuePrototype.Instance) { }
}