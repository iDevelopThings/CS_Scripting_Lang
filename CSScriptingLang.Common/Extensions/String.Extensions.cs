namespace CSScriptingLang.Common.Extensions;

public static class String_Extensions
{
    public static bool IsWhitespace(this char c) => c is ' ' or '\t' or '\r' or '\n';

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
}