using System.Reflection;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Execution.Expressions;

namespace CSScriptingLang.Lexing;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public class KeywordIdentifierAttribute : Attribute
{
    public string Name { get; set; }
    public KeywordIdentifierAttribute(string name) {
        Name = name;
    }
}

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
    Enum      = 1L << 11,
    Return    = 1L << 12,
    Var       = 1L << 13,
    Range     = 1L << 14,
    Defer     = 1L << 15,
    Async     = 1L << 16,
    Seq       = 1L << 17,
    Await     = 1L << 18,
    Coroutine = 1L << 19,
    Yield     = 1L << 20,
    Signal    = 1L << 21,
    Def       = 1L << 22,
    Match     = 1L << 23,
    Switch    = 1L << 24,
    Case      = 1L << 25,
    Break     = 1L << 26,
    Continue  = 1L << 27,
    Is        = 1L << 28,
    Or        = 1L << 29,

    True  = 1L << 30,
    False = 1L << 31,
    Null  = 1L << 32,

    [KeywordIdentifier("#MACRO")]
    Macro = 1L << 33,
    [KeywordIdentifier("#DEFINE")]
    Define = 1L << 34,

    Bool            = True | False,
    TypeDeclaration = Struct | Interface | Enum,
}

public class Keywords
{
    public static Dictionary<string, Keyword> KeywordTypes;
    public static Dictionary<Keyword, TokenType> KeywordTokenTypes = new() {
        {Keyword.True, TokenType.Boolean},
        {Keyword.False, TokenType.Boolean},
        {Keyword.Null, TokenType.Null},
    };

    public static IReadOnlyList<Keyword> FunctionModifierKeywords = new[] {
        Keyword.Async,
        Keyword.Coroutine,
        Keyword.Seq,
    };

    public static bool IsFunctionWithModifiers(Token token) {
        if (token.IsFunctionKeyword) {
            return true;
        }
        if (!FunctionModifierKeywords.Contains(token.Keyword)) {
            return false;
        }

        // We want to test if we have a function with modifiers
        var current = token;
        while (current != null) {
            if (current.IsTriviaToken) {
                current = current.Next;
                continue;
            }
            if (current.IsFunctionKeyword) {
                return true;
            }
            if (!FunctionModifierKeywords.Contains(current.Keyword)) {
                return false;
            }
            current = current.Next;
        }

        return false;
    }

    public struct FunctionModifier
    {
        public Action<InlineFunctionDeclaration> Apply;
        public Token                             Token;
    }

    public static bool HasFunctionModifiers(
        Token                token,
        out FunctionModifier applyFn,
        bool                 includeFnKeyword = false
    ) {
        applyFn = new FunctionModifier {
            Token = token,
        };

        if (token.IsDefKeyword) {
            applyFn.Apply = (fn) => fn.IsDef = true;
            return true;
        }
        if (token.IsSeqKeyword) {
            applyFn.Apply = (fn) => fn.IsSeq = true;
            return true;
        }
        if (token.IsAsyncKeyword) {
            applyFn.Apply = (fn) => fn.IsAsync = true;
            return true;
        }
        if (token.IsCoroutineKeyword) {
            applyFn.Apply = (fn) => fn.IsCoroutine = true;
            return true;
        }
        
        if (includeFnKeyword && token.IsFunctionKeyword) {
            applyFn.Apply = (fn) => { };
            return true;
        }

        return false;
    }

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

            var attr = field.GetCustomAttribute<KeywordIdentifierAttribute>();
            if (attr != null) {
                name = attr.Name;
            }

            keywords.Add(name, kw);
        }

        return keywords;
    }

    public static bool TryMatch(Token tok, out Keyword type) {
        return KeywordTypes.TryGetValue(tok.Value, out type);
    }
}