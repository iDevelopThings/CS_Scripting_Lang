using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Function")]
[PrototypeBoot(5)]
public partial class FunctionPrototype : Prototype<FunctionPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Function");
    
    
    
    public override List<string> Aliases { get; set; } = [
        "fn", "function",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Function;

    public FunctionPrototype() : base(RTVT.Function, PrototypeObject.Build, ValuePrototype.Instance) { }

}