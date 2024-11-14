using System.Reflection;

namespace CSScriptingLang.Common.Extensions;

public static class TypeExtensions
{

    public static bool GetAttribute(this Type @this, Type attributeType, out object attribute) {
        attribute = @this.GetCustomAttribute(attributeType);
        return attribute != null;
    }

    public static bool GetAttribute<T>(this Type @this, out T attribute) where T : Attribute {
        attribute = @this.GetCustomAttribute<T>();
        return attribute != null;
    }

}