namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LanguageModuleBindAttribute : Attribute
{
    public string Name               { get; set; }
    public bool   FunctionsAsGlobals { get; set; } = true;

    public LanguageModuleBindAttribute() { }
    public LanguageModuleBindAttribute(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class LanguageOperatorAttribute : Attribute
{
    public string Operator { get; set; }

    public LanguageOperatorAttribute(string op) {
        Operator = op;
    }
}