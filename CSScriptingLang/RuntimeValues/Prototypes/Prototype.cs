using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using Engine.Engine.Logging;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public class Prototype
{
    protected Value        _proto;
    protected ValueType    _type;
    protected List<string> _aliases = new();
    protected RTVT         _rtvt;

    /** The constructed prototype object from the source generator */
    public virtual Value Proto {
        get => _proto;
        set => _proto = value;
    }


    /** The value type object representing the prototype */
    public virtual ValueType ValueType {
        get => _type;
        set => _type = value;
    }

    public virtual RTVT Rtvt {
        get => _rtvt;
        set => _rtvt = value;
    }


    /** Aliases for the typename/prototype for the TypesTable */
    public virtual List<string> Aliases {
        get => _aliases;
        set => _aliases = value;
    }

    /** The symbol for the prototype */
    [LanguageFunction]
    public virtual Symbol Symbol => throw new NotImplementedException();

    /** Whether the prototype is a primitive type(types like string, int etc) */
    public virtual bool IsPrimitive { get; } = false;

    public static implicit operator Value(Prototype prototype) {
        if (prototype == null) {
            return null;
        }
        return prototype.Proto;
    }


    public delegate Value ZeroValueConstructor(params object[] args);

    public virtual ZeroValueConstructor GetZeroValue() => throw new NotImplementedException();

}

public class Prototype<T> : Prototype where T : Prototype<T>, new()
{
    protected static Logger Logger = Logs.Get<T>();

    private static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                _instance = new T();
            }
            return _instance;
        }
    }

    public static Value     ProtoType => Instance._proto;
    public static ValueType Type      => Instance._type;

    public Prototype() { }
    public Prototype(RTVT rtvt) {
        Rtvt      = rtvt;
        ValueType = new(rtvt, this);
    }
    public delegate Value BuildPrototype(T       protoDef, Value basePrototype                 = null);
    public delegate Value BuildPrototypeTo(Value buildTo,  T     protoDef, Value basePrototype = null);

    protected Prototype(
        RTVT           rtvt,
        BuildPrototype build,
        Prototype      basePrototype = null
    ) {
        Rtvt      = rtvt;
        ValueType = new(rtvt, this);
        Proto     = build(Instance, basePrototype);
    }

    protected Prototype(
        RTVT             rtvt,
        BuildPrototypeTo build,
        Value            buildTo,
        Prototype        basePrototype = null
    ) {
        Rtvt      = rtvt;
        ValueType = new(rtvt, this);
        Proto     = build(buildTo, Instance, basePrototype);
    }



    public static T Boot() {
        if (_instance == null) {
            _instance = new T();
        }
        return _instance;
    }

}