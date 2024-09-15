using System.Collections.Generic;

namespace CSScriptingLangGenerators.Utils.CodeWriter;

public static class CodeWriterExtensions
{
    public static void _(this Writer w, string str = null) {
        w.Write(str);
    }

    public static void __(this Writer w, string str = null) {
        w.WriteInline(str);
    }
    
    // public static void _(this Writer w, string str, params string[] strs) {
    //     w.Write(str, strs);
    // }
    
    public static void _(this Writer w, params string[] strs) {
        w._(strs as IEnumerable<string>);
    }
    public static void _(this Writer w, params object[] strs) {
        foreach (var str in strs) {
            switch (str) {
                case string s:
                    w.Write(s);
                    break;
                case IEnumerable<string> ss:
                    w._(ss);
                    break;
                default:
                    w.Write(str.ToString());
                    break;
            }
        }
    }
    
    public static void _(this Writer w, IEnumerable<string> strs) {
        foreach (var str in strs) {
            w.Write(str);
        }
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


    public static UsingHandle If(this Writer w, string expr) {
        return w.B($"if({expr})");
    }
}