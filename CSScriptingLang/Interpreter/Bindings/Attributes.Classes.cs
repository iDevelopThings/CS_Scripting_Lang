namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Class)]
public class LanguageClassBind : Attribute
{
    public string Name { get; set; }

    public LanguageClassBind() { }
    public LanguageClassBind(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
public class LanguageValueConstructorAttribute : Attribute {}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LanguagePropertyGetterAttribute : Attribute
{
    public string Name { get; set; }

    public LanguagePropertyGetterAttribute() { }
    public LanguagePropertyGetterAttribute(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LanguagePropertySetterAttribute : Attribute
{
    public string Name { get; set; }

    public LanguagePropertySetterAttribute() { }
    public LanguagePropertySetterAttribute(string name) {
        Name = name;
    }
}
