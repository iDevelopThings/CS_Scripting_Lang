using CSScriptingLang.Lexing;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace CSScriptingLang.Parsing.AST.NamedSymbol;

/*public record NamedSymbolPosition : IComparable<NamedSymbolPosition>, IComparable
{
    public NamedSymbolPosition() { }

    public NamedSymbolPosition(int line, int character) {
        Line      = line;
        Character = character;
    }

    /// <summary>
    /// Line position in a document (zero-based).
    /// </summary>
    /// <remarks>
    /// <see cref="uint"/> in the LSP spec
    /// </remarks>
    public int Line { get; set; }

    /// <summary>
    /// Character offset on a line in a document (zero-based). The meaning of this
    /// offset is determined by the negotiated `PositionEncodingKind`.
    ///
    /// If the character value is greater than the line length it defaults back
    /// to the line length.
    /// </summary>
    /// <remarks>
    /// <see cref="uint"/> in the LSP spec
    /// </remarks>
    public int Character { get; set; }

    public int CompareTo(NamedSymbolPosition other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var lineComparison = Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : Character.CompareTo(other.Character);
    }

    public int CompareTo(object obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is NamedSymbolPosition other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(NamedSymbolPosition)}");
    }

    public static implicit operator NamedSymbolPosition((int line, int character) value) => new(value.line, value.character);
    public static implicit operator NamedSymbolPosition(BaseNode                  value) => new((value.StartToken.Range.StartLine, value.StartToken.Range.StartColumn));

    public static bool operator <(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) < 0;

    public static bool operator >(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) > 0;

    public static bool operator <=(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) <= 0;

    public static bool operator >=(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) >= 0;

    /// <inheritdoc />
    public override string ToString() => $"(line: {Line}, char: {Character})";
}*/

public record NamedSymbolPosition : IComparable<NamedSymbolPosition>, IComparable
{
    public int ScriptId { get; set; }

    /// <summary>
    /// Line position in a document (zero-based).
    /// </summary>
    /// <remarks>
    /// <see cref="uint"/> in the LSP spec
    /// </remarks>
    public int Line { get; set; }

    /// <summary>
    /// Character offset on a line in a document (zero-based). The meaning of this
    /// offset is determined by the negotiated `PositionEncodingKind`.
    ///
    /// If the character value is greater than the line length it defaults back
    /// to the line length.
    /// </summary>
    /// <remarks>
    /// <see cref="uint"/> in the LSP spec
    /// </remarks>
    public int Character { get; set; }

    public NamedSymbolPosition() { }
    public NamedSymbolPosition(int line, int character) {
        Line      = line;
        Character = character;
    }


    public static NamedSymbolPosition Empty => new(-1, -1);

    public static implicit operator NamedSymbolPosition((int line, int character) value) => new(value.line, value.character);
    public static implicit operator NamedSymbolPosition(LSPPosition position)
        => new(position.Line, position.Character);

    public static implicit operator NamedSymbolPosition(Token token) {
        return new NamedSymbolPosition((token.Range.StartLine - 1, token.Range.StartColumn)) {
            ScriptId = token.ScriptId,
        };
    }
    public static implicit operator NamedSymbolPosition(BaseNode value) {
        return new NamedSymbolPosition((value.StartToken.Range.StartLine - 1, value.StartToken.Range.StartColumn)) {
            ScriptId = value.ScriptId,
        };
    }

    public static bool operator <(NamedSymbolPosition  left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) < 0;
    public static bool operator >(NamedSymbolPosition  left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) > 0;
    public static bool operator <=(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) <= 0;
    public static bool operator >=(NamedSymbolPosition left, NamedSymbolPosition right) => Comparer<NamedSymbolPosition>.Default.Compare(left, right) >= 0;

    public int CompareTo(NamedSymbolPosition other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var lineComparison = Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : Character.CompareTo(other.Character);
    }

    public int CompareTo(object obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is NamedSymbolPosition other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Position)}");
    }
    /// <inheritdoc />
    public override string ToString() => $"(line: {Line}, char: {Character})";
    
    public static implicit operator LSPPosition(NamedSymbolPosition position) {
        return new LSPPosition(position.Line, position.Character);
    }
}

public static class NamedSymbolPositionExtensions
{
    public static NamedSymbolPosition ToNamedSymbolPosition(this Position position, bool charOffset = false) {
        return new NamedSymbolPosition(position.Line, position.Character + (charOffset ? -1 : 0));
    }
}