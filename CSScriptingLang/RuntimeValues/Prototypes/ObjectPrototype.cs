using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Object")]
[PrototypeBoot(1)]
public partial class ObjectPrototype : Prototype<ObjectPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Object");
    
    
    public override List<string> Aliases { get; set; } = [
        "obj",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Object;

    public ObjectPrototype() : base(RTVT.Object, PrototypeObject.Build, ValuePrototype.Instance) { }

    /*
    [LanguageFunction]
    public static Value GetSymbol([LanguageInstance] Value instance)
        => Get(instance, "Symbol");

    [LanguageFunction]
    public static void SetSymbol([LanguageInstance] Value instance, string value)
        => Set(instance, "Symbol", new ValueString(value));
        */

    [LanguageFunction]
    public static Value Get([LanguageInstance] Value instance, string key) {
        throw new System.NotImplementedException();
    }
    [LanguageFunction]
    public static void Set([LanguageInstance] Value instance, string key, Value value) {
        throw new System.NotImplementedException();
    }

    [LanguageFunction]
    public static void Add([LanguageInstance] Value instance, string key, Value value) {
        throw new System.NotImplementedException();
    }

    [LanguageFunction]
    public static void Clear([LanguageInstance] Value instance) {
        throw new System.NotImplementedException();
    }

    [LanguageFunction]
    public static bool ContainsKey([LanguageInstance] Value instance, string key) {
        throw new System.NotImplementedException();
    }

    [LanguageFunction]
    public static bool ContainsValue([LanguageInstance] Value instance, Value value) {
        throw new System.NotImplementedException();
    }
}