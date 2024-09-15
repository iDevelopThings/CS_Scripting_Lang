using System.Reflection;

namespace CSScriptingLang.Utils.ReflectionUtils;

public static class ReflectionExtensions
{
    private static readonly Func<PropertyInfo, bool> IsInstance       = (PropertyInfo property) => !((property.GetMethod ?? property.SetMethod)!).IsStatic;
    private static readonly Func<PropertyInfo, bool> IsInstancePublic = (PropertyInfo property) => IsInstance(property) && ((property.GetMethod ?? property.SetMethod)!).IsPublic;

    public static bool IsSubclassOfInclGenerics(this Type type, Type other) {
        // if (type.BaseType is {IsGenericType: true} && type.BaseType.GetGenericTypeDefinition() == typeof(GlobalSingletonSystemBase<>)) {
        if (type.BaseType() is {IsGenericType: true} && type.BaseType().GetGenericTypeDefinition() == other) {
            return true;
        }

        return other.IsAssignableFrom(type);
    }

    public static IEnumerable<PropertyInfo> GetProperties(this Type type, bool includeNonPublic) {
        var predicate = includeNonPublic ? IsInstance : IsInstancePublic;

        return type.IsInterface()
            ? (new Type[] {type})
           .Concat(type.GetInterfaces())
           .SelectMany(i => i.GetRuntimeProperties().Where(predicate))
            : type.GetRuntimeProperties().Where(predicate);
    }
    public static PropertyInfo GetPublicProperty(this Type type, string name) {
        return type.GetRuntimeProperty(name);
    }
    public static Type BaseType(this Type type) {
        return type.GetTypeInfo().BaseType;
    }

    public static bool IsValueType(this Type type) {
        return type.GetTypeInfo().IsValueType;
    }

    public static bool IsGenericType(this Type type) {
        return type.GetTypeInfo().IsGenericType;
    }

    public static bool IsGenericTypeDefinition(this Type type) {
        return type.GetTypeInfo().IsGenericTypeDefinition;
    }

    public static bool IsInterface(this Type type) {
        return type.GetTypeInfo().IsInterface;
    }

    public static bool IsEnum(this Type type) {
        return type.GetTypeInfo().IsEnum;
    }

    public static bool IsRequired(this MemberInfo member) {
        var result = member.GetCustomAttributes<System.Runtime.CompilerServices.RequiredMemberAttribute>().Any();
        return result;
    }

    public static Attribute[] GetAllCustomAttributes<TAttribute>(this PropertyInfo member) {
        // IMemberInfo.GetCustomAttributes ignores it's "inherit" parameter for properties,
        // and the suggested replacement (Attribute.GetCustomAttributes) is not available
        // on netstandard1.3
        var result = new List<Attribute>();
        var type   = member.DeclaringType;
        var name   = member.Name;

        while (type != null) {
            var property = type.GetPublicProperty(name);

            if (property != null) {
                result.AddRange(property.GetCustomAttributes(typeof(TAttribute)));
            }

            type = type.BaseType();
        }

        return result.ToArray();
    }

    public static object ReadValue(this PropertyInfo property, object target) {
        return property.GetValue(target, null);
    }
    
    public static bool IsAsync(this MethodInfo method) {
        var rtType = method.ReturnType;
        if(rtType == typeof(Task)) {
            return true;
        }
        
        if(rtType.IsGenericType() && rtType.GetGenericTypeDefinition() == typeof(Task<>)) {
            return true;
        }
        
        return false;
    }
    
    public static IEnumerable<MemberInfo> GetPropertiesAndFields(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
        return type.GetProperties(flags).Cast<MemberInfo>().Concat(type.GetFields(flags));
    }
    
    public static IEnumerable<StandaloneObjectMember> GetPropertyProxies(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
        foreach (var member in type.GetPropertiesAndFields(flags)) {
            yield return new StandaloneObjectMember(member);
        }
    }
    
    public static IEnumerable<(MethodInfo, T)> MethodsWithAttribute<T>(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) where T : Attribute {
        return type.GetMethods(flags)
                   .Select(method => (method, method.GetCustomAttribute<T>()))
                   .Where(pair => pair.Item2 != null)
                   .Select(pair => (pair.method, pair.Item2!));
    }
}