namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Class)]
public class LanguagePrototypeAttribute : LanguageClassBind
{
    public LanguagePrototypeAttribute(string name = null) : base(name) { }
}

[AttributeUsage(AttributeTargets.Class)]
public class PrototypeBootAttribute : Attribute
{
    public int BootOrder { get; set; } = 0;

    public PrototypeBootAttribute() { }
    
    public PrototypeBootAttribute(int bootOrder) {
        BootOrder = bootOrder;
    }
}