using System.Diagnostics;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Types;

public class ValueTypeDebugView
{
    private readonly ValueType _value;

    public ValueTypeDebugView(ValueType value) {
        _value = value;
    }

    public string    Name              => _value.Name;
    public RTVT      ForType           => _value.ForType;
    public Prototype PrototypeInstance => _value.PrototypeInstance;
}

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
[DebuggerTypeProxy(typeof(ValueTypeDebugView))]
public partial class ValueType : Value
{
    public Prototype PrototypeInstance { get; set; }

    private string _name;
    public string Name {
        get {
            if (_name == null && PrototypeInstance != null) {
                return PrototypeInstance.Symbol.Name;
            }
            return _name;
        }
        set => _name = value;
    }

    public RTVT ForType { get; set; }


    public ValueType(RTVT type, Prototype prototype) : base(RTVT.Object) {
        ForType           = type;
        PrototypeInstance = prototype;
        Prototype         = prototype.Proto;
        SetSymbol("ValueType");
    }

    public ValueType(string name, RTVT type, Prototype prototype, ExecContext ctx) : base(RTVT.Object) {
        _context = ctx;

        Name              = name;
        ForType           = type;
        PrototypeInstance = prototype;
        Prototype         = prototype;

        if (prototype.Proto != null && prototype.Proto._context == null) {
            Prototype._context = ctx;
        }

        SetSymbol("ValueType");
    }

    public ValueType(string name, RTVT type, Value protoObject) : base(RTVT.Object) {
        Name      = name;
        ForType   = type;
        Prototype = protoObject.PrototypeType;

        SetSymbol("ValueType");
    }

    public FnClosure GetConstructorFn() {
        if (PrototypeInstance.Proto.GetMember("__ctor", out var ctor)) {
            return ctor.As.Fn();
        }
        return null;
    }

    public override string ToDebugString() {
        return $"{Name} -> {ForType} -> Symbol={PrototypeInstance?.Symbol.Name}";
    }
}