using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public class InputStream
{
    public string   Input;
    public Position Position;
    public char     Current;

    public Stack<Position> PositionMarkerStack { get; set; } = new();

    public string CurrentStringRange => Substring(Position.Start, Position.Current);

    public TokenRange GetCurrentPositionRange() => new TokenRange {
        Start = Position.Current,
        End   = Position.Current
    };

    public InputStream(string input) {
        Input = input;
        Position = new Position {
            Start   = 0,
            Current = 0,
            End     = input.Length
        };
        Current = Input.Length > 0 ? Input[Position.Current] : '\0';
    }

    public char Peek(int n = 1) {
        var peekChar = CharForIdx(Position.Forward(n));
        Position.Backward(n);
        return peekChar;
    }

    public void NextChar() {
        Current = CharForIdx(Position.Forward());
    }

    private char CharForIdx(int idx) => idx >= Input.Length ? '\0' : Input[idx];

    public void SkipWhitespace() {
        while (IsWhitespace()) {
            NextChar();
        }
    }

    public bool IsWhitespace()   => Current is ' ' or '\t' or '\r' or '\n';
    public bool IsEOF()          => Current == '\0';
    public bool IsDigit()        => Current is >= '0' and <= '9';
    public bool IsLetter()       => Current is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    public bool IsAlphaNumeric() => IsLetter() || IsDigit();
    public bool IsIdentifier()   => IsLetter() || Current == '_';

    public (OperatorType, int) GetOperatorType() {
        var next = Peek();
        return Current switch {
            '+' => next switch {
                '+' => (OperatorType.Increment, 2),
                '=' => (OperatorType.PlusEquals, 2),
                _   => (OperatorType.Plus, 1)
            },
            '-' => next switch {
                '-' => (OperatorType.Decrement, 2),
                '=' => (OperatorType.MinusEquals, 2),
                _   => (OperatorType.Minus, 1)
            },
            '/' => (OperatorType.Divide, 1),
            '*' => (OperatorType.Multiply, 1),
            '%' => (OperatorType.Modulus, 1),
            '=' => next switch {
                '=' => (OperatorType.Equals, 2),
                _   => (OperatorType.Assignment, 1)
            },
            '!' => next switch {
                '=' => (OperatorType.NotEquals, 2),
                _   => (OperatorType.Not, 1)
            },
            '>' => next switch {
                '=' => (OperatorType.GreaterThanOrEqual, 2),
                _   => (OperatorType.GreaterThan, 1)
            },
            '<' => next switch {
                '=' => (OperatorType.LessThanOrEqual, 2),
                _   => (OperatorType.LessThan, 1)
            },
            '&' => next switch {
                '&' => (OperatorType.And, 2),
                _   => (OperatorType.None, 1)
            },
            '|' => next switch {
                '|' => (OperatorType.Or, 2),
                _   => (OperatorType.None, 1)
            },
            _ => (OperatorType.None, 0)
        };
    }

    public Tuple<bool, TokenType, OperatorType, int> IsOperator() {
        var (type, chars) = GetOperatorType();

        return type switch {
            OperatorType.None => Tuple.Create(false, TokenType.Error, OperatorType.None, 0),
            _                 => Tuple.Create(true, TokenType.Operator, type, chars)
        };
    }

    public string Substring(int start, int end) => Input.Substring(start, end - start);


    public void PushMarker() {
        PositionMarkerStack.Push(Position.Rent().From(Position));
    }
    public void ResetToStart() {
        var marker = PositionMarkerStack.Pop();
        Position.From(marker);
        marker.Return();
    }
    public void MarkStart() {
        Position.Start = Position.Current;
        PushMarker();
    }
    public void MarkEnd() {
        Position.End = Position.Current;
    }
    public TokenRange GetMarkerRange() {
        var range = new TokenRange {
            Start = Position.Start,
            End   = Position.End
        };
        
        if (PositionMarkerStack.Count != 0) {
            var marker = PositionMarkerStack.Pop();
            range.Start = marker.Start;
            marker.Return();
        }

        return range;
    }
}

public struct TokenRange
{
    public int Start { get; set; }
    public int End   { get; set; }
    public int Total => (End - Start) + 1;

    public override string ToString() => $"{Start}:{End}";
}

public class Position : PooledObject<Position>
{
    public int Start   { get; set; }
    public int Current { get; set; }
    public int End     { get; set; }

    static Position() {
        ObjectPool<Position>.WarmUp(1024);
    }

    public int Forward(int  n = 1) => Current += n;
    public int Backward(int n = 1) => Current -= n;

    public static bool operator >=(Position position, int value)
        => position.Current >= value;
    public static bool operator <=(Position position, int value)
        => position.Current <= value;

    public Position From(Position position) {
        Start   = position.Start;
        Current = position.Current;
        End     = position.End;
        return this;
    }
}