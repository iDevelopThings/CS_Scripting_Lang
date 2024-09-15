using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CSScriptingLangGenerators.Utils;

public class GeneratorLogger
{
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
    public static void Log(string message) {
        /*
        Console.WriteLine(message);

        Util.Break();

        var cwd = Directory.GetCurrentDirectory();
        Console.WriteLine(cwd);
        */

    }
}