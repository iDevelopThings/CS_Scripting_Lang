namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.All)]
public class LanguageBindTypeHint : Attribute
{
    public string Name { get; set; }
    public Type   Type { get; set; }

    public LanguageBindTypeHint(string name, Type type) {
        Name = name;
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class LanguageClassBind : Attribute
{
    public string Name { get; set; }

    public LanguageClassBind() { }
    public LanguageClassBind(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class LanguageClassDataObjectBind : Attribute;

[AttributeUsage(AttributeTargets.Module, AllowMultiple = true, Inherited = true)]
public class LanguageClassWrappableObjectBind : Attribute
{
    public Type ClassType { get; set; }
    public LanguageClassWrappableObjectBind(Type classType) {
        ClassType = classType;
    }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
public class LanguageValueConstructorAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LanguagePropertyAttribute : Attribute
{
    public string Name      { get; set; }
    public bool   UsePrefix { get; set; }

    public LanguagePropertyAttribute() { }
    public LanguagePropertyAttribute(string name) {
        Name = name;
    }
    public LanguagePropertyAttribute(string name, bool usePrefix) {
        Name      = name;
        UsePrefix = usePrefix;
    }
}

public class LanguagePropertyGetterAttribute : LanguagePropertyAttribute
{
    public LanguagePropertyGetterAttribute() : base() { }
    public LanguagePropertyGetterAttribute(string name) : base(name) { }
    public LanguagePropertyGetterAttribute(string name, bool usePrefix) : base(name, usePrefix) { }
}

public class LanguagePropertySetterAttribute : LanguagePropertyAttribute
{
    public LanguagePropertySetterAttribute() : base() { }
    public LanguagePropertySetterAttribute(string name) : base(name) { }
    public LanguagePropertySetterAttribute(string name, bool usePrefix) : base(name, usePrefix) { }

}