using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;

namespace LanguageTests.ParserTests;

public class Statements : BaseCompilerTest
{
    [Test]
    public void Import() {
        var t = Parse(
            """
            import "std.math";
            import "math";
            """
        );

        var imports = t.Cursor.All.Of<ImportStatementNode>().ToList();

        Assert.That(imports, Has.Count.EqualTo(2));
        Assert.Multiple(() => {
            Assert.That(imports[0].Path.NativeValue, Is.EqualTo("std.math"));
            Assert.That(imports[1].Path.NativeValue, Is.EqualTo("math"));
        });


    }

}