namespace CSScriptingLang.Core.Serialization.JsonSerialization;

public struct JsonToken
{
    public JsonTokenType Type  { get; }
    public string        Value { get; }

    public JsonToken(JsonTokenType type, string value = null) {
        Type  = type;
        Value = value;
    }
}