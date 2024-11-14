namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
public class LanguageFunctionAttribute : Attribute
{
    public string Name { get; set; }

    public LanguageFunctionAttribute() { }
    public LanguageFunctionAttribute(string name) {
        Name = name;
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class LanguageInstanceGetterFunctionAttribute : Attribute
{
    public string Name { get; set; }

    public LanguageInstanceGetterFunctionAttribute() { }
    public LanguageInstanceGetterFunctionAttribute(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
public class LanguageGlobalFunctionAttribute : LanguageFunctionAttribute
{
    public LanguageGlobalFunctionAttribute() { }
    public LanguageGlobalFunctionAttribute(string name) : base(name) { }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
public class LanguageFunctionDisableParameterChecks : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
public class LanguageInstanceAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
public class LanguageParameterAttribute : Attribute
{
    public bool IsOptional { get; set; }
    public bool PassRawValue { get; set; }

    public LanguageParameterAttribute() { }
    public LanguageParameterAttribute(bool isOptional) {
        IsOptional = isOptional;
    }
    public LanguageParameterAttribute(bool isOptional, bool passRawValue) {
        IsOptional = isOptional;
        PassRawValue = passRawValue;
    }
}