namespace LanguageTests.InterpreterTests;

[TestFixture]
public class FunctionsTest : BaseCompilerTest
{
    [Test]
    public void FunctionDeclaration() {
        Execute(@"
            function add(int a, int b) {
                return a + b;
            }
        ");

        var doesExist = Symbols.GetFunctionDeclaration("add", out var add);
        Assert.That(doesExist);
    }

    [Test]
    public void FunctionExecution_ReturnValueVarStorage() {
        Execute(@"
            function add(int a, int b) {
                return a + b;
            }
            var result = add(10, 20);
        ");

        Assert.That(Symbols["result"].Value.Value, Is.EqualTo((double) 30));
    }

    [Test]
    public void FunctionExecution_Void() {
        Execute(@"
            function add(int a, int b) {
                print(a + b);
            }
            add(10, 20);
            var result = add(1, 2);
        ");

        Assert.That(Symbols["result"].Value.Value, Is.Null);
    }

    [Test]
    public void FunctionExecution_Nested() {
        Execute(@"
            function add(int a, int b) {
                function sub(int c, int d) {
                    return c - d;
                }
                return a + b + sub(a, b);
            }
            var result = add(10, 20);
        ");

        Assert.That(Symbols["result"].Value.Value, Is.EqualTo((double) 20));
    }

    [Test]
    public void FunctionExecution_Nested_InheritingParentScope() {
        TrackFunctionFrames();

        Execute(@"
            function add(int a, int b) {
                function sub(int c) {
                    return a - c;
                }
                return a + b + sub(b);
            }
            var result = add(10, 20);
        ");

        Assert.Multiple(() => {
            Assert.That(GetPushedFrame("add").Context.SymbolTable.GetFunctionDeclaration("add"), Is.Not.Null);
            Assert.That(Symbols.Get("a"), Is.Null);
            Assert.That(Symbols.Get("b"), Is.Null);
            Assert.That(Symbols.Get("c"), Is.Null);
            Assert.That(Symbols.Get("result"), Is.Not.Null);

            Assert.That(Symbols["result"].Value.Value, Is.EqualTo((double) 20));
        });
    }
}