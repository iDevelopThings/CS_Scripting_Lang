using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class Value
{
    /** Access a member by name, or an array element by index */
    public Value this[Value path] {
        get => Get_MemberAccess(path);
        set => Set_MemberAccess(path, value);
    }
    public Value this[int path] {
        get => Get_MemberAccess(path);
        set => Set_MemberAccess(path, value);
    }
    public Value this[string path] {
        get => Get_MemberAccess(path);
        set => Set_MemberAccess(path, value);
    }

    public Value this[IdentifierExpression path] {
        get => this[path.Name];
        set => this[path.Name] = value;
    }

    public Value this[IdentifierExpr path] {
        get => this[path.Name];
        set => this[path.Name] = value;
    }

    private void OnSetMember(Value path, Value v) {
        if (v != null) {
            // if v doesn't have `_context` set but `this` does, then set it, and vice versa
            if (v._context == null && _context != null) {
                v._context = _context;
            } else if (v._context != null && _context == null) {
                _context = v._context;
            }
        }
    }

    public virtual Value GetMemberByPath(string path) {
        var parts = path.Split('.');
        var value = this;
        foreach (var part in parts) {
            value = value.GetMember(part);
        }

        return value;
    }

    public bool  GetMember(Value key, out Value v) => TryGet_MemberAccess(key, out v);
    public Value GetMember(Value key)          => TryGet_MemberAccess(key, out var v) ? v : Null();
    public bool  SetMember(Value key, Value v) => TrySet_MemberAccess(key, v);

    private bool TryGet_IndexAccess(Value index, out Value v) {
        if (!Is.Array || !index.Is.Int32) {
            v = Null();
            return false;
        }
        if (index.As.Int32() < 0 || index.As.Int32() >= As.Array().Count) {
            v = Null();
            return false;
        }
        v = As.Array()[index.As.Int32()];
        return true;
    }
    private Value Get_IndexAccess(Value index) => TryGet_IndexAccess(index, out var v) ? v : Null();

    private bool TrySet_IndexAccess(Value index, Value v) {
        if (!Is.Array || !index.Is.Int32) {
            return false;
        }
        if (index.As.Int32() < 0 || index.As.Int32() >= As.Array().Count) {
            return false;
        }
        OnSetMember(index, v);
        As.Array()[index.As.Int32()] = v;
        return true;
    }
    private void Set_IndexAccess(Value index, Value v) => TrySet_IndexAccess(index, v);

    public Prototype GetRootPrototype() {
        return Type switch {
            RTVT.ValueReference => value != null ? ((Value) value).GetRootPrototype() : TypesTable.For(Type),
            _                   => TypesTable.For(Type),
        };
    }
    public IEnumerable<Value> ProtoChain() {
        var rootProto = GetRootPrototype()?.Proto;
        var hitRoot   = false;
        var current   = this;
        while (current != null) {
            if (ReferenceEquals(current, rootProto)) {
                hitRoot = true;
            }

            yield return current;

            if (hitRoot) {
                break;
            }

            current = current.Prototype;
        }
    }
    public IEnumerable<KeyValuePair<string, Value>> AllMembers()       => ProtoChain().SelectMany(v => v.Members);
    public IEnumerable<KeyValuePair<string, Value>> AllUniqueMembers() => AllMembers().DistinctBy(kv => kv.Key);

    public IEnumerable<KeyValuePair<string, MemberMeta>> AllDeclaredMembers() {
        var chain = ProtoChain();
        foreach (var proto in chain) {
            var protoType = proto.PrototypeType;
            if (protoType != null) {
                if (protoType is IPrototypeMembers members) {
                    foreach (var member in members.DeclaredMembers) {
                        yield return new KeyValuePair<string, MemberMeta>(member.Name, member);
                    }
                }
            }
        }
    }

    protected bool Get_Field(Value name, RTVT type, out Value v) {
        var chain = ProtoChain();
        foreach (var proto in chain) {
            if (proto.Members.TryGetValue(name.As.String(), out v)) {
                if (v.Is.A(type)) {
                    return true;
                }
            }
        }
        v = null;
        return false;
    }
    private bool Get_FieldAccess(Value name, out Value v) {
        var chain = ProtoChain();
        foreach (var proto in chain) {
            if (proto.Members.TryGetValue(name.As.String(), out v)) {
                return true;
            }
        }
        v = null;
        return false;
    }
    private Value Get_FieldAccess(Value name) => Get_FieldAccess(name, out var v) ? v : Null();

    private bool TrySet_FieldAccess(Value name, Value v) {
        OnSetMember(name, v);

        Members[name.As.String()] = v;
        return true;
    }
    private void Set_FieldAccess(Value name, Value v) => TrySet_FieldAccess(name, v);

    // We can get array elements, and object members(object members could also use int keys)
    private bool TryGet_MemberAccess(Value key, out Value v) {
        if (key.Is.Int32) {
            return TryGet_IndexAccess(key, out v);
        }
        return Get_FieldAccess(key, out v);
    }
    private Value Get_MemberAccess(Value key) => TryGet_MemberAccess(key, out var v) ? v : Null();

    private bool TrySet_MemberAccess(Value key, Value v) {
        if (key.Is.Int32) {
            return TrySet_IndexAccess(key, v);
        }
        return TrySet_FieldAccess(key, v);
    }

    private void Set_MemberAccess(Value key, Value v) => TrySet_MemberAccess(key, v);

    public bool HasMember(Value key) => TryGet_MemberAccess(key, out _);
    public bool HasValue(Value v) {
        var chain = ProtoChain();
        foreach (var proto in chain) {
            if (proto.Members.ContainsValue(v)) {
                return true;
            }
        }
        return false;
    }

}