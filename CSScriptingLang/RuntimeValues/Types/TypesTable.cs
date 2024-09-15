using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.ReflectionUtils;
using SharpX.Extensions;

namespace CSScriptingLang.RuntimeValues.Types;

public static class StaticType
{
    public static Prototype<NumberPrototype>   Int32()    => NumberPrototype.Instance;
    public static Prototype<NumberPrototype>   Int64()    => NumberPrototype.Instance;
    public static Prototype<NumberPrototype>   Float()    => NumberPrototype.Instance;
    public static Prototype<NumberPrototype>   Double()   => NumberPrototype.Instance;
    public static Prototype<StringPrototype>   String()   => StringPrototype.Instance;
    public static Prototype<BooleanPrototype>  Boolean()  => BooleanPrototype.Instance;
    public static Prototype<UnitPrototype>     Unit()     => UnitPrototype.Instance;
    public static Prototype<NullPrototype>     Null()     => NullPrototype.Instance;
    public static Prototype<ObjectPrototype>   Object()   => ObjectPrototype.Instance;
    public static Prototype<StructPrototype>   Struct()   => StructPrototype.Instance;
    public static Prototype<ArrayPrototype>    Array()    => ArrayPrototype.Instance;
    public static Prototype<FunctionPrototype> Function() => FunctionPrototype.Instance;


}

public class TypesTable
{
    /// <summary>
    /// A list of all unique ValueType instances
    /// </summary>
    public static HashSet<ValueType> Types = new();

    /// <summary>
    /// Symbol(unique) -> To unique ValueType instance
    /// </summary>
    public static Dictionary<Symbol, ValueType> TypesBySymbol = new();

    /// <summary>
    /// Name(Int32, String, Object) -> To unique ValueType instance
    /// </summary>
    public static Dictionary<string, ValueType> TypesByName = new();

    /// <summary>
    /// Each Prototype can define multiple aliases, this is a mapping of alias -> ValueType
    /// </summary>
    public static Dictionary<string, ValueType> TypesByAlias = new();

    /// <summary>
    /// The type has a corresponding prototype, this is a mapping of ValueType -> Prototype
    /// Mainly for reverse lookup
    /// </summary>
    public static Dictionary<ValueType, Prototype> TypePrototypes = new();

    /// <summary>
    /// Holds all value types by their RTVT
    /// So we'd have for example:
    /// RTVT.Object -> [Static Proto, User Proto A, User Proto B]
    /// </summary>
    public static Dictionary<RTVT, HashSet<ValueType>> TypesByRTVT = new();


    public static Dictionary<string, RTVT>       RTVTByAlias   = new();
    public static Dictionary<RTVT, List<string>> AliasesByRTVT = new();

    public static HashSet<Prototype> Prototypes = new();

    /// <summary>
    /// Mainly for generated types, objects/prototypes get a type registered with namespace like `CSScriptingLang.RuntimeValues.Values.Object`
    /// </summary>
    public static Dictionary<string, ValueType> TypesByFQN = new();

    static TypesTable() { }

    public static bool IsBindingPrototypes = false;

    public static void Initialize() {
        IsBindingPrototypes = true;

        var types = ReflectionStore.AllTypesWithAttributeIncludingAttr<PrototypeBootAttribute>()!
           .OrderBy(t => t.attribute.BootOrder)
           .Select(t => new {
                type       = t.type,
                attribute  = t.attribute,
                bootMethod = t.type.GetMethod("Boot", BindingFlags.Public | BindingFlags.Static),
            })
           .ToList();


        types.ForEach(t => {
            var proto = (Prototype) t.bootMethod.Invoke(null, null);
            RegisterStaticPrototype(proto);
        });

        Console.WriteLine("Binding prototypes");

        /*
        ValuePrototype.Boot();
        ObjectPrototype.Boot();

        UnitPrototype.Boot();
        NullPrototype.Boot();

        StructPrototype.Boot();
        FunctionPrototype.Boot();
        BooleanPrototype.Boot();
        NumberPrototype.Boot();
        StringPrototype.Boot();
        ArrayPrototype.Boot();
        SignalPrototype.Boot();
        */

        IsBindingPrototypes = false;
    }

    public static Prototype For(RTVT type) {
        if (IsBindingPrototypes) {
            return null;
            // throw new InvalidOperationException("Cannot call this method while binding prototypes");
        }

        return type switch {
            RTVT.Int32          => StaticType.Int32(),
            RTVT.Int64          => StaticType.Int64(),
            RTVT.Float          => StaticType.Float(),
            RTVT.Double         => StaticType.Double(),
            RTVT.String         => StaticType.String(),
            RTVT.Boolean        => StaticType.Boolean(),
            RTVT.Unit           => StaticType.Unit(),
            RTVT.Null           => StaticType.Null(),
            RTVT.Object         => StaticType.Object(),
            RTVT.Struct         => StaticType.Struct(),
            RTVT.Array          => StaticType.Array(),
            RTVT.Function       => StaticType.Function(),
            RTVT.ValueReference => null,
            _                   => throw new NotImplementedException($"Prototype::For({type}) not implemented"),
        };
    }

    private static void RegisterStaticPrototype(Prototype proto) {
        var type   = proto.ValueType;
        var symbol = proto.Symbol;

        Types.Add(type);
        TypesBySymbol[symbol]  = type;
        TypesByName[type.Name] = type;

        var aliases = proto.Aliases.Concat([type.Name.ToLower()]).ToList();
        aliases.ForEach(alias => TypesByAlias[alias] = type);

        TypePrototypes[type] = proto;

        Prototypes.Add(proto);

        TypesByRTVT.GetOrAdd(type.ForType).Add(type);

        aliases.ForEach(alias => RTVTByAlias[alias] = type.ForType);
        AliasesByRTVT.GetOrAdd(type.ForType).AddRange(aliases);

    }

    public static bool TryMakeFromName(ExecContext ctx, string name, out Value type, params object[] args) {
        if (RTVTByAlias.TryGetValue(name, out var rtvt)) {
            if (AliasesByRTVT.TryGetValue(rtvt, out var aliases)) {
                if (TypesByAlias.TryGetValue(aliases.First(), out var valueType)) {
                    type = valueType.PrototypeInstance.GetZeroValue().Invoke(args);
                    return true;
                }
            }

            throw new InterpreterRuntimeException($"Cannot find type for alias {name}");
        }

        throw new InterpreterRuntimeException($"Cannot find type for name {name}");
    }

    // A regular value proto, for example custom object using regular object prototype
    public static (ValueType type, Value proto) RegisterPrototype(string name, string fqn, Value proto) {
        var type   = new ValueType(name, proto.Type, proto);
        var symbol = proto.SetSymbol(name);

        Types.Add(type);
        TypesBySymbol[symbol] = type;
        TypesByName[name]     = type;
        TypesByFQN[fqn]       = type;

        TypePrototypes[type] = proto.PrototypeType;
        TypesByFQN[fqn]      = type;

        return (type, proto);
    }

    public static (Value structValue, StructPrototype proto) DeclareStruct(ExecContext ctx, StructDeclaration decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var (structValue, newProto) = StructPrototype.MakeChild(
            ctx, declName
        );
        
        var proto = newProto.Proto;
        
        foreach (var member in decl.Members) {
            proto[member.Name] = TryMakeFromName(ctx, member.Name, out var type) ? type : Value.Null();
        }

        foreach (var method in decl.Methods) {
            proto[method.Name] = ctx.MakeFunction(method);
        }
        
        RegisterStaticPrototype(newProto);

        return structValue;
    }

}