using CSScriptingLang.Core;
using CSScriptingLang.Core.Diagnostics;

namespace CSScriptingLang.Tests;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class LSPTestAttribute : Attribute;

public class LSPTestDiagnosticConsumer : DiagnosticConsumer
{
    public override bool Consume(Diagnostic diagnostic) {

        Console.WriteLine(diagnostic);
        Console.WriteLine();

        return true;
    }
}

[TestFixture]
public class LSPMode_CompilerTest : VirtualFS_CompilerTest
{
    public LSPMode_CompilerTest() {
        InterpreterConfig.Mode = InterpreterMode.Lsp;
        DiagnosticManager.AddConsumer(new LSPTestDiagnosticConsumer());
    }
    
    public override void SetupBase() {
        base.SetupBase();
    }

}