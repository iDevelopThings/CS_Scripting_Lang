using System.Diagnostics;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;

namespace CSScriptingLang.RuntimeValues.Types;

public interface ITypeAlias
{
    public string    Name      { get; set; }
    public ValueType ValueType { get; set; }
    public Prototype Prototype { get; set; }
    public RTVT      Type      { get; }

    ITypeAlias GetMember(string member);
}

public class TypeAliasDebugView
{
    private readonly ITypeAlias v;

    public TypeAliasDebugView(ITypeAlias value) {
        v = value;
    }


    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [DebuggerDisplay("Count = {Members.Count}")]
    public KeyValuePair<string, ITypeAlias>[] Members => v.Prototype.Proto.AllMembers().Select(
        m => new KeyValuePair<string, ITypeAlias>(m.Key, new TypeAlias(m.Key, m.Value.PrototypeType))
    ).ToArray();

    public string    Name      => v.Name;
    public ValueType ValueType => v.ValueType;
    public Prototype Prototype => v.Prototype;
    public RTVT      Type      => v.Type;
}

[DebuggerTypeProxy(typeof(TypeAliasDebugView))]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public class TypeAlias : ITypeAlias
{
    public string    Name      { get; set; }
    public ValueType ValueType { get; set; }
    public Prototype Prototype { get; set; }
    public RTVT      Type      => ValueType.ForType;
    public Ty        Ty        => Prototype.Ty;

    public TypeAlias(string name, Prototype prototype) {
        Name      = name;
        Prototype = prototype;
        ValueType = prototype.ValueType;
    }

    public static ITypeAlias Get(string name) {
        if (TypesTable.GetPrototypeTypeByName(name, out var type)) {
            return new TypeAlias(name, type.PrototypeInstance);
        }

        throw new Exception($"Type '{name}' is not registered in the TypesTable");
    }

    public ITypeAlias GetMember(string member) {
        // if(ValueType.PrototypeInstance.Proto.GetMember(member, out var memberType)) {
        // return new TypeAlias(member, memberType.PrototypeType);
        // }
        if (Prototype.Proto.GetMember(member, out var memberType)) {
            return new TypeAlias(member, memberType.PrototypeType);
        }
        throw new Exception($"Type '{Name}' does not have member '{member}'");
    }

    public virtual string ToDebugString() {
        return $"{Name} : {Type}";
    }
}

public class TypeAlias<T> : TypeAlias, ITypeAlias where T : Prototype
{
    private static TypeAlias<T> _instance;

    public TypeAlias(string name, Prototype prototype) : base(name, prototype) { }

    public static implicit operator Prototype(TypeAlias<T> alias) => alias.Prototype;

    public static implicit operator TypeAlias<T>(Prototype prototype) => new TypeAlias<T>(prototype.Symbol.Name, prototype);

    public static TypeAlias<T> Get() {
        if (_instance != null)
            return _instance;

        if (TypesTable.TypesByPrototypeNativeType.TryGetValue(typeof(T), out var type)) {
            return _instance = new TypeAlias<T>(type.Name, TypesTable.TypePrototypes[type]);
        }

        throw new Exception($"Type '{typeof(T).Name}' is not registered in the TypesTable");
    }
}