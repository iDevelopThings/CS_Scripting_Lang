using System.Reflection;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;


public class TypeTable : IDisposable
{
    public static TypeTable Current { get; set; } = new(null, true);

    public TypeTable Parent { get; set; }


    public HashSet<RuntimeType> Types { get; } = new();

    public Dictionary<string, RuntimeType> TypesByName { get; } = new();
    public Dictionary<string, RuntimeType> TypeAliases { get; } = new();

    public Dictionary<RTVT, List<RuntimeType>> TypesByType { get; } = new();

    // Holds types by their runtime type, for ex,
    // `typeof(CSScriptingLang.RuntimeValues.Values.Object)` -> RuntimeTypeInfo_Object
    public Dictionary<Type, List<RuntimeType>> TypesByRuntimeType { get; } = new();

    // This is essentially the raw native value type -> RuntimeTypeInfo mapping
    // For ex; typeof(int) -> RuntimeTypeInfo_Number, typeof(string) -> RuntimeTypeInfo_String
    public Dictionary<Type, HashSet<RuntimeType>> TypesByValueType { get; } = new();

    // This is the type info class type -> RuntimeTypeInfo mapping
    // For ex; typeof(RuntimeTypeInfo_Number) -> Number, typeof(RuntimeTypeInfo_Object) -> Object
    public Dictionary<Type, HashSet<RuntimeType>> TypesByNativeType { get; } = new();

    public static Dictionary<RTVT, int> UniqueTypeIds { get; } = new();

    public TypeTable(TypeTable parent = null, bool isGlobal = false) {
        if (parent != null) {
            Parent = parent;
        }

        if (isGlobal) {
            UniqueTypeIds?.Clear();

            RegisterStaticTypes();
        }
    }


    private void RegisterStaticTypes() {
        var staticTypes = typeof(StaticTypes)
           .GetFields(BindingFlags.Public | BindingFlags.Static)
           .Where(p => p.FieldType.IsSubclassOf(typeof(RuntimeType)))
           .ToDictionary(p => p.Name, p => p);

        foreach (var pair in staticTypes) {
            var rtType    = (RuntimeType) pair.Value.GetValue(null)!;
            var aliasAttr = pair.Value.GetCustomAttribute<StaticTypeAliasAttribute>();
            RegisterType(rtType, aliasAttr?.Aliases);
            if (rtType.ValueType != null)
                TypesByValueType.GetOrAdd(rtType.ValueType).Add(rtType);

            TypesByNativeType.GetOrAdd(rtType.GetType()).Add(rtType);
        }
    }

    public static RuntimeType GetFromValueType(object value) {
        return Current.FromValueType(value);
    }
    public static RuntimeType TryGet(Type type) => Current.Get(type);
    public static bool TryGet(Type type, out RuntimeType rtType) {
        var r = Current.Get(type);
        if (r != null) {
            rtType = r;
            return true;
        }

        rtType = null;
        return false;
    }
    public static RuntimeType TryGet(RTVT type) => Current.Get(type);
    public static bool TryGet(RTVT type, out RuntimeType rtType) {
        var r = Current.Get(type);
        if (r != null) {
            rtType = r;
            return true;
        }

        rtType = null;
        return false;
    }
    public static RuntimeType TryGet(string name) => Current.Get(name);
    public static bool TryGet(string name, out RuntimeType rtType) {
        var r = Current.Get(name);
        if (r != null) {
            rtType = r;
            return true;
        }

        rtType = null;
        return false;
    }

    public RuntimeType Get(Type type) {
        if (type == null)
            return StaticTypes.Null;


        return type switch {
            {IsValueType: true} when type == typeof(int)    => StaticTypes.Int32,
            {IsValueType: true} when type == typeof(long)   => StaticTypes.Int64,
            {IsValueType: true} when type == typeof(double) => StaticTypes.Double,
            {IsValueType: true} when type == typeof(float)  => StaticTypes.Float,

            {IsValueType: true} when type == typeof(bool)    => StaticTypes.Boolean,
            {IsValueType: false} when type == typeof(string) => StaticTypes.String,

            _ => throw new ArgumentException($"Cannot convert type {type?.Name} to a runtime value type")
        };
    }
    public RuntimeType Get(RTVT type) {
        return TypesByType.TryGetValue(type, out var types)
            ? types.First()
            : Parent?.Get(type);
    }
    public RuntimeType Get(string name) {
        if (TypesByName.TryGetValue(name, out var type)) {
            return type;
        }

        if (TypeAliases.TryGetValue(name, out type)) {
            return type;
        }

        return Parent?.Get(name);
    }

    public static string GenerateUniqueTypeId(RTVT type) {
        var id = UniqueTypeIds.GetValueOrDefault(type, 0);

        UniqueTypeIds[type] = id + 1;

        return $"{type}_{id}";
    }

    public RuntimeTypeInfo_Object RegisterObjectType(string name, RuntimeType owner, params string[] aliases) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Object);

        var rtType = new RuntimeTypeInfo_Object {
            Name  = name,
            Owner = owner,
        };
        RegisterType(rtType, aliases);
        return rtType;
    }
    public RuntimeTypeInfo_Function RegisterFunctionType(string name, FunctionDeclaration node, RuntimeType owner = null) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Function);

        var rtType = new RuntimeTypeInfo_Function {
            Name       = name,
            Owner      = owner,
            LinkedNode = node
        };
        RegisterType(rtType);
        return rtType;
    }
    public RuntimeTypeInfo_Signal RegisterSignalType(string name, SignalDeclarationNode node, RuntimeType owner = null) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Signal);

        var rtType = new RuntimeTypeInfo_Signal {
            Name       = name,
            Owner      = owner,
            LinkedNode = node
        };
        RegisterType(rtType);
        return rtType;
    }


    public void RegisterType(RuntimeType rtType, params string[] aliases) {
        if (rtType == null)
            throw new ArgumentNullException(nameof(rtType));

        if (!TypesByName.TryAdd(rtType.Name, rtType))
            throw new DeclarationException($"Type {rtType.Name} is already registered", TypesByName[rtType.Name]);

        if (aliases != null) {
            foreach (var alias in aliases) {
                if (!TypeAliases.TryAdd(alias, rtType))
                    throw new DeclarationException($"Type alias {alias} is already registered", TypeAliases[alias]);
            }
        }

        TypesByType.GetOrAdd(rtType.Type).Add(rtType);
        TypesByNativeType.GetOrAdd(rtType.GetType()).Add(rtType);
        if (rtType.ValueType != null)
            TypesByValueType.GetOrAdd(rtType.ValueType).Add(rtType);

        TypesByRuntimeType.GetOrAdd(rtType.RuntimeValueType).Add(rtType);

        Types.Add(rtType);

    }

    public RuntimeType FromValueType(object value) {
        if (value == null)
            return StaticTypes.Null;

        var resultType = value switch {
            BaseValue rtValue => rtValue.RuntimeType,
            RuntimeType info  => info,

            Type ty when TypesByValueType.TryGetValue(ty, out var tType)   => tType.First(),
            Type ty when TypesByNativeType.TryGetValue(ty, out var tType)  => tType.First(),
            Type ty when TypesByRuntimeType.TryGetValue(ty, out var tType) => tType.First(),

            // Type ty when StaticTypes.TypesByValueType.TryGetValue(ty, out var tType) => tType, 

            _ => null
        };


        if (resultType != null)
            return resultType;

        if (TypesByValueType.TryGetValue(value.GetType(), out var rtType))
            return rtType.First();

        if (Parent?.FromValueType(value) is { } parentType)
            return parentType;

        return Get(value.GetType());
    }

    public static bool AreSameRtType(RuntimeType a, RuntimeType b) => a.Type == b.Type;
    public static bool AreSameRtType(RuntimeType a, object      b) => a.Type == GetFromValueType(b).Type;

    public void Dispose() { }
}