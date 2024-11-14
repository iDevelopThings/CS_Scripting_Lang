using System.Collections;
using System.Globalization;
using System.Text;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Core.Serialization.JsonSerialization;

public class JsonLexer : IEnumerator<JsonToken>
{
    private readonly string _text;

    private int _position;
    private Dictionary<string, JsonTokenType> _keywords = new() {
        ["true"]  = JsonTokenType.True,
        ["false"] = JsonTokenType.False,
        ["null"]  = JsonTokenType.Null,
    };

    public JsonLexer(string text) {
        _text     = text;
        _position = 0;
    }

    public JsonToken Current { get; private set; }

    public bool MoveNext() {
        if (SkipWhiteSpace())
            return false;

        var ch = TakeChar();

        switch (ch) {
            case '{':
                Current = new JsonToken(JsonTokenType.ObjectStart);
                return true;

            case '}':
                Current = new JsonToken(JsonTokenType.ObjectEnd);
                return true;

            case '[':
                Current = new JsonToken(JsonTokenType.ArrayStart);
                return true;

            case ']':
                Current = new JsonToken(JsonTokenType.ArrayEnd);
                return true;

            case ':':
                Current = new JsonToken(JsonTokenType.Colon);
                return true;

            case ',':
                Current = new JsonToken(JsonTokenType.Comma);
                return true;

            case '"':
                var sb          = new StringBuilder();
                var stringStart = _position - 1;

                while (true) {
                    if (_position >= _text.Length)
                        throw new InterpreterRuntimeException("JsonLexer: unterminated string starting at position {0}",
                                                              stringStart);

                    ch = _text[_position++];

                    if (ch == '"')
                        break;

                    if (ch != '\\') {
                        sb.Append(ch);
                        continue;
                    }

                    ch = TakeChar();

                    switch (ch) {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(ch);
                            break;

                        case 'b':
                            sb.Append('\b');
                            break;

                        case 'f':
                            sb.Append('\f');
                            break;

                        case 'n':
                            sb.Append('\n');
                            break;

                        case 'r':
                            sb.Append('\r');
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case 'u':
                            var digits = "" + TakeChar() + TakeChar() + TakeChar() + TakeChar();

                            if (!short.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
                                goto default;

                            sb.Append((char) value);
                            continue;

                        default:
                            throw new InterpreterRuntimeException("JsonLexer: invalid escape sequence '{0}' at position {1}",
                                                                  ch, _position - 1);
                    }
                }

                Current = new JsonToken(JsonTokenType.String, sb.ToString());
                return true;

            case '-':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                var start = _position - 1;

                if (ch == '-') {
                    // - must be followed by a digit
                    if (!char.IsDigit(TakeChar()))
                        goto default;
                }

                var hasDecimal = false;
                var hasExp     = false;

                while (_position < _text.Length) {
                    ch = _text[_position++];

                    if (ch == '.' && !hasDecimal && !hasExp) {
                        hasDecimal = true;
                        continue;
                    }

                    if (ch == 'e' || ch == 'E' && !hasExp) {
                        hasExp = true;

                        ch = TakeChar();

                        // e must be followed by a digit or +/-
                        if (char.IsDigit(ch))
                            continue;

                        if (ch != '+' && ch != '-')
                            goto default;

                        // +/- must be followed by a digit
                        if (!char.IsDigit(TakeChar()))
                            goto default;

                        continue;
                    }

                    if (char.IsDigit(ch))
                        continue;

                    _position--;
                    break;
                }


                var numberStr = _text.Substring(start, _position - start);

                if (int.TryParse(numberStr, out var intVal)) {
                    Current = new JsonToken(JsonTokenType.Int32, numberStr);
                    return true;
                }

                if (long.TryParse(numberStr, out var longVal)) {
                    Current = new JsonToken(JsonTokenType.Int64, numberStr);
                    return true;
                }

                if (float.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal)) {
                    Current = new JsonToken(JsonTokenType.Float, numberStr);
                    return true;
                }

                if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal)) {
                    Current = new JsonToken(JsonTokenType.Double, numberStr);
                    return true;
                }

                throw new InterpreterRuntimeException("JsonLexer: invalid number '{0}' at position {1}", numberStr, start);

            default: {
                if (ch.IsIdentifier()) {
                    var ident = ch.ToString();
                    while (_position < _text.Length && _text[_position].IsIdentifier()) {
                        ident += _text[_position++];
                    }
                    if (!_keywords.TryGetValue(ident, out var type))
                        throw UnexpectedChar();

                    Current = new JsonToken(type);
                    return true;
                }


                throw UnexpectedChar();
            }
        }
    }

    private char TakeChar() {
        if (_position >= _text.Length)
            throw EndOfString();

        return _text[_position++];
    }

    private bool SkipWhiteSpace() {
        while (_position < _text.Length && _text[_position].IsWhitespace()) {
            _position++;
        }
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) {
            _position++;
        }

        return _position >= _text.Length;
    }

    private static Exception EndOfString() {
        return new InterpreterRuntimeException("JsonLexer: unexpected end of string");
    }

    private Exception UnexpectedChar() {
        return new InterpreterRuntimeException(
            "JsonLexer: unexpected character '{0}' at position {1}",
            _text[_position - 1],
            _position - 1
        );
    }

    public void Reset() {
        _position = 0;
    }

    public void Dispose() { }

    object IEnumerator.Current => Current;
}