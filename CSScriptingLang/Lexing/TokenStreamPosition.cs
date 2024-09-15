using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public struct TokenRange : IEquatable<TokenRange>
{
    public int Start       { get; set; }
    public int StartLine   { get; set; }
    public int StartColumn { get; set; }

    public int End       { get; set; }
    public int EndLine   { get; set; }
    public int EndColumn { get; set; }

    public int Total      => (End - Start) + 1;
    public int TotalLines => (EndLine - StartLine) + 1;


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
}

/*public struct LexerPosition
{
    public int Line   { get; set; }
    public int Column { get; set; }
    public int Index  { get; set; }
}*/

public class Position : PooledObject<Position>
{
    public string Input { get; set; } = string.Empty;

    public int Start   { get; set; }
    public int Current { get; set; }
    public int End     { get; set; }

    public int Line   { get; set; }
    public int Column { get; set; }


    static Position() {
        ObjectPool<Position>.WarmUp(1024);
    }

    public int Forward(int n = 1) {
        var nextIdx = Current + n;

        // handle line, column
        for (var i = Current; i < nextIdx; i++) {
            if (Input[i] == '\n') {
                Line++;
                Column = 0;
            } else {
                Column++;
            }
        }

        return Current += n;
    }

    public int Backward(int n = 1) {
        var nextIdx = Current - n;

        // handle line, column
        while (Current > nextIdx) {
            var currentChar = Current >= Input.Length ? '\0' : Input[Current];
            if (currentChar == '\n') {
                Line--;
                Column = 0;
            } else {
                Column--;
            }

            Current--;
        }
        
        return Current;
    }

    public static bool operator >=(Position position, int value)
        => position.Current >= value;
    public static bool operator <=(Position position, int value)
        => position.Current <= value;

    public Position From(Position position) {
        Start   = position.Start;
        Current = position.Current;
        End     = position.End;
        Line    = position.Line;
        Column  = position.Column;
        return this;
    }
}