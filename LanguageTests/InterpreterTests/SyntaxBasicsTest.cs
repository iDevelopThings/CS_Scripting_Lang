using System.Collections;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class SyntaxBasicsTest : BaseCompilerTest
{
    [Test]
    public void VarTypes_Int() {
        var interp = Execute(@"var a = 10;");
        Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo((double)10));
    }
    [Test]
    public void VarTypes_Bool() {
        var interp = Execute(@"var a = true; var b = false;");
        Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo(true));
        Assert.That(interp.Symbols["b"].Value.Value, Is.EqualTo(false));
    }
    
    [Test]
    public void VarTypes_String() {
        var interp = Execute(@"var a = ""Hello World""; var b = 'Hello World';");
        Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo("Hello World"));
        Assert.That(interp.Symbols["b"].Value.Value, Is.EqualTo("Hello World"));
    }
    
    [Test]
    public void TopLevelVariables() {
        var interp = Execute(@"
            var a = 10;
            var b = 20;
            var c = a + b;
        ");

        Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo((double)10));
        Assert.That(interp.Symbols["b"].Value.Value, Is.EqualTo((double)20));
        Assert.That(interp.Symbols["c"].Value.Value, Is.EqualTo((double)30));
    }
    
    public static IEnumerable IfStatementsTestCases {
        get {
            yield return new TestCaseData(2, 0, 1);
            yield return new TestCaseData(0, 2, 0);
        }
    }

    [Test]
    [TestCaseSource(typeof(SyntaxBasicsTest), nameof(IfStatementsTestCases))]
    public void IfStatements(double a, double b, double res) {
        var interp = Execute($@"
            var a = {a};
            var b = {b};
            var res = 0;

            if (a > b) {{
                res = 1;
            }}
        ");

        Assert.Multiple(() =>
        {
            Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo(a));
            Assert.That(interp.Symbols["b"].Value.Value, Is.EqualTo(b));
            Assert.That(interp.Symbols["res"].Value.Value, Is.EqualTo(res));
        });
    }
    
    [Theory]
    [TestCaseSource(typeof(SyntaxBasicsTest), nameof(IfStatementsTestCases))]
    public void IfElseStatements(double a, double b, double res) {
        var interp = Execute($@"
            var a = {a};
            var b = {b};
            var res = 0;

            if (a > b) {{
                res = 1;
            }} else {{
                res = 0;
            }}
        ");

        Assert.Multiple(() =>
        {
            Assert.That(interp.Symbols["a"].Value.Value, Is.EqualTo(a));
            Assert.That(interp.Symbols["b"].Value.Value, Is.EqualTo(b));
            Assert.That(interp.Symbols["res"].Value.Value, Is.EqualTo(res));
        });
    }
    
    
}