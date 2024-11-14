using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using Newtonsoft.Json;

namespace CSScriptingLang.Parsing.AST.NamedSymbol;

public record NamedSymbolRange
{
    public int ScriptId { get; set; } = -1;

    /// <summary>
    /// The range's start position.
    /// </summary>
    public NamedSymbolPosition Start { get; init; } = null!;

    /// <summary>
    /// The range's end position.
    /// </summary>
    public NamedSymbolPosition End { get; init; } = null!;

    [JsonIgnore]
    public Script Script {
        get {
            if (ScriptId != -1)
                return ModuleResolver.GetScriptById(ScriptId);
            if (Start != null)
                return ModuleResolver.GetScriptById(Start.ScriptId);
            if (End != null)
                return ModuleResolver.GetScriptById(End.ScriptId);
            return null;
        }
    }

    public static NamedSymbolRange Empty => new(NamedSymbolPosition.Empty, NamedSymbolPosition.Empty);

    public NamedSymbolRange() { }
    public NamedSymbolRange(LSPPosition start, LSPPosition end) {
        Start = start;
        End   = end;
    }
    public NamedSymbolRange(NamedSymbolPosition start, NamedSymbolPosition end) {
        Start = start;
        End   = end;
    }

    public NamedSymbolRange(int startLine, int startCharacter, int endLine, int endCharacter) {
        Start = (startLine, startCharacter);
        End   = (endLine, endCharacter);
    }

    public static implicit operator NamedSymbolRange((Token a, Token b) value)
        => new(
            new NamedSymbolPosition(value.a.Range.StartLine, value.a.Range.StartColumn) {
                ScriptId = value.a.ScriptId,
            },
            new NamedSymbolPosition(value.b.Range.EndLine, value.b.Range.EndColumn) {
                ScriptId = value.b.ScriptId,
            }
        );
    public static implicit operator NamedSymbolRange(Token token)
        => new(
            new NamedSymbolPosition(token.Range.StartLine - 1, token.Range.StartColumn) {
                ScriptId = token.ScriptId,
            },
            new NamedSymbolPosition(token.Range.EndLine - 1, token.Range.EndColumn) {
                ScriptId = token.ScriptId,
            }
        );

    public static implicit operator NamedSymbolRange(BaseNode value)
        => new(
            new NamedSymbolPosition(value.StartToken.Range.StartLine - 1, value.StartToken.Range.StartColumn) {
                ScriptId = value.ScriptId,
            },
            new NamedSymbolPosition(value.EndToken.Range.EndLine - 1, value.EndToken.Range.EndColumn) {
                ScriptId = value.ScriptId,
            }
        );
    public static implicit operator NamedSymbolRange(SyntaxNode value)
        => value.GetSymbolRange();

    public static implicit operator NamedSymbolRange((LSPPosition start, LSPPosition end) value) => new(value.start, value.end);

    public static implicit operator LspRange(NamedSymbolRange range)
        => new(range.Start, range.End);


    /// <summary>
    /// Test if this range is empty.
    /// </summary>
    public bool IsEmpty() => IsEmpty(this);

    /// <summary>
    /// Test if `range` is empty.
    /// </summary>
    public static bool IsEmpty(NamedSymbolRange range) => range.Start.Line == range.End.Line && range.Start.Character == range.End.Character;

    /// <summary>
    /// Test if position is in this range. If the position is at the edges, will return true.
    /// </summary>
    public bool Contains(NamedSymbolPosition position) => ContainsPosition(this, position);

    /// <summary>
    /// Test if `position` is in `range`. If the position is at the edges, will return true.
    /// </summary>
    public static bool ContainsPosition(NamedSymbolRange range, NamedSymbolPosition position) {
        if (position.Line < range.Start.Line || position.Line > range.End.Line) {
            return false;
        }

        if (position.Line == range.Start.Line && position.Character < range.Start.Character) {
            return false;
        }

        if (position.Line == range.End.Line && position.Character > range.End.Character) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Test if range is in this range. If the range is equal to this range, will return true.
    /// </summary>
    public bool Contains(NamedSymbolRange range) => ContainsRange(this, range);

    /// <summary>
    /// Test if `otherRange` is in `range`. If the ranges are equal, will return true.
    /// </summary>
    public static bool ContainsRange(NamedSymbolRange range, NamedSymbolRange otherRange) {
        if (otherRange.Start.Line < range.Start.Line || otherRange.End.Line < range.Start.Line) {
            return false;
        }

        if (otherRange.Start.Line > range.End.Line || otherRange.End.Line > range.End.Line) {
            return false;
        }

        if (otherRange.Start.Line == range.Start.Line && otherRange.Start.Character < range.Start.Character) {
            return false;
        }

        if (otherRange.End.Line == range.End.Line && otherRange.End.Character > range.End.Character) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Test if `range` is strictly in this range. `range` must start after and end before this range for the result to be true.
    /// </summary>
    public bool StrictContains(NamedSymbolRange range) => StrictContainsRange(this, range);

    /// <summary>
    /// Test if `otherRange` is strictly in `range` (must start after, and end before). If the ranges are equal, will return false.
    /// </summary>
    public static bool StrictContainsRange(NamedSymbolRange range, NamedSymbolRange otherRange) {
        if (otherRange.Start.Line < range.Start.Line || otherRange.End.Line < range.Start.Line) {
            return false;
        }

        if (otherRange.Start.Line > range.End.Line || otherRange.End.Line > range.End.Line) {
            return false;
        }

        if (otherRange.Start.Line == range.Start.Line && otherRange.Start.Character <= range.Start.Character) {
            return false;
        }

        if (otherRange.End.Line == range.End.Line && otherRange.End.Character >= range.End.Character) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// A reunion of the two ranges.
    /// The smallest position will be used as the start point, and the largest one as the end point.
    /// </summary>
    public static NamedSymbolRange operator +(NamedSymbolRange a, NamedSymbolRange b) {
        return PlusRange(a, b);
    }

    /// <summary>
    /// A reunion of the two ranges.
    /// The smallest position will be used as the start point, and the largest one as the end point.
    /// </summary>
    public static NamedSymbolRange PlusRange(NamedSymbolRange a, NamedSymbolRange b) {
        int startLineNumber;
        int startColumn;
        int endLineNumber;
        int endColumn;

        if (b.Start.Line < a.Start.Line) {
            startLineNumber = b.Start.Line;
            startColumn     = b.Start.Character;
        } else if (b.Start.Line == a.Start.Line) {
            startLineNumber = b.Start.Line;
            startColumn     = Math.Min(b.Start.Character, a.Start.Character);
        } else {
            startLineNumber = a.Start.Line;
            startColumn     = a.Start.Character;
        }

        if (b.End.Line > a.End.Line) {
            endLineNumber = b.End.Line;
            endColumn     = b.End.Character;
        } else if (b.End.Line == a.End.Line) {
            endLineNumber = b.End.Line;
            endColumn     = Math.Max(b.End.Character, a.End.Character);
        } else {
            endLineNumber = a.End.Line;
            endColumn     = a.End.Character;
        }

        return new NamedSymbolRange(
            (LSPPosition) (startLineNumber, startColumn),
            (LSPPosition) (endLineNumber, endColumn)
        );
    }

    /// <summary>
    /// A intersection of the two ranges.
    /// </summary>
    public NamedSymbolRange Intersection(NamedSymbolRange other) {
        return Intersection(this, other);
    }

    /// <summary>
    /// A intersection of the two ranges.
    /// </summary>
    public static NamedSymbolRange Intersection(NamedSymbolRange a, NamedSymbolRange b) {
        var resultStartLineNumber = a.Start.Line;
        var resultStartColumn     = a.Start.Character;
        var resultEndLineNumber   = a.End.Line;
        var resultEndColumn       = a.End.Character;
        var otherStartLineNumber  = b.Start.Line;
        var otherStartColumn      = b.Start.Character;
        var otherEndLineNumber    = b.End.Line;
        var otherEndColumn        = b.End.Character;

        if (resultStartLineNumber < otherStartLineNumber) {
            resultStartLineNumber = otherStartLineNumber;
            resultStartColumn     = otherStartColumn;
        } else if (resultStartLineNumber == otherStartLineNumber) {
            resultStartColumn = Math.Max(resultStartColumn, otherStartColumn);
        }

        if (resultEndLineNumber > otherEndLineNumber) {
            resultEndLineNumber = otherEndLineNumber;
            resultEndColumn     = otherEndColumn;
        } else if (resultEndLineNumber == otherEndLineNumber) {
            resultEndColumn = Math.Min(resultEndColumn, otherEndColumn);
        }

        // Check if selection is now empty
        if (resultStartLineNumber > resultEndLineNumber) {
            return null;
        }

        if (resultStartLineNumber == resultEndLineNumber && resultStartColumn > resultEndColumn) {
            return null;
        }

        return new NamedSymbolRange(
            (LSPPosition) (resultStartLineNumber, resultStartColumn),
            (LSPPosition) (resultEndLineNumber, resultEndColumn)
        );
    }

    /// <summary>
    /// Create a new empty range using this range's start position.
    /// </summary>
    public NamedSymbolRange CollapseToStart() => CollapseToStart(this);

    /// <summary>
    /// Create a new empty range using this range's start position.
    /// </summary>
    public static NamedSymbolRange CollapseToStart(NamedSymbolRange range) => new NamedSymbolRange(range.Start, range.Start);

    /// <summary>
    /// Create a new empty range using this range's start position.
    /// </summary>
    public NamedSymbolRange CollapseToEnd() => CollapseToEnd(this);

    /// <summary>
    /// Create a new empty range using this range's start position.
    /// </summary>
    public static NamedSymbolRange CollapseToEnd(NamedSymbolRange range) => new NamedSymbolRange(range.End, range.End);

    public static bool IsBefore(NamedSymbolRange a, NamedSymbolRange b) => a.End.Line < b.Start.Line || a.End.Line == b.Start.Line && a.End.Character < b.Start.Character;

    public bool IsBefore(NamedSymbolRange other) => IsBefore(this, other);

    public static bool IsBeforeOrTouching(NamedSymbolRange a, NamedSymbolRange b) => a.End.Line < b.Start.Line || a.End.Line == b.Start.Line && a.End.Character <= b.Start.Character;

    public bool IsBeforeOrTouching(NamedSymbolRange other) => IsBeforeOrTouching(this, other);

    public static bool IsAfter(NamedSymbolRange a, NamedSymbolRange b) => b.End.Line < a.Start.Line || b.End.Line == a.Start.Line && b.End.Character < a.Start.Character;

    public bool IsAfter(NamedSymbolRange other) => IsAfter(this, other);

    public static bool IsAfterOrTouching(NamedSymbolRange a, NamedSymbolRange b) => b.End.Line < a.Start.Line || b.End.Line == a.Start.Line && b.End.Character <= a.Start.Character;

    public bool IsAfterOrTouching(NamedSymbolRange other) => IsAfterOrTouching(this, other);

    /// <summary>
    /// Test if the two ranges are touching in any way. If the ranges are touching it returns false.
    /// </summary>
    public static bool AreIntersectingOrTouching(NamedSymbolRange a, NamedSymbolRange b) {
        // Check if `a` is before `b`
        if (IsBefore(a, b)) {
            return false;
        }

        // Check if `b` is before `a`
        if (IsAfter(a, b)) {
            return false;
        }

        // These ranges must intersect
        return true;
    }

    public bool IntersectsOrTouches(NamedSymbolRange other) => AreIntersectingOrTouching(this, other);

    /// <summary>
    /// Test if the two ranges are intersecting. If the ranges are touching it returns false.
    /// </summary>
    public bool Intersects(NamedSymbolRange other) {
        return AreIntersecting(this, other);
    }

    /// <summary>
    /// Test if the two ranges are intersecting. If the ranges are touching it returns false.
    /// </summary>
    public static bool AreIntersecting(NamedSymbolRange a, NamedSymbolRange b) {
        // Check if `a` is before `b`
        if (IsBeforeOrTouching(a, b)) {
            return false;
        }

        // Check if `b` is before `a`
        if (IsAfterOrTouching(a, b)) {
            return false;
        }

        // These ranges must intersect
        return true;
    }

    public static IComparer<NamedSymbolRange> AscendingComparer  { get; } = new StartPositionComparer();
    public static IComparer<NamedSymbolRange> CompareUsingStarts => AscendingComparer;
    public static IComparer<NamedSymbolRange> DescendingComparer { get; } = new EndPositionComparer();
    public static IComparer<NamedSymbolRange> CompareUsingEnds   => DescendingComparer;

    public class StartPositionComparer : IComparer<NamedSymbolRange>
    {
        public int Compare(NamedSymbolRange x, NamedSymbolRange y) => CompareRangesUsingStarts(x, y);
    }

    public class EndPositionComparer : IComparer<NamedSymbolRange>
    {
        public int Compare(NamedSymbolRange x, NamedSymbolRange y) => CompareRangesUsingEnds(x, y);
    }

    /// <summary>
    /// A function that compares ranges, useful for sorting ranges
    /// It will first compare ranges on the startPosition and then on the endPosition
    /// </summary>
    public static int CompareRangesUsingStarts(NamedSymbolRange a, NamedSymbolRange b) {
        if (a is not null && b is not null) {
            var aStartLineNumber = a.Start.Line | 0;
            var bStartLineNumber = b.Start.Line | 0;

            if (aStartLineNumber == bStartLineNumber) {
                var aStartColumn = a.Start.Character | 0;
                var bStartColumn = b.Start.Character | 0;

                if (aStartColumn == bStartColumn) {
                    var aEndLineNumber = a.End.Line | 0;
                    var bEndLineNumber = b.End.Line | 0;

                    if (aEndLineNumber == bEndLineNumber) {
                        var aEndColumn = a.End.Character | 0;
                        var bEndColumn = b.End.Character | 0;
                        return aEndColumn - bEndColumn;
                    }

                    return aEndLineNumber - bEndLineNumber;
                }

                return aStartColumn - bStartColumn;
            }

            return aStartLineNumber - bStartLineNumber;
        }

        var aExists = a is not null ? 1 : 0;
        var bExists = b is not null ? 1 : 0;
        return aExists - bExists;
    }

    /// <summary>
    /// A function that compares ranges, useful for sorting ranges
    /// It will first compare ranges on the endPosition and then on the startPosition
    /// </summary>
    public static int CompareRangesUsingEnds(NamedSymbolRange a, NamedSymbolRange b) {
        if (a.End.Line == b.End.Line) {
            if (a.End.Character == b.End.Character) {
                if (a.Start.Line == b.Start.Line) {
                    return a.Start.Character - b.Start.Character;
                }

                return a.Start.Line - b.Start.Line;
            }

            return a.End.Character - b.End.Character;
        }

        return a.End.Line - b.End.Line;
    }

    /// <summary>
    /// Test if the range spans multiple lines.
    /// </summary>
    public static bool SpansMultipleLines(NamedSymbolRange range) {
        return range.End.Line > range.Start.Line;
    }

    /// <summary>
    /// Test if the range spans multiple lines.
    /// </summary>
    public bool SpansMultipleLines() {
        return SpansMultipleLines(this);
    }
}