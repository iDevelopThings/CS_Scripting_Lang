using System.Collections.Generic;
using System.Diagnostics;

namespace CSScriptingLangGenerators.Utils;

public static class Util
{
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void WaitForDebugger() {
        Debugger.Launch();
        while (!Debugger.IsAttached) {
            System.Threading.Thread.Sleep(100);
        }
    }
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Break() {
        if (!Debugger.IsAttached) {
            WaitForDebugger();
            Debugger.Break();
        }
    }

    // Basically string.Join but for IEnumerable
    public static string Join<TSource>(this IEnumerable<TSource> source, string separator) {
        return string.Join(separator, source);
    }
}