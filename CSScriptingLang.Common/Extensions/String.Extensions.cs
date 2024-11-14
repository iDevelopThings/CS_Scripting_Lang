namespace CSScriptingLang.Common.Extensions;

public static class String_Extensions
{
    // public static bool IsWhitespace(this char c) => char.IsWhiteSpace(c);
    public static bool IsWhitespace(this char c) => c is ' ' or '\t' or '\f' or '\v' or '\r' or '\n';
    public static bool IsNewLine(this    char c) => c is '\n' or '\r';
    
    public static bool IsNumber(this     char c) => c is >= '0' and <= '9';
    public static bool IsLetter(this     char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    public static bool IsIdentifier(this char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    // public static bool IsIdentifier(this char c) => c.IsLetter() || c == '_' || c == '#';
    public static bool IsEOF(this   char c) => c == '\0';
    public static bool IsDigit(this char c) => char.IsDigit(c);

    public static bool IsNotNullOrEmpty(this      string str) => !string.IsNullOrEmpty(str);
    public static bool IsNotNullOrWhiteSpace(this string str) => !string.IsNullOrWhiteSpace(str);

    public static bool IsNullOrEmpty(this      string str) => string.IsNullOrEmpty(str);
    public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

    public static string ToCamelCase(this string identifier) {
        if (string.IsNullOrEmpty(identifier)) {
            return identifier;
        }

        if (!char.IsLetter(identifier[0])) {
            return identifier;
        }

        var chars = identifier.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }

    public static string StripQuotes(this string str) {
        if (str.Length < 2) {
            return str;
        }

        if (str[0] == '"' && str[^1] == '"') {
            return str[1..^1];
        }

        if (str[0] == '\'' && str[^1] == '\'') {
            return str[1..^1];
        }

        return str;
    }

    public static void ForEach(this string str, Action<char> action) {
        foreach (var c in str) {
            action(c);
        }
    }
}