using System.Diagnostics;
using System.Globalization;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using RTValue = CSScriptingLang.RuntimeValues.Values.Value;

namespace CSScriptingLang.Core.Serialization.JsonSerialization;

public enum JsonValueType
{
    Null,
    Boolean,

    Int32,
    Int64,
    Float,
    Double,

    String,

    Object,
    Array,
}

[DebuggerTypeProxy(typeof(JsonValueDebugView))]
[DebuggerDisplay("{Type} -> {Value}")]
public struct JsonValue
{
    public class JsonValueDebugView
    {
        private JsonValue _value;
        private RTValue   _rtValue;

        public JsonValueDebugView(JsonValue value) {
            _value   = value;
            _rtValue = value.ToValue();
        }

        public JsonValueType Type    => _value.Type;
        public object        Value   => _value.Value;
        public RTValue       RTValue => _rtValue;
    }

    public JsonValueType Type  { get; }
    public object        Value { get; }

    public JsonValue(JsonValueType type, object v) {
        Type  = type;
        Value = v;
    }


    public static JsonValue NullValue = new(JsonValueType.Null, JsonNull.Instance);

    public JsonBool          AsBool   => As<JsonBool>(JsonValueType.Boolean);
    public JsonString        AsString => As<JsonString>(JsonValueType.String);
    public JsonNumber_Int32  AsInt32  => As<JsonNumber_Int32>(JsonValueType.Int32);
    public JsonNumber_Int64  AsInt64  => As<JsonNumber_Int64>(JsonValueType.Int64);
    public JsonNumber_Float  AsFloat  => As<JsonNumber_Float>(JsonValueType.Float);
    public JsonNumber_Double AsDouble => As<JsonNumber_Double>(JsonValueType.Double);
    public JsonObject        AsObject => As<JsonObject>(JsonValueType.Object);
    public JsonArray         AsArray  => As<JsonArray>(JsonValueType.Array);

    public static JsonValue Null()               => NullValue;
    public static JsonValue Bool(bool     value) => (new JsonBool(value)).ToJsonValue();
    public static JsonValue String(string value) => (new JsonString(value)).ToJsonValue();
    public static JsonValue Int32(int     value) => (new JsonNumber_Int32(value)).ToJsonValue();
    public static JsonValue Int64(long    value) => (new JsonNumber_Int64(value)).ToJsonValue();
    public static JsonValue Float(float   value) => (new JsonNumber_Float(value)).ToJsonValue();
    public static JsonValue Double(double value) => (new JsonNumber_Double(value)).ToJsonValue();
    public static JsonValue Object()             => (new JsonObject()).ToJsonValue();
    public static JsonValue Array()              => (new JsonArray()).ToJsonValue();

    private T As<T>(JsonValueType valTypeToCompare) {
        if (Type != valTypeToCompare)
            throw new InterpreterRuntimeException("JsonValue: expected {0} but got {1}", valTypeToCompare, Type);

        return (T) Value;
    }

    public RTValue ToValue(RTValue parseToValue = null) {
        return Value switch {
            JsonBool b          => b.ToValue(),
            JsonString s        => s.ToValue(),
            JsonNumber_Int32 i  => i.ToValue(),
            JsonNumber_Int64 l  => l.ToValue(),
            JsonNumber_Float f  => f.ToValue(),
            JsonNumber_Double d => d.ToValue(),
            JsonObject o        => o.ToValue(parseToValue),
            JsonArray a         => a.ToValue(),
            _                   => throw new InterpreterRuntimeException("JsonValue: invalid value type {0}", Type),
        };
    }


    public override string ToString() => Value.ToString();
}

public interface IJsonValueType
{
    JsonValueType Type { get; }
    JsonValue     ToJsonValue();

    RTValue ToValue(RTValue parseToValue = null);
}

public struct JsonBool : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Boolean;

    public bool Value { get; }

    public JsonBool(bool value) => Value = value;

    public override string ToString() => Value.ToString();

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Boolean(Value);
}

public struct JsonString : IJsonValueType
{
    public JsonValueType Type => JsonValueType.String;

    public string Value { get; }

    public JsonString(string value) => Value = value;

    public override string ToString() => $"\"{Value}\"";

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.String(Value);
}

public struct JsonNull : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Null;

    public static readonly JsonNull Instance = new JsonNull();

    public override string ToString() => "null";

    public JsonValue ToJsonValue() => JsonValue.NullValue;

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Null();
}

public struct JsonNumber_Int32 : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Int32;

    public int Value { get; }

    public JsonNumber_Int32(int value) => Value = value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Number(Value);
}

public struct JsonNumber_Int64 : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Int64;

    public long Value { get; }

    public JsonNumber_Int64(long value) => Value = value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Number(Value);
}

public struct JsonNumber_Float : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Float;

    public float Value { get; }

    public JsonNumber_Float(float value) => Value = value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Number(Value);
}

public struct JsonNumber_Double : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Double;

    public double Value { get; }

    public JsonNumber_Double(double value) => Value = value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Number(Value);
}

public struct JsonObject : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Object;

    public Dictionary<string, JsonValue> Properties { get; }

    public JsonObject() => Properties = new Dictionary<string, JsonValue>();

    public JsonValue this[string key] {
        get => Properties[key];
        set => Properties[key] = value;
    }

    public override string ToString() => $"{{{string.Join(", ", Properties.Select(p => $"\"{p.Key}\": {p.Value}"))}}}";

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) {
        if (parseToValue != null) {
            var obj = parseToValue.As.Object();
            obj.Append(Properties.Select(p => (p.Key, p.Value.ToValue())));
            return parseToValue;
        }

        return RTValue.Object(Properties.ToDictionary(p => p.Key, p => p.Value.ToValue()));
    }
    public bool TryGetValue(string key, out JsonValue o) {
        return Properties.TryGetValue(key, out o);
    }
}

public struct JsonArray : IJsonValueType
{
    public JsonValueType Type => JsonValueType.Array;

    public List<JsonValue> Items { get; }

    public JsonArray() => Items = new List<JsonValue>();

    public void Add(JsonValue value) => Items.Add(value);

    public override string ToString() => $"[{string.Join(", ", Items)}]";

    public JsonValue ToJsonValue() => new(Type, this);

    public RTValue ToValue(RTValue parseToValue = null) => RTValue.Array(Items.Select(i => i.ToValue()).ToList());
}

public class JsonParser
{
    private JsonLexer       _lexer;
    private List<JsonToken> _read;

    public JsonParser(string text) {
        _lexer = new JsonLexer(text);
        _read  = new List<JsonToken>(4);
    }

    public JsonValue ParseJsonValue(Value parseToValue = null) {
        var token = Take();

        switch (token.Type) {
            case JsonTokenType.True:   return JsonValue.Bool(true);
            case JsonTokenType.False:  return JsonValue.Bool(false);
            case JsonTokenType.Null:   return JsonValue.NullValue;
            case JsonTokenType.String: return JsonValue.String(token.Value);

            case JsonTokenType.Int32: {
                if (!int.TryParse(token.Value, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return JsonValue.Int32(number);
            }

            case JsonTokenType.Int64: {
                if (!long.TryParse(token.Value, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return JsonValue.Int64(number);
            }

            case JsonTokenType.Float: {
                if (!float.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return JsonValue.Float(number);
            }

            case JsonTokenType.Double: {
                if (!double.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return JsonValue.Double(number);
            }

            case JsonTokenType.ObjectStart: return ParseJsonObject(parseToValue);
            case JsonTokenType.ArrayStart:  return ParseJsonArray();


            default:
                throw new InterpreterRuntimeException("JsonParser: expected Value but got {0}", token.Type);
        }
    }
    private JsonValue ParseJsonObject(Value parseToValue = null) {
        var obj = new JsonObject();

        var first = true;

        while (!Match(JsonTokenType.ObjectEnd)) {
            if (first)
                first = false;
            else
                Require(JsonTokenType.Comma);

            var key = Require(JsonTokenType.String);

            Require(JsonTokenType.Colon);

            var value = ParseJsonValue(parseToValue);

            obj[key.Value] = value;
        }

        Require(JsonTokenType.ObjectEnd);

        return obj.ToJsonValue();
    }
    private JsonValue ParseJsonArray() {
        var arr = new JsonArray();

        if (Match(JsonTokenType.ArrayEnd)) {
            Take();
            return arr.ToJsonValue();
        }

        arr.Add(ParseJsonValue());

        while (!Match(JsonTokenType.ArrayEnd)) {
            Require(JsonTokenType.Comma);
            arr.Add(ParseJsonValue());
        }

        Require(JsonTokenType.ArrayEnd);

        return arr.ToJsonValue();
    }

    public Value ParseValue(Value parseToValue = null) {
        var token = Take();

        switch (token.Type) {
            case JsonTokenType.True:   return Value.True();
            case JsonTokenType.False:  return Value.False();
            case JsonTokenType.Null:   return Value.Null();
            case JsonTokenType.String: return Value.String(token.Value);

            case JsonTokenType.Int32: {
                if (!int.TryParse(token.Value, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return Value.Number(number);
            }
            case JsonTokenType.Int64: {
                if (!long.TryParse(token.Value, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return Value.Number(number);
            }

            case JsonTokenType.Float: {
                if (!float.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return Value.Number(number);
            }

            case JsonTokenType.Double: {
                if (!double.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new InterpreterRuntimeException("JsonParser: invalid number '{0}'", token.Value);

                return Value.Number(number);
            }

            case JsonTokenType.ObjectStart: return ParseObject(parseToValue);
            case JsonTokenType.ArrayStart:  return ParseArray(parseToValue);

            default:
                throw new InterpreterRuntimeException("JsonParser: expected Value but got {0}", token.Type);
        }
    }

    private Value ParseObject(Value parseToValue = null) {
        var obj = parseToValue ?? Value.Object();

        if (parseToValue != null && parseToValue.Type != RTVT.Object)
            throw new InterpreterRuntimeException("JsonParser: expected object but got {0}", parseToValue.Type);

        var first = true;

        while (!Match(JsonTokenType.ObjectEnd)) {
            if (first)
                first = false;
            else
                Require(JsonTokenType.Comma);

            var key = Require(JsonTokenType.String);

            Require(JsonTokenType.Colon);

            var value = ParseValue();

            obj[key.Value] = value;
        }

        Require(JsonTokenType.ObjectEnd);
        return obj;
    }

    private Value ParseArray(Value parseToValue = null) {
        var arr = parseToValue ?? Value.Array();

        if (parseToValue != null && parseToValue.Type != RTVT.Array)
            throw new InterpreterRuntimeException("JsonParser: expected array but got {0}", parseToValue.Type);

        if (Match(JsonTokenType.ArrayEnd)) {
            Take();
            return arr;
        }

        var list = arr.As.List();

        list.Add(ParseValue());

        while (!Match(JsonTokenType.ArrayEnd)) {
            Require(JsonTokenType.Comma);
            list.Add(ParseValue());
        }

        Require(JsonTokenType.ArrayEnd);
        return arr;
    }

    public JsonToken Require(JsonTokenType type) {
        var token = Take();

        if (token.Type != type)
            throw new InterpreterRuntimeException("JsonParser: expected {0} but got {1}", type, token.Type);

        return token;
    }

    private bool Match(JsonTokenType type) {
        return Peek().Type == type;
    }

    private JsonToken Take() {
        Peek();

        var result = _read[0];
        _read.RemoveAt(0);
        return result;
    }

    private JsonToken Peek(int distance = 0) {
        if (distance < 0)
            throw new ArgumentOutOfRangeException(nameof(distance), "distance can't be negative");

        while (_read.Count <= distance) {
            var token = _lexer.MoveNext() ? _lexer.Current : new JsonToken(JsonTokenType.End);

            _read.Add(token);
        }

        return _read[distance];
    }
}