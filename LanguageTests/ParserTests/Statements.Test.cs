using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;

namespace LanguageTests.ParserTests;

public class Statements : BaseCompilerTest
{
    [Test]
    public void Import() {
        var (l, p, t) = Parse(
            """
            import "std.math";
            import "math";
            """
        );

        var imports = t.Cursor.All.Of<ImportStatementNode>().ToList();

        Assert.That(imports, Has.Count.EqualTo(2));
        Assert.Multiple(() => {
            Assert.That(imports[0].Path.Value, Is.EqualTo("std.math"));
            Assert.That(imports[1].Path.Value, Is.EqualTo("math"));
        });


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

        var deferred = Symbols.Get("defers");
        Assert.That(deferred, Is.Not.Null);
        Assert.That(deferred.RawValue, Is.EqualTo(2));
    }
}