using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;

namespace CSScriptingLang.Tests.ParserTests;

public class PositionTrackingTest : VirtualFS_CompilerTest
{
    [Test]
    public void VariableAndFunctionVariables() {
        Execute(
            """
            function main() {
                print('Hello');
                var b = 20;
            }
            
            var a = 10;
            var c = main;
                        
            """
        );

        var val = Vars["a"];
    }

}