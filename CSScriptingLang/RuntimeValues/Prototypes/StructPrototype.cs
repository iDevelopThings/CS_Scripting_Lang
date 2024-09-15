using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Struct")]
[PrototypeBoot(4)]
public partial class StructPrototype : Prototype<StructPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Struct");

    public override List<string> Aliases { get; set; } = [
        "struct",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Struct;

    // Static constructor
    public StructPrototype() : base(RTVT.Struct, PrototypeObject.Build, ObjectPrototype.Instance) { }

    // Child constructor
    public StructPrototype(Value builtTo, StructPrototype parentProto = null) : base(
        RTVT.Struct,
        PrototypeObject.BuildTo,
        builtTo,
        parentProto ?? Instance
    ) { }

    public static (Value structValue, StructPrototype newProtoType) MakeChild(ExecContext ctx, string name, StructPrototype parent = null) {
        var obj   = Value.Struct(ctx);
        var proto = new StructPrototype(obj, (parent ?? Instance));
        
        return (obj, proto);
    }

}