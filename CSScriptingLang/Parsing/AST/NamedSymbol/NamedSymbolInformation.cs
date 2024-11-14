using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.RuntimeValues.Prototypes.Types;

namespace CSScriptingLang.Parsing.AST.NamedSymbol;

public struct NamedSymbolInformation : IEquatable<NamedSymbolInformation>, IComparable<NamedSymbolInformation>
{
    public NamedSymbolRange    Range    { get; set; }
    public NamedSymbolPosition Position => (NamedSymbolPosition) Range.Start;

    public string          Name { get; set; }
    public NamedSymbolKind Kind { get; set; }

    public SyntaxNode SyntaxNode { get; set; }
    public BaseNode   Node       { get; set; }
    public Script     Script     => Node?.GetScript() ?? SyntaxNode?.GetScript();
    public Ty         Type       { get; set; }

    public static NamedSymbolInformation Empty => new(
        (BaseNode) null,
        string.Empty,
        NamedSymbolKind.None,
        NamedSymbolRange.Empty
    );

    public NamedSymbolInformation(
        BaseNode         node,
        string           name,
        NamedSymbolKind  kind,
        NamedSymbolRange range
    ) {
        Node  = node;
        Name  = name;
        Kind  = kind;
        Range = range;
    }
    public NamedSymbolInformation(
        SyntaxNode       node,
        string           name,
        NamedSymbolKind  kind,
        NamedSymbolRange range
    ) {
        SyntaxNode = node;
        Name       = name;
        Kind       = kind;
        Range      = range;
    }

    public bool Equals(NamedSymbolInformation other) {
        return Position.Equals(other.Position) && Name == other.Name && Kind == other.Kind;
    }
    public override bool Equals(object obj) {
        return obj is NamedSymbolInformation other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(Position, Name, (int) Kind);
    }
    public static bool operator ==(NamedSymbolInformation left, NamedSymbolInformation right) {
        return left.Equals(right);
    }
    public static bool operator !=(NamedSymbolInformation left, NamedSymbolInformation right) {
        return !left.Equals(right);
    }
    public int CompareTo(NamedSymbolInformation other) {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;
        return Kind.CompareTo(other.Kind);
    }
    public override string ToString() => $"{Name}(kind: {Kind}, position: {Position})";
    public string ToDebugString() {
        var str = $"{Name}(kind: {Kind}, position: {Position}) ";
        if (Node != null) {
            str += $"\n{Node.ToSimpleDebugString()}";
        }
        if (SyntaxNode != null) {
            str += $"\n{SyntaxNode.ToSimpleDebugString()}";
        }
        return str;
    }
}