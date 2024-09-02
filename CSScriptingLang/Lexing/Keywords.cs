using System.Reflection;

namespace CSScriptingLang.Lexing;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class KeywordAttribute : Attribute
{
    public string Identifier { get; set; }
    public KeywordAttribute(string identifier) => Identifier = identifier;
}

public class Keywords
{
    public static Dictionary<string, TokenType> KeywordTypes;

    static Keywords() {
        KeywordTypes = GetKeywords();
    }

    private static Dictionary<string, TokenType> GetKeywords() {
        var keywords = new Dictionary<string, TokenType>();

        var type   = typeof(TokenType);
        var fields = type.GetFields();

        foreach (var field in fields) {
            foreach (var keywordAttribute in field.GetCustomAttributes<KeywordAttribute>()) {
                keywords.Add(keywordAttribute.Identifier, (TokenType) field.GetValue(null)!);
            }
        }

        return keywords;
    }
}