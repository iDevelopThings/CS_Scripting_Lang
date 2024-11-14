using System.Reflection;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.ReflectionUtils;
using StructDeclaration = CSScriptingLang.Interpreter.Execution.Declaration.StructDeclaration;

namespace CSScriptingLang.RuntimeValues.Types;

public static class StaticType
{
    public static Prototype<Int32Prototype>    Int32()    => Int32Prototype.Instance;
    public static Prototype<Int64Prototype>    Int64()    => Int64Prototype.Instance;
    public static Prototype<FloatPrototype>    Float()    => FloatPrototype.Instance;
    public static Prototype<DoublePrototype>   Double()   => DoublePrototype.Instance;
    public static Prototype<NumberPrototype>   Number()   => NumberPrototype.Instance;
    public static Prototype<StringPrototype>   String()   => StringPrototype.Instance;
    public static Prototype<BooleanPrototype>  Boolean()  => BooleanPrototype.Instance;
    public static Prototype<UnitPrototype>     Unit()     => UnitPrototype.Instance;
    public static Prototype<NullPrototype>     Null()     => NullPrototype.Instance;
    public static Prototype<ObjectPrototype>   Object()   => ObjectPrototype.Instance;
    public static Prototype<StructPrototype>   Struct()   => StructPrototype.Instance;
    public static Prototype<ArrayPrototype>    Array()    => ArrayPrototype.Instance;
    public static Prototype<FunctionPrototype> Function() => FunctionPrototype.Instance;
    public static Prototype<SignalPrototype>   Signal()   => SignalPrototype.Instance;
    public static Prototype<EnumPrototype>     Enum()     => EnumPrototype.Instance;


}

public class TypesTable
{
    /// <summary>
    /// A list of all unique ValueType instances
    /// </summary>
    public static List<ValueType> Types = new();

    /// <summary>
    /// Symbol(unique) -> To unique ValueType instance
    /// </summary>
    public static Dictionary<Symbol, ValueType> TypesBySymbol = new();

    /// <summary>
    /// C# typeof prototype type class -> To unique ValueType instance
    /// </summary>
    public static Dictionary<Type, ValueType> TypesByPrototypeNativeType = new();

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
    public static Dictionary<RTVT, List<ValueType>> TypesByRTVT = new();


    public static Dictionary<string, RTVT>       RTVTByAlias   = new();
    public static Dictionary<RTVT, List<string>> AliasesByRTVT = new();

    public static List<Prototype> Prototypes = new();

    /// <summary>
    /// Mainly for generated types, objects/prototypes get a type registered with namespace like `CSScriptingLang.RuntimeValues.Values.Object`
    /// </summary>
    public static Dictionary<string, ValueType> TypesByFQN = new();

    static TypesTable() { }

    public static bool IsBindingPrototypes = false;

    public static void Initialize(ExecContext ctx) {
        IsBindingPrototypes = true;

        var types = ReflectionStore.AllTypesWithAttributeIncludingAttr<PrototypeBootAttribute>()!
           .OrderBy(t => t.attribute.BootOrder)
           .Select(t => new {
                type       = t.type,
                attribute  = t.attribute,
                bootMethod = t.type.GetMethod("Boot", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy),
            })
           .ToList();

        var protos = new List<Prototype>();

        foreach (var type in types) {
            var proto = (Prototype) type.bootMethod.Invoke(null, [ctx]);
            protos.Add(proto);
        }

        foreach (var proto in protos) {
            if (proto == null) {
                throw new InvalidOperationException("Prototype is null");
            }

            RegisterStaticPrototype(proto);

            var binding = proto.Binding;
            ctx.Libraries.Add(ctx, binding);
        }

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
            RTVT.Signal         => StaticType.Signal(),
            RTVT.Enum           => StaticType.Enum(),
            RTVT.EnumMember     => StaticType.Null(),
            RTVT.ValueReference => null,
            _                   => throw new NotImplementedException($"Prototype::For({type}) not implemented"),
        };
    }

    private static void RegisterStaticPrototype(Prototype proto) {
        var type   = proto.ValueType;
        var symbol = proto.Symbol;

        using var _ = type.SuppressDispatch();

        Types.Add(type);
        TypesBySymbol[symbol]                       = type;
        TypesByName[type.Name]                      = type;
        TypesByPrototypeNativeType[proto.GetType()] = type;

        var aliases = proto.Aliases.Concat([type.Name.ToLower(), type.Name])
           .Distinct()
           .Where(a => a != null)
           .ToList();
        aliases.ForEach(alias => TypesByAlias[alias] = type);

        TypePrototypes[type] = proto;

        Prototypes.Add(proto);

        TypesByRTVT.GetOrAdd(type.ForType).Add(type);

        aliases.ForEach(alias => RTVTByAlias[alias] = type.ForType);
        AliasesByRTVT.GetOrAdd(type.ForType).AddRange(aliases);

        TypesByFQN[proto.FQN] = type;
    }

    public static bool GetPrototypeTypeByName(string name, out ValueType type) {
        var val = GetPrototypeTypeByName(name);
        if (val != null) {
            type = val;
            return true;
        }

        type = null;
        return false;
    }
    public static ValueType GetPrototypeTypeByName(string name) {
        if (TypesByName.TryGetValue(name, out var type)) {
            return type;
        }

        if (TypesByAlias.TryGetValue(name, out type)) {
            return type;
        }

        return null;
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

    public static ObjectPrototype DeclareInterface(ExecContext ctx, InterfaceDeclaration decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var newProto = ObjectPrototype.MakeChild(ctx, declName);

        foreach (var member in decl.Members) {

            var m = new MemberMeta(member);
            m.ValueConstructor = () => member.Type switch {
                _ => throw new NotImplementedException($"Interface member type {member.Type} not implemented"),
            };

        }

        RegisterStaticPrototype(newProto);

        return newProto;
    }
    public static ObjectPrototype DeclareInterface(ExecContext ctx, InterfaceDecl decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var newProto = ObjectPrototype.MakeChild(ctx, declName);

        foreach (var member in decl.Members) {

            var m = new MemberMeta(member);
            m.ValueConstructor = () => member.Type switch {
                _ => throw new NotImplementedException($"Interface member type {member.Type} not implemented"),
            };

        }

        RegisterStaticPrototype(newProto);

        return newProto;
    }

    public static StructPrototype DeclareStruct(ExecContext ctx, StructDeclaration decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var newProto = StructPrototype.MakeChild(
            ctx, declName
        );

        var proto = newProto.Proto;

        foreach (var member in decl.Members) {
            var m = new MemberMeta(member);

            if (member.Type == TypeDeclMemberType.Field) {
                m.ValueConstructor = () => TryMakeFromName(ctx, member.TypeIdentifier, out var type) ? type : Value.Null();
                newProto.DeclaredMembers.Add(m);
            }

            if (member.Type == TypeDeclMemberType.Method) {
                m.ValueConstructor = () => ctx.MakeFunction(member.FunctionDeclaration);
                newProto.DeclaredMembers.Add(m);
            }

            if (member.Type == TypeDeclMemberType.Constructor) {
                m.ValueConstructor = () => ctx.MakeFunction(member.FunctionDeclaration);
                newProto.DeclaredMembers.Add(m);
            }

        }

        RegisterStaticPrototype(newProto);

        return newProto;
    }
    
    public static StructPrototype DeclareStruct(ExecContext ctx, StructDecl decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var newProto = StructPrototype.MakeChild(
            ctx, declName
        );

        var proto = newProto.Proto;

        foreach (var member in decl.Members) {
            var m = new MemberMeta(member);

            if (member.MemberKind == TypeDeclMemberType.Field) {
                m.ValueConstructor = () => TryMakeFromName(ctx, member.Type, out var type) ? type : Value.Null();
                newProto.DeclaredMembers.Add(m);
            }

            if (member.MemberKind == TypeDeclMemberType.Method) {
                m.ValueConstructor = () => ctx.MakeFunction(member.FunctionDecl);
                newProto.DeclaredMembers.Add(m);
            }

            if (member.MemberKind == TypeDeclMemberType.Constructor) {
                m.ValueConstructor = () => ctx.MakeFunction(member.FunctionDecl);
                newProto.DeclaredMembers.Add(m);
            }

        }

        RegisterStaticPrototype(newProto);

        return newProto;
    }

    public static EnumPrototype DeclareEnum(ExecContext ctx, EnumDeclaration decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var proto = new EnumPrototype(ctx, declName, null, declName);

        foreach (var ctor in decl.Constructors) {
            var m = new MemberMeta(ctor);
            proto.DeclaredMembers.Add(m);
        }
        
        foreach (var member in decl.EnumMembers) {
            var m = new MemberMeta(member);
            m.ValueConstructor = () => {
                var memberValue = proto.MakeEnumMember(ctx, m);
                return memberValue;
            };

            proto.DeclaredMembers.Add(m);

            proto.Proto[member.Name] = m.ValueConstructor();

        }

        
        RegisterStaticPrototype(proto);

        return proto;
    }
    
    public static EnumPrototype DeclareEnum(ExecContext ctx, EnumDecl decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var proto = new EnumPrototype(ctx, declName, null, declName);

        var ordinal = 0;
        foreach (var ctor in decl.Members) {
            var m = new MemberMeta(ctor) {
                Ordinal = ordinal++,
            };
            proto.DeclaredMembers.Add(m);
        }

        ordinal = 0;
        foreach (var member in decl.EnumMembers) {
            var m = new MemberMeta(member);
            m.Ordinal = ordinal++;
            m.ValueConstructor = () => {
                var memberValue = proto.MakeEnumMember(ctx, m);
                return memberValue;
            };

            var idx = proto.DeclaredMembers.FindIndex(mem => mem.Name == member.Name);
            if (idx != -1) {
                proto.DeclaredMembers[idx] = m;
            } else {
                proto.DeclaredMembers.Add(m);
            }

            proto.Proto[member.Name] = m.ValueConstructor();

        }

        
        RegisterStaticPrototype(proto);

        return proto;
    }

    public static SignalPrototype DeclareSignal(ExecContext ctx, SignalDecl decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var (value, newProto) = SignalPrototype.MakeChild(ctx, declName);
        var proto = newProto.Proto;

        var signalValue = new Signal() {
            Name           = declName,
            ParameterTypes = decl.Arguments.Select(p => p.Type.TypeName).ToList(),
        };

        proto.SetValue(signalValue);

        RegisterStaticPrototype(newProto);

        return newProto;
    }
    public static SignalPrototype DeclareSignal(ExecContext ctx, SignalDeclaration decl) {
        var declName = ctx.ModulePrefixedName(decl.Name);

        var (value, newProto) = SignalPrototype.MakeChild(ctx, declName);
        var proto = newProto.Proto;

        var signalValue = new Signal() {
            Name           = declName,
            ParameterTypes = decl.Parameters.Select(p => p.TypeIdentifier.Name).ToList(),
        };

        proto.SetValue(signalValue);

        RegisterStaticPrototype(newProto);

        return newProto;
    }

    public static ObjectPrototype DeclareCustomObjectPrototype(ExecContext ctx, string name, string fqn, Value proto) {
        var declName = ctx.ModulePrefixedName(name);

        var newProto = ObjectPrototype.MakeChild(ctx, declName, proto, fqn);

        RegisterStaticPrototype(newProto);

        return newProto;
    }

    public static object Dump() {
        return new {
            Types,
            TypesBySymbol,
            TypesByPrototypeNativeType,
            TypesByName,
            TypesByAlias,
            TypePrototypes,
            TypesByRTVT,
            RTVTByAlias,
            AliasesByRTVT,
            Prototypes,
            TypesByFQN,
        };
    }
}