namespace CSScriptingLang.Common.CodeWriter;

public static class CodeWriterExtensions
{
    public static void _(this Writer w, string str = null) {
        w.Write(str);
    }

    public static void _(this Writer w, string str, params string[] strs) {
        w.Write(str, strs);
    }

    public static UsingHandle b(this Writer w, params string[] strs) {
        return w.OpenBlock(strs, newLineAfterBlockEnd: false);
    }
    public static UsingHandle bNoIndent(this Writer w, params string[] strs) {
        return w.OpenBlock(strs, newLineAfterBlockEnd: false, indent: false);
    }

    public static UsingHandle B(this Writer w, params string[] strs) {
        return w.OpenBlock(strs, newLineAfterBlockEnd: true);
    }

    public static UsingHandle i(this Writer w, string begin = null, string end = null) {
        return w.OpenIndent(begin, end, newLineAfterBlockEnd: false);
    }

    public static UsingHandle I(this Writer w, string begin = null, string end = null) {
        return w.OpenIndent(begin, end, newLineAfterBlockEnd: true);
    }
}