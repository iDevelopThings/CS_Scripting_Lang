using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing;

namespace CSScriptingLang.Lexing;

public struct TokenRange : IEquatable<TokenRange>, IComparable<TokenRange>
{
    public int Start       { get; set; }
    public int StartLine   { get; set; }
    public int StartColumn { get; set; }

    public int End       { get; set; }
    public int EndLine   { get; set; }
    public int EndColumn { get; set; }

    public int Total      => (End - Start) + 1;
    public int TotalLines => (EndLine - StartLine) + 1;
    public int Length     => Total;

    public static implicit operator SourceRange(TokenRange range) => new(
        range.Start,
        range.End - range.Start
    );

    public TokenRange() { }
    public TokenRange(int start, int end) {
        Start       = start;
        StartLine   = -1;
        StartColumn = -1;
        End         = end;
        EndLine     = -1;
        EndColumn   = -1;
    }
    public TokenRange(int start, int startLine, int startColumn, int end, int endLine, int endColumn) {
        Start       = start;
        StartLine   = startLine;
        StartColumn = startColumn;
        End         = end;
        EndLine     = endLine;
        EndColumn   = endColumn;
    }

    public static TokenRange Empty => new() {
        Start       = -1,
        StartLine   = -1,
        StartColumn = -1,
        End         = -1,
        EndLine     = -1,
        EndColumn   = -1
    };
    public bool IsEmpty => Start == -1 && End == -1;

    public string InputSource { get; set; }


    public override string ToString() => $"{Start}:{End}";
    public bool Equals(TokenRange other) {
        return
            Start == other.Start
         && StartLine == other.StartLine
         && StartColumn == other.StartColumn
         && End == other.End
         && EndLine == other.EndLine
         && EndColumn == other.EndColumn
            ;
    }
    public override bool Equals(object obj) {
        return obj is TokenRange other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(Start, StartLine, StartColumn, End, EndLine, EndColumn);
    }

    public static implicit operator NamedSymbolPosition(TokenRange range) {
        return new NamedSymbolPosition(range.StartLine, range.StartColumn);
    }
    public static implicit operator NamedSymbolRange(TokenRange range) {
        return new NamedSymbolRange(
            range.StartLine, range.StartColumn, range.EndLine, range.EndColumn
        );
    }

    public static TokenRange operator +(TokenRange lhs, int offset) {
        var textSrc = (SourceText) lhs.InputSource;

        var start = lhs.Start + offset;
        var end   = lhs.End + offset;

        return new TokenRange {
            Start       = start,
            StartLine   = textSrc.GetLine(start),
            StartColumn = textSrc.GetCol(start),
            End         = end,
            EndLine     = textSrc.GetLine(end),
            EndColumn   = textSrc.GetCol(end),
            InputSource = lhs.InputSource,
        };
    }

    public int CompareTo(TokenRange other) {
        var startComparison = Start.CompareTo(other.Start);
        if (startComparison != 0) return startComparison;
        var startLineComparison = StartLine.CompareTo(other.StartLine);
        if (startLineComparison != 0) return startLineComparison;
        var startColumnComparison = StartColumn.CompareTo(other.StartColumn);
        if (startColumnComparison != 0) return startColumnComparison;
        var endComparison = End.CompareTo(other.End);
        if (endComparison != 0) return endComparison;
        var endLineComparison = EndLine.CompareTo(other.EndLine);
        if (endLineComparison != 0) return endLineComparison;
        return EndColumn.CompareTo(other.EndColumn);
    }

    public bool Contains(int offset) {
        return offset >= Start && offset < End;
    }
    public bool Contains(TokenRange range) {
        return Start <= range.Start && End >= range.End;
    }
    public bool Intersects(TokenRange range) {
        return Start < range.End && range.Start < End;
    }

    public bool Contains(LSPPosition position) {
        if (position.Line < StartLine || position.Line > EndLine) {
            return false;
        }

        if (position.Line == StartLine && position.Character < StartColumn) {
            return false;
        }

        if (position.Line == EndLine && position.Character > EndColumn) {
            return false;
        }

        return true;
    }
}