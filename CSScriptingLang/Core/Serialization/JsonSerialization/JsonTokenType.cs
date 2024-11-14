namespace CSScriptingLang.Core.Serialization.JsonSerialization;

public enum JsonTokenType
{
    ObjectStart,
    ObjectEnd,

    ArrayStart,
    ArrayEnd,

    Colon,
    Comma,

    True,
    False,
    Null,

    String,

    Int32,
    Int64,
    Float,
    Double,

    End
}

