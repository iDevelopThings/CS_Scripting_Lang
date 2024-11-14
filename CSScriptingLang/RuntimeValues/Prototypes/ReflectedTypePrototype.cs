using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public class ReflectedTypePrototypeDebugView
{
    private readonly ReflectedTypePrototype v;

    public ReflectedTypePrototypeDebugView(ReflectedTypePrototype value) {
        v = value;
    }
    public bool         IsPrimitive => v.IsPrimitive;
    public Symbol       Symbol      => v.Symbol;
    public List<string> Aliases     => v.Aliases;

    public string    FQN       => v.FQN;
    public Value     Proto     => v.Proto;
    public ValueType ValueType => v.ValueType;
    public RTVT      Rtvt      => v.Rtvt;

}

[LanguagePrototype("ReflectedType", RTVT.Object, typeof(ObjectPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(4)]
[DebuggerTypeProxy(typeof(ReflectedTypePrototypeDebugView))]
public partial class ReflectedTypePrototype : Prototype<ReflectedTypePrototype>
{
    public override bool IsPrimitive => false;

    public override Symbol Symbol { get; set; } = Symbol.For("ReflectedType");

    public override List<string> Aliases { get; set; } = [
        "ReflectedType",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Object;

    public ReflectedTypePrototype(ExecContext ctx) : base(RTVT.Object, ctx) {
        Ty    = Types.Ty.Object();
        Proto = Builder.Build(this, ctx, ObjectPrototype.Instance, Ty);
    }

    // Child constructor
    public ReflectedTypePrototype(
        string                 name,
        Value                  value,
        ReflectedTypePrototype parent,
        string                 fqn = null
    ) {
        Symbol = Symbol.For(name);
        if (fqn != null)
            FQN = fqn;
        Rtvt = RTVT.Object;

        Builder = parent.Builder;
        Proto   = value;

        ValueType = new(Rtvt, this);
    }

    public static ReflectedTypePrototype MakeChild(
        ExecContext            ctx,
        string                 name,
        Value                  obj    = null,
        string                 fqn    = null,
        ReflectedTypePrototype parent = null
    ) {
        var p = (parent ?? Instance);

        obj ??= Value.Struct(ctx, p.ValueType.PrototypeInstance.Proto);

        fqn ??= name;

        var proto = new ReflectedTypePrototype(name, obj, p, fqn) {
            Aliases = [name, fqn],
        };
        proto.Aliases = proto.Aliases.Distinct().ToList();

        p.Builder.BuildTo(obj, p);

        // proto.ValueType.PrototypeInstance.Proto

        obj.SetSymbol(name);

        return proto;
    }

    [LanguageGlobalFunction("GetType")]
    public static Value GetValueType(FunctionExecContext ctx, params Value[] args) {
        var type = Value.Object(ctx, Instance.Proto);
        type.SetSymbol("ReflectedType");

        Prototype protoType = null;

        if (ctx.TypeArgs.Count == 1) {
            var t = ctx.TypeArgs[0].Type;
            protoType = t.PrototypeInstance;

            if (protoType is StructPrototype structProto) {
                type["fields"] = Value.Array(
                    structProto.DeclaredFields.Select(f => Value.String(f.Name))
                );
            } else {
                type["fields"] = Value.Array(protoType.Proto.AllUniqueMembers().Select(m => Value.String(m.Key)));
            }
            
            type["fieldValues"] = Value.Array(
                protoType.Proto.AllUniqueMembers()
                   .Select(m => Value.Object(new Dictionary<string, Value>() {
                        {"name", Value.String(m.Key)},
                        {"value", m.Value},
                    }))
            );

        } else {
            protoType = args[0].PrototypeType;

            var value = args[0];
            type["_instance"] = value;

            var fields = Value.Array(
                value.AllUniqueMembers()
                   .Select(m => Value.Object(new Dictionary<string, Value>() {
                        {"name", Value.String(m.Key)},
                        {"value", m.Value},
                    }))
            );
            type["fieldValues"] = fields;
            
            type["fields"] = Value.Array(
                protoType.Proto.AllUniqueMembers().Select(m => Value.String(m.Key))
            );
        }


        type["fqn"]  = protoType.FQN;
        type["rtvt"] = protoType.Rtvt.Name();


        return type;
    }
}