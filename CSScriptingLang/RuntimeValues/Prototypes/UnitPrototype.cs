using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Unit", RTVT.Unit, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(2)]
public partial class UnitPrototype : Prototype<UnitPrototype>
{
    public override bool IsPrimitive => true;

    public override Symbol Symbol => Symbol.For("Unit");

    
    public override List<string> Aliases { get; set; } = [
        "void", "unit",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Unit;
    
    public UnitPrototype(ExecContext ctx) : base(RTVT.Unit, ctx) {
        Ty    = Types.Ty.Unit();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }
    
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Unit => (true, value => value),
            _ => (false, null),
        };
    }
}

[LanguagePrototype("Null", RTVT.Null, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(3)]
public partial class NullPrototype : Prototype<NullPrototype>
{
    public override bool   IsPrimitive => true;
    public override Symbol Symbol      => Symbol.For("Null");
    
    public override List<string> Aliases { get; set; } = [
        "null",
    ];
    
    public override ZeroValueConstructor GetZeroValue() => Value.Null;
 
    public NullPrototype(ExecContext ctx) : base(RTVT.Null, ctx) {
        Ty    = Types.Ty.Null();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }   
    
    
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Null => (true, value => value),
            RTVT.Int32 => (true, value => 0),
            RTVT.Int64 => (true, value => 0L),
            RTVT.Float => (true, value => 0f),
            RTVT.Double => (true, value => 0d),
            RTVT.String => (true, value => "null"),
            RTVT.Boolean => (true, value => false),
            
            _ => (false, null),
        };
    }
}