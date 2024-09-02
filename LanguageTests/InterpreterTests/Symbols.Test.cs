namespace LanguageTests.InterpreterTests;

[TestFixture]
public class Symbols_Test : BaseCompilerTest
{
    [Test]
    public void ReferenceCounter() {
        Execute(
            """
            var a = 1;
            inspect(a, '1');
            function scope_1(int varA) {
                inspect(a, '3');
                varA++; 
                inspect(a, '4');
                
                function scope_2(int varB) {
                    varB++;
                    inspect(a, '5');
                }
                scope_2(varA);
                inspect(a, '6');
            }
            
            inspect(a, '2');
            
            scope_1(a);
            
            inspect(a, '7');
            
            print('hi {0}', a);
            """
        );

        var a = Symbols.Get("a");
        
    }
}