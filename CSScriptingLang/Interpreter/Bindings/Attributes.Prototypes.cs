using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Class)]
public class LanguagePrototypeAttribute : LanguageClassBind
{
    public RTVT PrototypeType   { get; set; }
    public Type ParentPrototype { get; set; }
    public LanguagePrototypeAttribute(string name, RTVT type, Type parentPrototype) : base(name) {
        PrototypeType   = type;
        ParentPrototype = parentPrototype;
    }
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