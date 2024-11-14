namespace CSScriptingLang.Interpreter.Bindings;

/// <summary>
/// The closest version of an implementation def for the binding
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class LanguageMetaDefinition : Attribute
{
    public string DefString { get; set; }

    public LanguageMetaDefinition(string def) {
        DefString = def;
    }
}