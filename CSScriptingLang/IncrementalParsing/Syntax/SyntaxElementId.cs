namespace CSScriptingLang.IncrementalParsing.Syntax;

public readonly record struct SyntaxElementId(int ScriptId, int ElementId)
{
    public static readonly SyntaxElementId Empty = new(-1, 0);

    public static SyntaxElementId From(string idString)
    {
        var longId = long.Parse(idString);
        return new SyntaxElementId((int)(longId >> 32), (int)longId);
    }

    public long UniqueId => ((long)ScriptId << 32) | (uint)ElementId;

    public string Stringify => UniqueId.ToString();

    public override string ToString()
    {
        return Stringify;
    }
}