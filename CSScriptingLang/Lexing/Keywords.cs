using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using CSScriptingLang.Common.Extensions;

namespace CSScriptingLang.Lexing;

public enum Keyword : long
{
    None = 0,

    Import    = 1L << 1,
    Module    = 1L << 2,
    If        = 1L << 3,
    Else      = 1L << 4,
    While     = 1L << 5,
    For       = 1L << 6,
    Function  = 1L << 7,
    Type      = 1L << 8,
    Struct    = 1L << 9,
    Interface = 1L << 10,
    Return    = 1L << 11,
    Var       = 1L << 12,
    Range     = 1L << 13,
    Defer     = 1L << 14,
    Async     = 1L << 15,
    Await     = 1L << 16,
    Coroutine = 1L << 17,
    Yield     = 1L << 18,
    Signal    = 1L << 19,
    Def       = 1L << 20,
    Match     = 1L << 21,
    Switch    = 1L << 22,
    Case      = 1L << 23,
    Break     = 1L << 24,
    Is        = 1L << 25,
    Or        = 1L << 26,
    
    True      = 1L << 27,
    False     = 1L << 28,

    Bool            = True | False,
    TypeDeclaration = Struct | Interface,
}
public class Keywords
{
    public static Dictionary<string, Keyword> KeywordTypes;
    public static Dictionary<Keyword, TokenType> KeywordTokenTypes = new() {
        {Keyword.True, TokenType.Boolean},
        {Keyword.False, TokenType.Boolean},
    };

    static Keywords() {
        KeywordTypes = GetKeywords();
    }

    private static Dictionary<string, Keyword> GetKeywords() {
        var keywords = new Dictionary<string, Keyword>();

        var type   = typeof(Keyword);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields) {
            var kw   = (Keyword) field.GetValue(null)!;
            var name = field.Name.ToCamelCase();
            keywords.Add(name, kw);
        }

        return keywords;
    }
    
    public static bool TryMatch(Token tok, out Keyword type) {
        return KeywordTypes.TryGetValue(tok.Value, out type);
    }
}
