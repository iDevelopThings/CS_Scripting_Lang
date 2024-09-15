using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Value")]
[PrototypeBoot(0)]
public partial class ValuePrototype : Prototype<ValuePrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Value");
    
    public ValuePrototype() : base(RTVT.Object, PrototypeObject.Build, null) { }


}