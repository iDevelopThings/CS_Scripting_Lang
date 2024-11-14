using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing;

public record MarkEvent
{
    [DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
    public sealed record NodeStart(int Parent, SyntaxKind Kind, string Context) : MarkEvent
    {
        private string ToSimpleDebugString() => $"--> {Kind} Parent={Parent} Context='{Context}'";
    }

    [DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
    public sealed record EatToken(TokenRange Range, SourceRange SourceRange, TokenType Kind, int TokenIdx) : MarkEvent
    {
        private string ToSimpleDebugString() => $"[{Kind}@{Range}]  --- {Range.InputSource.Substring(SourceRange.StartOffset, SourceRange.Length)}";
    }

    [DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
    public sealed record RemapTokenType(TokenType FromType, TokenType ToType) : MarkEvent
    {
        private string ToSimpleDebugString() => $"{FromType} -> {ToType}";
    }

    [DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
    public sealed record Error(string Err, Caller Caller) : MarkEvent
    {
        public string ToSimpleDebugString() => $"'{Err}' at {Caller}";
    }

    [DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
    public sealed record NodeEnd(SyntaxKind Kind) : MarkEvent
    {
        private string ToSimpleDebugString() => $"<-- {Kind}";
    }

    public bool IsTriviaLike() {
        if (this is EatToken eatToken) {
            return eatToken.Kind.HasAny(TokenType.Whitespace | TokenType.NewLine | TokenType.LineComment | TokenType.BlockComment);
        }
        return false;
    }

}

public readonly struct Marker
{
    public readonly IncrementalParser p;

    public int Position      { get; }
    public int TokenPosition { get; }

    public Marker(IncrementalParser parser, int position, int tokenPosition) {
        p             = parser;
        Position      = position;
        TokenPosition = tokenPosition;
    }

    public CompleteMarker Complete(SyntaxKind kind) {
        if (p.Events[Position] is MarkEvent.NodeStart(_, _, _) start) {
            p.Events[Position] = start with {Kind = kind};
        }

        var finish = p.Events.Count;
        p.Events.Add(new MarkEvent.NodeEnd(kind));

        return new CompleteMarker(p, Position, finish, kind, true, TokenPosition);
    }

    public CompleteMarker Fail(SyntaxKind kind, string err, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (p.Events[Position] is MarkEvent.NodeStart(_, _, _) start) {
            p.Events[Position] = start with {Kind = kind};
        }

        var finish = p.Events.Count;
        p.Events.Add(new MarkEvent.Error(err, Caller.FromAttributes(file, line, member)));
        p.Events.Add(new MarkEvent.NodeEnd(kind));

        return new CompleteMarker(p, Position, finish, kind, false, TokenPosition);
    }

    public bool EnsureAnyOfAndConsume(
        SyntaxKind         kind,
        TokenType          type,
        string             message,
        out CompleteMarker marker,
        [CallerFilePath]
        string file = "",
        [CallerLineNumber]
        int line = 0,
        [CallerMemberName]
        string member = ""
    ) {

        foreach (var tokenType in type.GetFlags()) {
            if (p.Token.Is(tokenType)) {
                p.Advance();
                marker = CompleteMarker.Empty;
                return false;
            }
        }

        marker = Fail(kind, message, file, line, member);
        return true;
    }

    public bool EnsureAndConsume(TokenType type, string message, out CompleteMarker marker, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (p.Token.Is(type)) {
            p.Advance();
            marker = CompleteMarker.Empty;
            return false;
        }

        marker = Fail(SyntaxKind.Failed, message, file, line, member);
        return true;
    }
    public bool EnsureAndConsume(Keyword type, string message, out CompleteMarker marker, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (p.Token.Is(type)) {
            p.Advance();
            marker = CompleteMarker.Empty;
            return false;
        }

        marker = Fail(SyntaxKind.Failed, message, file, line, member);
        return true;
    }

    public CompleteMarker EnsureAndComplete(TokenType type, SyntaxKind kind, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (p.Token.Is(type)) {
            p.Advance();
            var cm = Complete(kind);
            return cm;
        }

        return Fail(kind, message, file, line, member);
    }

    public bool IsInvalid(IncrementalParser p) {
        return (p.Events.Count - 1) == Position;
    }

    public void Rollback() {
        // Remove all events after this marker
        p.Events.RemoveRange(Position - 1, p.Events.Count - Position + 1);
        p.Lexer.Rollback(TokenPosition);
    }

}

public struct CompleteMarker
{
    private readonly IncrementalParser p;

    public static CompleteMarker Empty { get; } = new(null, -1, -1, SyntaxKind.None, false, -1);

    public bool IsEmptyMarker => p == null && Start == -1 && Finish == -1;

    private int        Start         { get; }
    private int        Finish        { get; }
    public  SyntaxKind Kind          { get; }
    public  bool       IsComplete    { get; }
    public  int        TokenPosition { get; }

    public CompleteMarker(
        IncrementalParser parser,
        int               start,
        int               finish,
        SyntaxKind        kind,
        bool              isComplete,
        int               tokenPosition
    ) {
        p             = parser;
        Start         = start;
        Finish        = finish;
        Kind          = kind;
        IsComplete    = isComplete;
        TokenPosition = tokenPosition;
    }

    public Marker Precede() {
        var m = p.Marker();
        if (p.Events[Start] is MarkEvent.NodeStart(_, _, _) start) {
            p.Events[Start] = start with {Parent = m.Position};
        }

        return m;
    }

    public CompleteMarker Wrap(SyntaxKind kind) {
        var m = p.Marker();
        if (p.Events[Start] is MarkEvent.NodeStart(_, _, _) start) {
            p.Events[Start] = start with {Parent = m.Position};
        }

        return m.Complete(kind);
    }

    public bool Contains(TokenType type, Func<MarkEvent.EatToken, Token, bool> predicate = null) {
        for (var i = Start; i < Finish; i++) {
            if (
                p.Events[i] is MarkEvent.EatToken eatToken
             && eatToken.Kind.HasAny(type)
            ) {
                if (predicate != null) {
                    var tok = p.Lexer.Tokens[eatToken.TokenIdx];
                    if(!predicate(eatToken, tok)) {
                        continue;
                    }
                }
                return true;
            }
        }
        return false;
    }

    // transfer all inner events of this node into the specified Marker
    // public void TransferTo(ref Marker marker) {
    // for (var i = Start + 1; i < Finish; i++) {
    // marker.p.Events.Add(p.Events[i]);
    // }
    // }

    public Marker Reset() {
        if (p.Events[Start] is MarkEvent.NodeStart(_, _, _) start) {
            p.Events[Start] = start with {Kind = SyntaxKind.None};
        }

        p.Events.RemoveAt(Finish);
        return new Marker(p, Start, TokenPosition);
    }
}