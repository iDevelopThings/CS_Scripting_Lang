﻿using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;

public static class StaticTypes
{
    public static RuntimeTypeInfo_Int32    Int32    = new();
    public static RuntimeTypeInfo_Int64    Int64    = new();
    public static RuntimeTypeInfo_Float    Float    = new();
    public static RuntimeTypeInfo_Double   Double   = new();
    public static RuntimeTypeInfo_String   String   = new();
    public static RuntimeTypeInfo_Boolean  Boolean  = new();
    public static RuntimeTypeInfo_Null     Null     = new();
    public static RuntimeTypeInfo_Object   Object   = new();
    public static RuntimeTypeInfo_Function Function = new();
    public static RuntimeTypeInfo_Array    Array    = new();

    public static Dictionary<RTVT, RuntimeTypeInfo> Types { get; } = new() {
        {RTVT.Int32, Int32},
        {RTVT.Int64, Int64},
        {RTVT.Float, Float},
        {RTVT.Double, Double},
        {RTVT.String, String},
        {RTVT.Boolean, Boolean},
        {RTVT.Null, Null},
        {RTVT.Object, Object},
        {RTVT.Function, Function},
        {RTVT.Array, Array},
    };

    public static Dictionary<Type, RuntimeTypeInfo> TypesByValueType { get; } = new() {
        {typeof(int), Int32},
        {typeof(long), Int64},
        {typeof(float), Float},
        {typeof(double), Double},
        {typeof(string), String},
        {typeof(bool), Boolean},
        {typeof(Dictionary<string, RuntimeValue>), Object},
        {typeof(RuntimeValue_Function), Function},
        {typeof(List<RuntimeValue>), Array},
    };
}

public class TypeTable : IDisposable
{
    public static TypeTable GlobalTypeTable { get; set; }

    public InterpreterExecutionContext InterpreterContext { get; set; }
    public ExecutionContext            Context            { get; set; }
    public TypeTable                   Parent             { get; set; }

    public Dictionary<string, RuntimeTypeInfo> Types { get; } = new();

    public Dictionary<RTVT, List<RuntimeTypeInfo>> TypesByType { get; } = new();

    // This is essentially the raw native value type -> RuntimeTypeInfo mapping
    public Dictionary<Type, RuntimeTypeInfo> TypesByValueType { get; } = new();

    // This is the type info class type -> RuntimeTypeInfo mapping
    // For ex; typeof(RuntimeTypeInfo_Number) -> Number, typeof(RuntimeTypeInfo_Object) -> Object
    public Dictionary<Type, RuntimeTypeInfo> TypesByNativeType { get; } = new();

    public static Dictionary<RTVT, int> UniqueTypeIds { get; } = new();

    public TypeTable() { }
    public TypeTable(InterpreterExecutionContext context, TypeTable parent) {
        InterpreterContext = context;
        Parent             = parent;
    }

    public void RegisterStaticTypes() {
        var staticTypes = typeof(StaticTypes)
           .GetFields(BindingFlags.Public | BindingFlags.Static)
           .Where(p => p.FieldType.IsSubclassOf(typeof(RuntimeTypeInfo)))
           .ToDictionary(p => p.Name, p => p);

        foreach (var pair in staticTypes) {
            var rtType = (RuntimeTypeInfo) pair.Value.GetValue(null)!;
            RegisterType(rtType);
            if (rtType.ValueType != null)
                TypesByValueType[rtType.ValueType] = rtType;

            TypesByNativeType[rtType.GetType()] = rtType;
        }

        /*
        foreach (var typeInfo in TypeInfo.StaticTypes) {
            var rtType = new RuntimeTypeInfo {
                Type      = typeInfo.Type,
                Name      = typeInfo.GetType().GetCustomAttribute<StaticTypeRegistrationAttribute>()!.Name,
                ValueType = typeInfo.ValueType
            };
            RegisterType(rtType);

            if (staticTypes.TryGetValue(rtType.Name, out var prop)) {
                prop.SetValue(null, rtType);

                rtType.IsPrimitive = true;
            }
        }*/
    }

    public RuntimeTypeInfo this[string name] => Get(name);


    public RuntimeTypeInfo Get(Type type) {
        if (type == null)
            return StaticTypes.Null;


        return type switch {
            {IsValueType: true} when type == typeof(int)    => StaticTypes.Int32,
            {IsValueType: true} when type == typeof(long)   => StaticTypes.Int64,
            {IsValueType: true} when type == typeof(double) => StaticTypes.Double,
            {IsValueType: true} when type == typeof(float)  => StaticTypes.Float,

            {IsValueType: true} when type == typeof(bool)    => StaticTypes.Boolean,
            {IsValueType: false} when type == typeof(string) => StaticTypes.String,
            // {IsValueType: false} when type == typeof(Dictionary<string, RuntimeValue>) => Object,
            // {IsValueType: false} when type == typeof(RuntimeValue_Function)            => Function,

            _ => throw new ArgumentException($"Cannot convert type {type?.Name} to a runtime value type")
        };
    }
    public RuntimeTypeInfo Get(RTVT type) {
        if (TypesByType.TryGetValue(type, out var types)) {
            return types.First();
        }

        return Parent?.Get(type);
    }
    public RuntimeTypeInfo Get(string name) {
        if (Types.TryGetValue(name, out var type)) {
            return type;
        }

        return Parent?.Get(name);
    }

    public static string GenerateUniqueTypeId(RTVT type) {
        var id = UniqueTypeIds.GetValueOrDefault(type, 0);

        UniqueTypeIds[type] = id + 1;

        return $"{type}_{id}";
    }

    public RuntimeTypeInfo_Object RegisterObjectType(string name, RuntimeTypeInfo owner) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Object);

        var rtType = new RuntimeTypeInfo_Object {
            Name  = name,
            Owner = owner,
        };
        RegisterType(rtType);
        return rtType;
    }
    public RuntimeTypeInfo_Function RegisterFunctionType(string name, int index, RuntimeTypeInfo owner) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Function);

        var rtType = new RuntimeTypeInfo_Function {
            Name  = name,
            Index = index,
            Owner = owner
        };
        RegisterType(rtType);
        return rtType;
    }


    private void RegisterType(RuntimeTypeInfo rtType) {
        if (Types.TryAdd(rtType.Name, rtType)) {
            rtType.Table = this;

            TypesByType.GetOrAdd(rtType.Type).Add(rtType);
            return;
        }

        throw new Exception($"Type {rtType.Name} is already registered");
    }

    public RuntimeTypeInfo FromValueType(object value) {
        if (value == null)
            return StaticTypes.Null;

        RuntimeTypeInfo resultType = value switch {
            RuntimeValue rtValue => rtValue.RuntimeType,
            RuntimeTypeInfo info => info,

            Type ty when TypesByValueType.TryGetValue(ty, out var tType)  => tType,
            Type ty when TypesByNativeType.TryGetValue(ty, out var tType) => tType,

            // Type ty when StaticTypes.TypesByValueType.TryGetValue(ty, out var tType) => tType, 

            _ => null
        };


        if (resultType != null)
            return resultType;

        // if (value is Type ty && TypesByValueType.TryGetValue(ty, out var tType))
        // return tType;

        if (TypesByValueType.TryGetValue(value.GetType(), out var rtType))
            return rtType;

        if (Parent?.FromValueType(value) is { } parentType)
            return parentType;

        return Get(value.GetType());
    }

    public static RuntimeTypeInfo GetFromValueType(object value) {
        return GlobalTypeTable.FromValueType(value);
    }


    public static bool AreSameRtType(RuntimeTypeInfo a, RuntimeTypeInfo b) => a.Type == b.Type;
    public static bool AreSameRtType(RuntimeTypeInfo a, object          b) => a.Type == GetFromValueType(b).Type;

    public void Dispose() { }
}