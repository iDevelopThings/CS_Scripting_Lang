using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Interpreter.SyntaxAnalysis;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.AST;

public interface IPrintableNode
{
    public string ToString(int indent = 0);
}

public class TypeReference
{
    private bool _isResolved;

    private RuntimeType _type;
    private string      _typeName;

    public BaseNode FromNode { get; set; }

    public List<TypeReference> TypeArguments { get; set; } = new();

    public Module DeclaredModule { get; set; }

    public string Name {
        get => _typeName;
        set {
            var prevName = _typeName;

            _typeName = value;

            if (prevName != null) {
                // Console.WriteLine($"[TypeReference] -> Changing type name from '{prevName}' to '{value}'");

                if (prevName != value && (_type != null || Type != null)) {
                    // Console.WriteLine($"[TypeReference] -> Type name changed({prevName} -> {value}), but type is not null. Clearing type.");
                    // CallerList.Dump("Type name change");
                    _type = null;
                }
            }
        }
    }
    public RuntimeType Type {
        get => _type;
        set => Set(value);
    }

    public TypeReference(TypeReference other) {
        _type          = other._type;
        _typeName      = other._typeName;
        _isResolved    = other._isResolved;
        FromNode       = other.FromNode;
        DeclaredModule = other.DeclaredModule;
        TypeArguments  = other.TypeArguments;
    }

    public TypeReference(BaseNode fromNode) {
        FromNode = fromNode;
    }

    public TypeReference(BaseNode fromNode, RuntimeType type) : this(fromNode) {
        Set(type);
    }
    public TypeReference(BaseNode fromNode, string name) : this(fromNode) {
        Set(name);
    }


    public void Set(string name) {
        if (name.ToLower() is "void" or "unit") {
            Name  = "Unit";
            _type = StaticTypes.Unit;
        } else {
            Name = name;
        }
    }
    public void Set(RuntimeType type, bool registerTypeReference = true) {
        _type = type;
        if (_type != null && Name == null)
            Name = _type.Name;

        if (registerTypeReference)
            TypeAnalyzer.RegisterTypeReference(this);
    }

    public RuntimeType GetWithoutResolving() => _type;
    public RuntimeType Get() {
        if (_isResolved)
            return _type;

        ResolveType();

        return _type;
    }

    public bool IsResolvedOrDefined() {
        return _isResolved || _type != null;
    }

    private void ResolveType() {
        if (_isResolved)
            return;

        if (_type == null && Name != null) {

            _type = TypeAnalyzer.ResolveTypeReference(this);
        }

        if (_type != null && Name == null)
            Name = _type.Name;

        foreach (var typeArg in TypeArguments) {
            typeArg.ResolveType();
        }

        _isResolved = true;
    }
    public void SetType(RuntimeType type, Module module, bool registerTypeReference = true) {
        _type       = type;
        Name        = type?.Name;
        _isResolved = true;
        if (registerTypeReference)
            TypeAnalyzer.RegisterTypeReference(this);
    }
}

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public abstract partial class BaseNode : IPrintableNode
{
    public Token StartToken { get; set; }
    public Token EndToken   { get; set; }

    public int ScriptId { get; set; }

    public TypeReference TypeReference;

    public RuntimeType Type {
        get => TypeReference.Type;
        set => TypeReference.Type = value;
    }

    public string TypeName {
        get => TypeReference.Name;
        set => TypeReference.Name = value;
    }

    public T TypeAs<T>() where T : RuntimeType => Type as T;

    public BaseNode Parent   { get; set; }
    public BaseNode Previous { get; set; }
    public BaseNode Next     { get; set; }

    public NodeCursor Cursor => new(this);

    protected BaseNode() {
        TypeReference = new TypeReference(this);
    }

    public T ParentAs<T>() where T : BaseNode => Parent as T;

    public virtual string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}";
    }

    public Script      GetScript()  => ModuleResolver.GetScriptById(ScriptId);
    public ProgramExpression GetProgram() => GetScript()?.AstData.Program;

    public virtual string ToSimpleDebugString() {
        // var program = (this is ProgramNode ? this : Cursor.First.Parent<ProgramNode>()) as ProgramNode;

        try {
            var script = GetScript();
            if (script == null) {
                return $"Type({GetType().ToShortName()}): No script found.";
            }
            if(StartToken == null || EndToken == null) {
                return $"Type({GetType().ToShortName()}): No token range found.";
            }
            
            var (start, end) = Cursor.FindTokenRange();
            var strRange = script?.Source?.Substring(start.Range.Start, end.Range.End - start.Range.Start);

            return $"Type({GetType().ToShortName()}): {strRange}";
        }
        catch (NodeCursor.FailedToFindTokenRangeException) {
            return $"Type({GetType().ToShortName()}): Failed to find token range.";
        }
    }
    public virtual void Accept(IAstVisitor visitor) {
        // visitor.Visit(this);
    }
    public virtual IEnumerable<BaseNode> AllNodes() {
        yield break;
    }
    
    public BaseNode SetStartToken(Token start) {
        StartToken = start;
        return this;
    }
    public BaseNode SetEndToken(Token end) {
        EndToken = end;
        return this;
    }
}

public interface INodeList
{
    public IEnumerable<BaseNode> NodesAsBaseNode { get; }
    public IEnumerable<TNode>    NodesOfType<TNode>() where TNode : BaseNode;
}

[ASTNode]
public partial class NodeList<T> : BaseNode, INodeList, IEnumerable<T>
{
    public List<T> Nodes { get; set; } = [];

    public IEnumerable<BaseNode> NodesAsBaseNode => Nodes.Cast<BaseNode>();

    public int Count => Nodes.Count;

    protected NodeList() { }
    protected NodeList(IEnumerable<T> nodes) {
        Nodes = nodes.ToList();
    }
    protected NodeList(IEnumerable<BaseNode> statements) {
        Nodes = statements.Cast<T>().ToList();
    }

    public NodeList<T> Add(T node) {
        if (node == null) {
            Console.WriteLine("Warning: Attempted to add null node to program node.");
            return this;
        }

        Nodes.Add(node);

        OnNodeAdded(node);

        return this;
    }
    public virtual NodeList<T> Add(BaseNode node, [CallerMemberName] string caller = "") {
        switch (node) {
            case null:
                Console.WriteLine($"[{caller}] Attempted to add null node to NodeList<{typeof(T).Name}>.");
                return this;
            case T t:
                Add(t);
                break;
            default:
                Console.WriteLine($"[{caller}] Attempted to add node of type {node.GetType().Name} to NodeList<{typeof(T).Name}>.");
                break;
        }

        return this;
    }

    public static implicit operator List<T>(NodeList<T> nodeList) {
        return nodeList.Nodes;
    }
    public static NodeList<T> operator +(NodeList<T> nodeList, T node)
        => nodeList.Add(node);

    public string PrintNodes(int indent = 0, string sepChar = ", ") {
        var nodeStrings = new List<string>();
        foreach (var node in Nodes) {
            if (node is IPrintableNode printableNode)
                nodeStrings.Add(printableNode.ToString(indent));
            else
                nodeStrings.Add($"{new string(' ', indent)}{node}");
        }

        return string.Join(sepChar, nodeStrings);
    }

    public virtual void OnNodeAdded(T node) { }

    public IEnumerable<TNode> NodesOfType<TNode>() where TNode : BaseNode
        => Nodes.OfType<TNode>();

    public override IEnumerable<BaseNode> AllNodes() {
        if (Nodes != null) {
            foreach (var node in Nodes) {
                if (node is BaseNode baseNode) {
                    yield return baseNode;
                }
            }
        }

        foreach (var node in base.AllNodes()) {
            yield return node;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        return Nodes.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable) Nodes).GetEnumerator();
    }

    public T this[int index] => Nodes[index];
}