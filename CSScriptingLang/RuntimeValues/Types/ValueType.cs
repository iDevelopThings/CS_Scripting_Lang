using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Types;

// [LanguageClassBind("Type")]
public partial class ValueType : Value
{
    public  Prototype PrototypeInstance { get; set; }
    
    private string    _name;
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
        Prototype         = prototype;
    }
    
    public ValueType(string name, RTVT type, Prototype prototype) : base(RTVT.Object) {
        Name              = name;
        ForType           = type;
        PrototypeInstance = prototype;
        Prototype         = prototype;
    }

    public ValueType(string name, RTVT type, Value protoObject) : base(RTVT.Object) {
        Name      = name;
        ForType   = type;
        Prototype = protoObject.PrototypeType;
    }
}