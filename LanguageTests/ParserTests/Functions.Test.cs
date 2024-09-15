using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;

namespace LanguageTests.ParserTests;

public class Functions : BaseCompilerTest
{
    [Test]
    public void AsyncFn() {
        Execute(
            @"
            async function main() {
                print('Hello');
            }

            main();
            "
        );
    }

    [Test]
    public void CoroutineFn() {
        Execute(
            """
                coroutine function main() {
                    // Main will push scope + frame to the internal stack
                    
                    //coroutine function child_coro_a() {
                        // Child will push scope + frame to the internal stack
                        // Now we have
                        // - global 
                        //   - main
                        //     - child_coro_a
                        // yield sleep(1000);
                    //}
            
                    print('pre sleep');
            
                    yield sleep(1000); 
                    
                    print('post sleep');
                }
            
                main();
            """
        );
    }

    [Test]
    public void AsyncCoroutineFn() {
        Execute(
            @"
            async coroutine function main() {
                print('Hello');
            }

            main();
            "
        );
    }

    [Test]
    public void DeferFunction() {
        Execute(
            """
            var defers = 0;

            function main() {
                defer cleanup();
                defer function() {
                    defers++;
                    print('defer -> function()');
                }();
            }

            function cleanup() {
                defers++;
                print('defer -> cleanup()');
            }

            main();

            """
        );

        var deferred = Variables.Get("defers");
        Assert.That(deferred, Is.Not.Null);
        Assert.That(deferred.RawValue, Is.EqualTo(2));
    }
}