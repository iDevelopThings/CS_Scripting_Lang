using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public class SourceText
{

    struct LineOffset
    {
        public int  StartOffset    { get; set; }
        public int  Length         { get; set; }
        public bool ExistSurrogate { get; set; }
    }

    private List<LineOffset> _indexes = [];

    private string _input = string.Empty;

    public string Input {
        get => _input;
        set {
            // _input = value;
            _input = value.Replace("\r\n", "\n");
            Parse(_input);
        }
    }
    public string[] Lines { get; private set; } = [];

    public int Length => Input.Length;

    public SourceText(string input) => Input = input;

    public static implicit operator SourceText(string input)  => new(input);
    public static implicit operator string(SourceText source) => source.Input;

    public char this[int index] => Input[index];

    private void Parse(string input) {
        _indexes = new();

        Lines = input.Split('\n');

        var lineOffset = new LineOffset {
            StartOffset    = 0,
            Length         = 0,
            ExistSurrogate = false,
        };
        _indexes.Add(lineOffset);

        for (var pos = 0; pos < input.Length; pos++) {
            var ch = input[pos];

            lineOffset.Length++;
            if (char.IsSurrogate(ch)) {
                lineOffset.Length++;
                lineOffset.ExistSurrogate = true;
                pos++;
            } else if (ch is '\r' or '\n') {
                if (ch is '\r' && pos + 1 < input.Length && input[pos + 1] is '\n') {
                    pos++;
                    lineOffset.Length++;
                }

                if (pos + 1 >= input.Length) continue;
                lineOffset = new LineOffset {
                    StartOffset    = pos + 1,
                    Length         = 0,
                    ExistSurrogate = false,
                };
                _indexes.Add(lineOffset);
            }
        }
    }

    public int ColumOfLastCharOfLine(int line) {
        if (line >= Lines.Length)
            return -1;

        return Lines[line].Length;
    }

    public int GetLine(int offset) {
        var index = _indexes.BinarySearch(
            new LineOffset {
                StartOffset = offset,
            }, Comparer<LineOffset>.Create((a, b) => a.StartOffset.CompareTo(b.StartOffset))
        );

        if (index < 0) {
            index = ~index - 1;
        }

        return index;
    }

    public int GetCol(int offset) => GetCol(offset, Input);
    public int GetCol(int offset, string source) {
        if (offset > source.Length) {
            offset = source.Length;
        }
        if (offset < 0) {
            offset = 0;
        }

        var line       = GetLine(offset);
        var lineOffset = _indexes[line];
        var colOffset  = offset - lineOffset.StartOffset;
        var col        = 0;
        if (lineOffset.ExistSurrogate) {
            for (var pos = lineOffset.StartOffset; pos <= offset; pos++) {
                col++;
                if (pos < source.Length && char.IsSurrogate(source[pos])) {
                    pos++;
                }
            }
        } else {
            col = colOffset;
        }

        return col;
    }

    public int GetOffset(int line, int col) => GetOffset(line, col, Input);
    public int GetOffset(int line, int col, string source) {
        if (line >= _indexes.Count) {
            return source.Length;
        }
        if (line < 0) {
            line = 0;
        }

        var lineOffset = _indexes[line];
        var offset     = lineOffset.StartOffset;
        if (lineOffset.ExistSurrogate) {
            var colOffset = 0;
            for (var pos = lineOffset.StartOffset; pos < source.Length; pos++) {
                if (colOffset == col) {
                    offset = pos;
                    break;
                }

                colOffset++;
                if (char.IsSurrogate(source[pos])) {
                    pos++;
                }
            }
        } else {
            offset += col;
        }

        return offset;
    }

    // This should get the substring, excluding all the newlines
    public string GetRange(SourceRange range) {
        if (range.StartOffset >= Input.Length) {
            return string.Empty;
        }

        var span = range.EndOffset >= Input.Length
            ? Input[range.StartOffset..]
            : Input.Substring(range.StartOffset, range.Length);

        return span;
    }

    public int TotalLine => _indexes.Count;

    public NamedSymbolRange GetTokenRange(SourceRange sourceRange, int scriptId = -1) {
        var startLine = GetLine(sourceRange.StartOffset);
        var endLine   = GetLine(sourceRange.EndOffset);

        var startCol = GetCol(sourceRange.StartOffset);
        var endCol   = GetCol(sourceRange.EndOffset);

        return new NamedSymbolRange(
            startLine,
            startCol,
            endLine,
            endCol
        ) {
            ScriptId = scriptId
        };
    }
}

public class Position /*: PooledObject<Position>*/
{
    public SourceText Input { get; set; }

    public int Start   { get; set; }
    public int Current { get; set; }
    public int End     { get; set; }

    public int Line   { get; set; } = 1;
    public int Column { get; set; }

    private bool IsEof       => Current >= Input.Length && !CurrentChar.IsEOF();
    public  char CurrentChar => Current >= Input.Length ? '\0' : Input[Current];

    // static Position() { ObjectPool<Position>.WarmUp(1024); }

    public int EatWhen(char ch) {
        var count = 0;
        while (!IsEof && CurrentChar == ch) {
            ++count;
            Forward();
        }

        return count;
    }

    public int EatWhen(Func<char, bool> func) {
        var count = 0;
        while (!IsEof && func(CurrentChar)) {
            ++count;
            Forward();
        }

        return count;
    }

    public int Forward(int n = 1) {
        var nextIdx = Current + n;

        // handle line, column
        for (var i = Current; i < nextIdx; i++) {
            if (CurrentChar.IsNewLine()) {
                // var oldChar = CurrentChar;
                Line++;
                Column = 0;
                // i++;
                continue;
            }

            Column++;
        }

        return Current += n;
    }

    public int Backward(int n = 1) {
        var nextIdx = Current - n;

        // handle line, column
        while (Current > nextIdx) {
            if (CurrentChar.IsNewLine()) {
                Line--;
                Column = Input.ColumOfLastCharOfLine(Line);
                Current--;
                continue;
            }

            Column--;
            Current--;
        }

        return Current;
    }

    public static bool operator >=(Position position, int value)
        => position.Current >= value;
    public static bool operator <=(Position position, int value)
        => position.Current <= value;

    public static Position From(Position position) {
        var pos = new Position {
            Input   = position.Input,
            Start   = position.Start,
            Current = position.Current,
            End     = position.End,
            Line    = position.Line,
            Column  = position.Column
        };
        return pos;
    }
}