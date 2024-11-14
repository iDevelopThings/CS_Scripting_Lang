using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public interface IPrototypeObjectBinding : ILibrary
{
    Value BuildTo(Value obj, Prototype protoDef, Value basePrototype = null, Ty type = null);

    Value Build(Prototype protoDef, ExecContext ctx, Value basePrototype = null, Ty type = null);
}

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public class Prototype
{
    protected List<string> _aliases = new();

    private string _fqn;
    public string FQN {
        get => _fqn ?? $"{GetType().Name.Replace("Prototype", "")}";
        set => _fqn = value;
    }

    /** The constructed prototype object from the source generator */
    public virtual Value Proto { get; set; }
    
    /** The value type object representing the prototype */
    public virtual ValueType ValueType { get; set; }

    public virtual RTVT Rtvt { get; set; }

    public virtual Ty Ty { get; set; } 

    /** Aliases for the typename/prototype for the TypesTable */
    public virtual List<string> Aliases {
        get => _aliases;
        set => _aliases = value;
    }

    /** The symbol for the prototype */
    [LanguageFunction]
    public virtual Symbol Symbol { get; set; }

    /** Whether the prototype is a primitive type(types like string, int etc) */
    public virtual bool IsPrimitive { get; } = false;

    public IPrototypeObjectBinding Binding => GetType().GetField("Builder")!.GetValue(this) as IPrototypeObjectBinding;

    public static implicit operator Value(Prototype prototype) {
        if (prototype == null) {
            return null;
        }
        return prototype.Proto;
    }


    public delegate Value ZeroValueConstructor(params object[] args);

    public virtual ZeroValueConstructor GetZeroValue() => throw new NotImplementedException();

    public virtual string ToDebugString() => $"{Symbol.Name}(RTVT='{Rtvt}', Aliases=({Aliases.Select(n => $"'{n}'").Join(", ")}), FQN='{FQN}')";

    public virtual (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) => throw new InterpreterRuntimeException($"Cannot cast prototype {GetType().Name} to type {type}").WithCaller();

    public bool CanCastTo<T>() => CanCastTo(typeof(T));
    public virtual bool CanCastTo(RTVT type) {
        var (canCast, _) = GetCaster(type);
        return canCast;
    }
    public virtual bool CanCastTo(Type t) {
        var (canCast, _) = GetCaster(RTVTUtil.FromType(t));
        return canCast;
    }


    public bool CastTo<T>(Value value, out Value result) => CastTo(value, typeof(T), out result);

    public bool CastTo(Value value, Type t, out Value result) => CastTo(value, RTVTUtil.FromType(t), out result);

    public bool CastTo(Value value, RTVT type, out Value result) {
        var (canCast, cast) = GetCaster(type);
        if (!canCast) {
            result = null;
            return false;
        }

        result = cast(value);
        return true;
    }

    public Value CastTo<T>(Value value)            => CastTo<T>(value, out var result) ? result : null;
    public Value CastTo(Value    value, Type t)    => CastTo(value, t, out var result) ? result : null;
    public Value CastTo(Value    value, RTVT type) => CastTo(value, type, out var result) ? result : null;
}

public class Prototype<T> : Prototype where T : Prototype<T>
{
    protected static Logger Logger = Logs.Get<T>();

    private static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                throw new InvalidOperationException("Prototype not initialized");
            }
            return _instance;
        }
    }


    public static Value     ProtoType => Instance.Proto;
    public static ValueType Type      => Instance.ValueType;

    public Prototype(RTVT rtvt, ExecContext ctx) {
        Rtvt      = rtvt;
        ValueType = new(Symbol.Name, rtvt, this, ctx);
    }
    public delegate Value BuildPrototype(T       protoDef, Value basePrototype                 = null);
    public delegate Value BuildPrototypeTo(Value buildTo,  T     protoDef, Value basePrototype = null);

    protected Prototype() { }

    protected Prototype(
        RTVT           rtvt,
        BuildPrototype build,
        Prototype      basePrototype = null
    ) {
        Rtvt = rtvt;

        ValueType = new(rtvt, this);
        Proto     = build(Instance, basePrototype);

        Logger.Debug($"Built prototype {typeof(T).Name}");

    }

    protected Prototype(
        RTVT   rtvt,
        string name
    ) {
        Symbol    = Symbol.For(name);
        Rtvt      = rtvt;
        ValueType = new(rtvt, this);
    }


    public static T Boot(ExecContext ctx) {
        if (_instance == null) {
            _instance = Activator.CreateInstance(typeof(T), ctx) as T;

        }
        return _instance;
    }

}