using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Signal", RTVT.Signal, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(9)]
public partial class SignalPrototype : Prototype<SignalPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      { get; set; } = Symbol.For("Signal");


    public override List<string> Aliases { get; set; } = [
        "signal",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Signal;

    public SignalPrototype(ExecContext ctx) : base(RTVT.Signal, ctx) {
        Ty    = Types.Ty.Object();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
}
    // Child constructor
    public SignalPrototype(string name, Value value, SignalPrototype parent) : base(RTVT.Signal, name) {
        Builder = parent.Builder;
        Proto   = value;
    }
    public static (Value signalValue, SignalPrototype newProtoType) MakeChild(ExecContext ctx, string name, SignalPrototype parent = null) {
        var obj = Value.Signal(ctx);
        var p   = (parent ?? Instance);
        var t   = p.Ty;
        obj = p.Builder.BuildTo(obj, p, null, t);

        var proto = new SignalPrototype(name, obj, p);

        return (obj, proto);
    }
    
    [LanguageValueConstructor]
    public static Value SignalValueCtor(FunctionExecContext ctx, [LanguageInstance] Value instance, params Value[] args) {
        if (instance is not ValueType type) {
            throw new InterpreterRuntimeException($"Expected a signal type, got {instance}");
        }

        var obj         = Value.Signal(ctx);
        var structProto = (type.PrototypeInstance as SignalPrototype)!;
        obj.Prototype = structProto.Proto;
        obj.SetValue(structProto.Proto.Clone());

        return obj;
    }

    
    [LanguageFunction]
    public static void Emit(FunctionExecContext ctx, [LanguageInstance] Value inst, params Value[] args) {
        inst.Is.ThrowIfNot(RTVT.Signal);

        var signal = inst.As.Signal();
        
        signal.Emit(ctx, args);
    }

}