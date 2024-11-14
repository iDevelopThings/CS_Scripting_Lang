using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Prototypes.Types;

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public class Ty
{
    protected static Logger Logger = Logs.Get<Ty>();

    private bool Locked { get; set; }
    public  RTVT Type   { get; set; }

    private Ty _prototype;
    public Ty Prototype {
        get => _prototype;
        set {
            if (Locked) {
                throw new InvalidOperationException("Cannot change the prototype of a locked type");
            }
            _prototype = value;
        }
    }

    public Ty ElementType { get; set; } = null;

    private string _name;
    public string Name {
        get => _name ?? Type.ToString();
        set => _name = value;
    }
    
    public List<NamedSymbolInformation> NamedSymbols = new();

    public Dictionary<string, Ty> Members = new();

    protected Ty(RTVT type, Ty prototype = null) {
        Type      = type;
        Prototype = prototype ?? type.Prototype()?.Ty;
    }

    public string ToDebugString() {
        var str = $"{Type}";
        if (Prototype != null) {
            str += $" -> Protos({ProtoChain(false).Select(x => x.Name).Join(" : ")})";
        }

        if (Members.Count > 0) {
            str += $" {{ {Members.Select(x => x.Key).Join()} }}";
        }
        
        if(Prototype != null) {
            str += $" {{ {ProtoChain(false).SelectMany(x => x.Members).Select(x => x.Key).Join()} }}";
        }

        return str;
    }

    public Ty Lock(bool locked = true) {
        Locked = locked;
        return this;
    }
    public Ty Unlock() => Lock(false);
    public Ty UnlockedOp(Action<Ty> op) {
        var wasLocked = Locked;
        Locked = false;
        op(this);
        Locked = wasLocked;
        return this;
    }


    public IEnumerable<Ty> ProtoChain(bool includeSelf = true) {
        var current = this;
        while (current != null) {
            if (!includeSelf && current == this) {
                current = current.Prototype;
                continue;
            }
            yield return current;
            current = current.Prototype;
        }
    }

    public Ty this[string name] {
        get => GetMember(name, out var member) ? member : Null();
        set => SetMember(name, value);
    }

    public bool GetMember(string name, out Ty member) {
        var chain = ProtoChain();
        foreach (var proto in chain) {
            if (proto.Members.TryGetValue(name, out member)) {
                return true;
            }
        }
        member = null;
        return false;
    }
    public Ty GetMember(string name) => GetMember(name, out var member) ? member : Null();

    public bool HasMember(string name) => GetMember(name, out _);

    public Ty SetMember(string name, Ty member) {
        if (Locked) {
            throw new InvalidOperationException("Cannot change members of a locked type");
        }
        Members[name] = member;
        return this;
    }

    public static Ty Int32()   => new(RTVT.Int32);
    public static Ty Int64()   => new(RTVT.Int64);
    public static Ty Float()   => new(RTVT.Float);
    public static Ty Double()  => new(RTVT.Double);
    public static Ty String()  => new(RTVT.String);
    public static Ty Bool()    => new(RTVT.Boolean);
    public static Ty Boolean() => new(RTVT.Boolean);

    public static Ty Null() => new(RTVT.Null);
    public static Ty Unit() => new(RTVT.Unit);

    public static Ty Array(Ty elementType = null) {
        var t = new Ty(RTVT.Array);
        t.ElementType = elementType ?? Object();
        return t;
    }

    public static Ty Object() => new(RTVT.Object);
    public static Ty Struct() => new(RTVT.Struct);
    
    public static Ty Enum()   => new(RTVT.Enum);

    public static Ty Function(string name, Ty returnType = null) {
        var t = new Ty(RTVT.Function);
        t.Name = name;
        return t;
    }

}