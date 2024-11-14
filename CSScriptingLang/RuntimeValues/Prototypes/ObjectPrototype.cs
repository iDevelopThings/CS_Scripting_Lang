using System.Diagnostics;
using CSScriptingLang.Core.Serialization.JsonSerialization;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Object", RTVT.Object, typeof(ValuePrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(1)]
public partial class ObjectPrototype : Prototype<ObjectPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      { get; set; } = Symbol.For("Object");

    public override List<string> Aliases { get; set; } = [
        "obj",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Object;

    public ObjectPrototype(ExecContext ctx) : base(RTVT.Object, ctx) {
        Ty = Ty.Object();
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance, Ty);
    }

    // Child constructor
    public ObjectPrototype(
        string          name,
        Value           value,
        ObjectPrototype parent,
        string          fqn = null
    ) {
        Symbol = Symbol.For(name);
        if (fqn != null)
            FQN = fqn;
        Rtvt      = RTVT.Object;
        ValueType = new(Rtvt, this);

        Builder = parent.Builder;
        Proto   = value;
    }

    public static ObjectPrototype MakeChild(
        ExecContext     ctx,
        string          name,
        Value           obj    = null,
        string          fqn    = null,
        ObjectPrototype parent = null
    ) {
        obj ??= Value.Object(ctx);

        var p = (parent ?? Instance);
        var t = p.Ty;

        var proto = new ObjectPrototype(name, obj, p, fqn) {
            Aliases = [name, fqn],
        };

        p.Builder.BuildTo(obj, p, null, t);

        return proto;
    }

    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Object => (true, v => v),
            _           => (false, null),
        };
    }


    [LanguageFunction]
    public static Value ToJson([LanguageInstance] Value instance) => Lib_Json.Serialize(instance);

    [LanguageFunction]
    public static Value FromJson([LanguageInstance] Value instance, string json) {
        return Lib_Json.DeserializeTo(json, instance);
    }

    [LanguageFunction]
    public static Value SetPrototype([LanguageInstance] Value instance, Value prototype) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        instance.Prototype = prototype;

        return instance;
    }
    
    [LanguageFunction]
    public static Value Get([LanguageInstance] Value instance, string key) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        return instance[key];
    }
    
    [LanguageFunction]
    public static void Set([LanguageInstance] Value instance, string key, Value value) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        instance[key] = value;
    }

    [LanguageFunction]
    public static void Clear([LanguageInstance] Value instance) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        var obj = instance.As.Object();
        
        obj.Clear();
    }

    [LanguageFunction]
    public static bool ContainsKey([LanguageInstance] Value instance, string key) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        return instance.HasMember(key);
    }

    [LanguageFunction]
    public static bool ContainsValue([LanguageInstance] Value instance, Value value) {
        instance.Is.ThrowIfNot(RTVT.Object);
        
        return instance.HasValue(value);
    }
    
    [LanguageFunction]
    public static Value GetKeys([LanguageInstance] Value instance) {
        var obj    = instance.As.Object();
        var result = Value.Array(obj.Keys.Select(Value.String));
        return result;
    }
    
    [LanguageFunction]
    public static Value[] GetValues([LanguageInstance] Value instance) {
        var obj    = instance.As.Object();
        // var result = Value.Array(obj.Values.Select(v => v));
        var result = obj.Values.Select(v => v).ToArray();
        return result;
    }
    
    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Object);
        
        var value = inst.As.Object();
        
        var enumerator = Value.Object(ctx);
        
        var keys = value.Keys.ToList();
        var i    = 0;
        
        enumerator["current"] = Value.Null();
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= keys.Count) {
                return false;
            }
            
            var pair = Value.Object(ctx);
            pair["key"] = keys[i];
            pair["value"] = value[keys[i]];
            
            enumerator["current"] = pair;
            i++;
            return true;
        });
        
        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());
        
        return enumerator;
    }
}